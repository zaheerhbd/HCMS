using HCM.Application.Fhir.DataHandlers;
using HCM.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

// FHIR R4 facade controller. All routes live under /fhir/R4/.
// Responses use application/fhir+json; errors return OperationOutcome (via ExceptionHandlerMiddleware).
[ApiController]
[Route("fhir/R4")]
[Authorize]
[Produces("application/fhir+json")]
public class FhirController(IFhirDataHandler handler, IFhirMappingService mapping) : ControllerBase
{
    private readonly IFhirDataHandler _handler = handler;
    private readonly IFhirMappingService _mapping = mapping;

    private string BaseUrl =>
        $"{Request.Scheme}://{Request.Host}";

    // ── Metadata ─────────────────────────────────────────────────────────────────

    /// <summary>GET /fhir/R4/metadata — CapabilityStatement (unauthenticated per FHIR spec)</summary>
    [HttpGet("metadata")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMetadata()
    {
        var json = await _handler.GetCapabilityStatementAsync(BaseUrl);
        return Content(json, "application/fhir+json");
    }

    // ── Patient ──────────────────────────────────────────────────────────────────

    /// <summary>GET /fhir/R4/Patient — search patients by _id, name, birthdate, identifier</summary>
    [HttpGet("Patient")]
    public async Task<IActionResult> SearchPatients(
        [FromQuery(Name = "_id")] string? id,
        [FromQuery] string? name,
        [FromQuery] string? birthdate,
        [FromQuery] string? identifier,
        CancellationToken ct)
    {
        var json = await _handler.SearchPatientsAsync(id, name, birthdate, identifier, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    /// <summary>GET /fhir/R4/Patient/{mrn} — read single Patient by MRN</summary>
    [HttpGet("Patient/{mrn}")]
    public async Task<IActionResult> GetPatient(string mrn, CancellationToken ct)
    {
        var json = await _handler.GetPatientAsync(mrn, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    /// <summary>GET /fhir/R4/Patient/{mrn}/$everything — full Bundle for a patient</summary>
    [HttpGet("Patient/{mrn}/$everything")]
    public async Task<IActionResult> GetPatientEverything(string mrn, CancellationToken ct)
    {
        var json = await _handler.GetEverythingAsync(mrn, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    /// <summary>POST /fhir/R4/ — import a FHIR Transaction Bundle atomically (Patient + Encounter + Appointments + Tasks)</summary>
    [HttpPost("")]
    public async Task<IActionResult> ImportTransactionBundle(CancellationToken ct)
    {
        string bundleJson;
        using (var reader = new StreamReader(Request.Body))
            bundleJson = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(bundleJson))
            return BadRequest(FhirOperationOutcome("invalid", "Request body must contain a FHIR Bundle JSON resource."));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await _handler.ImportTransactionBundleAsync(bundleJson, userId, ct);

        if (!result.Success)
        {
            return UnprocessableEntity(new
            {
                resourceType = "OperationOutcome",
                issue = result.Errors.Select(e => new { severity = "error", code = "processing", diagnostics = e }).ToArray()
            });
        }

        // Return a FHIR transaction-response Bundle
        var responseJson = _mapping.MapTransactionResponse(result.Entries);
        return Content(responseJson, "application/fhir+json");
    }

    /// <summary>POST /fhir/R4/$import — upload a FHIR Transaction Bundle JSON file via multipart/form-data</summary>
    [HttpPost("$import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadTransactionBundle(IFormFile bundleFile, CancellationToken ct)
    {
        if (bundleFile == null || bundleFile.Length == 0)
            return BadRequest(FhirOperationOutcome("invalid", "Request must include a bundleFile (.json) in multipart/form-data."));

        if (!bundleFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
            !bundleFile.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(FhirOperationOutcome("invalid", "Uploaded file must be a FHIR Bundle JSON file (.json)."));

        string bundleJson;
        using (var stream = bundleFile.OpenReadStream())
        using (var reader = new StreamReader(stream))
            bundleJson = await reader.ReadToEndAsync(ct);

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await _handler.ImportTransactionBundleAsync(bundleJson, userId, ct);

        if (!result.Success)
        {
            return UnprocessableEntity(new
            {
                resourceType = "OperationOutcome",
                issue = result.Errors.Select(e => new { severity = "error", code = "processing", diagnostics = e }).ToArray()
            });
        }

        var responseJson = _mapping.MapTransactionResponse(result.Entries);
        return Content(responseJson, "application/fhir+json");
    }

    /// <summary>POST /fhir/R4/Patient — import a FHIR Patient resource; creates a new patient with MRN</summary>
    [HttpPost("Patient")]
    public async Task<IActionResult> ImportPatient(CancellationToken ct)
    {
        string fhirJson;
        using (var reader = new StreamReader(Request.Body))
            fhirJson = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(fhirJson))
            return BadRequest(FhirOperationOutcome("invalid", "Request body must contain a FHIR Patient JSON resource."));

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var result = await _handler.ImportPatientAsync(fhirJson, userId, ct);

        if (!result.Success)
            return UnprocessableEntity(FhirOperationOutcome("invalid", string.Join("; ", result.Errors)));

        return Created($"/fhir/R4/Patient/{result.Mrn}", new { result.Mrn, result.Message });
    }

    // ── Encounter ────────────────────────────────────────────────────────────────

    /// <summary>GET /fhir/R4/Encounter — search encounters by patient MRN and/or status</summary>
    [HttpGet("Encounter")]
    public async Task<IActionResult> SearchEncounters(
        [FromQuery] string? patient,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var json = await _handler.SearchEncountersAsync(patient, status, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    /// <summary>GET /fhir/R4/Encounter/{caseNumber} — read single Encounter by CaseNumber</summary>
    [HttpGet("Encounter/{caseNumber}")]
    public async Task<IActionResult> GetEncounter(string caseNumber, CancellationToken ct)
    {
        var json = await _handler.GetEncounterAsync(caseNumber, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    // ── Appointment ──────────────────────────────────────────────────────────────

    /// <summary>GET /fhir/R4/Appointment — search by _id (GUID), patient (MRN), status</summary>
    [HttpGet("Appointment")]
    public async Task<IActionResult> SearchAppointments(
        [FromQuery(Name = "_id")] string? id,
        [FromQuery] string? patient,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var json = await _handler.SearchAppointmentsAsync(id, patient, status, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    /// <summary>GET /fhir/R4/Appointment/{id} — read single Appointment by GUID</summary>
    [HttpGet("Appointment/{id}")]
    public async Task<IActionResult> GetAppointment(string id, CancellationToken ct)
    {
        var json = await _handler.GetAppointmentAsync(id, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    // ── Task ─────────────────────────────────────────────────────────────────────

    /// <summary>GET /fhir/R4/Task — search by _id (GUID), patient (MRN), focus (case number), status</summary>
    [HttpGet("Task")]
    public async Task<IActionResult> SearchTasks(
        [FromQuery(Name = "_id")] string? id,
        [FromQuery] string? patient,
        [FromQuery] string? focus,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var json = await _handler.SearchTasksAsync(id, patient, focus, status, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    /// <summary>GET /fhir/R4/Task/{id} — read single Task by GUID</summary>
    [HttpGet("Task/{id}")]
    public async Task<IActionResult> GetTask(string id, CancellationToken ct)
    {
        var json = await _handler.GetTaskAsync(id, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    // ── DocumentReference ────────────────────────────────────────────────────────

    /// <summary>GET /fhir/R4/DocumentReference/{id} — read single DocumentReference by GUID</summary>
    [HttpGet("DocumentReference/{id}")]
    public async Task<IActionResult> GetDocumentReference(string id, CancellationToken ct)
    {
        var json = await _handler.GetDocumentReferenceAsync(id, BaseUrl, ct);
        return Content(json, "application/fhir+json");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static object FhirOperationOutcome(string code, string diagnostics) => new
    {
        resourceType = "OperationOutcome",
        issue = new[]
        {
            new { severity = "error", code, diagnostics }
        }
    };
}
