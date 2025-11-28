namespace TelegramPlantBot.Models
{
    public class PlantJournal
    {
        public long ChatId { get; set; }
        public string PlantName { get; set; } = string.Empty;
        public List<JournalEntry> Entries { get; set; } = new();
    }

    public class JournalEntry
    {
        public DateTime Date { get; set; }
        public string Action { get; set; } = string.Empty; // полив, удобрение, пересадка, обрезка
        public string Notes { get; set; } = string.Empty;
        public string? PhotoFileId { get; set; }
    }
}