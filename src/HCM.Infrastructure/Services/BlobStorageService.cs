using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using HCM.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HCM.Infrastructure.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration.GetConnectionString("BlobStorage")
            ?? configuration["BlobStorage:ConnectionString"]
            ?? "UseDevelopmentStorage=true";

        _client = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(
        string containerName, string blobName, Stream content, string contentType,
        CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blob = container.GetBlobClient(blobName);
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blob.UploadAsync(content, uploadOptions, ct);
        _logger.LogInformation("Uploaded blob {BlobName} to container {Container}", blobName, containerName);

        return blobName;
    }

    public async Task<string> GenerateSasUrlAsync(
        string containerName, string blobName, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        // Check if blob exists before generating SAS
        var exists = await blob.ExistsAsync(ct);
        if (!exists.Value)
            throw new FileNotFoundException($"Blob {blobName} not found.");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // FR-25: max 1 hour SAS
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        // UserDelegation SAS requires Managed Identity; fall back to account key SAS for local dev
        var uri = blob.GenerateSasUri(sasBuilder);

        // SAS URLs must never be logged — only log the blob name
        _logger.LogInformation("Generated SAS URL for blob {BlobName}", blobName);

        return uri.ToString();
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var container = _client.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
        _logger.LogInformation("Deleted blob {BlobName} from container {Container}", blobName, containerName);
    }
}
