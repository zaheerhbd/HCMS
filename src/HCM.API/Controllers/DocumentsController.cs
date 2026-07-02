using System.Security.Claims;
using HCM.Application.Documents.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/cases/{caseNumber}/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentDataHandler _handler;

    public DocumentsController(IDocumentDataHandler handler)
    {
        _handler = handler;
    }

    // Lists all active documents for a case
    [HttpGet]
    public async Task<IActionResult> GetDocuments(string caseNumber, CancellationToken ct)
    {
        try
        {
            var docs = await _handler.GetDocumentsByCaseNumberAsync(caseNumber, ct);
            return Ok(docs);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // Uploads a document to blob storage and records metadata in SQL (CareCoordinator, Supervisor, Admin)
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB — matches FR-23
    public async Task<IActionResult> UploadDocument(
        string caseNumber,
        IFormFile file,
        [FromForm] string category,
        [FromForm] string? description,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            using var stream = file.OpenReadStream();
            var doc = await _handler.UploadDocumentAsync(
                caseNumber, file.FileName, file.ContentType, file.Length,
                category, description, stream, userId, ct);

            return CreatedAtAction(nameof(GetDownloadUrl), new { caseNumber, documentId = doc.Id }, doc);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FluentValidation.ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Returns a 1-hour SAS URL for secure document download — URL itself is never logged (FR-25)
    [HttpGet("{documentId:guid}/download-url")]
    public async Task<IActionResult> GetDownloadUrl(string caseNumber, Guid documentId, CancellationToken ct)
    {
        try
        {
            var sasUrl = await _handler.GetDownloadUrlAsync(documentId, ct);
            return Ok(new { url = sasUrl, expiresInMinutes = 60 });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (FileNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    // Soft-deletes a document (marks IsDeleted; physical blob removal is a background job)
    [HttpDelete("{documentId:guid}")]
    [Authorize(Roles = "Admin,Supervisor,CareCoordinator")]
    public async Task<IActionResult> DeleteDocument(string caseNumber, Guid documentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            await _handler.DeleteDocumentAsync(documentId, userId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
