namespace TelegramPlantBot.Models
{
    public class PlantReminder
    {
        public long ChatId { get; set; }
        public string PlantName { get; set; } = string.Empty;
        public DateTime LastWatered { get; set; }
        public int WateringIntervalDays { get; set; }
        public DateTime NextWatering => LastWatered.AddDays(WateringIntervalDays);
        public bool IsActive { get; set; } = true;
    }
}