using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlantBot.Models;
using TelegramPlantBot.Services;

namespace TelegramPlantBot.Handlers
{
    public static class MessageHandler
    {
        // Временное хранилище для данных во время добавления растения
        private static Dictionary<long, UserPlant> _tempPlantData = new();
        private static EnhancedPlantService? _enhancedPlantService;
        private static TreflePlantService? _treflePlantService;

        // ДОБАВЛЯЕМ СЕРВИС БАЗЫ РАСТЕНИЙ
        private static PlantDatabaseService? _plantDatabaseService;

        // Метод для доступа к сервису базы растений из других классов
        public static PlantDatabaseService? GetPlantDatabaseService()
        {
            return _plantDatabaseService;
        }
        public static UserPlant? GetTempPlant(long chatId)
        {
            return _tempPlantData.ContainsKey(chatId) ? _tempPlantData[chatId] : null;
        }

        // Добавляем состояния пользователя
        public enum UserState
        {
            None,
            AwaitingQuestion,
            AddingPlant,
            AwaitingPlantName,
            AwaitingWatering,
            SearchingPlants // ДОБАВЛЯЕМ НОВОЕ СОСТОЯНИЕ
        }

        private static Dictionary<long, UserState> userStates = new Dictionary<long, UserState>();

        // Метод для инициализации сервиса (вызовите его из Program.cs)
        public static void InitializeEnhancedPlantService(string apiKey)
        {
            _enhancedPlantService = new EnhancedPlantService(apiKey);
        }
     
        public static void InitializeTreflePlantService(string apiToken)
        {
            _treflePlantService = new TreflePlantService(apiToken);
        }
        public static TreflePlantService? GetTreflePlantService()
        {
            return _treflePlantService;
        }

        // ДОБАВЛЯЕМ МЕТОД ДЛЯ ИНИЦИАЛИЗАЦИИ БАЗЫ РАСТЕНИЙ
        public static void InitializePlantDatabaseService(string apiKey)
        {
            _plantDatabaseService = new PlantDatabaseService(apiKey);
        }

        public static async Task HandleMessage(Message message, ITelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text ?? "";

            try
            {
                // ПЕРВОЕ: Проверяем команду отмены - она должна обрабатываться в ЛЮБОМ состоянии
                if (messageText.ToLower() == "отмена" || messageText.ToLower() == "/cancel" || messageText == "❌ Отмена")
                {
                    await HandleCancelCommand(chatId, botClient);
                    return;
                }

                // ВТОРОЕ: Проверяем, находится ли пользователь в процессе добавления растения
                if (_tempPlantData.ContainsKey(chatId))
                {
                    await HandleAddPlantFlow(message, botClient);
                    return;
                }

                // ТРЕТЬЕ: Проверяем, начинается ли процесс добавления растения с фото
                if (message.Photo != null && message.Photo.Any())
                {
                    // Если пользователь отправил фото без команды, начинаем процесс добавления
                    _tempPlantData[chatId] = new UserPlant { ChatId = chatId };
                    await HandlePlantPhoto(chatId, message.Photo, botClient);
                    return;
                }

                // ЧЕТВЕРТОЕ: Проверяем состояние ожидания вопроса к AI
                if (userStates.ContainsKey(chatId) && userStates[chatId] == UserState.AwaitingQuestion)
                {
                    await HandleAIQuestion(chatId, messageText, botClient, message);
                    return;
                }

                // ПЯТОЕ: Обрабатываем обычные команды и сообщения
                switch (messageText.ToLower())
                {
                    case "/start":
                    case "главное меню":
                    case "меню":
                    case "/menu":
                        await SendMainMenu(chatId, botClient);
                        break;

                    case "/ai":
                    case "ai помощник":
                    case "🤖 ai помощник":
                    case "спросить ai":
                    case "задать вопрос":
                        await StartAIQuestion(chatId, botClient);
                        break;

                    case "каталог растений":
                    case "/catalog":
                    case "📚 каталог растений":
                        await PlantService.SendPlantCatalog(chatId, botClient);
                        break;

                    case "мои растения":
                    case "/myplants":
                    case "🌱 мои растения":
                        await UserPlantService.SendUserPlantsList(chatId, botClient);
                        break;

                    case "полив":
                    case "/water":
                    case "отметить полив":
                    case "💧 отметить полив":
                    case "💧 полив":
                        await UserPlantService.SendWateringMenu(chatId, botClient);
                        break;

                    case "добавить растение":
                    case "/addplant":
                    case "➕ добавить растение":
                    case "➕ добавить":
                        await UserPlantService.StartAddPlantFlow(chatId, botClient);
                        _tempPlantData[chatId] = new UserPlant { ChatId = chatId };
                        break;

                    case "сезонные советы":
                    case "/tips":
                    case "🌸 сезонные советы":
                        await botClient.SendTextMessageAsync(
                            chatId, PlantService.GetSeasonalAdvice());
                        break;

                    case "помощь":
                    case "/help":
                    case "ℹ️ помощь":
                        await SendHelpMessage(chatId, botClient);
                        break;

                    case "поиск растения":
                    case "🔍 поиск растения":
                    case "/search":
                        userStates[chatId] = UserState.SearchingPlants;
                        await botClient.SendTextMessageAsync(
                            chatId,
                            "🔍 **Поиск в базе растений** 🌿\n\n" +
                            "Введите название растения на русском или английском:\n\n" +
                            "**Примеры запросов:**\n" +
                            "• роза\n• орхидея\n• фикус\n• кактус\n" +
                            "• rose\n• orchid\n• ficus\n• cactus\n\n" +
                            "Или выберите действие ниже:",
                            parseMode: ParseMode.Markdown,
                            replyMarkup: new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("🌟 Популярные растения", "popular_plants"),
                                    InlineKeyboardButton.WithCallbackData("🌿 Случайное растение", "random_plant")
                                },
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
                                }
                            }));
                        break;

