using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Alumni.Application.Queries;

public record AlumniDirectoryItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    int GraduationYear,
    string ProgramName,
    string? BatchName,
    string? CurrentEmployer,
    string? CurrentJobTitle,
    string? CurrentCity,
    string CurrentCountry,
    string? LinkedInUrl,
    string? AvatarUrl
);

public record SearchAlumniDirectoryQuery(
    Guid TenantId,
    int? GraduationYear,
    string? ProgramName,
    string? CurrentCity,
    string? CurrentCountry,
    string? Search,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<AlumniDirectoryItemDto>>>;

public class SearchAlumniDirectoryHandler : IRequestHandler<SearchAlumniDirectoryQuery, Result<PagedResult<AlumniDirectoryItemDto>>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SearchAlumniDirectoryHandler(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<Result<PagedResult<AlumniDirectoryItemDto>>> Handle(SearchAlumniDirectoryQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var offset = (request.Page - 1) * request.PageSize;

        var dataSql = """
            SELECT ap.Id, ap.FirstName, ap.LastName, ap.GraduationYear, ap.ProgramName,
                   ap.BatchName, ap.CurrentEmployer, ap.CurrentJobTitle, ap.CurrentCity,
                   ap.CurrentCountry, ap.LinkedInUrl, ap.AvatarUrl
            FROM alumni_profiles ap
            WHERE ap.TenantId = @TenantId AND ap.IsDirectoryVisible = 1 AND ap.IsDeleted = 0
              AND (@GraduationYear IS NULL OR ap.GraduationYear = @GraduationYear)
              AND (@ProgramName IS NULL OR ap.ProgramName = @ProgramName)
              AND (@CurrentCity IS NULL OR ap.CurrentCity = @CurrentCity)
              AND (@CurrentCountry IS NULL OR ap.CurrentCountry = @CurrentCountry)
              AND (@Search IS NULL OR ap.FirstName LIKE CONCAT('%',@Search,'%')
                   OR ap.LastName LIKE CONCAT('%',@Search,'%')
                   OR ap.CurrentEmployer LIKE CONCAT('%',@Search,'%'))
            ORDER BY ap.GraduationYear DESC, ap.LastName
            LIMIT @PageSize OFFSET @Offset
            """;

        var countSql = """
            SELECT COUNT(*)
            FROM alumni_profiles ap
            WHERE ap.TenantId = @TenantId AND ap.IsDirectoryVisible = 1 AND ap.IsDeleted = 0
              AND (@GraduationYear IS NULL OR ap.GraduationYear = @GraduationYear)
              AND (@ProgramName IS NULL OR ap.ProgramName = @ProgramName)
              AND (@CurrentCity IS NULL OR ap.CurrentCity = @CurrentCity)
              AND (@CurrentCountry IS NULL OR ap.CurrentCountry = @CurrentCountry)
              AND (@Search IS NULL OR ap.FirstName LIKE CONCAT('%',@Search,'%')
                   OR ap.LastName LIKE CONCAT('%',@Search,'%')
                   OR ap.CurrentEmployer LIKE CONCAT('%',@Search,'%'))
            """;

        var parameters = new
        {
            request.TenantId,
            request.GraduationYear,
            request.ProgramName,
            request.CurrentCity,
            request.CurrentCountry,
            request.Search,
            request.PageSize,
            Offset = offset
        };

        var items = (await conn.QueryAsync<AlumniDirectoryItemDto>(dataSql, parameters)).ToList();
        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);

        return Result.Success(new PagedResult<AlumniDirectoryItemDto>(items, total, request.Page, request.PageSize));
    }
}
