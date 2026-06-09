using Dapper;
using ERP.Shared.Application.Abstractions;
using MediatR;

namespace ERP.MobileApi.Application.Queries;

public record FeeInstallmentMobileDto(string Label, decimal Amount, DateTime DueDate, bool IsPaid);

public record StudentFeesDto(
    decimal TotalDue,
    decimal TotalPaid,
    bool IsFullyPaid,
    IReadOnlyList<FeeInstallmentMobileDto> Installments
);

public record GetStudentFeesQuery(Guid TenantId, Guid UserId) : IRequest<StudentFeesDto>;

public class GetStudentFeesHandler : IRequestHandler<GetStudentFeesQuery, StudentFeesDto>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetStudentFeesHandler(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<StudentFeesDto> Handle(GetStudentFeesQuery request, CancellationToken cancellationToken)
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
            return new StudentFeesDto(0m, 0m, true, Array.Empty<FeeInstallmentMobileDto>());

        const string accountSql = @"
            SELECT Id, DueAmount, PaidAmount, IsFullyPaid FROM student_fee_accounts
            WHERE TenantId = @TenantId AND StudentId = @StudentId AND IsDeleted = 0
            ORDER BY AcademicYear DESC LIMIT 1";

        var account = await conn.QueryFirstOrDefaultAsync<FeeAccountRow>(accountSql, new
        {
            request.TenantId,
            StudentId = studentId.Value
        });

        if (account is null)
            return new StudentFeesDto(0m, 0m, true, Array.Empty<FeeInstallmentMobileDto>());

        const string installmentSql = @"
            SELECT Label, Amount, DueDate, IsPaid FROM fee_installments
            WHERE TenantId = @TenantId AND FeeAccountId = @FeeAccountId AND IsDeleted = 0
            ORDER BY DueDate";

        var installmentRows = await conn.QueryAsync<InstallmentRow>(installmentSql, new
        {
            request.TenantId,
            FeeAccountId = account.Id
        });

        var installments = installmentRows
            .Select(r => new FeeInstallmentMobileDto(r.Label, r.Amount, r.DueDate, r.IsPaid))
            .ToList();

        return new StudentFeesDto(account.DueAmount, account.PaidAmount, account.IsFullyPaid, installments);
    }

    private class FeeAccountRow
    {
        public Guid Id { get; set; }
        public decimal DueAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsFullyPaid { get; set; }
    }

    private class InstallmentRow
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }
    }
}