                    case "популярные растения":
                    case "/popular":
                        if (_plantDatabaseService != null)
                        {
                            await _plantDatabaseService.SendPopularPlants(chatId, botClient);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "❌ Сервис базы растений недоступен");
                        }
                        break;

                    case "/reload_gigachat":
                        await HandleReloadGigaChat(chatId, botClient);
                        break;

                    default:
                        await HandleDefaultMessage(chatId, messageText, botClient);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId, "❌ Произошла ошибка при обработке запроса");
            }
        }


        public static async Task AddPlantFromDatabase(long chatId, int plantId, ITelegramBotClient botClient)
        {
            if (_plantDatabaseService == null)
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Сервис базы растений недоступен");
                return;
            }

            try
            {
                var plantDetails = await _plantDatabaseService.GetPlantDetailsAsync(plantId);

                if (plantDetails == null)
                {
                    await botClient.SendTextMessageAsync(chatId, "❌ Растение не найдено в базе");
                    return;
                }

                // Создаем UserPlant из данных базы
                var userPlant = new UserPlant
                {
                    ChatId = chatId,
                    Name = plantDetails.Common_Name,
                    Description = plantDetails.Description,
                    PlantType = "from_database",
                    WateringFrequency = plantDetails.Watering,
                    CareInstructions = $"Свет: {plantDetails.Sunlight}\nСложность ухода: {plantDetails.Care_Level}",
                    LastWatered = DateTime.Now
                };

                // Добавляем растение в коллекцию пользователя
                UserPlantService.AddUserPlant(chatId, userPlant);

                var message = $"✅ **{plantDetails.Common_Name}** добавлено в вашу коллекцию! 🌱\n\n" +
                             $"**Описание:** {Truncate(plantDetails.Description, 200)}\n" +
                             $"**💧 Полив:** {plantDetails.Watering}\n" +
                             $"**☀️ Свет:** {plantDetails.Sunlight}\n" +
                             $"**🔄 Уход:** {plantDetails.Care_Level}";

                // Если есть фото - отправляем фото
                if (plantDetails.Default_Image != null && !string.IsNullOrEmpty(plantDetails.Default_Image.Regular))
                {
                    await botClient.SendPhotoAsync(
                        chatId,
                        new InputFileUrl(plantDetails.Default_Image.Regular),
                        caption: message,
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        message,
                        parseMode: ParseMode.Markdown);
                }

                await botClient.SendTextMessageAsync(
                    chatId,
                    "💡 **Совет:** Вы можете настроить индивидуальный график полива для этого растения в разделе '🌱 Мои растения'",
                    replyMarkup: await GetMainMenuKeyboard());

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления растения из базы: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "❌ Ошибка при добавлении растения из базы");
            }
        }

        // Вспомогательный метод для обрезки текста
        private static string Truncate(string text, int length)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= length ? text : text.Substring(0, length) + "...";
        }

        // Новый метод для начала AI-диалога
        private static async Task StartAIQuestion(long chatId, ITelegramBotClient botClient)
        {
            userStates[chatId] = UserState.AwaitingQuestion;

            await botClient.SendTextMessageAsync(
                chatId,
                "🤖 **AI-помощник по растениям** 🌿\n\n" +
                "Задайте ваш вопрос о растениях, и я помогу!\n\n" +
                "**Примеры вопросов:**\n" +
                "• Как ухаживать за орхидеей?\n" +
                "• Почему желтеют листья у фикуса?\n" +
                "• Какие растения подходят для темной комнаты?\n" +
                "• Как бороться с вредителями?\n" +
                "• Когда пересаживать кактусы?\n\n" +
                "💡 *Отвечаю на русском языке с подробными рекомендациями*",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("❌ Отмена") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                });
        }

        // Метод для обработки AI-вопросов
        private static async Task HandleAIQuestion(long chatId, string question, ITelegramBotClient botClient, Message message)
        {
            userStates[chatId] = UserState.None;

            // Показываем что обрабатываем запрос
            var processingMessage = await botClient.SendTextMessageAsync(
                chatId,
                "🤔 Анализирую ваш вопрос...");

            try
            {
                // Получаем ответ от AI
                var aiResponse = await Program.GetAIResponseAsync(question);

                // Удаляем сообщение "Думаю над ответом"
                await botClient.DeleteMessageAsync(chatId, processingMessage.MessageId);

                // Отправляем ответ
                await botClient.SendTextMessageAsync(
                    chatId,
                    aiResponse,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: await GetMainMenuKeyboard()
                );

                Console.WriteLine($"✅ AI ответ отправлен для chatId: {chatId}");
            }
            catch (Exception ex)
            {
                // Удаляем сообщение "Думаю над ответом"
                try { await botClient.DeleteMessageAsync(chatId, processingMessage.MessageId); } catch { }

                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Произошла ошибка при обработке запроса. Попробуйте позже.\n\n" +
                    "💡 *Пока что вот базовый совет:*\n" + GetFallbackAdvice(question),
                    parseMode: ParseMode.Markdown,
                    replyMarkup: await GetMainMenuKeyboard()
                );
                Console.WriteLine($"❌ Ошибка AI запроса: {ex.Message}");
            }
        }

        // Метод для перезагрузки GigaChat
        private static async Task HandleReloadGigaChat(long chatId, ITelegramBotClient botClient)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🔄 Перезагружаем GigaChat...");

            try
            {
                // Здесь можно добавить логику переинициализации GigaChat
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ GigaChat перезагружен! Теперь можете задавать вопросы.",
                    replyMarkup: await GetMainMenuKeyboard());
            }
            catch (Exception ex)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"❌ Ошибка перезагрузки GigaChat: {ex.Message}",
                    replyMarkup: await GetMainMenuKeyboard());
            }
        }

        // Метод для обработки команды отмены
        private static async Task HandleCancelCommand(long chatId, ITelegramBotClient botClient)
        {
            // Очищаем состояние
            if (userStates.ContainsKey(chatId))
                userStates.Remove(chatId);

            if (_tempPlantData.ContainsKey(chatId))
                _tempPlantData.Remove(chatId);

            await botClient.SendTextMessageAsync(
                chatId,
                "❌ Действие отменено.",
                replyMarkup: await GetMainMenuKeyboard());
        }

        // Fallback советы
        private static string GetFallbackAdvice(string prompt)
        {
            prompt = prompt.ToLower();

            if (prompt.Contains("полив") || prompt.Contains("вода"))
                return "💧 **Основы полива:**\n\n• Проверяйте влажность почвы пальцем\n• Поливайте когда верхний слой сухой\n• Используйте отстоянную воду комнатной температуры";

            else if (prompt.Contains("свет") || prompt.Contains("освещение"))
                return "☀️ **Освещение:**\n\n• Яркий рассеянный свет - большинство растений\n• Прямое солнце - кактусы, суккуленты\n• Полутень - спатифиллум, замиокулькас";

            else if (prompt.Contains("орхиде"))
                return "🌸 **Уход за орхидеей:**\n\n• Полив 1 раз в 7-10 дней погружением\n• Яркий рассеянный свет\n• Температура 18-25°C\n• Высокая влажность";

            else if (prompt.Contains("фикус"))
                return "🌳 **Уход за фикусом:**\n\n• Умеренный полив\n• Яркий рассеянный свет\n• Температура 18-24°C\n• Регулярно протирать листья";

            else
                return "🌿 **Общие советы по уходу:**\n\n• 💧 Полив - по мере просыхания почвы\n• ☀️ Свет - яркий рассеянный\n• 🌡 Температура - 18-25°C\n• 💨 Влажность - регулярное опрыскивание";
        }

        // Метод для обработки обычных сообщений
        private static async Task HandleDefaultMessage(long chatId, string messageText, ITelegramBotClient botClient)
        {
            // 0. ПРОВЕРЯЕМ СОСТОЯНИЕ ПОИСКА РАСТЕНИЙ
            if (userStates.ContainsKey(chatId) && userStates[chatId] == UserState.SearchingPlants)
            {
                userStates[chatId] = UserState.None;
                if (_plantDatabaseService != null)
                {
                    await _plantDatabaseService.SearchAndSendResults(chatId, messageText, botClient);
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "❌ Сервис базы растений недоступен");
                }
                return;
            }

            // 1. ПРОВЕРЯЕМ КОМАНДЫ ДЛЯ ДОБАВЛЕНИЯ ИЗ БАЗЫ
            if (messageText.StartsWith("/details_"))
            {
                var plantId = messageText.Replace("/details_", "");
                if (int.TryParse(plantId, out int id) && _plantDatabaseService != null)
                {
                    await _plantDatabaseService.SendPlantDetails(chatId, id, botClient);
                    return;
                }
            }
            else if (messageText.StartsWith("/add_"))
            {
                var plantId = messageText.Replace("/add_", "");
                if (int.TryParse(plantId, out int id))
                {
                    await AddPlantFromDatabase(chatId, id, botClient);
                    return;
                }
            }

            // 2. ПРОВЕРЯЕМ AI-ЗАПРОСЫ
            if (IsAiQuery(messageText))
            {
                await HandleAiQuery(chatId, messageText, botClient);
                return;
            }

            // 3. Проверяем, является ли сообщение названием растения в локальной базе
            if (_treflePlantService != null && IsPlantSearchQuery(messageText))
            {
                await _treflePlantService.SearchAndSendResults(chatId, messageText, botClient);
                return;
            }
            {
                await PlantService.SendPlantInfo(chatId, messageText, botClient);
                return;
            }

            // 4. Если не растение и не команда, проверяем возможность поиска в API
            if (messageText.Length > 2 && !IsPlantProblemDescription(messageText) && IsPlantSearchQuery(messageText))
            {
                // Предлагаем поиск в базе растений
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔍 Искать в базе", $"search_db_{messageText}"),
                        InlineKeyboardButton.WithCallbackData("➕ Добавить своё", "add_custom_plant")
                    }
                });

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"🔍 Найдено растение **{messageText}**\n\n" +
                    "Хотите найти его в базе растений или добавить как пользовательское?",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard);
                return;
            }

            // 5. Если не растение и не поисковый запрос, проверяем на проблему
            if (IsPlantProblemDescription(messageText))
            {
                var diagnosis = DiagnosisService.DiagnosePlantProblem(messageText);
                await botClient.SendTextMessageAsync(
                    chatId,
                    diagnosis,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: await GetMainMenuKeyboard());
            }
            else
            {
                // 6. Если ничего не подошло - используем AI как запасной вариант
                await HandleAiQuery(chatId, messageText, botClient);
            }
        }

        // Метод для обработки AI-запросов
        private static async Task HandleAiQuery(long chatId, string messageText, ITelegramBotClient botClient)
        {
            await botClient.SendChatActionAsync(chatId, ChatAction.Typing);

            // Убираем префикс /ai если есть
            var cleanMessage = messageText.StartsWith("/ai ") ? messageText.Substring(4) : messageText;
            cleanMessage = cleanMessage.StartsWith("ai ") ? cleanMessage.Substring(3) : cleanMessage;

            // Используем общий метод для AI ответов
            var aiResponse = await Program.GetAIResponseAsync(cleanMessage);
            await botClient.SendTextMessageAsync(
                chatId,
                aiResponse,
                parseMode: ParseMode.Markdown,
                replyMarkup: await GetMainMenuKeyboard());
        }

        // Метод для определения AI-запросов
        private static bool IsAiQuery(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var aiKeywords = new[]
            {
                "/ai", "ai ", "совет", "помоги", "вопрос", "?", "как", "почему", "что",
                "расскажи", "объясни", "посоветуй", "рекомендац", "помощь", "зачем",
                "можно ли", "стоит ли", "какой", "какая", "какое", "какие"
            };

            text = text.ToLower().Trim();

            // Если текст начинается с /ai - точно AI запрос
            if (text.StartsWith("/ai") || text.StartsWith("ai "))
                return true;

            // Если содержит вопросительные слова или знаки
            return aiKeywords.Any(keyword => text.Contains(keyword));
        }

        // Метод для определения поисковых запросов растений
        private static bool IsPlantSearchQuery(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var plantIndicators = new[]
            {
                "роза", "фиалка", "фикус", "кактус", "орхидея", "пальма", "суккулент",
                "rose", "violet", "ficus", "cactus", "orchid", "palm", "succulent",
                "монстера", "спатифиллум", "антуриум", "замиокулькас", "драцена",
                "сансевиерия", "хлорофитум", "герань", "пеларгония", "бегония"
            };

            text = text.ToLower().Trim();

            // Исключаем AI-запросы
            if (IsAiQuery(text))
                return false;

            // Проверяем, похоже ли на название растения
            return plantIndicators.Any(plant => text.Contains(plant)) ||
                   (text.Length < 25 && !text.Contains(" ")); // Короткие одиночные слова
        }

        // Метод для проблем растений
        private static bool IsPlantProblemDescription(string text)
        {
            var problemKeywords = new[]
            {
                "желте", "сохн", "опада", "вял", "гни", "пятн", "насеком", "вредител",
                "боле", "черне", "мучни", "паутин", "щитовк", "тля", "клещ"
            };

            text = text.ToLower();
            return problemKeywords.Any(keyword => text.Contains(keyword));
        }

        public static async Task SendMainMenu(long chatId, ITelegramBotClient botClient)
        {
            var userPlantsCount = UserPlantService.GetUserPlants(chatId).Count;

            var message = $"🌿 **Добро пожаловать в помощник по растениям!**\n\n" +
                         $"📊 Ваша коллекция: {userPlantsCount} растений\n\n" +
                         "Выберите действие из меню ниже:";

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                parseMode: ParseMode.Markdown,
                replyMarkup: await GetMainMenuKeyboard());
        }

        // Вынесем создание клавиатуры в отдельный метод для переиспользования
        public static async Task<ReplyKeyboardMarkup> GetMainMenuKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[]
                {
                    new KeyboardButton("🤖 AI Помощник"),
                    new KeyboardButton("📚 Каталог растений")
                },
                new[]
                {
                    new KeyboardButton("🌱 Мои растения"),
                    new KeyboardButton("➕ Добавить растение")
                },
                new[]
                {
                    new KeyboardButton("💧 Полив"),
                    new KeyboardButton("🌸 Сезонные советы")
                },
                new[]
                {
                    new KeyboardButton("ℹ️ Помощь")
                }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = false
            };
        }

        private static async Task SendHelpMessage(long chatId, ITelegramBotClient botClient)
        {
            var message = "🆘 **Помощь по боту**\n\n" +
                         "**🤖 AI-помощник (GigaChat):**\n" +
                         "• Просто напишите любой вопрос о растениях\n" +
                         "• Используйте префикс /ai для явного указания\n" +
                         "• Примеры: \"/ai как ухаживать за орхидеей?\"\n" +
                         "• Или просто: \"Почему желтеют листья?\"\n\n" +

                         "**🔍 Поиск растений:**\n" +
                         "• Напишите название растения для поиска в базе\n" +
                         "• Используйте команду /search для онлайн-поиска\n" +
                         "• Или выберите '🔍 Поиск растения' в меню\n\n" +

                         "**Основные команды:**\n" +
                         "/start - Главное меню\n" +
                         "/menu - Показать меню\n" +
                         "/catalog - Каталог растений\n" +
                         "/myplants - Мои растения\n" +
                         "/water - Отметить полив 💧\n" +
                         "/addplant - Добавить растение с фото\n" +
                         "/search - Поиск растений онлайн\n" +
                         "/tips - Сезонные советы\n" +
                         "/reload_gigachat - Перезагрузить AI\n" +
                         "/cancel - Отмена текущего действия\n\n" +

                         "**Или просто напишите:**\n" +
                         "• Название растения (например: \"фикус\")\n" +
                         "• Описание проблемы (например: \"желтеют листья\")\n" +
                         "• Любой вопрос о растениях для AI-помощника";

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                parseMode: ParseMode.Markdown,
                replyMarkup: await GetMainMenuKeyboard());
        }

        private static async Task HandleAddPlantFlow(Message message, ITelegramBotClient botClient)
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text ?? "";

            // Если пришло фото в процессе добавления растения
            if (message.Photo != null && message.Photo.Any())
            {
                await HandlePlantPhoto(chatId, message.Photo, botClient);
                return;
            }

            var tempPlant = _tempPlantData[chatId];

            // Процесс уже начат, обрабатываем шаги
            if (string.IsNullOrEmpty(tempPlant.Name))
            {
                // Шаг 1: Название растения
                tempPlant.Name = messageText;
                await UserPlantService.HandlePlantName(chatId, messageText, botClient);
            }
            else if (string.IsNullOrEmpty(tempPlant.Species))
            {
               
                tempPlant.Species = messageText;
                await UserPlantService.HandlePlantSpecies(chatId, messageText, botClient);
            }
            else if (string.IsNullOrEmpty(tempPlant.Description))
            {
                // Шаг 3: Описание растения
                tempPlant.Description = messageText;
                await UserPlantService.HandlePlantDescription(chatId, messageText, botClient);
            }
            else
            {
                // Шаг 4: Частота полива
                var success = await UserPlantService.HandleWateringFrequency(chatId, messageText, tempPlant, botClient);
                if (success)
                {
                    // Завершаем процесс и показываем меню
                    _tempPlantData.Remove(chatId);
                    await SendMainMenu(chatId, botClient);
                }
            }
        }
        private static async Task HandlePlantPhoto(long chatId, PhotoSize[] photos, ITelegramBotClient botClient)
        {
            if (_tempPlantData.ContainsKey(chatId))
            {
                var photo = photos.OrderByDescending(p => p.Width * p.Height).First();
                _tempPlantData[chatId].PhotoFileId = photo.FileId;

                // Создаем клавиатуру с кнопкой отмены для процесса добавления
                var cancelKeyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("❌ Отмена") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await botClient.SendTextMessageAsync(
                    chatId,
                    "📸 Фото получено! Теперь введите название растения:\n\n" +
                    "Для отмены нажмите '❌ Отмена' или введите 'отмена'",
                    parseMode: ParseMode.Markdown,
                    replyMarkup: cancelKeyboard);
            }
        }

        // Методы для работы с процессом добавления растения
        public static bool IsAddingPlant(long chatId)
        {
            return _tempPlantData.ContainsKey(chatId);
        }

        public static void CancelPlantAddition(long chatId)
        {
            if (_tempPlantData.ContainsKey(chatId))
            {
                _tempPlantData.Remove(chatId);
            }
        }

        public static void StartPlantAddition(long chatId)
        {
            _tempPlantData[chatId] = new UserPlant { ChatId = chatId };
        }
        public static async Task AddPlantFromTrefle(long chatId, int plantId, ITelegramBotClient botClient)
        {
            try
            {
                var trefleService = GetTreflePlantService();
                if (trefleService == null)
                {
                    await botClient.SendTextMessageAsync(chatId, "❌ Сервис Trefle API недоступен");
                    return;
                }

                // Здесь можно получить детали растения из Trefle и создать UserPlant
                // Пока что создаем базовое растение
                var userPlant = new UserPlant
                {
                    ChatId = chatId,
                    Name = $"Растение из базы #{plantId}",
                    Species = "Из онлайн-базы Trefle",
                    Description = "Добавлено из онлайн-базы растений",
                    WateringFrequency = "💧💧💧 Раз в неделю",
                    LastWatered = DateTime.Now
                };

                UserPlantService.AddUserPlant(chatId, userPlant);

                await botClient.SendTextMessageAsync(
                    chatId,
                    $"✅ Растение добавлено в вашу коллекцию! 🌱\n\n" +
                    $"Теперь вы можете настроить индивидуальный уход для него.",
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                new[] { InlineKeyboardButton.WithCallbackData("🌱 Мои растения", "view_my_plants") },
                new[] { InlineKeyboardButton.WithCallbackData("✏️ Редактировать", $"edit_plant_{UserPlantService.GetUserPlants(chatId).Count - 1}") }
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка добавления растения из Trefle: {ex.Message}");
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ Произошла ошибка при добавлении растения.");
            }
        }
    }
}