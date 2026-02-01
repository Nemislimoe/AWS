using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using System.Web;

namespace CloudMediaGallery.Services
{
    // Параметри з appsettings.json
    public class StorageOptions
    {
        public bool UseLocal { get; set; } = true;
        public string ContainerName { get; set; } = "media";
        public string? ConnectionString { get; set; }
    }

    public class BlobService : IBlobService
    {
        private readonly StorageOptions _options;
        private readonly BlobContainerClient? _containerClient;
        private readonly string _uploadsFolder;

        // Додаємо IOptions<StorageOptions> для конфігурації
        public BlobService(IOptions<StorageOptions> options)
        {
            _options = options.Value;
            _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(_uploadsFolder);

            if (!_options.UseLocal)
            {
                if (string.IsNullOrWhiteSpace(_options.ConnectionString))
                    throw new ArgumentException("Storage connection string is not configured.");

                var blobServiceClient = new BlobServiceClient(_options.ConnectionString);
                _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
                _containerClient.CreateIfNotExists(PublicAccessType.Blob);
            }
        }

        // Завантаження файлу: повертає URL (абсолютний для Azure або відносний для локального)
        public async Task<string> UploadAsync(Stream stream, string originalFileName, string contentType, CancellationToken cancellationToken = default)
        {
            // Генеруємо унікальне ім'я: GUID + оригінальне ім'я
            var safeFileName = Path.GetFileName(originalFileName);
            var blobName = $"{Guid.NewGuid():N}_{safeFileName}";

            if (_options.UseLocal)
            {
                // Локальний режим: зберігаємо у wwwroot/uploads
                var localPath = Path.Combine(_uploadsFolder, blobName);
                stream.Position = 0;
                using (var fs = File.Create(localPath))
                {
                    await stream.CopyToAsync(fs, cancellationToken);
                }
                // Повертаємо відносний URL для використання у <img src="">
                return $"/uploads/{HttpUtility.UrlPathEncode(blobName)}";
            }
            else
            {
                // Azure Blob
                if (_containerClient == null) throw new InvalidOperationException("Blob container client is not initialized.");

                var blobClient = _containerClient.GetBlobClient(blobName);
                stream.Position = 0;
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
                return blobClient.Uri.ToString();
            }
        }

        // Видалення файлу за URL
        public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl)) return;

            if (_options.UseLocal)
            {
                // Очікуємо відносний шлях починається з /uploads/
                var trimmed = blobUrl.TrimStart('/');
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trimmed.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            else
            {
                if (_containerClient == null) throw new InvalidOperationException("Blob container client is not initialized.");

                // Отримуємо ім'я блоба з URL
                var uri = new Uri(blobUrl);
                // Шлях після контейнера: /containerName/blobName
                var segments = uri.Segments.Select(s => s.Trim('/')).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                if (segments.Length >= 2)
                {
                    // Перший сегмент — containerName, решта — шлях до блоба
                    var blobName = string.Join('/', segments.Skip(1));
                    var blobClient = _containerClient.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    // Альтернативний варіант: якщо URL містить SAS або інші параметри, спробуємо витягти ім'я як останній сегмент
                    var blobName = uri.Segments.Last().Trim('/');
                    var blobClient = _containerClient.GetBlobClient(blobName);
                    await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
                }
            }
        }
    }
}
