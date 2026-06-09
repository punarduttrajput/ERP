using Dapper;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Placement.Application.Queries;

public record TopCompanyDto(string CompanyName, int Offers, decimal HighestPackageLpa);

public record PlacementStatisticsDto(
    int TotalEligibleStudents,
    int TotalPlaced,
    decimal PlacedPercent,
    decimal AveragePackageLpa,
    decimal HighestPackageLpa,
    int TotalDrives,
    int TotalCompanies,
    IReadOnlyList<TopCompanyDto> TopCompanies
);

public record GetPlacementStatisticsQuery(
    Guid TenantId,
    int? AcademicYear,
    int? TotalEligibleStudents
) : IRequest<Result<PlacementStatisticsDto>>;

public class GetPlacementStatisticsHandler : IRequestHandler<GetPlacementStatisticsQuery, Result<PlacementStatisticsDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetPlacementStatisticsHandler(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task<Result<PlacementStatisticsDto>> Handle(GetPlacementStatisticsQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var mainSql = """
            SELECT
                COUNT(DISTINCT po.StudentId)    AS TotalPlaced,
                COALESCE(AVG(po.OfferedPackageLpa), 0) AS AveragePackageLpa,
                COALESCE(MAX(po.OfferedPackageLpa), 0) AS HighestPackageLpa,
                COUNT(DISTINCT pd.CompanyId)    AS TotalCompanies,
                COUNT(DISTINCT pd.Id)           AS TotalDrives
            FROM placement_offers po
            JOIN placement_drives pd ON po.DriveId = pd.Id
            WHERE po.TenantId = @TenantId
              AND po.Status IN (1, 2)
              AND po.IsDeleted = 0
              AND pd.IsDeleted = 0
              AND (@AcademicYear IS NULL OR pd.AcademicYear = @AcademicYear)
            """;

        var main = await conn.QuerySingleAsync<MainStats>(mainSql, new
        {
            request.TenantId,
            request.AcademicYear
        });

        var topSql = """
            SELECT pd2.CompanyName, COUNT(po2.Id) AS Offers, MAX(po2.OfferedPackageLpa) AS HighestPackageLpa
            FROM placement_offers po2
            JOIN placement_drives pd2 ON po2.DriveId = pd2.Id
            WHERE po2.TenantId = @TenantId AND po2.IsDeleted = 0
              AND (@AcademicYear IS NULL OR pd2.AcademicYear = @AcademicYear)
            GROUP BY pd2.CompanyName
            ORDER BY Offers DESC, HighestPackageLpa DESC
            LIMIT 10
            """;

        var topCompanies = (await conn.QueryAsync<TopCompanyDto>(topSql, new
        {
            request.TenantId,
            request.AcademicYear
        })).ToList();

        // TotalEligibleStudents is passed from the caller; default to TotalPlaced when not provided
        var eligible = request.TotalEligibleStudents ?? main.TotalPlaced;
        var placedPercent = eligible > 0 ? Math.Round((decimal)main.TotalPlaced / eligible * 100, 2) : 0;

        var dto = new PlacementStatisticsDto(
            eligible,
            main.TotalPlaced,
            placedPercent,
            main.AveragePackageLpa,
            main.HighestPackageLpa,
            main.TotalDrives,
            main.TotalCompanies,
            topCompanies
        );

        return Result.Success(dto);
    }

    private record MainStats(int TotalPlaced, decimal AveragePackageLpa, decimal HighestPackageLpa, int TotalCompanies, int TotalDrives);
}
