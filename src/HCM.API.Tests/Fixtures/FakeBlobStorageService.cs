using HCM.Application.Interfaces;

namespace HCM.API.Tests.Fixtures;

// Stands in for Azure Blob Storage in integration tests — no Azurite/Azure dependency required.
public class FakeBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken ct = default)
        => Task.FromResult(fileName);

    public Task<string> GenerateSasUrlAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var expiry = DateTime.UtcNow.AddHours(1).ToString("O");
        return Task.FromResult($"https://blob.test/{containerName}/{blobName}?sv=2024-01-01&se={expiry}&sig=fake");
    }

    public Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default)
        => Task.CompletedTask;
}
