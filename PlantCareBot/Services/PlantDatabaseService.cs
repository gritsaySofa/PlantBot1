using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlantBot.Handlers;

namespace TelegramPlantBot.Services
{
    public class PlantDatabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public PlantDatabaseService(string apiKey)
        {
            _httpClient = new HttpClient();
            _apiKey = apiKey;
            _httpClient.BaseAddress = new Uri("https://your-plant-api.com/"); // Замените на реальный API
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task SearchAndSendResults(long chatId, string query, ITelegramBotClient botClient)
        {
            try
            {
                await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

                // Здесь должен быть реальный вызов API базы растений
                // Пока используем заглушку с Trefle API как пример
                var trefleService = MessageHandler.GetTreflePlantService();
                if (trefleService != null)
                {
                    await trefleService.SearchAndSendResults(chatId, query, botClient);
                    return;
                }

                // Если Trefle недоступен, показываем локальные результаты
                await ShowLocalPlantResults(chatId, query, botClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка поиска в базе: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "❌ Ошибка при поиске в базе растений");
            }
        }

        private async Task ShowLocalPlantResults(long chatId, string query, ITelegramBotClient botClient)
        {
            // Локальная база растений как fallback
            var localPlants = new[]
            {
                new { Name = "Роза", ScientificName = "Rosa", Description = "Красивые цветы с шипами" },
                new { Name = "Орхидея", ScientificName = "Orchidaceae", Description = "Экзотические цветы" },
                new { Name = "Фикус", ScientificName = "Ficus", Description = "Популярное комнатное растение" }
            };

            var foundPlants = localPlants.Where(p =>
                p.Name.ToLower().Contains(query.ToLower()) ||
                p.ScientificName.ToLower().Contains(query.ToLower())
            ).ToArray();

            if (foundPlants.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"🌿 **Найдено в локальной базе: {foundPlants.Length}**\n" +
                    $"Запрос: '{query}'",
                    parseMode: ParseMode.Markdown);

                foreach (var plant in foundPlants.Take(3))
                {
                    var message = $"🌿 **{plant.Name}**\n" +
                                 $"🔬 *{plant.ScientificName}*\n" +
                                 $"📝 {plant.Description}";

                    await botClient.SendTextMessageAsync(
                        chatId,
                        message,
                        parseMode: ParseMode.Markdown,
                        replyMarkup: new InlineKeyboardMarkup(new[]
                        {
                            new[] { InlineKeyboardButton.WithCallbackData("➕ Добавить в коллекцию", $"add_local_{plant.Name}") }
                        }));
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ По запросу '{query}' в локальной базе не найдено.\n\n" +
                    "💡 Попробуйте поиск в онлайн-базе:",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("🔍 Искать онлайн", $"search_online_{query}") }
                    }));
            }
        }

        public async Task SendPopularPlants(long chatId, ITelegramBotClient botClient)
        {
            // Реализация популярных растений
            await botClient.SendTextMessageAsync(
                chatId,
                "🌟 **Популярные растения из базы:**\n\n" +
                "• 🌹 Роза\n• 🌸 Орхидея\n• 🌳 Фикус\n• 🌵 Кактус\n• 🍃 Папоротник",
                parseMode: ParseMode.Markdown);
        }

        public async Task<PlantDetails> GetPlantDetailsAsync(int plantId)
        {
            // Заглушка - реализуйте получение деталей растения
            return new PlantDetails();
        }

        public async Task SendPlantDetails(long chatId, int plantId, ITelegramBotClient botClient)
        {
            await botClient.SendTextMessageAsync(chatId, "📖 Детали растения будут здесь");
        }
    }

    public class PlantDetails
    {
        public string Common_Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Watering { get; set; } = string.Empty;
        public string Sunlight { get; set; } = string.Empty;
        public string Care_Level { get; set; } = string.Empty;
        public PlantImage Default_Image { get; set; } = new PlantImage();
    }

    public class PlantImage
    {
        public string Regular { get; set; } = string.Empty;
    }
}