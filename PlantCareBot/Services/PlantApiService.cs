//using System.Text.Json;
//using TelegramPlantBot.Models;

//namespace TelegramPlantBot.Services
//{
//    public class PlantApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _apiKey;
//        private readonly string _baseUrl;

//        public PlantApiService(string apiKey, string baseUrl = "https://trefle.io/api/v1/")
//        {
//            _httpClient = new HttpClient();
//            _apiKey = apiKey;
//            _baseUrl = baseUrl;
//            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
//        }

//        public async Task<PlantApiResponse> SearchPlantsAsync(string query, int limit = 10)
//        {
//            try
//            {
//                var url = $"{_baseUrl}plants?q={Uri.EscapeDataString(query)}&limit={limit}";
//                var response = await _httpClient.GetAsync(url);

//                if (!response.IsSuccessStatusCode)
//                {
//                    return new PlantApiResponse
//                    {
//                        Success = false,
//                        Message = $"Ошибка API: {response.StatusCode}"
//                    };
//                }

//                var content = await response.Content.ReadAsStringAsync();
//                var apiData = JsonSerializer.Deserialize<TrefleApiResponse>(content);

//                if (apiData?.Data == null)
//                {
//                    return new PlantApiResponse
//                    {
//                        Success = false,
//                        Message = "Не удалось получить данные о растениях"
//                    };
//                }

//                var plants = apiData.Data.Select(p => new ApiPlant
//                {
//                    Id = p.Id,
//                    CommonName = p.CommonName ?? "Неизвестно",
//                    ScientificName = p.ScientificName ?? "Неизвестно",
//                    Family = p.Family ?? "Неизвестно",
//                    Genus = p.Genus ?? "Неизвестно",
//                    ImageUrl = p.ImageUrl ?? string.Empty
//                }).ToList();

//                return new PlantApiResponse
//                {
//                    Data = plants,
//                    Success = true,
//                    Message = $"Найдено {plants.Count} растений"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new PlantApiResponse
//                {
//                    Success = false,
//                    Message = $"Ошибка: {ex.Message}"
//                };
//            }
//        }

//        public async Task<PlantApiResponse> GetPlantDetailsAsync(int plantId)
//        {
//            try
//            {
//                var url = $"{_baseUrl}plants/{plantId}";
//                var response = await _httpClient.GetAsync(url);

//                if (!response.IsSuccessStatusCode)
//                {
//                    return new PlantApiResponse
//                    {
//                        Success = false,
//                        Message = $"Ошибка API: {response.StatusCode}"
//                    };
//                }

//                var content = await response.Content.ReadAsStringAsync();
//                var plantData = JsonSerializer.Deserialize<TreflePlantDetail>(content);

//                if (plantData?.Data == null)
//                {
//                    return new PlantApiResponse
//                    {
//                        Success = false,
//                        Message = "Не удалось получить данные о растении"
//                    };
//                }

//                var plant = new ApiPlant
//                {
//                    Id = plantData.Data.Id,
//                    CommonName = plantData.Data.CommonName ?? "Неизвестно",
//                    ScientificName = plantData.Data.ScientificName ?? "Неизвестно",
//                    Family = plantData.Data.Family ?? "Неизвестно",
//                    Genus = plantData.Data.Genus ?? "Неизвестно",
//                    Description = plantData.Data.Description ?? "Описание отсутствует",
//                    ImageUrl = plantData.Data.ImageUrl ?? string.Empty
//                };

//                return new PlantApiResponse
//                {
//                    Data = new List<ApiPlant> { plant },
//                    Success = true
//                };
//            }
//            catch (Exception ex)
//            {
//                return new PlantApiResponse
//                {
//                    Success = false,
//                    Message = $"Ошибка: {ex.Message}"
//                };
//            }
//        }

//        public async Task<PlantApiResponse> GetPlantsByFamilyAsync(string family, int limit = 20)
//        {
//            try
//            {
//                var url = $"{_baseUrl}plants?family={Uri.EscapeDataString(family)}&limit={limit}";
//                var response = await _httpClient.GetAsync(url);

//                if (!response.IsSuccessStatusCode)
//                {
//                    return new PlantApiResponse
//                    {
//                        Success = false,
//                        Message = $"Ошибка API: {response.StatusCode}"
//                    };
//                }

//                var content = await response.Content.ReadAsStringAsync();
//                var apiData = JsonSerializer.Deserialize<TrefleApiResponse>(content);

//                if (apiData?.Data == null)
//                {
//                    return new PlantApiResponse
//                    {
//                        Success = false,
//                        Message = "Не удалось получить данные о растениях"
//                    };
//                }

//                var plants = apiData.Data.Select(p => new ApiPlant
//                {
//                    Id = p.Id,
//                    CommonName = p.CommonName ?? "Неизвестно",
//                    ScientificName = p.ScientificName ?? "Неизвестно",
//                    Family = p.Family ?? "Неизвестно",
//                    Genus = p.Genus ?? "Неизвестно",
//                    ImageUrl = p.ImageUrl ?? string.Empty
//                }).ToList();

//                return new PlantApiResponse
//                {
//                    Data = plants,
//                    Success = true,
//                    Message = $"Найдено {plants.Count} растений в семействе {family}"
//                };
//            }
//            catch (Exception ex)
//            {
//                return new PlantApiResponse
//                {
//                    Success = false,
//                    Message = $"Ошибка: {ex.Message}"
//                };
//            }
//        }
//    }

//    // Модели для Trefle API
//    public class TrefleApiResponse
//    {
//        public List<TreflePlant> Data { get; set; } = new();
//    }

//    public class TreflePlantDetail
//    {
//        public TreflePlant Data { get; set; } = new();
//    }

//    public class TreflePlant
//    {
//        public int Id { get; set; }
//        public string? CommonName { get; set; }
//        public string? ScientificName { get; set; }
//        public string? Family { get; set; }
//        public string? Genus { get; set; }
//        public string? Description { get; set; }
//        public string? ImageUrl { get; set; }
//    }
//}