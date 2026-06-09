using ERP.SIS.API.Dtos;
using ERP.SIS.Application.Commands;
using ERP.SIS.Application.Queries;
using ERP.SIS.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.SIS.API;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStudent(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateStudentCommand(
            id, request.FirstName, request.LastName, request.MiddleName,
            request.MobileNumber, request.DateOfBirth, request.Gender,
            request.BloodGroup, request.PermanentAddress, request.CurrentAddress,
            request.Category), ct);
        return result.IsSuccess ? NoContent() : NotFound(result.Error);
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStudentDocumentsQuery(id), ct);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromBody] UploadDocumentRequest request, CancellationToken ct)
    {
        var bytes = Convert.FromBase64String(request.FileContentBase64);
        var result = await _mediator.Send(new UploadDocumentCommand(
            id, request.DocumentType, request.OriginalFileName, bytes, request.ContentType), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetDocuments), new { id }, new { documentId = result.Value })
            : NotFound(result.Error);
    }

    [HttpGet("{id:guid}/certificates/{type}")]
    public async Task<IActionResult> GenerateCertificate(Guid id, string type, CancellationToken ct)
    {
        if (!Enum.TryParse<CertificateType>(type, ignoreCase: true, out var certType))
            return BadRequest($"Unknown certificate type '{type}'. Valid values: Bonafide, Character, Provisional.");

        var result = await _mediator.Send(new GenerateCertificateCommand(id, certType), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return File(result.Value!, "application/pdf", $"{type}-certificate.pdf");
    }

    [HttpGet("{id:guid}/id-card")]
    public async Task<IActionResult> GenerateIdCard(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateIdCardCommand(id), ct);
        if (!result.IsSuccess)
            return NotFound(result.Error);

        return File(result.Value!, "application/pdf", "student-id-card.pdf");
    }
}
