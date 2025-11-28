using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramPlantBot.Services
{
    public class TreflePlantService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiToken;

        public TreflePlantService(string apiToken)
        {
            _httpClient = new HttpClient();
            _apiToken = apiToken?.Trim();
            _httpClient.BaseAddress = new Uri("https://trefle.io/api/v1/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Увеличил таймаут
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramPlantBot/1.0");

            Console.WriteLine($"🌐 Инициализация Trefle API с токеном: {_apiToken?.Substring(0, Math.Min(8, _apiToken?.Length ?? 0))}...");
        }

        public async Task SearchAndSendResults(long chatId, string query, ITelegramBotClient botClient)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    await botClient.SendTextMessageAsync(chatId, "❌ Введите название растения для поиска");
                    return;
                }

                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                var searchUrl = $"plants/search?token={_apiToken}&q={Uri.EscapeDataString(query)}";
                Console.WriteLine($"🔍 Поиск растений: {searchUrl}");

                var response = await _httpClient.GetAsync(searchUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TrefleSearchResponse>(responseContent);

                    if (result?.Data?.Length > 0)
                    {
                        var plants = result.Data.Take(5).ToArray();

                        await botClient.SendTextMessageAsync(
                            chatId,
                            $"🌿 **Найдено растений: {result.Data.Length}**\n" +
                            $"📋 Показано первых {plants.Length}:",
                            parseMode: ParseMode.Markdown);

                        foreach (var plant in plants)
                        {
                            await SendPlantCard(chatId, plant, botClient);
                        }

                        // Кнопки для дополнительных действий
                        var actionKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🔍 Новый поиск", "new_search"),
                                InlineKeyboardButton.WithCallbackData("📊 Популярные растения", "popular_trefle")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "main_menu")
                            }
                        });

                        await botClient.SendTextMessageAsync(
                            chatId,
                            "💡 Используйте кнопки под каждым растением для подробностей или добавления в коллекцию.",
                            replyMarkup: actionKeyboard);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId,
                            $"❌ По запросу \"{query}\" растения не найдены.\n\n" +
                            "💡 Попробуйте:\n" +
                            "• Другие названия (например, 'rose' вместо 'роза')\n" +
                            "• Более общие запросы ('flower', 'tree')\n" +
                            "• Латинские названия",
                            replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("🔍 Новый поиск", "new_search") }
                            }));
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка API: {response.StatusCode} - {responseContent}");
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Ошибка при поиске растений. Сервис временно недоступен.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка поиска растений: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Произошла ошибка при поиске растений. Попробуйте позже.");
            }
        }

        private async Task SendPlantCard(long chatId, TreflePlant plant, ITelegramBotClient botClient)
        {
            try
            {
                var commonName = !string.IsNullOrEmpty(plant.CommonName) ? plant.CommonName : "Без русского названия";
                var message = $"🌿 **{commonName}**\n" +
                             $"🔬 *{plant.ScientificName}*\n" +
                             $"👨‍👩‍👧‍👦 Семейство: {plant.Family ?? "Не указано"}\n" +
                             $"🆔 ID: {plant.Id}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📖 Подробнее", $"details_t_{plant.Id}"),
                        InlineKeyboardButton.WithCallbackData("➕ Добавить", $"add_t_{plant.Id}")
                    }
                });

                // Пытаемся отправить с фото
                if (!string.IsNullOrEmpty(plant.ImageUrl))
                {
                    try
                    {
                        await botClient.SendPhotoAsync(
                            chatId,
                            InputFile.FromUri(plant.ImageUrl),
                            caption: message,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard);
                        return;
                    }
                    catch (Exception photoEx)
                    {
                        Console.WriteLine($"❌ Ошибка отправки фото: {photoEx.Message}");
                        // Продолжаем с текстовым сообщением
                    }
                }

                // Отправляем текстовое сообщение если фото не удалось
                await botClient.SendTextMessageAsync(
                    chatId,
                    message,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка отправки карточки растения: {ex.Message}");
            }
        }

        public async Task SendPlantDetails(long chatId, int plantId, ITelegramBotClient botClient)
        {
            try
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                var detailsUrl = $"plants/{plantId}?token={_apiToken}";
                Console.WriteLine($"🔍 Запрос деталей растения: {detailsUrl}");

                var response = await _httpClient.GetAsync(detailsUrl);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<TreflePlantResponse>(content);
                    var plant = result?.Data;

                    if (plant != null)
                    {
                        var commonName = !string.IsNullOrEmpty(plant.CommonName) ? plant.CommonName : "Без русского названия";

                        var message = $"🌿 **{commonName}**\n\n" +
                                     $"🔬 **Научное название:** {plant.ScientificName}\n" +
                                     $"👨‍👩‍👧‍👦 **Семейство:** {plant.Family ?? "Не указано"}\n" +
                                     $"🌍 **Род:** {plant.Genus ?? "Не указан"}\n";

                        // Добавляем информацию о распространении если есть
                        if (!string.IsNullOrEmpty(plant.Distribution?.Native))
                        {
                            var native = plant.Distribution.Native.Length > 200
                                ? plant.Distribution.Native.Substring(0, 200) + "..."
                                : plant.Distribution.Native;
                            message += $"🗺️ **Естественный ареал:** {native}\n";
                        }

                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("➕ Добавить в коллекцию", $"add_t_{plant.Id}"),
                                InlineKeyboardButton.WithCallbackData("🔙 Назад к поиску", "search_more")
                            }
                        });

                        // Пытаемся отправить с фото
                        if (!string.IsNullOrEmpty(plant.ImageUrl))
                        {
                            try
                            {
                                await botClient.SendPhotoAsync(
                                    chatId,
                                    InputFile.FromUri(plant.ImageUrl),
                                    caption: message,
                                    parseMode: ParseMode.Markdown,
                                    replyMarkup: keyboard);
                                return;
                            }
                            catch (Exception photoEx)
                            {
                                Console.WriteLine($"❌ Ошибка отправки фото: {photoEx.Message}");
                            }
                        }

                        await botClient.SendTextMessageAsync(
                            chatId,
                            message,
                            parseMode: ParseMode.Markdown,
                            replyMarkup: keyboard);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Информация о растении не найдена.");
                    }
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "❌ Не удалось загрузить информацию о растении.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения деталей растения: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Произошла ошибка при загрузке информации о растении.");
            }
        }

        public async Task SendPopularPlants(long chatId, ITelegramBotClient botClient)
        {
            try
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                // Популярные растения для демонстрации
                var popularPlants = new[] { "rose", "sunflower", "lavender", "tulip", "orchid" };
                var random = new Random();
                var randomPlant = popularPlants[random.Next(popularPlants.Length)];

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"🌟 **Популярные растения: {randomPlant}**\n" +
                    "🔍 Ищу в онлайн-базе...");

                await SearchAndSendResults(chatId, randomPlant, botClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка получения популярных растений: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Произошла ошибка при загрузке популярных растений.");
            }
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                Console.WriteLine("🔍 Тестирование подключения к Trefle API...");

                var testUrl = $"plants?token={_apiToken}&q=rose&page=1";
                var response = await _httpClient.GetAsync(testUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📥 HTTP статус: {(int)response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<TrefleSearchResponse>(responseContent);
                    var plantCount = result?.Data?.Length ?? 0;
                    Console.WriteLine($"✅ Trefle API работает! Найдено растений: {plantCount}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка Trefle API: {response.StatusCode}");
                    Console.WriteLine($"📄 Ответ: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка подключения к Trefle API: {ex.Message}");
                return false;
            }
        }
    }

    // Модели данных для Trefle API
    public class TrefleSearchResponse
    {
        public TreflePlant[] Data { get; set; } = Array.Empty<TreflePlant>();
        public TrefleMeta Meta { get; set; } = new TrefleMeta();
    }

    public class TreflePlantResponse
    {
        public TreflePlantDetail Data { get; set; } = new TreflePlantDetail();
    }

    public class TreflePlant
    {
        public int Id { get; set; }
        public string CommonName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class TreflePlantDetail
    {
        public int Id { get; set; }
        public string CommonName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string Genus { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public TrefleDistribution Distribution { get; set; } = new TrefleDistribution();
    }

    public class TrefleDistribution
    {
        public string Native { get; set; } = string.Empty;
        public string Introduced { get; set; } = string.Empty;
    }

    public class TrefleMeta
    {
        public int Total { get; set; }
    }
}