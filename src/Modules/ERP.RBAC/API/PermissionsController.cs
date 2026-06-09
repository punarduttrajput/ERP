using ERP.RBAC.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERP.RBAC.API;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IRbacDbContext _db;

    public PermissionsController(IRbacDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var permissions = await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Action)
            .Select(p => new { p.Id, p.Name, p.Module, p.Action, p.Description })
            .ToListAsync(cancellationToken);

        return Ok(new { success = true, data = permissions, traceId = HttpContext.TraceIdentifier });
    }
}
