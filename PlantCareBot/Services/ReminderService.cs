using Telegram.Bot;
using TelegramPlantBot.Models;

namespace TelegramPlantBot.Services
{
    public class ReminderService
    {
        private static List<PlantReminder> _reminders = new();
        private static Timer? _reminderTimer;

        public static void InitializeReminderSystem(ITelegramBotClient botClient)
        {
            // Проверка напоминаний каждые 30 минут
            _reminderTimer = new Timer(async _ => await CheckReminders(botClient),
                null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        }

        public static async Task AddReminder(long chatId, string plantName, int intervalDays, ITelegramBotClient botClient)
        {
            var reminder = new PlantReminder
            {
                ChatId = chatId,
                PlantName = plantName,
                LastWatered = DateTime.Now,
                WateringIntervalDays = intervalDays
            };

            _reminders.Add(reminder);

            await botClient.SendTextMessageAsync(
                chatId,
                $"✅ Напоминание добавлено!\n" +
                $"Растение: {plantName}\n" +
                $"Полив каждые: {intervalDays} дней\n" +
                $"Следующий полив: {reminder.NextWatering:dd.MM.yyyy}");
        }

        public static async Task SendCareCalendar(long chatId, ITelegramBotClient botClient)
        {
            var userReminders = _reminders
                .Where(r => r.ChatId == chatId && r.IsActive)
                .ToList();

            if (!userReminders.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "📅 У вас нет активных напоминаний о поливе.\n" +
                    "Добавьте растения через каталог!");
                return;
            }

            var message = "📅 **Ваши растения и график полива:**\n\n";

            foreach (var reminder in userReminders)
            {
                var daysUntilWatering = (reminder.NextWatering - DateTime.Now).Days;
                var status = daysUntilWatering <= 0 ? "🔴 ПОРА ПОЛИВАТЬ!" :
                            daysUntilWatering <= 1 ? "🟡 Завтра" : "🟢 Ок";

                message += $"🌱 {reminder.PlantName}\n" +
                          $"💧 Следующий полив: {reminder.NextWatering:dd.MM} ({status})\n" +
                          $"⏱ Интервал: {reminder.WateringIntervalDays} дней\n\n";
            }

            await botClient.SendTextMessageAsync(
                chatId,
                message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
        }

        private static async Task CheckReminders(ITelegramBotClient botClient)
        {
            var dueReminders = _reminders
                .Where(r => r.IsActive && r.NextWatering <= DateTime.Now)
                .ToList();

            foreach (var reminder in dueReminders)
            {
                await botClient.SendTextMessageAsync(
                    reminder.ChatId,
                    $"🔔 **Напоминание о поливе!**\n\n" +
                    $"🌱 Растение: {reminder.PlantName}\n" +
                    $"💧 Пора полить растение!\n\n" +
                    $"После полива используйте команду /polil чтобы обновить дату.",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);

                // Обновляем дату следующего полива
                reminder.LastWatered = DateTime.Now;
            }
        }

        public static List<PlantReminder> GetUserReminders(long chatId)
        {
            return _reminders.Where(r => r.ChatId == chatId).ToList();
        }
    }
}