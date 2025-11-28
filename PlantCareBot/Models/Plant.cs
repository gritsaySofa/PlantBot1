namespace TelegramPlantBot.Models
{
    public class Plant
    {
        public string Name { get; set; } = string.Empty;
        public string WateringSchedule { get; set; } = string.Empty;
        public string LightRequirements { get; set; } = string.Empty;
        public string Temperature { get; set; } = string.Empty;
        public string Fertilizing { get; set; } = string.Empty;
        public string Humidity { get; set; } = string.Empty;
        public string CommonProblems { get; set; } = string.Empty;
        public string CareTips { get; set; } = string.Empty;
    }
}