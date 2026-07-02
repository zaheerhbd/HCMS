using FluentValidation;
using HCM.Application.Documents.DTOs;
using HCM.Application.Interfaces;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Documents.DataHandlers;

public class DocumentDataHandler : IDocumentDataHandler
{
    private const string ContainerName = "case-documents";
    private static readonly long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/png",
        "image/jpeg"
    ];

    private readonly IApplicationDbContext _db;
    private readonly IBlobStorageService _blob;

    public DocumentDataHandler(IApplicationDbContext db, IBlobStorageService blob)
    {
        _db = db;
        _blob = blob;
    }

    public async Task<List<DocumentDto>> GetDocumentsByCaseNumberAsync(string caseNumber, CancellationToken ct = default)
    {
        var caseEntity = await GetCaseByNumberAsync(caseNumber, ct);

        var docs = await _db.Documents
            .AsNoTracking()
            .Include(d => d.UploadedByUser)
            .Where(d => d.CaseId == caseEntity.Id && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        return docs.Select(MapToDto).ToList();
    }

    public async Task<List<DocumentDto>> GetDocumentsByPatientMrnAsync(string patientMrn, CancellationToken ct = default)
    {
        var patient = await _db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == patientMrn && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Patient {patientMrn} not found.");

        var docs = await _db.Documents
            .AsNoTracking()
            .Include(d => d.UploadedByUser)
            .Where(d => d.PatientId == patient.Id && !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        return docs.Select(MapToDto).ToList();
    }

    public async Task<DocumentDto> UploadDocumentAsync(
        string caseNumber, string fileName, string contentType, long fileSizeBytes,
        string category, string? description, Stream fileStream, Guid uploadedBy,
        CancellationToken ct = default)
    {
        if (fileSizeBytes > MaxFileSizeBytes)
            throw new ValidationException("File exceeds 20 MB limit.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new ValidationException("Unsupported file type. Allowed: PDF, DOCX, PNG, JPG.");

        var caseEntity = await _db.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");

        // Unique blob name prevents collisions when the same filename is uploaded twice
        var blobName = $"{caseNumber}/{Guid.NewGuid():N}_{fileName}";
        await _blob.UploadAsync(ContainerName, blobName, fileStream, contentType, ct);

        var doc = new Document
        {
            CaseId = caseEntity.Id,
            PatientId = caseEntity.PatientId,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            BlobContainerName = ContainerName,
            BlobName = blobName,
            Category = category,
            Description = description?.Trim(),
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow
        };

        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct);

        return await GetDocumentByIdAsync(doc.Id, ct);
    }

    public async Task<string> GetDownloadUrlAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        // 1-hour SAS URL — never logged per security rules
        return await _blob.GenerateSasUrlAsync(doc.BlobContainerName, doc.BlobName, ct);
    }

    public async Task DeleteDocumentAsync(Guid documentId, Guid deletedBy, CancellationToken ct = default)
    {
        var doc = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, ct)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        doc.IsDeleted = true;
        doc.DeletedAt = DateTime.UtcNow;
        doc.DeletedBy = deletedBy;

        _db.Documents.Update(doc);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<DocumentDto> GetDocumentByIdAsync(Guid id, CancellationToken ct)
    {
        var doc = await _db.Documents
            .AsNoTracking()
            .Include(d => d.UploadedByUser)
            .FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException($"Document {id} not found.");

        return MapToDto(doc);
    }

    private async Task<Case> GetCaseByNumberAsync(string caseNumber, CancellationToken ct)
    {
        return await _db.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber && c.IsActive, ct)
            ?? throw new KeyNotFoundException($"Case {caseNumber} not found.");
    }

    private static DocumentDto MapToDto(Document d) => new()
    {
        Id = d.Id,
        CaseId = d.CaseId,
        PatientId = d.PatientId,
        FileName = d.FileName,
        ContentType = d.ContentType,
        FileSizeBytes = d.FileSizeBytes,
        Category = d.Category,
        Description = d.Description,
        IsDeleted = d.IsDeleted,
        UploadedBy = d.UploadedBy,
        UploadedByUserName = d.UploadedByUser != null ? $"{d.UploadedByUser.FirstName} {d.UploadedByUser.LastName}" : null,
        UploadedAt = d.UploadedAt
    };
}
