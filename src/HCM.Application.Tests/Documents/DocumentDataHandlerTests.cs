using FluentAssertions;
using FluentValidation;
using HCM.Application.Documents.DataHandlers;
using HCM.Application.Tests.Helpers;
using HCM.Domain.Entities;
using Xunit;

namespace HCM.Application.Tests.Documents;

public class DocumentDataHandlerTests
{
    private static async Task<(HCM.Infrastructure.Persistence.ApplicationDbContext Db, DocumentDataHandler Sut, FakeBlobStorageService Blob, Case Case, Guid UploaderId)> Seed()
    {
        var db = TestDbContextFactory.Create();
        var blob = new FakeBlobStorageService();
        var sut = new DocumentDataHandler(db, blob);

        var uploader = new User { Id = Guid.NewGuid(), Username = "uploader", Email = "uploader@hcms.local", PasswordHash = "x", FirstName = "Up", LastName = "Loader", CreatedAt = DateTime.UtcNow };
        var patient = new Patient { Id = Guid.NewGuid(), MRN = "MRN-2026-00001", FirstName = "A", LastName = "B", DateOfBirth = new DateTime(1990, 1, 1), CreatedBy = Guid.NewGuid() };
        var caseType = new CaseType { Id = 1, Name = "Chronic Disease", IsActive = true };
        var caseEntity = new Case { Id = Guid.NewGuid(), CaseNumber = "CASE-2026-00001", PatientId = patient.Id, CaseTypeId = 1, CurrentStatus = CaseStatus.Open, CreatedBy = Guid.NewGuid() };
        db.Users.Add(uploader);
        db.Patients.Add(patient);
        db.CaseTypes.Add(caseType);
        db.Cases.Add(caseEntity);
        await db.SaveChangesAsync();

        return (db, sut, blob, caseEntity, uploader.Id);
    }

    [Fact]
    public async Task UploadDocumentAsync_WithAllowedType_UploadsToBlobAndPersistsMetadata()
    {
        var (db, sut, blob, caseEntity, uploaderId) = await Seed();
        await using var _ = db;
        using var stream = new MemoryStream([1, 2, 3]);

        var result = await sut.UploadDocumentAsync(
            caseEntity.CaseNumber, "referral.pdf", "application/pdf", 1024,
            "Referral", "Initial referral letter", stream, uploaderId);

        result.FileName.Should().Be("referral.pdf");
        blob.UploadedBlobs.Should().ContainSingle(b => b.Container == "case-documents");
    }

    [Fact]
    public async Task UploadDocumentAsync_ExceedingSizeLimit_ThrowsValidationException()
    {
        var (db, sut, _, caseEntity, uploaderId) = await Seed();
        await using var _ = db;
        using var stream = new MemoryStream();

        var act = async () => await sut.UploadDocumentAsync(
            caseEntity.CaseNumber, "big.pdf", "application/pdf", 21 * 1024 * 1024,
            "Referral", null, stream, uploaderId);

        await act.Should().ThrowAsync<ValidationException>().WithMessage("*20 MB*");
    }

    [Fact]
    public async Task UploadDocumentAsync_WithDisallowedContentType_ThrowsValidationException()
    {
        var (db, sut, _, caseEntity, uploaderId) = await Seed();
        await using var _ = db;
        using var stream = new MemoryStream();

        var act = async () => await sut.UploadDocumentAsync(
            caseEntity.CaseNumber, "malware.exe", "application/x-msdownload", 1024,
            "Referral", null, stream, uploaderId);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetDownloadUrlAsync_ReturnsSasUrlThatExpiresWithinOneHour()
    {
        var (db, sut, _, caseEntity, uploaderId) = await Seed();
        await using var _ = db;
        using var stream = new MemoryStream([1]);
        var doc = await sut.UploadDocumentAsync(caseEntity.CaseNumber, "note.pdf", "application/pdf", 100, "Note", null, stream, uploaderId);

        var url = await sut.GetDownloadUrlAsync(doc.Id);

        url.Should().Contain("?sv=");
        var query = new Uri(url).Query;
        var seValue = query
            .TrimStart('?')
            .Split('&')
            .Select(p => p.Split('=', 2))
            .First(p => p[0] == "se")[1];
        DateTime.Parse(seValue).Should().BeOnOrBefore(DateTime.UtcNow.AddHours(1).AddMinutes(1));
    }

    [Fact]
    public async Task DeleteDocumentAsync_SetsIsDeletedFlag_SoftDelete()
    {
        var (db, sut, _, caseEntity, uploaderId) = await Seed();
        await using var _ = db;
        using var stream = new MemoryStream([1]);
        var doc = await sut.UploadDocumentAsync(caseEntity.CaseNumber, "note.pdf", "application/pdf", 100, "Note", null, stream, uploaderId);

        await sut.DeleteDocumentAsync(doc.Id, Guid.NewGuid());

        var stored = await db.Documents.FindAsync(doc.Id);
        stored!.IsDeleted.Should().BeTrue();
        stored.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadDocumentAsync_WithUnknownCaseNumber_ThrowsKeyNotFoundException()
    {
        var db = TestDbContextFactory.Create();
        await using var _ = db;
        var sut = new DocumentDataHandler(db, new FakeBlobStorageService());
        using var stream = new MemoryStream([1]);

        var act = async () => await sut.UploadDocumentAsync("CASE-2026-99999", "note.pdf", "application/pdf", 100, "Note", null, stream, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
