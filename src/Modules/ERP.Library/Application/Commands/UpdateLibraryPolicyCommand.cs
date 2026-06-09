using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Commands;

public record UpdateLibraryPolicyCommand(
    MemberType MemberType,
    int MaxBooksAllowed,
    int LoanPeriodDays,
    decimal FinePerDay,
    int MaxRenewals,
    int GracePeriodDays,
    Guid TenantId
) : IRequest<Result>;

public class UpdateLibraryPolicyCommandHandler : IRequestHandler<UpdateLibraryPolicyCommand, Result>
{
    private readonly ILibraryDbContext _db;

    public UpdateLibraryPolicyCommandHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateLibraryPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _db.LibraryPolicies
            .FirstOrDefaultAsync(x => x.MemberType == request.MemberType, cancellationToken);

        if (policy is null)
        {
            policy = new LibraryPolicy
            {
                TenantId = request.TenantId,
                MemberType = request.MemberType
            };
            _db.LibraryPolicies.Add(policy);
        }

        policy.MaxBooksAllowed = request.MaxBooksAllowed;
        policy.LoanPeriodDays = request.LoanPeriodDays;
        policy.FinePerDay = request.FinePerDay;
        policy.MaxRenewals = request.MaxRenewals;
        policy.GracePeriodDays = request.GracePeriodDays;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
