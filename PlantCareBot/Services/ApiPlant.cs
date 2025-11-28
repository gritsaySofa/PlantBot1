namespace TelegramPlantBot.Models
{
    public class ApiPlant
    {
        public int Id { get; set; }
        public string CommonName { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public string Family { get; set; } = string.Empty;
        public string Genus { get; set; } = string.Empty;
        public List<string> Synonyms { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public WateringInfo Watering { get; set; } = new();
        public LightInfo Light { get; set; } = new();
        public TemperatureInfo Temperature { get; set; } = new();
        public List<string> CareTips { get; set; } = new();
        public List<string> CommonProblems { get; set; } = new();
        public string ImageUrl { get; set; } = string.Empty;

        // Новые свойства для Perenual API
        public string CareLevel { get; set; } = string.Empty;
        public string Sunlight { get; set; } = string.Empty;
        public string GrowthRate { get; set; } = string.Empty;
        public string Maintenance { get; set; } = string.Empty;
        public string Soil { get; set; } = string.Empty;
        public string Dimensions { get; set; } = string.Empty;
        public string CareInstructions { get; set; } = string.Empty;
    }

    public class WateringInfo
    {
        public string Frequency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Tips { get; set; } = string.Empty;
    }

    public class LightInfo
    {
        public string Level { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TemperatureInfo
    {
        public string Min { get; set; } = string.Empty;
        public string Max { get; set; } = string.Empty;
        public string Ideal { get; set; } = string.Empty;
    }

    public class PlantApiResponse
    {
        public List<ApiPlant> Data { get; set; } = new();
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Total { get; set; }
        public int CurrentPage { get; set; }
    }
}