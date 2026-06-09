using ERP.HRMS.Application.Commands;
using ERP.HRMS.Application.Queries;
using ERP.HRMS.Domain;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.HRMS.API;

[Authorize]
[ApiController]
[Route("api/hrms/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _tenant;
    private readonly ICurrentUser _user;

    public EmployeesController(IMediator mediator, ICurrentTenant tenant, ICurrentUser user)
    {
        _mediator = mediator;
        _tenant = tenant;
        _user = user;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEmployeeCommand(
            _tenant.TenantId!.Value, dto.DepartmentId, dto.Designation, dto.EmploymentType,
            dto.FirstName, dto.LastName, dto.Email, dto.MobileNumber,
            dto.DateOfBirth, dto.Gender, dto.PanNumber, dto.AadharNumber,
            dto.JoiningDate, dto.ReportingManagerId
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        [FromQuery] EmploymentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEmployeesQuery(_tenant.TenantId!.Value, departmentId, status, page, pageSize), ct);
        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmployeeQuery(id, _tenant.TenantId!.Value), ct);
        if (!result.IsSuccess) return NotFound(new { result.Error });
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest dto, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateEmployeeCommand(
            id, _tenant.TenantId!.Value, dto.Designation, dto.EmploymentType,
            dto.Status, dto.MobileNumber, dto.PanNumber, dto.AadharNumber,
            dto.ConfirmationDate, dto.ReportingManagerId
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return NoContent();
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> UploadDocument(
        Guid id,
        [FromForm] UploadDocumentRequest dto,
        CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await dto.File.CopyToAsync(ms, ct);

        var result = await _mediator.Send(new UploadEmployeeDocumentCommand(
            id, _tenant.TenantId!.Value, dto.DocumentType, dto.File.FileName,
            ms.ToArray(), dto.File.ContentType
        ), ct);

        if (!result.IsSuccess) return BadRequest(new { result.Error });
        return Ok(new { id = result.Value });
    }
}

public record CreateEmployeeRequest(
    Guid DepartmentId, string Designation, string EmploymentType,
    string FirstName, string LastName, string Email, string? MobileNumber,
    DateOnly DateOfBirth, string Gender, string? PanNumber, string? AadharNumber,
    DateOnly JoiningDate, Guid? ReportingManagerId
);

public record UpdateEmployeeRequest(
    string Designation, string EmploymentType, EmploymentStatus Status,
    string? MobileNumber, string? PanNumber, string? AadharNumber,
    DateOnly? ConfirmationDate, Guid? ReportingManagerId
);

public record UploadDocumentRequest(
    string DocumentType,
    Microsoft.AspNetCore.Http.IFormFile File
);
