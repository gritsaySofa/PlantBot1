using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramPlantBot.Services
{
    public class EnhancedPlantService
    {
        private readonly PlantDatabaseService _plantDatabaseService;

        public EnhancedPlantService(string apiKey)
        {
            _plantDatabaseService = new PlantDatabaseService(apiKey);
            Console.WriteLine($"🌿 EnhancedPlantService инициализирован с PlantDatabaseService");
        }

        // ДОБАВЛЯЕМ НЕДОСТАЮЩИЕ МЕТОДЫ

        public async Task SendPlantDetails(long chatId, string plantId, ITelegramBotClient botClient)
        {
            if (int.TryParse(plantId, out int id))
            {
                await _plantDatabaseService.SendPlantDetails(chatId, id, botClient);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Неверный ID растения");
            }
        }

        public async Task ShowPlantsByFamily(long chatId, string family, ITelegramBotClient botClient)
        {
            var familyNames = new Dictionary<string, string>
            {
                { "araceae", "Ароидные" },
                { "cactaceae", "Кактусовые" },
                { "orchidaceae", "Орхидные" },
                { "moraceae", "Тутовые" }
            };

            var familyName = familyNames.GetValueOrDefault(family, family);

            var message = $"🌿 **Растения семейства {familyName}**\n\n";

            switch (family)
            {
                case "araceae":
                    message += "• Монстера деликатесная\n• Спатифиллум\n• Диффенбахия\n• Антуриум\n• Аглаонема";
                    break;
                case "cactaceae":
                    message += "• Опунция\n• Маммиллярия\n• Эхинопсис\n• Ребуция\n• Астрофитум";
                    break;
                case "orchidaceae":
                    message += "• Фаленопсис\n• Дендробиум\n• Каттлея\n• Ванда\n• Цимбидиум";
                    break;
                case "moraceae":
                    message += "• Фикус Бенджамина\n• Фикус каучуконосный\n• Фикус лировидный\n• Шелковица";
                    break;
                default:
                    message += "Информация о растениях этого семейства временно недоступна";
                    break;
            }

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔍 Искать в базе", $"search_db_{familyName.ToLower()}"),
                    InlineKeyboardButton.WithCallbackData("➕ Добавить своё", "add_custom_plant")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔙 К семействам", "plant_families"),
                    InlineKeyboardButton.WithCallbackData("📚 Главное меню", "main_menu")
                }
            });

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                parseMode: ParseMode.Markdown,
                replyMarkup: inlineKeyboard);
        }

        // Существующие методы
        public async Task SearchPlantsInApi(long chatId, string query, ITelegramBotClient botClient)
        {
            try
            {
                await _plantDatabaseService.SearchAndSendResults(chatId, query, botClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в SearchPlantsInApi: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Ошибка при поиске растений. Попробуйте позже.");
            }
        }

        public async Task ShowEnhancedCatalog(long chatId, ITelegramBotClient botClient)
        {
            var message = "📚 **База данных растений**\n\n" +
                         "Выберите источник информации:\n\n" +
                         "• 🔍 **Поиск в онлайн-базе** - огромная база растений со всего мира\n" +
                         "• 📖 **Локальная база** - проверенные растения с детальными инструкциями\n" +
                         "• 🌟 **Популярные растения** - самые популярные растения из базы";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔍 Поиск в онлайн-базе", "online_search"),
                    InlineKeyboardButton.WithCallbackData("📖 Локальная база", "local_catalog")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🌟 Популярные растения", "popular_plants"),
                    InlineKeyboardButton.WithCallbackData("🌿 Случайное растение", "random_plant")
                }
            });

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                parseMode: ParseMode.Markdown,
                replyMarkup: inlineKeyboard);
        }

        public async Task ShowPlantFamilies(long chatId, ITelegramBotClient botClient)
        {
            var message = "🌿 **Популярные семейства растений**\n\n" +
                         "Выберите семейство для просмотра растений:";

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🌹 Розовые (Rosa)", "search_db_rose"),
                    InlineKeyboardButton.WithCallbackData("🌵 Кактусовые", "search_db_cactus")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🌸 Орхидные", "search_db_orchid"),
                    InlineKeyboardButton.WithCallbackData("🌳 Тутовые (Ficus)", "search_db_ficus")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", "enhanced_catalog")
                }
            });

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                replyMarkup: inlineKeyboard);
        }
    }
}