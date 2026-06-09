using Dapper;
using ERP.Shared.Application.Abstractions;
using MediatR;

namespace ERP.MobileApi.Application.Queries;

public record StudentResultMobileDto(Guid SemesterId, decimal? Gpa, decimal? Cgpa, bool IsPublished, DateTime PublishedAt);

public record GetStudentResultsQuery(Guid TenantId, Guid UserId) : IRequest<IReadOnlyList<StudentResultMobileDto>>;

public class GetStudentResultsHandler : IRequestHandler<GetStudentResultsQuery, IReadOnlyList<StudentResultMobileDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetStudentResultsHandler(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<StudentResultMobileDto>> Handle(GetStudentResultsQuery request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        const string studentSql = @"
            SELECT Id FROM students
            WHERE TenantId = @TenantId AND UserId = @UserId AND IsDeleted = 0 LIMIT 1";

        var studentId = await conn.QueryFirstOrDefaultAsync<Guid?>(studentSql, new
        {
            request.TenantId,
            request.UserId
        });

        if (studentId is null)
            return Array.Empty<StudentResultMobileDto>();

        const string sql = @"
            SELECT SemesterId, GPA, CGPA, IsPublished, UpdatedAt AS PublishedAt
            FROM student_results
            WHERE TenantId = @TenantId AND StudentId = @StudentId AND IsPublished = 1 AND IsDeleted = 0
            ORDER BY CreatedAt DESC";

        var rows = await conn.QueryAsync<ResultRow>(sql, new
        {
            request.TenantId,
            StudentId = studentId.Value
        });

        return rows.Select(r => new StudentResultMobileDto(
            r.SemesterId, r.GPA, r.CGPA, r.IsPublished, r.PublishedAt
        )).ToList();
    }

    private class ResultRow
    {
        public Guid SemesterId { get; set; }
        public decimal? GPA { get; set; }
        public decimal? CGPA { get; set; }
        public bool IsPublished { get; set; }
        public DateTime PublishedAt { get; set; }
    }
}
