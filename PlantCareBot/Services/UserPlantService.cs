using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlantBot.Models;

namespace TelegramPlantBot.Services
{
    public static class UserPlantService
    {
        private static Dictionary<long, List<UserPlant>> _userPlants = new();

        // ДОБАВЛЯЕМ НОВЫЙ МЕТОД ДЛЯ ОБРАБОТКИ ВИДА РАСТЕНИЯ
        public static async Task HandlePlantSpecies(long chatId, string species, ITelegramBotClient botClient)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                $"🌿 Вид сохранен: **{species}**\n\n" +
                "Теперь введите описание растения (или нажмите 'Пропустить'):",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("Пропустить"), new KeyboardButton("❌ Отмена") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                });
        }

        public static List<UserPlant> GetUserPlants(long chatId)
        {
            return _userPlants.ContainsKey(chatId) ? _userPlants[chatId] : new List<UserPlant>();
        }

        public static async Task SendUserPlantsList(long chatId, ITelegramBotClient botClient)
        {
            var plants = GetUserPlants(chatId);

            if (!plants.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "🌱 У вас пока нет растений в коллекции.\n\n" +
                    "Добавьте первое растение через меню '➕ Добавить растение'");
                return;
            }

            // Создаем инлайн-клавиатуру для выбора растения
            var buttons = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < plants.Count; i++)
            {
                var plant = plants[i];
                var hasPhoto = !string.IsNullOrEmpty(plant.PhotoFileId);
                var buttonText = hasPhoto ? $"🖼 {plant.Name}" : $"📝 {plant.Name}";

                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(buttonText, $"view_plant_{i}")
                });
            }

            // Добавляем кнопки действий
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("📸 Показать все фото", "view_all_photos"),
                InlineKeyboardButton.WithCallbackData("🔙 Главное меню", "main_menu")
            });

            await botClient.SendTextMessageAsync(
                chatId,
                $"🌿 **Ваша коллекция растений** ({plants.Count} шт.)\n\n" +
                "Выберите растение для просмотра:\n" +
                "🖼 - есть фото\n" +
                "📝 - без фото",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        
        public static async Task SendPlantDetails(long chatId, int plantIndex, ITelegramBotClient botClient)
        {
            var plants = GetUserPlants(chatId);

            if (plantIndex < 0 || plantIndex >= plants.Count)
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Растение не найдено");
                return;
            }

            var plant = plants[plantIndex];
            var message = $"🌿 **{plant.Name}**\n\n";

            // ДОБАВЛЯЕМ ВИД РАСТЕНИЯ
            if (!string.IsNullOrEmpty(plant.Species))
                message += $"🌱 *Вид:* {plant.Species}\n\n";

            if (!string.IsNullOrEmpty(plant.Description))
                message += $"📝 *Описание:* {plant.Description}\n\n";

            message += $"💧 *Полив:* {plant.WateringFrequency}\n";
            message += $"🔄 *Последний полив:* {(plant.LastWatered.HasValue ? plant.LastWatered.Value.ToString("dd.MM.yyyy") : "еще не поливали")}\n";
            message += $"📅 *Следующий полив:* {plant.NextWatering:dd.MM.yyyy}\n";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("💧 Отметить полив", $"water_{plantIndex}"),
            InlineKeyboardButton.WithCallbackData("🖼 Показать фото", $"show_photo_{plantIndex}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("🔙 К списку растений", "back_to_plants"),
            InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"delete_plant_{plantIndex}")
        }
    });

            // Если есть фото - отправляем фото с подписью
            if (!string.IsNullOrEmpty(plant.PhotoFileId))
            {
                await botClient.SendPhotoAsync(
                    chatId,
                    new InputFileId(plant.PhotoFileId),
                    caption: message,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard);
            }
            else
            {
                // Если фото нет - отправляем просто текст
                message += "\n📷 *Фото:* не добавлено";
                await botClient.SendTextMessageAsync(
                    chatId,
                    message,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: keyboard);
            }
        }


        // Метод для показа фото растения
        // Метод для показа фото растения
        public static async Task SendPlantPhoto(long chatId, int plantIndex, ITelegramBotClient botClient)
        {
            var plants = GetUserPlants(chatId);

            if (plantIndex < 0 || plantIndex >= plants.Count)
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Растение не найдено");
                return;
            }

            var plant = plants[plantIndex];

            if (string.IsNullOrEmpty(plant.PhotoFileId))
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    $"❌ У растения **{plant.Name}** нет фото.\n\n" +
                    "Вы можете добавить фото при редактировании растения.",
                    parseMode: ParseMode.Markdown);
                return;
            }

            // ДОБАВЛЯЕМ ВИД РАСТЕНИЯ В ПОДПИСЬ К ФОТО
            var caption = $"🖼 **{plant.Name}**\n";

            if (!string.IsNullOrEmpty(plant.Species))
                caption += $"🌱 *Вид:* {plant.Species}\n\n";
            else
                caption += "\n";

            caption += $"💧 Следующий полив: {plant.NextWatering:dd.MM.yyyy}";

            await botClient.SendPhotoAsync(
                chatId,
                new InputFileId(plant.PhotoFileId),
                caption: caption,
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", $"view_plant_{plantIndex}")));
        }
        // Метод для показа всех фото растений
        // Метод для показа всех фото растений
        public static async Task SendAllPlantsPhotos(long chatId, ITelegramBotClient botClient)
        {
            var plants = GetUserPlants(chatId);
            var plantsWithPhotos = plants.Where(p => !string.IsNullOrEmpty(p.PhotoFileId)).ToList();

            if (!plantsWithPhotos.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "📷 У вас пока нет растений с фотографиями.\n\n" +
                    "Добавьте фото при добавлении новых растений!",
                    parseMode: ParseMode.Markdown);
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId,
                $"🖼 **Галерея ваших растений** ({plantsWithPhotos.Count} фото)\n\n" +
                "Отправляю фотографии...",
                parseMode: ParseMode.Markdown);

            // Отправляем каждое фото отдельным сообщением с улучшенной подписью
            foreach (var plant in plantsWithPhotos)
            {
                var caption = $"🌿 **{plant.Name}**\n";

                // ДОБАВЛЯЕМ ВИД РАСТЕНИЯ
                if (!string.IsNullOrEmpty(plant.Species))
                    caption += $"🌱 *Вид:* {plant.Species}\n";

                caption += $"💧 *Полив:* {plant.WateringFrequency}";

                await botClient.SendPhotoAsync(
                    chatId,
                    new InputFileId(plant.PhotoFileId),
                    caption: caption,
                    parseMode: ParseMode.Markdown);
            }

            // Кнопка возврата
            var backKeyboard = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("🔙 К списку растений", "back_to_plants"));

            await botClient.SendTextMessageAsync(
                chatId,
                "✅ Вот все фото ваших растений!",
                replyMarkup: backKeyboard);
        }

        public static async Task SendWateringMenu(long chatId, ITelegramBotClient botClient)
        {
            var plants = GetUserPlants(chatId);

            if (!plants.Any())
            {
                await botClient.SendTextMessageAsync(
                    chatId,
                    "❌ У вас нет растений для отметки полива.\n" +
                    "Сначала добавьте растение через '➕ Добавить растение'");
                return;
            }

            var buttons = plants.Select((plant, index) =>
                new[] { InlineKeyboardButton.WithCallbackData(
                    $"💧 {plant.Name}",
                    $"water_{index}")
                }).ToList();

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("🔙 Назад", "main_menu") });

            await botClient.SendTextMessageAsync(
                chatId,
                "💧 **Выберите растение для отметки полива:**",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons));
        }

        public static async Task HandleWatering(long chatId, string plantId, ITelegramBotClient botClient)
        {
            if (int.TryParse(plantId, out int index))
            {
                var plants = GetUserPlants(chatId);
                if (index >= 0 && index < plants.Count)
                {
                    var plant = plants[index];
                    plant.LastWatered = DateTime.Now;

                    await botClient.SendTextMessageAsync(
                        chatId,
                        $"✅ **{plant.Name}** полито! 💧\n\n" +
                        $"Следующий полив: {plant.NextWatering:dd.MM.yyyy}",
                        parseMode: ParseMode.Markdown);

                    return;
                }
            }

            await botClient.SendTextMessageAsync(chatId, "❌ Растение не найдено");
        }

        public static async Task StartAddPlantFlow(long chatId, ITelegramBotClient botClient)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                "📸 **Добавление нового растения**\n\n" +
                "Пожалуйста, отправьте фото вашего растения:",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("❌ Отмена") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                });
        }

        public static async Task HandlePlantName(long chatId, string name, ITelegramBotClient botClient)
        {
            await botClient.SendTextMessageAsync(
                chatId,
                $"📝 Название сохранено: **{name}**\n\n" +
                "Теперь введите вид растения (например: Фикус Бенджамина, Орхидея Фаленопсис):",
                parseMode: ParseMode.Markdown,
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("❌ Отмена") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                });
        }

        public static async Task HandlePlantDescription(long chatId, string description, ITelegramBotClient botClient)
        {
            if (description.ToLower() == "пропустить")
                description = "";

            await botClient.SendTextMessageAsync(
                chatId,
                string.IsNullOrEmpty(description) ?
                    "📝 Описание пропущено.\n\n" :
                    $"📝 Описание сохранено.\n\n",
                parseMode: ParseMode.Markdown);

            await botClient.SendTextMessageAsync(
                chatId,
                "💧 **Укажите частоту полива:**\n\n" +
                "• Каждый день\n" +
                "• Раз в 2 дня\n" +
                "• Раз в неделю\n" +
                "• Раз в 2 недели\n" +
                "• Раз в месяц",
                replyMarkup: new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("Каждый день"), new KeyboardButton("Раз в 2 дня") },
                    new[] { new KeyboardButton("Раз в неделю"), new KeyboardButton("Раз в 2 недели") },
                    new[] { new KeyboardButton("Раз в месяц"), new KeyboardButton("❌ Отмена") }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                });
        }

        public static async Task<bool> HandleWateringFrequency(long chatId, string frequency, UserPlant tempPlant, ITelegramBotClient botClient)
        {
            try
            {
                // Устанавливаем частоту полива через свойство
                tempPlant.WateringFrequency = frequency;
                tempPlant.PlantType = "custom";
                tempPlant.LastWatered = DateTime.Now;

                // Сохраняем растение
                AddUserPlant(chatId, tempPlant);

                var message = $"✅ **Растение добавлено!** 🌱\n\n" +
                             $"**Название:** {tempPlant.Name}\n" +
                             $"**Вид:** {(string.IsNullOrEmpty(tempPlant.Species) ? "не указан" : tempPlant.Species)}\n" + // ОБНОВЛЯЕМ ОТОБРАЖЕНИЕ ВИДА
                             $"**Описание:** {(string.IsNullOrEmpty(tempPlant.Description) ? "не указано" : tempPlant.Description)}\n" +
                             $"**Полив:** {tempPlant.WateringFrequency}\n" +
                             $"**Следующий полив:** {tempPlant.NextWatering:dd.MM.yyyy}";

                // Если есть фото - отправляем фото с подписью
                if (!string.IsNullOrEmpty(tempPlant.PhotoFileId))
                {
                    await botClient.SendPhotoAsync(
                        chatId,
                        new InputFileId(tempPlant.PhotoFileId),
                        caption: message,
                        parseMode: ParseMode.Markdown);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId,
                        message,
                        parseMode: ParseMode.Markdown);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка добавления растения: {ex.Message}");
                await botClient.SendTextMessageAsync(chatId, "❌ Ошибка при добавлении растения");
                return false;
            }
        }
        public static void AddUserPlant(long chatId, UserPlant plant)
        {
            if (!_userPlants.ContainsKey(chatId))
                _userPlants[chatId] = new List<UserPlant>();

            // Генерируем уникальный ID если его нет
            if (string.IsNullOrEmpty(plant.PlantId))
                plant.PlantId = Guid.NewGuid().ToString();

            _userPlants[chatId].Add(plant);
        }

        // Дополнительные полезные методы
        public static void RemoveUserPlant(long chatId, string plantId)
        {
            if (_userPlants.ContainsKey(chatId))
            {
                _userPlants[chatId].RemoveAll(p => p.PlantId == plantId);
            }
        }

        public static List<UserPlant> GetPlantsNeedWatering()
        {
            var plantsNeedWatering = new List<UserPlant>();
            foreach (var userPlants in _userPlants.Values)
            {
                plantsNeedWatering.AddRange(userPlants.Where(p => p.NextWatering <= DateTime.Now));
            }
            return plantsNeedWatering;
        }

        public static UserPlant? GetPlantById(long chatId, string plantId)
        {
            return GetUserPlants(chatId).FirstOrDefault(p => p.PlantId == plantId);
        }

        // Метод для удаления растения
        public static async Task DeletePlant(long chatId, int plantIndex, ITelegramBotClient botClient)
        {
            var plants = GetUserPlants(chatId);

            if (plantIndex < 0 || plantIndex >= plants.Count)
            {
                await botClient.SendTextMessageAsync(chatId, "❌ Растение не найдено");
                return;
            }

            var plant = plants[plantIndex];
            _userPlants[chatId].RemoveAt(plantIndex);

            await botClient.SendTextMessageAsync(
                chatId,
                $"🗑 **{plant.Name}** удалено из вашей коллекции.",
                parseMode: ParseMode.Markdown);
        }
    }
}