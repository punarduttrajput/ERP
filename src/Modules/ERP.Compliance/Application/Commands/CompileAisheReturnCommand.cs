using Dapper;
using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Compliance.Application.Commands;

public record CompileAisheReturnCommand(Guid TenantId, int AcademicYear) : IRequest<Result<Guid>>;

public class CompileAisheReturnHandler : IRequestHandler<CompileAisheReturnCommand, Result<Guid>>
{
    private readonly IComplianceDbContext _db;
    private readonly IDbConnectionFactory _connectionFactory;

    public CompileAisheReturnHandler(IComplianceDbContext db, IDbConnectionFactory connectionFactory)
    {
        _db = db;
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<Guid>> Handle(CompileAisheReturnCommand request, CancellationToken cancellationToken)
    {
        using var conn = await _connectionFactory.CreateReadConnectionAsync(cancellationToken);

        var studentStats = await conn.QuerySingleOrDefaultAsync<StudentStats>(
            @"SELECT
                COUNT(*) AS Total,
                SUM(CASE WHEN Gender = 'Male' THEN 1 ELSE 0 END) AS Male,
                SUM(CASE WHEN Gender = 'Female' THEN 1 ELSE 0 END) AS Female,
                SUM(CASE WHEN Category = 'SC' THEN 1 ELSE 0 END) AS Sc,
                SUM(CASE WHEN Category = 'ST' THEN 1 ELSE 0 END) AS St,
                SUM(CASE WHEN Category = 'OBC' THEN 1 ELSE 0 END) AS Obc
            FROM students
            WHERE TenantId = @TenantId AND AcademicYear = @AcademicYear AND IsActive = 1 AND IsDeleted = 0",
            new { request.TenantId, request.AcademicYear });

        var facultyStats = await conn.QuerySingleOrDefaultAsync<FacultyStats>(
            @"SELECT
                COUNT(*) AS Total,
                SUM(CASE WHEN Gender = 'Male' THEN 1 ELSE 0 END) AS Male,
                SUM(CASE WHEN Gender = 'Female' THEN 1 ELSE 0 END) AS Female
            FROM employees
            WHERE TenantId = @TenantId AND Status = 1 AND EmploymentType IN ('Permanent', 'Contract') AND IsDeleted = 0",
            new { request.TenantId });

        var libraryBooks = await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(SUM(TotalCopies), 0) FROM library_books WHERE TenantId = @TenantId AND IsDeleted = 0",
            new { request.TenantId });

        var departmentCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM departments WHERE TenantId = @TenantId AND IsActive = 1 AND IsDeleted = 0",
            new { request.TenantId });

        var programmeCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM academic_programs WHERE TenantId = @TenantId AND IsActive = 1 AND IsDeleted = 0",
            new { request.TenantId });

        var existing = _db.AisheReturns
            .FirstOrDefault(r => r.TenantId == request.TenantId && r.AcademicYear == request.AcademicYear && !r.IsDeleted);

        AisheReturn aisheReturn;
        if (existing is not null)
        {
            aisheReturn = existing;
        }
        else
        {
            aisheReturn = new AisheReturn
            {
                TenantId = request.TenantId,
                AcademicYear = request.AcademicYear
            };
            _db.AisheReturns.Add(aisheReturn);
        }

        aisheReturn.TotalStudentsEnrolled = studentStats?.Total;
        aisheReturn.MaleStudents = studentStats?.Male;
        aisheReturn.FemaleStudents = studentStats?.Female;
        aisheReturn.ScStudents = studentStats?.Sc;
        aisheReturn.StStudents = studentStats?.St;
        aisheReturn.ObcStudents = studentStats?.Obc;
        aisheReturn.TotalFaculty = facultyStats?.Total;
        aisheReturn.MaleFaculty = facultyStats?.Male;
        aisheReturn.FemaleFaculty = facultyStats?.Female;
        aisheReturn.TotalLibraryBooks = libraryBooks;
        aisheReturn.TotalDepartments = departmentCount;
        aisheReturn.TotalProgrammes = programmeCount;
        aisheReturn.Status = AisheReturnStatus.Compiled;
        aisheReturn.CompiledAt = DateTime.UtcNow;
        aisheReturn.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(aisheReturn.Id);
    }

    private record StudentStats(int Total, int Male, int Female, int Sc, int St, int Obc);
    private record FacultyStats(int Total, int Male, int Female);
}
