using HCM.Application.Hl7.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

// HL7 v2.5.1 ingestion endpoints. Accepts pipe-delimited text/plain HL7 messages.
[ApiController]
[Route("api/hl7")]
[Authorize]
public class Hl7Controller(IHl7DataHandler handler) : ControllerBase
{
    private readonly IHl7DataHandler _handler = handler;

    /// <summary>POST /api/hl7/adt — ingest an ADT^A01 patient admission message; creates a new Patient record</summary>
    [HttpPost("adt")]
    [Consumes("text/plain")]
    public async Task<IActionResult> ProcessAdt(CancellationToken ct)
    {
        string hl7Message;
        using (var reader = new StreamReader(Request.Body))
            hl7Message = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(hl7Message))
            return BadRequest(new { error = "Request body must contain an HL7 v2 ADT^A01 message." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await _handler.ProcessAdtA01Async(hl7Message, userId, ct);

        if (!result.Success)
            return UnprocessableEntity(new { error = result.Message });

        return Ok(new { mrn = result.Mrn, message = result.Message });
    }

    /// <summary>POST /api/hl7/oru — ingest an ORU^R01 observation result; appends a read-only note to the patient's active case</summary>
    [HttpPost("oru")]
    [Consumes("text/plain")]
    public async Task<IActionResult> ProcessOru(CancellationToken ct)
    {
        string hl7Message;
        using (var reader = new StreamReader(Request.Body))
            hl7Message = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(hl7Message))
            return BadRequest(new { error = "Request body must contain an HL7 v2 ORU^R01 message." });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await _handler.ProcessOruR01Async(hl7Message, userId, ct);

        if (!result.Success)
            return UnprocessableEntity(new { error = result.Message });

        return Ok(new { noteId = result.NoteId, message = result.Message });
    }
}
