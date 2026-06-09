using ERP.Chatbot.Application.Commands;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ERP.Chatbot.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public ChatHub(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    public async Task SendMessage(string message, string? sessionKey)
    {
        var userId = GetUserId();
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var result = await _mediator.Send(new SendMessageCommand(tenantId, userId, message, sessionKey));

        if (result.IsSuccess)
            await Clients.Caller.SendAsync("ReceiveMessage", result.Value);
        else
            await Clients.Caller.SendAsync("Error", result.Error);
    }

    private Guid GetUserId() =>
        Guid.TryParse(Context.User?.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
}
