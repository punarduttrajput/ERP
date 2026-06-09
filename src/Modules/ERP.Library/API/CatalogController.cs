using ERP.Library.Application.Commands;
using ERP.Library.Application.Queries;
using ERP.Shared.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Library.API;

[ApiController]
[Route("api/library/catalog")]
[Authorize]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentTenant _currentTenant;

    public CatalogController(IMediator mediator, ICurrentTenant currentTenant)
    {
        _mediator = mediator;
        _currentTenant = currentTenant;
    }

    [HttpPost("books")]
    public async Task<IActionResult> AddBook([FromBody] AddBookRequest request, CancellationToken cancellationToken)
    {
        var command = new AddBookCommand(
            request.ISBN, request.Title, request.Authors,
            request.Publisher, request.PublicationYear, request.Edition,
            request.Category, request.Language ?? "English",
            request.ShelfLocation, request.CoverImageUrl,
            request.IsbnLookup,
            _currentTenant.TenantId ?? Guid.Empty
        );

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetBook), new { id = result.Value }, new { id = result.Value });
    }

    [HttpGet("books")]
    public async Task<IActionResult> SearchCatalog(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SearchCatalogQuery(search, _currentTenant.TenantId ?? Guid.Empty, page, pageSize),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("books/{id:guid}")]
    public async Task<IActionResult> GetBook(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBookQuery(id), cancellationToken);
        if (!result.IsSuccess)
            return NotFound(new { error = result.Error });

        return Ok(result.Value);
    }

    [HttpPost("books/{id:guid}/copies")]
    public async Task<IActionResult> AddCopy(Guid id, [FromBody] AddCopyRequest request, CancellationToken cancellationToken)
    {
        var command = new AddBookCopyCommand(
            id, request.Barcode, request.AcquisitionDate,
            request.Price, _currentTenant.TenantId ?? Guid.Empty
        );

        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
            return BadRequest(new { error = result.Error });

        return Ok(new { id = result.Value });
    }
}

public record AddBookRequest(
    string ISBN,
    string Title,
    string Authors,
    string? Publisher,
    int? PublicationYear,
    string? Edition,
    string? Category,
    string? Language,
    string? ShelfLocation,
    string? CoverImageUrl,
    bool IsbnLookup = false
);

public record AddCopyRequest(
    string Barcode,
    DateOnly AcquisitionDate,
    decimal? Price
);
