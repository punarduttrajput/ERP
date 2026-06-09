using System.Data;

namespace ERP.Shared.Application.Abstractions;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateWriteConnectionAsync(CancellationToken cancellationToken = default);
    Task<IDbConnection> CreateReadConnectionAsync(CancellationToken cancellationToken = default);
}
