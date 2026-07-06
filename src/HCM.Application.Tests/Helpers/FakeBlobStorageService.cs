using HCM.Application.Interfaces;

namespace HCM.Application.Tests.Helpers;

// In-memory stand-in for Azure Blob Storage. Records calls so tests can assert
// on SAS URL expiry and that URLs are shaped as expected without touching Azure.
public class FakeBlobStorageService : IBlobStorageService
{
    public List<(string Container, string BlobName)> UploadedBlobs { get; } = [];
    public List<(string Container, string BlobName)> DeletedBlobs { get; } = [];

    public Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken ct = default)
    {
        UploadedBlobs.Add((containerName, fileName));
        return Task.FromResult(fileName);
    }

    public Task<string> GenerateSasUrlAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        // se= (signed expiry) encodes a 1-hour expiry, matching the production SAS policy
        var expiry = DateTime.UtcNow.AddHours(1).ToString("O");
        return Task.FromResult($"https://blob.test/{containerName}/{blobName}?sv=2024&se={expiry}&sig=fake");
    }

    public Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        DeletedBlobs.Add((containerName, blobName));
        return Task.CompletedTask;
    }
}
