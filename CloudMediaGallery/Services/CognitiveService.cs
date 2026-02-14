using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;

namespace CloudMediaGallery.Services
{
    /// <summary>
    /// Налаштування Azure Cognitive Services з appsettings
    /// </summary>
    public class CognitiveServicesOptions
    {
        public ContentSafetySettings ContentSafety { get; set; } = new();
        public ComputerVisionSettings ComputerVision { get; set; } = new();

        public class ContentSafetySettings
        {
            public string Endpoint { get; set; } = "";
            public string SubscriptionKey { get; set; } = "";
        }

        public class ComputerVisionSettings
        {
            public string Endpoint { get; set; } = "";
            public string SubscriptionKey { get; set; } = "";
        }
    }

    /// <summary>
    /// Реалізація сервісу Azure Cognitive Services
    /// </summary>
    public class CognitiveService : ICognitiveService
    {
        private readonly CognitiveServicesOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<CognitiveService> _logger;

        public CognitiveService(
            IOptions<CognitiveServicesOptions> options,
            HttpClient httpClient,
            ILogger<CognitiveService> logger)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ContentModerationResult> ModerateImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
        {
            var result = new ContentModerationResult { IsApproved = true };

            // Якщо сервіс не налаштований, пропускаємо модерацію
            if (string.IsNullOrWhiteSpace(_options.ContentSafety.Endpoint) ||
                string.IsNullOrWhiteSpace(_options.ContentSafety.SubscriptionKey))
            {
                _logger.LogWarning("Azure Content Safety не налаштований. Модерація пропущена.");
                return result;
            }

            try
            {
                // Azure Content Safety API endpoint (NEW API)
                var url = $"{_options.ContentSafety.Endpoint.TrimEnd('/')}/contentsafety/image:analyze?api-version=2023-10-01";

                // Конвертуємо зображення в base64
                imageStream.Position = 0;
                using var ms = new MemoryStream();
                await imageStream.CopyToAsync(ms, cancellationToken);
                var base64Image = Convert.ToBase64String(ms.ToArray());

                var requestBody = new
                {
                    image = new { content = base64Image },
                    categories = new[] { "Hate", "SelfHarm", "Sexual", "Violence" }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ContentSafety.SubscriptionKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    var safetyResponse = JsonSerializer.Deserialize<ContentSafetyResponse>(responseJson, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (safetyResponse?.CategoriesAnalysis != null)
                    {
                        // Перевіряємо рівні серйозності (0-7, де 0-2 безпечно, 3+ потребує перевірки)
                        var maxSeverity = safetyResponse.CategoriesAnalysis.Max(c => c.Severity);
                        
                        result.IsApproved = maxSeverity <= 2; // 0-2 = safe
                        result.ReviewRecommendation = result.IsApproved ? "Approved" : "Review Required";

                        // Зберігаємо деталі для логування
                        var violentContent = safetyResponse.CategoriesAnalysis.FirstOrDefault(c => c.Category == "Violence");
                        var sexualContent = safetyResponse.CategoriesAnalysis.FirstOrDefault(c => c.Category == "Sexual");
                        var hateContent = safetyResponse.CategoriesAnalysis.FirstOrDefault(c => c.Category == "Hate");

                        if (violentContent != null)
                        {
                            result.IsGoryContent = violentContent.Severity > 2;
                            result.GoreScore = violentContent.Severity / 7.0;
                        }

                        if (sexualContent != null)
                        {
                            result.IsAdultContent = sexualContent.Severity > 2;
                            result.AdultScore = sexualContent.Severity / 7.0;
                        }

                        _logger.LogInformation($"Content Safety Analysis: Max Severity = {maxSeverity}, Approved = {result.IsApproved}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning($"Content Safety API returned {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час модерації контенту");
                // У разі помилки дозволяємо контент, але логуємо помилку
                result.IsApproved = true;
            }

            return result;
        }

        public async Task<(List<string> Tags, string Description)> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
        {
            var tags = new List<string>();
            var description = "";

            // Якщо Computer Vision не налаштований, повертаємо порожні дані
            if (string.IsNullOrWhiteSpace(_options.ComputerVision.Endpoint) ||
                string.IsNullOrWhiteSpace(_options.ComputerVision.SubscriptionKey))
            {
                _logger.LogWarning("Computer Vision не налаштований. Аналіз зображення пропущений.");
                return (tags, description);
            }

            try
            {
                // Azure Computer Vision API endpoint (use v3.2/v4 compatible query)
                // The REST API expects `visualFeatures=Tags,Description` (not `features=tags,caption`).
                var url = $"{_options.ComputerVision.Endpoint.TrimEnd('/')}/vision/v3.2/analyze?visualFeatures=Tags,Description";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ComputerVision.SubscriptionKey);

                imageStream.Position = 0;
                var streamContent = new StreamContent(imageStream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                request.Content = streamContent;

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var analysisResponse = JsonSerializer.Deserialize<VisionAnalysisResponse>(json, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });

                    if (analysisResponse != null)
                    {
                        // Отримуємо теги
                        tags = analysisResponse.Tags?
                            .Where(t => t.Confidence > 0.7)
                            .Select(t => t.Name)
                            .ToList() ?? new List<string>();

                        // Отримуємо опис (перший caption якщо є)
                        description = analysisResponse.Description?.Captions?.FirstOrDefault()?.Text ?? "";

                        _logger.LogInformation($"Computer Vision: Found {tags.Count} tags, Description: {description}");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning($"Computer Vision API returned {response.StatusCode}: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час аналізу зображення");
            }

            return (tags, description);
        }

        // DTO класи для десеріалізації відповідей Azure API
        
        // Content Safety Response (NEW)
        private class ContentSafetyResponse
        {
            public List<CategoryAnalysis>? CategoriesAnalysis { get; set; }
        }

        private class CategoryAnalysis
        {
            public string Category { get; set; } = "";
            public int Severity { get; set; } // 0-7
        }

        // Computer Vision Response (v3.2) - tags[] and description.captions[]
        private class VisionAnalysisResponse
        {
            public List<TagItem>? Tags { get; set; }
            public DescriptionResult? Description { get; set; }
        }

        private class TagItem
        {
            public string Name { get; set; } = "";
            public double Confidence { get; set; }
        }

        private class DescriptionResult
        {
            public List<CaptionResult>? Captions { get; set; }
            public List<string>? Tags { get; set; }
        }

        private class CaptionResult
        {
            public string Text { get; set; } = "";
            public double Confidence { get; set; }
        }
    }
}
