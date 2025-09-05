using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageResizer
{
    public class ResizeImage
    {
        private readonly ILogger _logger;
        // The connection string is read from the Function App's configuration settings.
        private readonly string _connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        private const int ThumbnailWidth = 128;

        public ResizeImage(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ResizeImage>();
        }

        [Function("ResizeImage")]
        public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
        {
            _logger.LogInformation("C# EventGrid trigger function processed an event.");
            _logger.LogInformation("Event Type: {type}, Subject: {subject}", eventGridEvent.EventType, eventGridEvent.Subject);

            try
            {
                // 1. Parse event data to get the blob URL from the event's JSON payload.
                var eventData = JObject.Parse(eventGridEvent.Data.ToString());
                string blobUrl = eventData["url"]!.ToString();
                var blobUri = new Uri(blobUrl);
                string blobName = Path.GetFileName(blobUri.LocalPath);

                _logger.LogInformation("New blob detected: {name}", blobName);

                // 2. Connect to Azure Storage using the connection string.
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var sourceContainerClient = blobServiceClient.GetBlobContainerClient("uploads");
                var sourceBlobClient = sourceContainerClient.GetBlobClient(blobName);

                // 3. Download the newly uploaded image into a memory stream.
                using var inputStream = new MemoryStream();
                await sourceBlobClient.DownloadToAsync(inputStream);
                inputStream.Position = 0;

                // 4. Resize the image using the ImageSharp library.
                using var image = await Image.LoadAsync(inputStream);
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailWidth, 0), // Resize based on width, preserving aspect ratio
                    Mode = ResizeMode.Max
                }));

                // 5. Save the resized thumbnail to an output stream.
                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream); // Save as JPEG, or choose another format
                outputStream.Position = 0;

                // 6. Upload the thumbnail to the 'thumbnails' container.
                _logger.LogInformation("Uploading resized thumbnail '{name}' to 'thumbnails' container.", blobName);
                var destContainerClient = blobServiceClient.GetBlobContainerClient("thumbnails");
                var destBlobClient = destContainerClient.GetBlobClient(blobName);
                await destBlobClient.UploadAsync(outputStream, overwrite: true);

                _logger.LogInformation("Successfully resized and uploaded {name}.", blobName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during image resizing: {errorMessage}", ex.Message);
            }
        }
    }
}