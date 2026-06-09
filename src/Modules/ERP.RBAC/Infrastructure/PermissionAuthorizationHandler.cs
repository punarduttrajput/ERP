using ERP.Shared.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace ERP.RBAC.Infrastructure;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionClaims = context.User.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value);

        if (permissionClaims.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogDebug("User does not have permission: {Permission}", requirement.Permission);
        }

        return Task.CompletedTask;
    }
}
