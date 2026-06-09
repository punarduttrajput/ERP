using ERP.Alumni.Application.Commands;
using ERP.Alumni.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Alumni.API;

[ApiController]
[Route("api/alumni/donations")]
[Authorize]
public class DonationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public DonationController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;

    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDonationCampaignCommand(
            TenantId, request.Title, request.Description, request.TargetAmount,
            request.StartDate, request.EndDate, request.Section80GEligible,
            request.Section80GRegistrationNumber), ct);

        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { campaignId = result.Value });
    }

    [HttpGet("campaigns")]
    public async Task<IActionResult> ListCampaigns(CancellationToken ct)
    {
        var campaigns = await _mediator.Send(new ListDonationCampaignsQuery(TenantId), ct);
        return Ok(campaigns.Value);
    }

    [HttpGet("campaigns/{id:guid}")]
    public async Task<IActionResult> GetCampaign(Guid id, CancellationToken ct)
    {
        var statsResult = await _mediator.Send(new GetDonationStatsQuery(TenantId, id), ct);
        if (!statsResult.IsSuccess) return NotFound(new { error = statsResult.Error });
        return Ok(statsResult.Value);
    }

    [HttpPost("campaigns/{id:guid}/pledge")]
    public async Task<IActionResult> Pledge(Guid id, [FromBody] PledgeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new PledgeDonationCommand(TenantId, id, request.AlumniId, request.Amount), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(new { pledgeId = result.Value });
    }

    [HttpPost("pledges/{id:guid}/pay")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] PaymentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RecordDonationPaymentCommand(TenantId, id, request.Amount), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }

    [HttpGet("pledges/{id:guid}/receipt")]
    public async Task<IActionResult> DownloadReceipt(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateSection80GReceiptCommand(TenantId, id), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return File(result.Value!, "application/pdf", $"80G-receipt-{id}.pdf");
    }
}

public record CreateCampaignRequest(
    string Title,
    string? Description,
    decimal TargetAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool Section80GEligible,
    string? Section80GRegistrationNumber
);

public record PledgeRequest(Guid AlumniId, decimal Amount);

public record PaymentRequest(decimal Amount);
