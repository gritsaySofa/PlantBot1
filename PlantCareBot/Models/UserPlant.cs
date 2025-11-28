using System;

namespace TelegramPlantBot.Models
{
    public class UserPlant
    {
        // ИЗМЕНЯЕМ ТИП НА string И УБИРАЕМ АВТОМАТИЧЕСКУЮ ГЕНЕРАЦИЮ В СВОЙСТВЕ
        public string PlantId { get; set; } = string.Empty;

        public long ChatId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PlantType { get; set; } = string.Empty;

        public string WateringFrequency { get; set; } = string.Empty;
        public int WateringFrequencyDays { get; set; } = 7;

        public string CareInstructions { get; set; } = string.Empty;
        public string PhotoFileId { get; set; } = string.Empty;

        public DateTime? LastWatered { get; set; } = DateTime.Now;
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public DateTime NextWatering
        {
            get
            {
                var lastWateredDate = LastWatered ?? AddedDate;
                return lastWateredDate.AddDays(WateringFrequencyDays);
            }
        }

        public string? LightRequirements { get; set; }
        public string? Temperature { get; set; }
        public string? Humidity { get; set; }

        // Конструктор для автоматической генерации ID
        public UserPlant()
        {
            PlantId = Guid.NewGuid().ToString(); // ИСПОЛЬЗУЕМ GUID ВМЕСТО Random
        }

        // Остальные методы без изменений...
        public void SetWateringFrequency(string frequencyText)
        {
            WateringFrequency = frequencyText;
            WateringFrequencyDays = frequencyText.ToLower() switch
            {
                "💧 ежедневно" or "ежедневно" => 1,
                "💧💧 каждые 2-3 дня" or "каждые 2-3 дня" => 3,
                "💧💧💧 раз в неделю" or "раз в неделю" => 7,
                "💧 раз в 2 недели" or "раз в 2 недели" => 14,
                "🌵 редко (раз в месяц)" or "редко" or "раз в месяц" => 30,
                _ => 7
            };
        }

        public string GetWateringFrequencyText()
        {
            return WateringFrequencyDays switch
            {
                1 => "💧 Ежедневно",
                2 or 3 => "💧💧 Каждые 2-3 дня",
                7 => "💧💧💧 Раз в неделю",
                14 => "💧 Раз в 2 недели",
                30 => "🌵 Редко (раз в месяц)",
                _ => $"💧 Каждые {WateringFrequencyDays} дней"
            };
        }

        public bool NeedsWatering()
        {
            return DateTime.Now >= NextWatering;
        }

        public string GetWateringStatus()
        {
            if (NeedsWatering())
            {
                var daysOverdue = (DateTime.Now - NextWatering).Days;
                return daysOverdue > 0
                    ? $"❌ Просрочен на {daysOverdue} дней"
                    : "⚠️ Требуется полив сегодня";
            }
            else
            {
                var daysUntilWatering = (NextWatering - DateTime.Now).Days;
                return daysUntilWatering switch
                {
                    0 => "💧 Полив сегодня",
                    1 => "💧 Полив завтра",
                    _ => $"✅ Полив через {daysUntilWatering} дней"
                };
            }
        }

        public void WaterPlant()
        {
            LastWatered = DateTime.Now;
        }

        public string GetPlantInfo()
        {
            var info = $"🌱 **{Name}**\n";

            // ДОБАВЛЯЕМ ВИД РАСТЕНИЯ
            if (!string.IsNullOrEmpty(Species))
                info += $"🌿 **Вид:** {Species}\n\n";
            else
                info += "\n";

            info += $"🆔 **ID:** {PlantId}\n" +
                   $"📝 **Описание:** {Description}\n" +
                   $"💧 **Полив:** {GetWateringFrequencyText()}\n" +
                   $"📅 **Последний полив:** {(LastWatered?.ToString("dd.MM.yyyy") ?? "Никогда")}\n" +
                   $"⏰ **Следующий полив:** {NextWatering:dd.MM.yyyy}\n" +
                   $"🔔 **Статус:** {GetWateringStatus()}";

            return info;
        }
    }
}