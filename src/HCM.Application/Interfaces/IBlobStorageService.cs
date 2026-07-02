namespace HCM.Application.Interfaces;

// Abstraction over Azure Blob Storage so Application layer stays infrastructure-free
public interface IBlobStorageService
{
    // Uploads a stream to blob storage and returns the blob name (path)
    Task<string> UploadAsync(string containerName, string fileName, Stream content, string contentType, CancellationToken ct = default);

    // Generates a time-limited (1 hour) SAS URL for secure download — never logged
    Task<string> GenerateSasUrlAsync(string containerName, string blobName, CancellationToken ct = default);

    // Marks a blob for deletion (soft delete via metadata flag; physical removal is a background job)
    Task DeleteAsync(string containerName, string blobName, CancellationToken ct = default);
}
