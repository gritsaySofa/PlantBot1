using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TelegramPlantBot.Services
{
    public static class GigaChatService
    {
        private static HttpClient _httpClient;
        private static string _accessToken = string.Empty;
        private static string _apiKey = string.Empty;
        private static bool _isInitialized = false;
        private static DateTime _tokenExpiresAt = DateTime.MinValue;

        // URLs для GigaChat API
        private const string AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
        private const string ApiUrl = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

        public static void Initialize(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GIGACHAT_API_KEY")
            {
                Console.WriteLine("❌ GigaChat: API ключ не установлен");
                return;
            }

            _apiKey = apiKey;

            try
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                _httpClient = new HttpClient(handler);
                _httpClient.Timeout = TimeSpan.FromSeconds(30);
                _httpClient.DefaultRequestHeaders.Clear();

                Console.WriteLine("✅ GigaChat сервис инициализирован");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации GigaChat: {ex.Message}");
                _isInitialized = false;
            }
        }

        private static async Task<bool> EnsureValidTokenAsync()
        {
            // Если токен еще действителен (оставляем запас в 5 минут)
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
            {
                Console.WriteLine("♻️ Используем существующий токен GigaChat");
                return true;
            }

            try
            {
                Console.WriteLine("🔄 Получение нового токена GigaChat...");

                var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl);
                request.Headers.Add("Authorization", $"Basic {_apiKey}");
                request.Headers.Add("RqUID", Guid.NewGuid().ToString());
                request.Headers.Add("Accept", "application/json");

                request.Content = new StringContent(
                    "scope=GIGACHAT_API_PERS",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded"
                );

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 Ответ аутентификации: {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);

                    if (jsonDoc.RootElement.TryGetProperty("access_token", out var tokenElement))
                    {
                        _accessToken = tokenElement.GetString();

                        if (jsonDoc.RootElement.TryGetProperty("expires_in", out var expiresElement))
                        {
                            var expiresIn = expiresElement.GetInt32();
                            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                            Console.WriteLine($"✅ Токен GigaChat получен, действителен {expiresIn} секунд");
                        }
                        else if (jsonDoc.RootElement.TryGetProperty("expires_at", out var expiresAtElement))
                        {
                            var expiresAt = expiresAtElement.GetInt64();
                            _tokenExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).DateTime;
                            Console.WriteLine($"✅ Токен GigaChat получен, действителен до {_tokenExpiresAt}");
                        }
                        else
                        {
                            // Значение по умолчанию - 30 минут
                            _tokenExpiresAt = DateTime.UtcNow.AddMinutes(30);
                            Console.WriteLine("✅ Токен GigaChat получен (время по умолчанию: 30 минут)");
                        }

                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"❌ Токен не найден в ответе: {responseContent}");
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка аутентификации: {response.StatusCode}");
                    Console.WriteLine($"📄 Ответ: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения токена GigaChat: {ex.Message}");
                return false;
            }
        }
        public static async Task<string> GetPlantAdviceAsync(string prompt)
        {
            if (!_isInitialized)
            {
                return "❌ GigaChat не инициализирован. " + GetFallbackAdvice(prompt);
            }

            try
            {
                if (!await EnsureValidTokenAsync())
                {
                    return "❌ Не удалось получить доступ к GigaChat. " + GetFallbackAdvice(prompt);
                }

                Console.WriteLine($"🤖 GigaChat запрос: {prompt}");

                var requestBody = new
                {
                    model = "GigaChat",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = @"Ты эксперт по уходу за растениями с ботаническим образованием. Отвечай ТОЛЬКО на русском языке.

ТВОЯ РОЛЬ:
- Эксперт по комнатным и садовым растениям
- Дружелюбный и полезный помощник
- Даешь практические, конкретные советы
- Специализируешься на диагностике проблем растений

ФОРМАТИРОВАНИЕ:
- Используй Markdown разметку
- Добавляй эмодзи для наглядности
- Структурируй ответ с помощью заголовков
- Используй списки для рекомендаций
- Будь кратким, но информативным

ВАЖНО:
- Отвечай только на вопросы связанные с растениями
- Если вопрос не о растениях, вежливо откажись отвечать
- Максимальная длина ответа: 1500 символов"
                        },
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    temperature = 0.7,
                    max_tokens = 1000
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/json");

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine("📤 Отправка запроса к GigaChat API...");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 GigaChat ответ: {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);

                        if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) &&
                            choices.GetArrayLength() > 0)
                        {
                            var message = choices[0].GetProperty("message").GetProperty("content").GetString();

                            if (!string.IsNullOrEmpty(message))
                            {
                                Console.WriteLine("✅ Успешный ответ от GigaChat!");
                                return message.Trim();
                            }
                        }

                        Console.WriteLine($"❌ Неверный формат ответа GigaChat: {responseContent}");
                        return "❌ Неверный формат ответа от GigaChat. " + GetFallbackAdvice(prompt);
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"❌ Ошибка парсинга JSON от GigaChat: {jsonEx.Message}");
                        Console.WriteLine($"📄 Ответ: {responseContent}");
                        return "❌ Ошибка обработки ответа от GigaChat. " + GetFallbackAdvice(prompt);
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка HTTP от GigaChat: {response.StatusCode}");
                    Console.WriteLine($"📄 Текст ошибки: {responseContent}");

                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Неверный токен доступа",
                        System.Net.HttpStatusCode.TooManyRequests => "Слишком много запросов",
                        System.Net.HttpStatusCode.BadRequest => "Неверный формат запроса",
                        System.Net.HttpStatusCode.Forbidden => "Доступ запрещен",
                        System.Net.HttpStatusCode.ServiceUnavailable => "Сервис недоступен",
                        _ => $"Ошибка: {response.StatusCode}"
                    };

                    return $"❌ GigaChat: {errorMessage}. " + GetFallbackAdvice(prompt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка GigaChat: {ex.Message}");
                return "❌ Ошибка подключения к GigaChat. " + GetFallbackAdvice(prompt);
            }
        }

        public static async Task<bool> TestConnection()
        {
            if (!_isInitialized)
            {
                Console.WriteLine("❌ GigaChat не инициализирован для теста");
                return false;
            }

            try
            {
                Console.WriteLine("🔍 Тестирование подключения к GigaChat...");

                var testResponse = await GetPlantAdviceAsync("Ответь коротко одной фразой на русском: 'GigaChat работает'");
                var success = !testResponse.Contains("❌ Ошибка") &&
                             !testResponse.Contains("не инициализирован") &&
                             testResponse.Length > 5;

                if (success)
                {
                    Console.WriteLine("✅ GigaChat подключение успешно!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ GigaChat тест не пройден: {testResponse}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка тестирования GigaChat: {ex.Message}");
                return false;
            }
        }

        private static string GetFallbackAdvice(string prompt)
        {
            prompt = prompt.ToLower();

            if (prompt.Contains("полив") || prompt.Contains("вода"))
                return "💧 **Совет по поливу:**\n\n• Проверяйте влажность почвы пальцем\n• Поливайте когда верхний слой сухой на 2-3 см\n• Используйте отстоянную воду комнатной температуры";

            else if (prompt.Contains("свет") || prompt.Contains("освещен"))
                return "☀️ **Совет по освещению:**\n\n• Большинству растений нужен яркий рассеянный свет\n• Прямое солнце может обжечь листья\n• Теневыносливые растения: сансевиерия, замиокулькас";

            else if (prompt.Contains("удобр") || prompt.Contains("подкорм"))
                return "🌱 **Совет по удобрениям:**\n\n• Удобряйте в период активного роста (весна-лето)\n• Используйте специализированные удобрения\n• Соблюдайте дозировку";

            else if (prompt.Contains("желт") || prompt.Contains("сохн") || prompt.Contains("опада"))
                return "🔍 **Диагностика проблемы:**\n\n• **Желтеют листья:** перелив, недостаток света\n• **Сохнут кончики:** сухой воздух\n• **Опадают листья:** сквозняк, перелив";

            else if (prompt.Contains("орхиде"))
                return "🌸 **Уход за орхидеей:**\n\n• Полив 1 раз в 7-10 дней погружением\n• Яркий рассеянный свет\n• Температура 18-25°C\n• Высокая влажность";

            else if (prompt.Contains("фикус"))
                return "🌳 **Уход за фикусом:**\n\n• Умеренный полив\n• Яркий рассеянный свет\n• Температура 18-24°C\n• Регулярно протирать листья";

            else
                return "🌿 **Советы по уходу:**\n\n• 💧 Полив - по мере просыхания почвы\n• ☀️ Свет - яркий рассеянный\n• 🌡 Температура - 18-25°C\n• 💨 Влажность - регулярное опрыскивание\n\n*Укажите название растения для точных рекомендаций*";
        }
    }
}