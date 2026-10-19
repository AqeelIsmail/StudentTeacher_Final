
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Authentication_API.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;

        public BlobStorageService(IConfiguration configuration)
        {
            // Read the Azurite connection string from configuration.
            string connectionString =
                configuration["AzureStorage:ConnectionString"]!;

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadImageAsync(
            IFormFile image,
            string containerName)
        {
            // Get the container for course or profile images.
            var container = _blobServiceClient
                .GetBlobContainerClient(containerName);

            // Create the container if it does not exist.
            await container.CreateIfNotExistsAsync(
                PublicAccessType.Blob);

            // Generate a unique filename to avoid overwriting images.
            string extension = Path.GetExtension(image.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";

            // Get the blob that will store the image.
            var blob = container.GetBlobClient(fileName);

            // Open and upload the image stream.
            using var stream = image.OpenReadStream();

            await blob.UploadAsync(
                stream,
                new BlobHttpHeaders
                {
                    ContentType = image.ContentType
                });

            // Return the image URL for saving in SQL Server.
            return blob.Uri.ToString();
        }
    }
}
