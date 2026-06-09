using BCrypt.Net;
using ERP.Auth.Domain;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Users.Domain;
using ERP.Users.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Users.Application.Commands;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IUsersDbContext _db;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(IUsersDbContext db, ICurrentTenant currentTenant, ICurrentUser currentUser, ILogger<CreateUserHandler> logger)
    {
        _db = db;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (!_currentTenant.TenantId.HasValue)
            return Result<Guid>.Failure("Tenant context is required.");

        var tenantId = _currentTenant.TenantId.Value;
        var email = request.Email.Trim().ToLowerInvariant();

        // Check uniqueness against the canonical users table
        var emailTaken = await _db.Users
            .AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);

        if (emailTaken)
            return Result<Guid>.Failure($"Email '{email}' is already registered.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
        var userId = Guid.NewGuid();

        // 1. Auth identity record — owns email, password, lockout state
        var user = new User
        {
            Id         = userId,
            TenantId   = tenantId,
            Email      = email,
            PasswordHash = passwordHash,
            FirstName  = request.FirstName,
            LastName   = request.LastName,
            IsActive   = true,
            CreatedBy  = _currentUser.UserId
        };

        // 2. Extended profile record — same PK, owns profile fields
        var profile = new UserProfile
        {
            Id         = userId,
            TenantId   = tenantId,
            FirstName  = request.FirstName,
            LastName   = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Department = request.Department,
            JobTitle   = request.JobTitle,
            CreatedBy  = _currentUser.UserId
        };

        await _db.Users.AddAsync(user, cancellationToken);
        await _db.UserProfiles.AddAsync(profile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User created: {UserId} ({Email})", userId, email);
        return Result<Guid>.Success(userId);
    }
}
