using Hangfire.Dashboard;

namespace ERP.Host.Middleware;

public sealed class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true
            && http.User.HasClaim("permission", "system:hangfire");
    }
}
