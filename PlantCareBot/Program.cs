using System.Net;
using TelegramPlantBot.Services;
using TelegramPlantBot.Handlers;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Добавляем поддержку контроллеров
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Настройка TLS
ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

// Регистрируем бота как сервис
builder.Services.AddSingleton<ITelegramBotClient>(provider =>
{
    var botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "8333277594:AAEd9WUGu-ACV2q4FY1E2JhujmyiRvrsrKU";
    return new TelegramBotClient(botToken);
});

var app = builder.Build();

// Для Render - используем порт из переменной окружения
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// Инициализация сервисов при запуске
await InitializeServices(app.Services);

Console.WriteLine($"🚀 PlantCareBot запущен на порту {port}");
Console.WriteLine("🌐 Режим: Webhook (Render.com)");

app.Run();

async Task InitializeServices(IServiceProvider services)
{
    Console.WriteLine("🔧 Инициализация сервисов...");

    // Получаем токены из переменных окружения Render
    var plantApiKey = Environment.GetEnvironmentVariable("PLANT_API_KEY") ?? "usr-YB2PvGeGSOifaJEyCKilytSzBizT4hzf_mgnnX9tWm4";
    var gigaChatApiKey = Environment.GetEnvironmentVariable("GIGACHAT_API_KEY") ?? "MDE5YWJjOGQtMjkxYy03NDViLTk1MGYtNDQ0OWI4MmQ5ZTYyOjMyN2ExN2Q5LTI2NTYtNGZhMy04NWZhLTU2Mjc1NzI4MjBhMg==";

    // Инициализируем сервисы
    CallbackQueryHandler.InitializeEnhancedPlantService(plantApiKey);
    MessageHandler.InitializeEnhancedPlantService(plantApiKey);
    MessageHandler.InitializePlantDatabaseService(plantApiKey);

    // Инициализируем GigaChat
    await InitializeGigaChat(gigaChatApiKey);

    // Получаем бота и проверяем подключение
    var botClient = services.GetRequiredService<ITelegramBotClient>();
    var me = await botClient.GetMeAsync();
    Console.WriteLine($"✅ Бот @{me.Username} инициализирован");

    // Тестируем Trefle
    await TestTrefleConnection();

    Console.WriteLine("✅ Все сервисы инициализированы");
}

async Task InitializeGigaChat(string apiKey)
{
    try
    {
        GigaChatService.Initialize(apiKey);
        var connectionSuccess = await GigaChatService.TestConnection();

        if (connectionSuccess)
        {
            Console.WriteLine("✅ GigaChat активен");
        }
        else
        {
            Console.WriteLine("⚠️ GigaChat недоступен, используем fallback");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка GigaChat: {ex.Message}");
    }
}

async Task TestTrefleConnection()
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
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка тестирования Trefle API: {ex.Message}");
    }
}