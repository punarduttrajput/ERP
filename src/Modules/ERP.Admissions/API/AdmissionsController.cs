using ERP.Admissions.Application.Commands;
using ERP.Admissions.Application.Queries;
using ERP.Admissions.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Admissions.API;

[ApiController]
[Route("api/admissions")]
[Authorize]
public class AdmissionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdmissionsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit([FromBody] SubmitApplicationRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SubmitApplicationCommand(
            request.ApplicantName, request.ApplicantEmail, request.ApplicantMobile,
            request.ProgramId, request.ProgramName, request.Category, request.AcademicYear,
            request.Documents.Select(d => new DocumentUpload(d.DocumentType, d.BlobUrl, d.FileName)).ToList()),
            ct);
        return result.IsSuccess
            ? Ok(new { success = true, data = new { applicationId = result.Value } })
            : BadRequest(new { success = false, message = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? programId = null, [FromQuery] int? year = null,
        [FromQuery] ApplicationState? state = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetApplicationsQuery(page, pageSize, programId, year, state), ct);
        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost("{id:guid}/verify-documents")]
    public async Task<IActionResult> VerifyDocuments(Guid id, [FromBody] VerifyDocsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyDocumentsCommand(id, request.Approved, request.RejectionReason), ct);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPost("merit/evaluate")]
    public async Task<IActionResult> EvaluateMerit([FromBody] MeritEvalRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new EvaluateMeritCommand(request.ProgramId, request.AcademicYear), ct);
        return result.IsSuccess
            ? Ok(new { success = true, data = new { ranked = result.Value } })
            : BadRequest(new { success = false, message = result.Error });
    }

    [HttpGet("merit")]
    public async Task<IActionResult> MeritList([FromQuery] Guid programId, [FromQuery] int year,
        [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMeritListQuery(programId, year, category), ct);
        return Ok(new { success = true, data = result.Value });
    }

    [HttpPost("offers/make")]
    public async Task<IActionResult> MakeOffers([FromBody] MeritEvalRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MakeOfferCommand(request.ProgramId, request.AcademicYear), ct);
        return result.IsSuccess
            ? Ok(new { success = true, data = new { offersIssued = result.Value } })
            : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPost("{id:guid}/accept-offer")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptOffer(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new AcceptOfferCommand(id), ct);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RejectApplicationCommand(id, request.Reason), ct);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { success = false, message = result.Error });
    }

    [HttpPost("{id:guid}/withdraw")]
    [AllowAnonymous]
    public async Task<IActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new WithdrawApplicationCommand(id), ct);
        return result.IsSuccess ? Ok(new { success = true }) : BadRequest(new { success = false, message = result.Error });
    }
}

public record SubmitApplicationRequest(
    string ApplicantName, string ApplicantEmail, string ApplicantMobile,
    Guid ProgramId, string ProgramName, string Category, int AcademicYear,
    IReadOnlyList<DocumentUploadDto> Documents);
public record DocumentUploadDto(string DocumentType, string BlobUrl, string FileName);
public record VerifyDocsRequest(bool Approved, string? RejectionReason);
public record MeritEvalRequest(Guid ProgramId, int AcademicYear);
public record RejectRequest(string Reason);
