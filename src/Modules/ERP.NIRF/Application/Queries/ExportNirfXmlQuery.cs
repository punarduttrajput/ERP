using Dapper;
using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace ERP.NIRF.Application.Queries;

public record ExportNirfXmlQuery(Guid TenantId, Guid SubmissionId) : IRequest<Result<string>>;

public class ExportNirfXmlHandler : IRequestHandler<ExportNirfXmlQuery, Result<string>>
{
    private readonly INirfDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public ExportNirfXmlHandler(INirfDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<string>> Handle(ExportNirfXmlQuery request, CancellationToken cancellationToken)
    {
        var submission = _db.NirfSubmissions
            .Include(s => s.ParameterScores.Where(p => !p.IsDeleted))
            .FirstOrDefault(s => s.Id == request.SubmissionId && s.TenantId == request.TenantId && !s.IsDeleted);

        if (submission is null)
            return Result.Failure<string>("Submission not found.");

        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);
        var tenantName = await conn.ExecuteScalarAsync<string>(
            "SELECT Name FROM tenants WHERE Id = @TenantId AND IsDeleted = 0",
            new { request.TenantId }) ?? "Unknown Institution";

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("NIRFSubmission",
                new XAttribute("year", submission.RankingYear),
                new XAttribute("category", submission.Category),
                new XElement("Institution",
                    new XElement("Name", tenantName),
                    new XElement("SubmissionDate", (submission.SubmittedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd"))
                ),
                new XElement("Parameters",
                    submission.ParameterScores.Select(p =>
                        new XElement("Parameter",
                            new XAttribute("name", p.Parameter),
                            new XAttribute("weight", p.Weight),
                            new XAttribute("rawScore", p.RawScore),
                            new XAttribute("weightedScore", p.WeightedScore),
                            new XElement("Data", p.DataJson)
                        )
                    )
                ),
                new XElement("OverallScore", submission.OverallScore?.ToString("F2") ?? "0.00")
            )
        );

        return Result.Success(doc.Declaration + doc.ToString());
    }
}
