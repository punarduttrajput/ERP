using System.Data;
using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Library.Application.Queries;

public record BookCatalogDto(
    Guid Id,
    string ISBN,
    string Title,
    string Authors,
    string? Publisher,
    int? PublicationYear,
    string? Category,
    int TotalCopies,
    int AvailableCopies,
    string? ShelfLocation
);

public record SearchCatalogQuery(
    string? Search,
    Guid TenantId,
    int Page,
    int PageSize
) : IRequest<PagedResult<BookCatalogDto>>;

public class SearchCatalogQueryHandler : IRequestHandler<SearchCatalogQuery, PagedResult<BookCatalogDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SearchCatalogQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<BookCatalogDto>> Handle(SearchCatalogQuery request, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var offset = (request.Page - 1) * request.PageSize;
        var search = request.Search ?? string.Empty;

        const string dataSql = """
            SELECT b.Id, b.ISBN, b.Title, b.Authors, b.Publisher, b.PublicationYear,
                   b.Category, b.TotalCopies, b.AvailableCopies, b.ShelfLocation
            FROM library_books b
            WHERE b.TenantId = @TenantId AND b.IsDeleted = 0
              AND (
                b.Title LIKE CONCAT('%', @Search, '%')
                OR b.Authors LIKE CONCAT('%', @Search, '%')
                OR b.ISBN LIKE CONCAT('%', @Search, '%')
                OR b.Category LIKE CONCAT('%', @Search, '%')
              )
            ORDER BY b.Title
            LIMIT @PageSize OFFSET @Offset
            """;

        const string countSql = """
            SELECT COUNT(*)
            FROM library_books b
            WHERE b.TenantId = @TenantId AND b.IsDeleted = 0
              AND (
                b.Title LIKE CONCAT('%', @Search, '%')
                OR b.Authors LIKE CONCAT('%', @Search, '%')
                OR b.ISBN LIKE CONCAT('%', @Search, '%')
                OR b.Category LIKE CONCAT('%', @Search, '%')
              )
            """;

        var param = new { request.TenantId, Search = search, request.PageSize, Offset = offset };

        var items = (await connection.QueryAsync<BookCatalogDto>(dataSql, param)).ToList();
        var total = await connection.ExecuteScalarAsync<int>(countSql, param);

        return new PagedResult<BookCatalogDto>(items, total, request.Page, request.PageSize);
    }
}
