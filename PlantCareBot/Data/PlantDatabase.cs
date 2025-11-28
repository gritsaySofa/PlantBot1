using TelegramPlantBot.Models;

namespace TelegramPlantBot.Data
{
    public static class PlantDatabase
    {
        public static Dictionary<string, Plant> Plants = new()
        {
            {
                "фикус", new Plant
                {
                    Name = "Фикус Бенджамина",
                    WateringSchedule = "Умеренный полив 1-2 раза в неделю летом, зимой раз в 7-10 дней",
                    LightRequirements = "Яркий рассеянный свет, защита от прямых солнечных лучей",
                    Temperature = "20-25°C летом, 15-18°C зимой",
                    Humidity = "Высокая влажность, регулярное опрыскивание",
                    Fertilizing = "Раз в 2 недели с марта по сентябрь",
                    CommonProblems = "Сбрасывает листья при сквозняках, переливе или недостатке света",
                    CareTips = "Регулярно поворачивайте для равномерного роста"
                }
            },
            {
                "суккулент", new Plant
                {
                    Name = "Суккуленты",
                    WateringSchedule = "Редкий полив раз в 10-14 дней, полное просушивание почвы",
                    LightRequirements = "Прямой солнечный свет 4-6 часов в день",
                    Temperature = "18-28°C, зимой не ниже 10°C",
                    Humidity = "Низкая влажность, не опрыскивать",
                    Fertilizing = "Раз в месяц в период роста (весна-лето)",
                    CommonProblems = "Загнивание от перелива, вытягивание при недостатке света",
                    CareTips = "Используйте хорошо дренированную почву"
                }
            },
            {
                "орхидея", new Plant
                {
                    Name = "Орхидея Фаленопсис",
                    WateringSchedule = "Погружением раз в 7-10 дней, полное стекание воды",
                    LightRequirements = "Яркий рассеянный свет, восточные или западные окна",
                    Temperature = "18-25°C, перепад день/ночь 5-7°C",
                    Humidity = "Высокая влажность 60-80%",
                    Fertilizing = "Специальным удобрением для орхидей раз в 2 недели",
                    CommonProblems = "Отсутствие цветения, загнивание корней",
                    CareTips = "Прозрачный горшок для фотосинтеза корней"
                }
            }
        };

        public static List<string> GetPlantNames()
        {
            return Plants.Keys.ToList();
        }

        public static PlantInfo GetRandomPlant()
        {
            var random = new Random();
            var plants = GetAllPlants();
            if (plants.Any())
            {
                return plants[random.Next(plants.Length)];
            }
            return null;
        }
        public static PlantInfo[] GetAllPlants()
        {
            return new PlantInfo[]
            {
                new PlantInfo { Name = "Роза", /* остальные свойства */ },
                new PlantInfo { Name = "Орхидея", /* остальные свойства */ },
                new PlantInfo { Name = "Фикус", /* остальные свойства */ },
                new PlantInfo { Name = "Кактус", /* остальные свойства */ },
                new PlantInfo { Name = "Спатифиллум", /* остальные свойства */ },
                // Добавьте остальные растения из вашей базы
            };
        }
    }

    public class PlantInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Watering { get; set; } = string.Empty;
        public string Light { get; set; } = string.Empty;
        public string Temperature { get; set; } = string.Empty;
        // Другие свойства...
    }
}
