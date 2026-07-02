using HCM.Application.Documents.DTOs;

namespace HCM.Application.Documents.DataHandlers;

public interface IDocumentDataHandler
{
    Task<List<DocumentDto>> GetDocumentsByCaseNumberAsync(string caseNumber, CancellationToken ct = default);
    Task<List<DocumentDto>> GetDocumentsByPatientMrnAsync(string patientMrn, CancellationToken ct = default);
    Task<DocumentDto> UploadDocumentAsync(string caseNumber, string fileName, string contentType, long fileSizeBytes, string category, string? description, Stream fileStream, Guid uploadedBy, CancellationToken ct = default);
    Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid documentId, Guid deletedBy, CancellationToken ct = default);
}
