
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Authentication_API.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;


        public BlobStorageService(IConfiguration configuration)
        {
            // Read the Azurite connection string.
            string connectionString =
                configuration["AzureStorage:ConnectionString"]!;

            // Use an Azure Storage API version supported by our local Azurite.
            var options = new BlobClientOptions(
                BlobClientOptions.ServiceVersion.V2023_11_03);

            // Create the Blob Storage client.
            _blobServiceClient = new BlobServiceClient(
                connectionString, options);
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
    new BlobUploadOptions
    {
        HttpHeaders = new BlobHttpHeaders
        {
            ContentType = image.ContentType
        }
    });

            // Return the image URL for saving in SQL Server.
            return blob.Uri.ToString();
        }



        public async Task DeleteImageAsync(
            string imageUrl,
            string containerName)
        {
            // Get the container containing the image.
            var container = _blobServiceClient
                .GetBlobContainerClient(containerName);

            // Check that the URL belongs to this container.
            string containerUrl =
                container.Uri.AbsoluteUri.TrimEnd('/') + "/";

            var imageUri = new Uri(imageUrl);

            if (!imageUri.AbsoluteUri.StartsWith(
                containerUrl,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Image does not belong to this container.");
            }

            // Extract the image filename from the URL.
            string fileName = Uri.UnescapeDataString(
                imageUri.Segments[^1]);

            // Get the existing blob.
            var blob = container.GetBlobClient(fileName);

            // Delete the blob if it exists.
            await blob.DeleteIfExistsAsync();
        }

    }
}
