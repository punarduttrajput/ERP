using ERP.Shared.Application.Abstractions;
using System.Security.Claims;

namespace ERP.Host;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public string? Name => User?.FindFirst("name")?.Value;

    public Guid? TenantId
    {
        get
        {
            var tid = User?.FindFirst("tid")?.Value;
            return Guid.TryParse(tid, out var id) ? id : null;
        }
    }

    public IReadOnlyList<string> Roles => User?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToList() ?? new List<string>();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission) =>
        User?.Claims.Any(c => c.Type == "permission" && c.Value == permission) ?? false;
}
