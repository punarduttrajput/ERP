using ERP.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Auth.Application.Commands;

public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
