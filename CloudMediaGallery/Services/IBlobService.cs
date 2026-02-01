namespace CloudMediaGallery.Services
{
    // Інтерфейс для сервісу роботи з Blob (щоб можна було мокати у тестах)
    public interface IBlobService
    {
        Task<string> UploadAsync(Stream stream, string originalFileName, string contentType, CancellationToken cancellationToken = default);
        Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
    }
}
