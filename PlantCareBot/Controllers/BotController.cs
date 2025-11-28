using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramPlantBot.Handlers;
using System.Threading.Tasks;

namespace PlantCareBot.Controllers
{
    [ApiController]
    [Route("bot")]
    public class BotController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;

        public BotController(ITelegramBotClient botClient)
        {
            _botClient = botClient;
        }

        [HttpPost("{token}")]
        public async Task<IActionResult> Post([FromRoute] string token)
        {
            try
            {
                var update = await GetUpdateFromRequest();

                // Обрабатываем update
                if (update.Message is { } message)
                {
                    await MessageHandler.HandleMessage(message, _botClient);
                }
                else if (update.CallbackQuery is { } callbackQuery)
                {
                    await CallbackQueryHandler.HandleCallbackQuery(callbackQuery, _botClient);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в BotController: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        private async Task<Update> GetUpdateFromRequest()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Update>(body);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("🌿 PlantCareBot is running!");
        }
    }
}