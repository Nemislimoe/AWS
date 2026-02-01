using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

/*class Program
{
    static async Task Main()
    {
        string conn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        string containerName = "practical-container";
        string localFile = "./data/sample.txt";

        var service = new BlobServiceClient(conn);
        var container = service.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None);

        var blob = container.GetBlobClient(Path.GetFileName(localFile));
        using var fs = File.OpenRead(localFile);
        await blob.UploadAsync(fs, overwrite: true);

        Console.WriteLine("Blobs:");
        await foreach (var item in container.GetBlobsAsync()) Console.WriteLine(item.Name);

        var downloadPath = "./data/downloaded_" + Path.GetFileName(localFile);
        var dl = await blob.DownloadAsync();
        using var outFs = File.OpenWrite(downloadPath);
        await dl.Value.Content.CopyToAsync(outFs);

        Console.WriteLine("Done");
    }
}*/

class Program
{
    static async Task Main()
    {
        string? connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("ERROR: Set AZURE_STORAGE_CONNECTION_STRING environment variable and restart.");
            return;
        }

        string containerName = "photos";
        string prefix = "2025/09/holiday/";
        string imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "images");

        if (!Directory.Exists(imagesFolder))
        {
            Console.WriteLine($"ERROR: Create folder 'images' in project directory and put image files there: {imagesFolder}");
            return;
        }

        var serviceClient = new BlobServiceClient(connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        Console.WriteLine($"Container '{containerName}' ready (Public access: Blob).");

        var files = Directory.GetFiles(imagesFolder);
        if (files.Length == 0)
        {
            Console.WriteLine("ERROR: No files found in images folder.");
            return;
        }

        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);
            string blobName = (prefix + fileName).Replace("\\", "/");
            var blobClient = containerClient.GetBlobClient(blobName);

            using (var fs = File.OpenRead(file))
            {
                await blobClient.UploadAsync(fs, overwrite: true);
            }

            Console.WriteLine($"Uploaded: {blobName}");
        }

        Console.WriteLine();
        Console.WriteLine("Public URLs (direct access):");

        await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, default))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            Console.WriteLine(blobClient.Uri);
        }

        Console.WriteLine();
        Console.WriteLine("Done. Open the URLs above in your browser.");
    }
}
