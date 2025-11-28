using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramPlantBot.Handlers;
using TelegramPlantBot.Services;
using System.Net;
using System.Collections.Generic;

namespace TelegramPlantBot
{
    class Program
    {
        private static TelegramBotClient? botClient;
        private static readonly string botToken = "8333277594:AAEd9WUGu-ACV2q4FY1E2JhujmyiRvrsrKU";
       
        private static readonly string gigaChatApiKey = "MDE5YWJjOGQtMjkxYy03NDViLTk1MGYtNDQ0OWI4MmQ5ZTYyOjMyN2ExN2Q5LTI2NTYtNGZhMy04NWZhLTU2Mjc1NzI4MjBhMg==";

        // ЗАМЕНИТЕ НА ВАШ TREFLE API ТОКЕН
        private static readonly string plantApiKey = "usr-YB2PvGeGSOifaJEyCKilytSzBizT4hzf_mgnnX9tWm4";

        // Текущий активный AI сервис
        private static string currentAIService = "fallback";

        static async Task Main(string[] args)
        {
            Console.WriteLine("🔧 Настройка окружения...");

            // Настройка TLS
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try
            {
                botClient = new TelegramBotClient(botToken);

                // Проверяем подключение к Telegram API
                var me = await botClient.GetMeAsync();
                Console.WriteLine($"✅ Бот @{me.Username} инициализирован");

                Console.WriteLine("🚀 Запуск сервисов...");

                // Инициализируем все сервисы
                CallbackQueryHandler.InitializeEnhancedPlantService(plantApiKey);
                MessageHandler.InitializeEnhancedPlantService(plantApiKey);

                // Инициализируем базу растений
                MessageHandler.InitializePlantDatabaseService(plantApiKey);

                Console.WriteLine("✅ EnhancedPlantService инициализирован");
                Console.WriteLine("✅ Trefle API инициализирован");
                Console.WriteLine("✅ PlantDatabaseService инициализирован");

                // Тестируем подключение к Trefle API
                await TestTrefleConnection();

                // Инициализируем GigaChat как основной AI
                await InitializeGigaChat();

                using CancellationTokenSource cts = new();

                ReceiverOptions receiverOptions = new()
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                };

                // Инициализируем сервис напоминаний
                ReminderService.InitializeReminderSystem(botClient);

                Console.WriteLine("🔄 Запуск получения сообщений...");

                botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );

                Console.WriteLine($"🌿 Бот @{me.Username} запущен!");
                Console.WriteLine($"🤖 Активный AI: {currentAIService}");
                Console.WriteLine("🌐 База растений: Trefle API");

                if (currentAIService == "gigachat")
                {
                    Console.WriteLine("💚 GigaChat активен и готов к работе!");
                }
                else
                {
                    Console.WriteLine("⚠️  Используются fallback советы (без AI)");
                }

                Console.WriteLine("Нажмите любую клавишу для остановки...");
                Console.ReadKey();

                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Критическая ошибка при запуске: {ex.Message}");
                Console.WriteLine($"🔍 Детали: {ex}");
                Console.WriteLine("Нажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        }

        private static async Task TestTrefleConnection()
        {
            try
            {
                Console.WriteLine("🔍 Тестируем подключение к Trefle API...");
                var trefleService = MessageHandler.GetTreflePlantService();

                if (trefleService != null)
                {
                    var isConnected = await trefleService.TestConnection();
                    if (isConnected)
                    {
                        Console.WriteLine("✅ Trefle API подключен и работает!");
                    }
                    else
                    {
                        Console.WriteLine("❌ Trefle API недоступен!");
                        Console.WriteLine("💡 Проверьте:");
                        Console.WriteLine("1. Правильность API токена");
                        Console.WriteLine("2. Активность аккаунта на trefle.io");
                        Console.WriteLine("3. Интернет-подключение");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка тестирования Trefle API: {ex.Message}");
            }
        }

        private static async Task InitializeGigaChat()
        {
            Console.WriteLine("🤖 Инициализация GigaChat...");

            try
            {
                GigaChatService.Initialize(gigaChatApiKey);

                // Тестируем подключение
                Console.WriteLine("🔍 Тестируем подключение к GigaChat...");
                var connectionSuccess = await GigaChatService.TestConnection();

                if (connectionSuccess)
                {
                    currentAIService = "gigachat";
                    Console.WriteLine("✅ GigaChat выбран как основной AI сервис");
                }
                else
                {
                    currentAIService = "fallback";
                    Console.WriteLine("❌ GigaChat недоступен, используем fallback советы");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка инициализации GigaChat: {ex.Message}");
                currentAIService = "fallback";
            }
        }

        public static async Task<string> GetAIResponseAsync(string prompt)
        {
            try
            {
                var response = currentAIService switch
                {
                    "gigachat" => await GigaChatService.GetPlantAdviceAsync(prompt),
                    _ => GetFallbackAdvice(prompt)
                };

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в GetAIResponseAsync: {ex.Message}");
                return GetFallbackAdvice(prompt);
            }
        }

        private static string GetFallbackAdvice(string prompt)
        {
            prompt = prompt.ToLower();

            if (prompt.Contains("привет") || prompt.Contains("start") || prompt.Contains("начать"))
                return "🌿 **Привет! Я ваш помощник по уходу за растениями!**\n\nЗадайте вопрос о растениях:\n• Как ухаживать за орхидеей?\n• Почему желтеют листья?\n• Какие растения подходят для кухни?";

            else if (prompt.Contains("полив") || prompt.Contains("вода"))
                return "💧 **Основы полива:**\n\n• Проверяйте влажность почвы пальцем\n• Поливайте когда верхний слой сухой\n• Используйте отстоянную воду комнатной температуры\n• Избегайте перелива";

            else if (prompt.Contains("свет") || prompt.Contains("освещение"))
                return "☀️ **Освещение:**\n\n• Яркий рассеянный свет - большинство растений\n• Прямое солнце - кактусы, суккуленты\n• Полутень - спатифиллум, замиокулькас";

            else if (prompt.Contains("орхиде"))
                return "🌸 **Уход за орхидеей:**\n\n• Полив 1 раз в 7-10 дней погружением\n• Яркий рассеянный свет\n• Температура 18-25°C\n• Высокая влажность";

            else if (prompt.Contains("фикус"))
                return "🌳 **Уход за фикусом:**\n\n• Умеренный полив\n• Яркий рассеянный свет\n• Температура 18-24°C\n• Регулярно протирать листья";

            else
                return "🌿 **Советы по уходу:**\n\n• 💧 Полив - по мере просыхания почвы\n• ☀️ Свет - яркий рассеянный\n• 🌡 Температура - 18-25°C\n• 💨 Влажность - регулярное опрыскивание\n\n*Укажите название растения для точных рекомендаций*";
        }

        static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is { } message)
                {
                    await MessageHandler.HandleMessage(message, botClient);
                }
                else if (update.CallbackQuery is { } callbackQuery)
                {
                    await CallbackQueryHandler.HandleCallbackQuery(callbackQuery, botClient);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки update: {ex.Message}");
            }
        }

        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}