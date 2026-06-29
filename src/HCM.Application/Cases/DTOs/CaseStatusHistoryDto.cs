namespace HCM.Application.Cases.DTOs;

public class CaseStatusHistoryDto
{
    public Guid Id { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = null!;
    public string? Comment { get; set; }
    public Guid ChangedBy { get; set; }
    public string ChangedByUserName { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
}
