using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Admissions.Application.Commands;

public sealed class SubmitApplicationHandler : IRequestHandler<SubmitApplicationCommand, Result<Guid>>
{
    private readonly IAdmissionsDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<SubmitApplicationHandler> _logger;

    public SubmitApplicationHandler(
        IAdmissionsDbContext db, ICurrentTenant currentTenant,
        ICurrentUser currentUser, ILogger<SubmitApplicationHandler> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result<Guid>.Failure("Tenant context required.");

        var tenantId = _currentTenant.TenantId.Value;

        var seat = await _db.SeatMatrices.FirstOrDefaultAsync(
            s => s.ProgramId == request.ProgramId
              && s.AcademicYear == request.AcademicYear
              && s.Category == request.Category,
            cancellationToken);

        if (seat is null)
            return Result<Guid>.Failure("No seat matrix found for this program, year, and category.");

        if (seat.AvailableSeats <= 0)
            return Result<Guid>.Failure("No seats available in this category.");

        var duplicate = await _db.Applications.AnyAsync(
            a => a.ApplicantEmail == request.ApplicantEmail.ToLowerInvariant()
              && a.ProgramId == request.ProgramId
              && a.AcademicYear == request.AcademicYear
              && a.State != ApplicationState.Withdrawn
              && a.State != ApplicationState.Rejected,
            cancellationToken);

        if (duplicate)
            return Result<Guid>.Failure("An active application already exists for this program and year.");

        var definition = await _db.WorkflowDefinitions
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (definition is null)
            return Result<Guid>.Failure("No active admission workflow definition found.");

        var applicationId = Guid.NewGuid();

        var application = new AdmissionApplication
        {
            Id = applicationId,
            TenantId = tenantId,
            ApplicantName = request.ApplicantName,
            ApplicantEmail = request.ApplicantEmail.ToLowerInvariant(),
            ApplicantMobile = request.ApplicantMobile,
            ProgramId = request.ProgramId,
            ProgramName = request.ProgramName,
            Category = request.Category,
            AcademicYear = request.AcademicYear,
            WorkflowDefinitionId = definition.Id,
            WorkflowDefinitionVersion = definition.Version,
            CreatedBy = _currentUser.UserId
        };

        application.Transition(ApplicationState.Submitted, _currentUser.UserId ?? Guid.Empty, "Application submitted");

        foreach (var doc in request.Documents)
        {
            application.Documents.Add(new ApplicationDocument
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ApplicationId = applicationId,
                DocumentType = doc.DocumentType,
                BlobUrl = doc.BlobUrl,
                FileName = doc.FileName
            });
        }

        await _db.Applications.AddAsync(application, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Application {Id} submitted for program {ProgramId}", applicationId, request.ProgramId);
        return Result<Guid>.Success(applicationId);
    }
}
