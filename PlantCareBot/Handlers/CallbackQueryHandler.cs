using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlantBot.Services;

namespace TelegramPlantBot.Handlers
{
    public static class CallbackQueryHandler
    {
        private static EnhancedPlantService? _enhancedPlantService;

        public static void InitializeEnhancedPlantService(string apiKey)
        {
            _enhancedPlantService = new EnhancedPlantService(apiKey);
        }

        public static async Task HandleCallbackQuery(CallbackQuery callbackQuery, ITelegramBotClient botClient)
        {
            var chatId = callbackQuery.Message?.Chat.Id ?? 0;
            var data = callbackQuery.Data;

            if (chatId == 0 || string.IsNullOrEmpty(data))
            {
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Ошибка данных");
                return;
            }

            try
            {
                Console.WriteLine($"🔔 Callback получен: {data} от chatId: {chatId}");

                // Обработка callback-ов для Trefle API
                if (data.StartsWith("search_db_"))
                {
                    var query = data.Replace("search_db_", "");
                    var trefleService = MessageHandler.GetTreflePlantService();
                    if (trefleService != null)
                    {
                        await trefleService.SearchAndSendResults(chatId, query, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Сервис Trefle API недоступен");
                    }
                }
                // Обработка callback-ов для деталей растения Trefle
                else if (data.StartsWith("details_t_"))
                {
                    var plantIdStr = data.Replace("details_t_", "");
                    if (int.TryParse(plantIdStr, out int plantId))
                    {
                        var trefleService = MessageHandler.GetTreflePlantService();
                        if (trefleService != null)
                        {
                            await trefleService.SendPlantDetails(chatId, plantId, botClient);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "❌ Сервис Trefle API недоступен");
                        }
                    }
                }
                // Обработка callback-ов для добавления растения из Trefle
                else if (data.StartsWith("add_t_"))
                {
                    var plantIdStr = data.Replace("add_t_", "");
                    if (int.TryParse(plantIdStr, out int plantId))
                    {
                        await MessageHandler.AddPlantFromTrefle(chatId, plantId, botClient);
                    }
                }
                else if (data == "popular_trefle")
                {
                    var trefleService = MessageHandler.GetTreflePlantService();
                    if (trefleService != null)
                    {
                        await trefleService.SendPopularPlants(chatId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Сервис Trefle API недоступен");
                    }
                }
                else if (data == "popular_plants")
                {
                    // Используем Trefle API для популярных растений
                    var trefleService = MessageHandler.GetTreflePlantService();
                    if (trefleService != null)
                    {
                        await trefleService.SendPopularPlants(chatId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Сервис растений недоступен");
                    }
                }
                else if (data.StartsWith("add_from_db_"))
                {
                    var plantId = data.Replace("add_from_db_", "");
                    if (int.TryParse(plantId, out int id))
                    {
                        // Используем Trefle API для добавления растений
                        await MessageHandler.AddPlantFromTrefle(chatId, id, botClient);
                    }
                }
                else if (data == "new_search")
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "🔍 **Новый поиск растений**\n\nВведите название растения:",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        replyMarkup: new ReplyKeyboardRemove());
                }
                else if (data == "search_more")
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "🔍 **Поиск растений**\n\nВведите название растения:",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        replyMarkup: new ReplyKeyboardRemove());
                }
                else if (data.StartsWith("details_"))
                {
                    var plantId = data.Replace("details_", "");
                    if (int.TryParse(plantId, out int id))
                    {
                        // Используем Trefle API для деталей растения
                        var trefleService = MessageHandler.GetTreflePlantService();
                        if (trefleService != null)
                        {
                            await trefleService.SendPlantDetails(chatId, id, botClient);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "❌ Сервис растений недоступен");
                        }
                    }
                }
                // Обработка callback-ов для EnhancedPlantService
                else if (data.StartsWith("plant_"))
                {
                    var plantId = data.Replace("plant_", "");
                    if (_enhancedPlantService != null)
                    {
                        await _enhancedPlantService.SendPlantDetails(chatId, plantId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Сервис растений не инициализирован");
                    }
                }
                else if (data == "online_search")
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "🔍 **Поиск в онлайн-базе**\n\nВведите название растения:",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        replyMarkup: new ReplyKeyboardRemove());
                }
                else if (data == "enhanced_catalog")
                {
                    if (_enhancedPlantService != null)
                    {
                        await _enhancedPlantService.ShowEnhancedCatalog(chatId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Расширенный каталог недоступен");
                    }
                }
                else if (data == "plant_families")
                {
                    if (_enhancedPlantService != null)
                    {
                        await _enhancedPlantService.ShowPlantFamilies(chatId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Сервис семейств растений недоступен");
                    }
                }
                else if (data.StartsWith("family_"))
                {
                    var family = data.Replace("family_", "");
                    if (_enhancedPlantService != null)
                    {
                        await _enhancedPlantService.ShowPlantsByFamily(chatId, family, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Сервис растений недоступен");
                    }
                }
                // Обработка callback-ов для пользовательских растений
                else if (data.StartsWith("add_plant_"))
                {
                    MessageHandler.StartPlantAddition(chatId);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "📸 Отлично! Теперь отправьте фото вашего растения:",
                        replyMarkup: new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("❌ Отмена") }
                        })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        });
                }
                else if (data.StartsWith("water_"))
                {
                    var plantId = data.Replace("water_", "");
                    await UserPlantService.HandleWatering(chatId, plantId, botClient);
                }
                else if (data.StartsWith("view_plant_"))
                {
                    var plantIndexStr = data.Replace("view_plant_", "");
                    if (int.TryParse(plantIndexStr, out int plantIndex))
                    {
                        await UserPlantService.SendPlantDetails(chatId, plantIndex, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Неверный индекс растения");
                    }
                }
                else if (data.StartsWith("show_photo_"))
                {
                    var plantIndexStr = data.Replace("show_photo_", "");
                    if (int.TryParse(plantIndexStr, out int plantIndex))
                    {
                        await UserPlantService.SendPlantPhoto(chatId, plantIndex, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Неверный индекс растения");
                    }
                }
                else if (data == "view_all_photos")
                {
                    await UserPlantService.SendAllPlantsPhotos(chatId, botClient);
                }
                else if (data == "back_to_plants")
                {
                    await UserPlantService.SendUserPlantsList(chatId, botClient);
                }
                else if (data.StartsWith("delete_plant_"))
                {
                    var plantIndexStr = data.Replace("delete_plant_", "");
                    if (int.TryParse(plantIndexStr, out int plantIndex))
                    {
                        await UserPlantService.DeletePlant(chatId, plantIndex, botClient);
                        await UserPlantService.SendUserPlantsList(chatId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(chatId, "❌ Неверный индекс растения");
                    }
                }
                // Обработка callback-ов для локального каталога - УДАЛЕНО
                else if (data == "local_catalog")
                {
                    // Перенаправляем на онлайн-каталог
                    if (_enhancedPlantService != null)
                    {
                        await _enhancedPlantService.ShowEnhancedCatalog(chatId, botClient);
                    }
                    else
                    {
                        var trefleService = MessageHandler.GetTreflePlantService();
                        if (trefleService != null)
                        {
                            await trefleService.SendPopularPlants(chatId, botClient);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "❌ Сервис каталога недоступен");
                        }
                    }
                }
                // Обработка callback-ов для добавления кастомных растений
                else if (data == "add_custom_plant")
                {
                    MessageHandler.StartPlantAddition(chatId);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "📸 **Добавление пользовательского растения**\n\nОтправьте фото растения:",
                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                        replyMarkup: new ReplyKeyboardMarkup(new[]
                        {
                            new[] { new KeyboardButton("❌ Отмена") }
                        })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        });
                }
                // Общие команды
                else if (data == "main_menu")
                {
                    await MessageHandler.SendMainMenu(chatId, botClient);
                }
                else if (data == "cancel")
                {
                    MessageHandler.CancelPlantAddition(chatId);
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Действие отменено.",
                        replyMarkup: await MessageHandler.GetMainMenuKeyboard());
                }
                else if (data == "reload_gigachat")
                {
                    await botClient.SendTextMessageAsync(chatId, "🔄 Перезагружаем GigaChat...");
                    // Здесь можно добавить логику переинициализации GigaChat
                    await botClient.SendTextMessageAsync(chatId, "✅ GigaChat перезагружен!");
                }
                else if (data == "view_my_plants")
                {
                    await UserPlantService.SendUserPlantsList(chatId, botClient);
                }
                else if (data == "random_plant")
                {
                    // Используем Trefle API для случайного растения
                    var trefleService = MessageHandler.GetTreflePlantService();
                    if (trefleService != null)
                    {
                        await trefleService.SendPopularPlants(chatId, botClient);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(
                            chatId,
                            "🎲 **Случайное растение из онлайн-базы**\n\n" +
                            "Используйте поиск растений для просмотра случайных растений из базы Trefle.",
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                            replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[] { InlineKeyboardButton.WithCallbackData("🔍 Поиск растений", "new_search") }
                            }));
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Неизвестный callback: {data}");
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Неизвестная команда");
                    return;
                }

                // Подтверждаем обработку callback
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки callback {data}: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");

                try
                {
                    await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Произошла ошибка");
                    await botClient.SendTextMessageAsync(
                        chatId,
                        "❌ Произошла ошибка при обработке запроса. Попробуйте еще раз.",
                        replyMarkup: await MessageHandler.GetMainMenuKeyboard());
                }
                catch
                {
                    // Игнорируем ошибки при ответе на callback
                }
            }
        }
    }
}