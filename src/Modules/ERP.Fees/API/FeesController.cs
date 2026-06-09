using ERP.Fees.Application.Commands;
using ERP.Fees.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Fees.API;

[ApiController]
[Route("api/fees")]
[Authorize]
public class FeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("structures")]
    public async Task<IActionResult> CreateFeeStructure([FromBody] CreateFeeStructureRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _mediator.Send(new CreateFeeStructureCommand(
            tenantId,
            request.ProgramId,
            request.ProgramName,
            request.SemesterNumber,
            request.Category,
            request.AcademicYear,
            request.Components.Select(c => new ComponentDto(c.Name, c.Amount, c.IsRefundable)).ToList()
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpGet("structures")]
    public async Task<IActionResult> GetFeeStructures([FromQuery] Guid? programId, [FromQuery] int? semesterNumber, [FromQuery] int? academicYear, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFeeStructureQuery(programId, semesterNumber, academicYear), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("installment-schedules")]
    public async Task<IActionResult> CreateInstallmentSchedule([FromBody] CreateInstallmentScheduleRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _mediator.Send(new CreateInstallmentScheduleCommand(
            tenantId,
            request.FeeStructureId,
            request.Installments.Select(i => new InstallmentDto(i.InstallmentNumber, i.DueDate, i.Amount, i.LateFinePerDay, i.MaxLateFine)).ToList()
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok();
    }

    [HttpGet("accounts/{studentId:guid}")]
    public async Task<IActionResult> GetFeeAccount(Guid studentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFeeAccountQuery(studentId), ct);
        if (!result.IsSuccess)
            return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPost("payments/initiate")]
    public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new InitiatePaymentCommand(
            request.AccountId,
            request.InstallmentId,
            request.Amount,
            request.PaymentMethod
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpGet("payments/{paymentId:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid paymentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetReceiptQuery(paymentId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return File(result.Value!, "application/pdf", $"receipt-{paymentId}.pdf");
    }

    [HttpPost("scholarships")]
    public async Task<IActionResult> CreateScholarship([FromBody] CreateScholarshipRequest request, CancellationToken ct)
    {
        var tenantId = GetTenantId();
        var result = await _mediator.Send(new CreateScholarshipCommand(
            tenantId,
            request.Name,
            request.ScholarshipType,
            request.DiscountAmount,
            request.DiscountPercent,
            request.MinMeritScore,
            request.EligibleCategories,
            request.MaxBeneficiaries
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpPost("scholarships/apply")]
    public async Task<IActionResult> ApplyScholarship([FromBody] ApplyScholarshipRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApplyScholarshipCommand(
            request.StudentId,
            request.AcademicYear,
            request.MeritScore,
            request.Category
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok();
    }

    [HttpPost("scholarships/apply-manual")]
    public async Task<IActionResult> ApplyScholarshipManual([FromBody] ApplyScholarshipManualRequest request, CancellationToken ct)
    {
        var appliedBy = GetCurrentUserId();
        var result = await _mediator.Send(new ApplyScholarshipManualCommand(
            request.StudentId,
            request.ScholarshipId,
            request.AcademicYear,
            appliedBy
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok();
    }

    [HttpPost("refunds/initiate")]
    public async Task<IActionResult> InitiateRefund([FromBody] InitiateRefundRequest request, CancellationToken ct)
    {
        var initiatedBy = GetCurrentUserId();
        var result = await _mediator.Send(new InitiateRefundCommand(
            request.StudentId,
            request.PaymentId,
            request.Amount,
            request.Reason,
            initiatedBy
        ), ct);

        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }

    [HttpPatch("refunds/{id:guid}/approve")]
    public async Task<IActionResult> ApproveRefund(Guid id, CancellationToken ct)
    {
        var approvedBy = GetCurrentUserId();
        var result = await _mediator.Send(new ApproveRefundCommand(id, approvedBy), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok();
    }

    [HttpPatch("refunds/{id:guid}/process")]
    public async Task<IActionResult> ProcessRefund(Guid id, [FromBody] ProcessRefundRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ProcessRefundCommand(id, request.GatewayRefundId), ct);
        if (!result.IsSuccess)
            return BadRequest(new { result.Error });
        return Ok();
    }

    private Guid GetTenantId()
    {
        var claim = User.FindFirst("tenant_id")?.Value;
        return claim is not null ? Guid.Parse(claim) : Guid.Empty;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? Guid.Parse(claim) : Guid.Empty;
    }
}

public record CreateFeeStructureRequest(
    Guid ProgramId,
    string ProgramName,
    int SemesterNumber,
    string Category,
    int AcademicYear,
    IReadOnlyList<ComponentRequestDto> Components
);

public record ComponentRequestDto(string Name, decimal Amount, bool IsRefundable);

public record CreateInstallmentScheduleRequest(
    Guid FeeStructureId,
    IReadOnlyList<InstallmentRequestDto> Installments
);

public record InstallmentRequestDto(int InstallmentNumber, DateOnly DueDate, decimal Amount, decimal LateFinePerDay, decimal MaxLateFine);

public record InitiatePaymentRequest(Guid AccountId, Guid? InstallmentId, decimal Amount, string? PaymentMethod);

public record CreateScholarshipRequest(
    string Name,
    Domain.ScholarshipType ScholarshipType,
    decimal? DiscountAmount,
    decimal? DiscountPercent,
    decimal? MinMeritScore,
    string? EligibleCategories,
    int? MaxBeneficiaries
);

public record ApplyScholarshipRequest(Guid StudentId, int AcademicYear, decimal? MeritScore, string Category);

public record ApplyScholarshipManualRequest(Guid StudentId, Guid ScholarshipId, int AcademicYear);

public record InitiateRefundRequest(Guid StudentId, Guid PaymentId, decimal Amount, string Reason);

public record ProcessRefundRequest(string GatewayRefundId);
