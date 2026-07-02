namespace HCM.Application.Documents.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid PatientId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string Category { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public Guid UploadedBy { get; set; }
    public string? UploadedByUserName { get; set; }
    public DateTime UploadedAt { get; set; }
}
