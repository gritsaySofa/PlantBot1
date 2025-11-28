using System.Text.Json.Serialization;

namespace PlantCareBot.Models
{
    public class PlantSearchResult
    {
        public PlantData[] Data { get; set; } = Array.Empty<PlantData>();
    }

    public class PlantData
    {
        public int Id { get; set; }
        public string Common_Name { get; set; } = string.Empty;
        public string Scientific_Name { get; set; } = string.Empty;
        public string[] Other_Name { get; set; } = Array.Empty<string>();
        public string Cycle { get; set; } = string.Empty;
        public string Watering { get; set; } = string.Empty;
        public string Sunlight { get; set; } = string.Empty;
        public DefaultImage Default_Image { get; set; }
    }

    public class DefaultImage
    {
        public string Thumbnail { get; set; } = string.Empty;
        public string Regular { get; set; } = string.Empty;
    }

    public class PlantDetails
    {
        public int Id { get; set; }
        public string Common_Name { get; set; } = string.Empty;
        public string Scientific_Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Watering { get; set; } = string.Empty;
        public string Sunlight { get; set; } = string.Empty;
        public string Care_Level { get; set; } = string.Empty;
        public string Maintenance { get; set; } = string.Empty;
        public string Growth_Rate { get; set; } = string.Empty;
        public string Pruning_Month { get; set; } = string.Empty;
        public string[] Pruning_Count { get; set; } = Array.Empty<string>();
        public string Soil { get; set; } = string.Empty;
        public string Hardiness { get; set; } = string.Empty;
        public DefaultImage Default_Image { get; set; }
        public WateringGuide Watering_Guide { get; set; }
    }

    public class WateringGuide
    {
        public string Watering_General_Benchmark { get; set; } = string.Empty;
    }
}