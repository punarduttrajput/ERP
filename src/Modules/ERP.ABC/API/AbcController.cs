using ERP.ABC.Application.Commands;
using ERP.ABC.Application.Queries;
using ERP.ABC.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.ABC.API;

[ApiController]
[Route("api/abc")]
[Authorize]
public class AbcController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public AbcController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;
    private Guid UserId => _currentUser.UserId ?? Guid.Empty;

    [HttpPost("link")]
    public async Task<IActionResult> LinkAbcId([FromBody] LinkAbcIdRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LinkAbcIdCommand(TenantId, request.StudentId, request.AbcId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { linkedTo = result.Value });
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetProfile(Guid studentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentAbcProfileQuery(TenantId, studentId), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("transfers")]
    public async Task<IActionResult> RequestTransfer([FromBody] RequestCreditTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestCreditTransferCommand(
            TenantId, request.StudentId, request.AbcId, request.Direction,
            request.SourceInstitution, request.DestinationInstitution,
            request.SubjectCode, request.SubjectName, request.CreditsRequested, request.AcademicYear), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { transferId = result.Value });
    }

    [HttpGet("transfers")]
    public async Task<IActionResult> GetTransfers(
        [FromQuery] Guid? studentId,
        [FromQuery] TransferStatus? status,
        [FromQuery] TransferDirection? direction,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCreditTransfersQuery(TenantId, studentId, status, direction, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("transfers/{id:guid}/approve")]
    public async Task<IActionResult> ApproveTransfer(Guid id, [FromBody] ApproveTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveCreditTransferCommand(TenantId, id, request.CreditsApproved, UserId, request.AbcRegistryReference), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }

    [HttpPost("transfers/{id:guid}/reject")]
    public async Task<IActionResult> RejectTransfer(Guid id, [FromBody] RejectTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RejectCreditTransferCommand(TenantId, id, UserId, request.Reason), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }

    [HttpGet("students/{studentId:guid}/pathway-eligibility")]
    public async Task<IActionResult> GetPathwayEligibility(Guid studentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPathwayEligibilityQuery(TenantId, studentId), ct);
        if (!result.IsSuccess) return NotFound(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpPost("students/{studentId:guid}/choose-pathway")]
    public async Task<IActionResult> ChoosePathway(Guid studentId, [FromBody] ChoosePathwayRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChoosePathwayCommand(TenantId, studentId, request.PathwayType), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { pathwayId = result.Value });
    }

    [HttpPost("students/{studentId:guid}/approve-pathway")]
    public async Task<IActionResult> ApprovePathway(Guid studentId, [FromBody] ApprovePathwayRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApprovePathwayCommand(TenantId, request.PathwayId, UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok();
    }
}

public record LinkAbcIdRequest(Guid StudentId, string AbcId);
public record RequestCreditTransferRequest(
    Guid StudentId,
    string AbcId,
    TransferDirection Direction,
    string SourceInstitution,
    string? DestinationInstitution,
    string SubjectCode,
    string SubjectName,
    int CreditsRequested,
    int AcademicYear);
public record ApproveTransferRequest(int CreditsApproved, string? AbcRegistryReference);
public record RejectTransferRequest(string Reason);
public record ChoosePathwayRequest(PathwayType PathwayType);
public record ApprovePathwayRequest(Guid PathwayId);
