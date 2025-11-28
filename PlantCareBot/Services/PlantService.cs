using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlantBot.Models;
using TelegramPlantBot.Handlers;

namespace TelegramPlantBot.Services
{
    public class PlantService
    {
        public static async Task SendPlantCatalog(long chatId, ITelegramBotClient botClient)
        {
            // Используем Trefle API для показа каталога
            var trefleService = MessageHandler.GetTreflePlantService();
            if (trefleService != null)
            {
                await trefleService.SendPopularPlants(chatId, botClient);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "🌿 **Онлайн-каталог растений**\n\n" +
                    "Используйте поиск растений для просмотра базы Trefle API.\n\n" +
                    "**Популярные запросы:**\n" +
                    "• rose (роза)\n" +
                    "• orchid (орхидея)\n" +
                    "• cactus (кактус)\n" +
                    "• ficus (фикус)\n" +
                    "• sunflower (подсолнух)\n" +
                    "• lavender (лаванда)",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("🔍 Поиск растений", "new_search") },
                        new[] { InlineKeyboardButton.WithCallbackData("🌟 Популярные", "popular_trefle") }
                    }));
            }
        }

        public static async Task SendPlantInfo(long chatId, string plantName, ITelegramBotClient botClient)
        {
            // Используем Trefle API для поиска информации о растении
            var trefleService = MessageHandler.GetTreflePlantService();
            if (trefleService != null)
            {
                await trefleService.SearchAndSendResults(chatId, plantName, botClient);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"🔍 **Поиск: {plantName}**\n\n" +
                    "Сервис поиска растений временно недоступен.\n\n" +
                    "💡 Вы можете:\n" +
                    "• Добавить растение вручную через фото\n" +
                    "• Задать вопрос AI-помощнику\n" +
                    "• Попробовать позже",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[] {
                            InlineKeyboardButton.WithCallbackData("➕ Добавить растение", "add_custom_plant"),
                            InlineKeyboardButton.WithCallbackData("🤖 AI Помощник", "ask_ai")
                        }
                    }));
            }
        }

        public static string GetSeasonalAdvice()
        {
            var month = DateTime.Now.Month;
            var seasonAdvice = month switch
            {
                >= 3 and <= 5 => "🌱 **Весна**: время активного роста! Увеличивайте полив, начинайте подкормки, можно пересаживать растения.",
                >= 6 and <= 8 => "☀️ **Лето**: следите за поливом, защищайте от прямого солнца, повышайте влажность.",
                >= 9 and <= 11 => "🍂 **Осень**: сокращайте полив и подкормки, готовьте растения к периоду покоя.",
                _ => "❄️ **Зима**: период покоя. Минимальный полив, без удобрений, защита от холодных сквозняков."
            };

            return $"{seasonAdvice}\n\n🌿 *Совет от AI-помощника*";
        }

        // Новый метод для быстрого поиска популярных растений
        public static async Task SendQuickPlantSearch(long chatId, string plantName, ITelegramBotClient botClient)
        {
            var trefleService = MessageHandler.GetTreflePlantService();
            if (trefleService != null)
            {
                await botClient.SendChatActionAsync(chatId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                await trefleService.SearchAndSendResults(chatId, plantName, botClient);
            }
        }

        // Метод для показа основных категорий растений
        public static async Task SendPlantCategories(long chatId, ITelegramBotClient botClient)
        {
            var message = "🌿 **Категории растений**\n\n" +
                         "Выберите категорию для поиска:\n\n" +
                         "• 🌹 **Цветущие** (roses, orchids, tulips)\n" +
                         "• 🌳 **Комнатные деревья** (ficus, palm, dracaena)\n" +
                         "• 🍃 **Лиственные** (fern, calathea, monstera)\n" +
                         "• 🌵 **Суккуленты** (cactus, aloe, echeveria)\n" +
                         "• 🌿 **Травы** (lavender, mint, basil)\n" +
                         "• 🍓 **Плодовые** (strawberry, tomato, lemon)";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🌹 Цветущие", "search_db_flower"),
                    InlineKeyboardButton.WithCallbackData("🌳 Деревья", "search_db_tree")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🍃 Лиственные", "search_db_leafy"),
                    InlineKeyboardButton.WithCallbackData("🌵 Суккуленты", "search_db_succulent")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🌿 Травы", "search_db_herb"),
                    InlineKeyboardButton.WithCallbackData("🍓 Плодовые", "search_db_fruit")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔍 Произвольный поиск", "new_search")
                }
            });

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: keyboard);
        }
    }
}