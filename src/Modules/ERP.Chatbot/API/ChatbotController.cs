using ERP.Chatbot.Application.Commands;
using ERP.Chatbot.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Chatbot.API;

[ApiController]
[Route("api/chatbot")]
[Authorize]
public class ChatbotController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public ChatbotController(IMediator mediator, ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    private Guid TenantId => _currentTenant.TenantId ?? Guid.Empty;
    private Guid UserId => _currentUser.UserId ?? Guid.Empty;

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SendMessageCommand(TenantId, UserId, request.Message, request.SessionKey), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string? sessionKey, [FromQuery] int lastN = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetConversationHistoryQuery(TenantId, UserId, sessionKey, lastN), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return Ok(result.Value);
    }

    [HttpDelete("conversation")]
    public async Task<IActionResult> DeleteConversation(CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteConversationCommand(TenantId, UserId), ct);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return NoContent();
    }
}

public record SendMessageRequest(string Message, string? SessionKey = null);
