namespace CloudMediaGallery.Services
{
    /// <summary>
    /// Результат модерації контенту
    /// </summary>
    public class ContentModerationResult
    {
        public bool IsApproved { get; set; }
        public bool IsAdultContent { get; set; }
        public bool IsRacyContent { get; set; }
        public bool IsGoryContent { get; set; }
        public double AdultScore { get; set; }
        public double RacyScore { get; set; }
        public double GoreScore { get; set; }
        public string? ReviewRecommendation { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? Description { get; set; }
    }

    /// <summary>
    /// Інтерфейс для роботи з Azure Cognitive Services
    /// </summary>
    public interface ICognitiveService
    {
        /// <summary>
        /// Перевірка зображення на неприпустимий контент
        /// </summary>
        Task<ContentModerationResult> ModerateImageAsync(Stream imageStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Аналіз зображення та отримання тегів/опису
        /// </summary>
        Task<(List<string> Tags, string Description)> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
    }
}
