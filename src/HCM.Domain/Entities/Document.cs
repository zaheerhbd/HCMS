namespace HCM.Domain.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Guid PatientId { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string BlobContainerName { get; set; } = null!;
    public string BlobName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Case Case { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
    public virtual User UploadedByUser { get; set; } = null!;
}
