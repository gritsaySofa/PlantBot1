using Telegram.Bot;
using TelegramPlantBot.Models;

namespace TelegramPlantBot.Services
{
    public class JournalService
    {
        private static List<PlantJournal> _journals = new();

        public static async Task AddJournalEntry(long chatId, string plantName, string action, string notes, ITelegramBotClient botClient)
        {
            var journal = _journals.FirstOrDefault(j => j.ChatId == chatId && j.PlantName == plantName);

            if (journal == null)
            {
                journal = new PlantJournal { ChatId = chatId, PlantName = plantName };
                _journals.Add(journal);
            }

            journal.Entries.Add(new JournalEntry
            {
                Date = DateTime.Now,
                Action = action,
                Notes = notes
            });

            await botClient.SendTextMessageAsync(
                chatId,
                $"📔 Запись добавлена в дневник!\n" +
                $"🌱 Растение: {plantName}\n" +
                $"📝 Действие: {action}\n" +
                $"💬 Заметки: {notes}");
        }

        public static async Task SendPlantJournal(long chatId, string plantName, ITelegramBotClient botClient)
        {
            var journal = _journals.FirstOrDefault(j => j.ChatId == chatId && j.PlantName == plantName);

            if (journal == null || !journal.Entries.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"📔 Дневник растения \"{plantName}\" пуст.\n" +
                    $"Добавьте первую запись через меню растения!");
                return;
            }

            var message = $"📔 **Дневник ухода: {plantName}**\n\n";

            foreach (var entry in journal.Entries.OrderByDescending(e => e.Date).Take(10))
            {
                message += $"📅 {entry.Date:dd.MM.yyyy}\n" +
                          $"🔧 {entry.Action}\n" +
                          $"📝 {entry.Notes}\n\n";
            }

            await botClient.SendTextMessageAsync(chatId, message);
        }
    }
}