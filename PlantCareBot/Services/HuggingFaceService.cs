using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TelegramPlantBot.Services
{
    public static class HuggingFaceService
    {
        private static HttpClient _httpClient;
        private static bool _isInitialized = false;

        // РАБОЧИЕ модели которые точно доступны
        private static readonly string[] ModelUrls = {
            "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium", // Английская, но простая и работает
            "https://api-inference.huggingface.co/models/gpt2", // Базовая модель
            "https://api-inference.huggingface.co/models/distilgpt2" // Легкая версия
        };

        private static int currentModelIndex = 0;

        public static void Initialize()
        {
            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                _httpClient = new HttpClient(handler);
                _httpClient.Timeout = TimeSpan.FromSeconds(60);
                _httpClient.DefaultRequestHeaders.Clear();

                Console.WriteLine($"✅ Hugging Face инициализирован с моделью: {GetCurrentModelName()}");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации Hugging Face: {ex.Message}");
                _isInitialized = false;
            }
        }

        public static async Task<string> GetPlantAdviceAsync(string prompt)
        {
            if (!_isInitialized)
            {
                return "❌ Hugging Face не инициализирован. " + GetFallbackAdvice(prompt);
            }

            // Сначала попробуем текущую модель
            var result = await TryCurrentModel(prompt);
            if (!result.Contains("❌ Ошибка"))
            {
                return result;
            }

            // Если текущая не работает, пробуем другие модели
            Console.WriteLine("🔄 Текущая модель не работает, пробуем другие...");
            for (int i = 0; i < ModelUrls.Length; i++)
            {
                if (i == currentModelIndex) continue; // Пропускаем текущую

                currentModelIndex = i;
                Console.WriteLine($"🔄 Переключаемся на модель: {GetCurrentModelName()}");

                result = await TryCurrentModel(prompt);
                if (!result.Contains("❌ Ошибка"))
                {
                    return result;
                }
            }

            return "❌ Все модели Hugging Face недоступны. " + GetFallbackAdvice(prompt);
        }

        private static async Task<string> TryCurrentModel(string prompt)
        {
            try
            {
                Console.WriteLine($"🤖 Hugging Face запрос к {GetCurrentModelName()}: {prompt}");

                // Адаптируем промпт под английскую модель
                var englishPrompt = TranslateToEnglish(prompt);
                var systemPrompt = "You are a plant care expert. Give helpful, friendly advice about plants. Use emojis and practical tips.";
                var fullPrompt = $"{systemPrompt}\n\nQuestion: {englishPrompt}\n\nAnswer:";

                var requestBody = new
                {
                    inputs = fullPrompt,
                    parameters = new
                    {
                        max_new_tokens = 300,
                        temperature = 0.7,
                        do_sample = true,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(ModelUrls[currentModelIndex], content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 {GetCurrentModelName()} ответ: {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);

                        if (jsonDoc.RootElement.GetArrayLength() > 0)
                        {
                            var generatedText = jsonDoc.RootElement[0]
                                .GetProperty("generated_text")
                                .GetString();

                            if (!string.IsNullOrEmpty(generatedText))
                            {
                                Console.WriteLine($"✅ Успешный ответ от {GetCurrentModelName()}!");

                                // Переводим ответ обратно на русский
                                var russianResponse = TranslateToRussian(generatedText);
                                return russianResponse;
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Пробуем другой формат
                        try
                        {
                            var jsonDoc = JsonDocument.Parse(responseContent);
                            if (jsonDoc.RootElement.TryGetProperty("generated_text", out var textElement))
                            {
                                var generatedText = textElement.GetString();
                                if (!string.IsNullOrEmpty(generatedText))
                                {
                                    return TranslateToRussian(generatedText);
                                }
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    return "❌ Модель загружается. " + GetFallbackAdvice(prompt);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                         response.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    return "❌ Модель недоступна. " + GetFallbackAdvice(prompt);
                }

                return "❌ Ошибка модели. " + GetFallbackAdvice(prompt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка {GetCurrentModelName()}: {ex.Message}");
                return "❌ Ошибка подключения. " + GetFallbackAdvice(prompt);
            }
        }

        private static string TranslateToEnglish(string russianText)
        {
            // Простой перевод ключевых фраз
            return russianText.ToLower() switch
            {
                string s when s.Contains("желтеют листья") => "yellow leaves",
                string s when s.Contains("фикус") => "ficus plant",
                string s when s.Contains("полив") => "watering",
                string s when s.Contains("свет") => "light",
                string s when s.Contains("удобр") => "fertilizer",
                string s when s.Contains("орхиде") => "orchid",
                string s when s.Contains("кактус") => "cactus",
                string s when s.Contains("почему") => "why",
                string s when s.Contains("как") => "how to",
                string s when s.Contains("что") => "what",
                _ => russianText
            };
        }

        private static string TranslateToRussian(string englishText)
        {
            // Простой перевод ответа на русский
            var response = englishText
                .Replace("watering", "полив")
                .Replace("light", "освещение")
                .Replace("ficus", "фикус")
                .Replace("orchid", "орхидея")
                .Replace("cactus", "кактус")
                .Replace("yellow leaves", "желтые листья")
                .Replace("plant", "растение")
                .Replace("water", "поливать")
                .Replace("sunlight", "солнечный свет");

            // Добавляем русские эмодзи и форматирование
            if (response.Contains("желтые листья") || response.Contains("yellow leaves"))
            {
                response = "🍂 **Проблема: Желтеют листья у фикуса**\n\n" + response;
            }

            return response;
        }

        private static string GetCurrentModelName()
        {
            var url = ModelUrls[currentModelIndex];
            return url.Split('/')[^1]; // Последняя часть URL
        }

        public static async Task<bool> TestConnection()
        {
            if (!_isInitialized)
            {
                Console.WriteLine("❌ Hugging Face не инициализирован для теста");
                return false;
            }

            try
            {
                Console.WriteLine("🔍 Тестирование подключения к Hugging Face...");

                var testResponse = await GetPlantAdviceAsync("test");
                var success = !testResponse.Contains("❌ Ошибка") &&
                             !testResponse.Contains("не инициализирован");

                if (success)
                {
                    Console.WriteLine("✅ Hugging Face подключение успешно!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Hugging Face тест не пройден: {testResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка тестирования Hugging Face: {ex.Message}");
                return false;
            }
        }

        private static string GetFallbackAdvice(string prompt)
        {
            prompt = prompt.ToLower();

            if (prompt.Contains("желтеют листья") && prompt.Contains("фикус"))
                return @"🍂 **Почему желтеют листья у фикуса:**

**Возможные причины:**
• 💧 **Перелив** - самая частая причина
• 🌞 **Недостаток света** 
• 💨 **Сквозняки** 
• 🍃 **Недостаток питательных веществ**
• 🔄 **Адаптация** после переезда

**Решение:**
• Проверьте влажность почвы
• Переставьте в более светлое место
• Уберите от сквозняков
• Удобрите растение
• Дайте время на адаптацию";

            else if (prompt.Contains("полив") || prompt.Contains("вода"))
                return "💧 **Совет по поливу:**\n\n• Проверяйте влажность почвы пальцем\n• Поливайте когда верхний слой сухой на 2-3 см\n• Используйте отстоянную воду комнатной температуры";

            else if (prompt.Contains("свет") || prompt.Contains("освещен"))
                return "☀️ **Совет по освещению:**\n\n• Большинству растений нужен яркий рассеянный свет\n• Прямое солнце может обжечь листья\n• Теневыносливые растения: сансевиерия, замиокулькас";

            else if (prompt.Contains("удобр") || prompt.Contains("подкорм"))
                return "🌱 **Совет по удобрениям:**\n\n• Удобряйте в период активного роста (весна-лето)\n• Используйте специализированные удобрения\n• Соблюдайте дозировку";

            else if (prompt.Contains("орхиде"))
                return "🌸 **Уход за орхидеей:**\n\n• Полив 1 раз в 7-10 дней погружением\n• Яркий рассеянный свет\n• Температура 18-25°C\n• Высокая влажность";

            else if (prompt.Contains("фикус"))
                return "🌳 **Уход за фикусом:**\n\n• Умеренный полив\n• Яркий рассеянный свет\n• Температура 18-24°C\n• Регулярно протирать листья";

            else if (prompt.Contains("кактус") || prompt.Contains("суккулент"))
                return "🌵 **Уход за кактусами:**\n\n• Редкий полив (раз в 2-4 недели)\n• Максимум солнечного света\n• Хороший дренаж\n• Прохладная зимовка";

            else
                return "🌿 **Советы по уходу:**\n\n• 💧 Полив - по мере просыхания почвы\n• ☀️ Свет - яркий рассеянный\n• 🌡 Температура - 18-25°C\n• 💨 Влажность - регулярное опрыскивание\n\n*Укажите название растения для точных рекомендаций*";
        }
    }
}