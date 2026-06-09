using System.Data;
using ERP.Shared.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace ERP.Shared.Infrastructure;

public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _writeConnectionString;
    private readonly string _readConnectionString;

    public MySqlConnectionFactory(IConfiguration configuration)
    {
        _writeConnectionString = configuration.GetConnectionString("Write")
            ?? throw new InvalidOperationException("Write connection string is required.");
        // Fall back to Write if Read replica is not configured (e.g. local dev)
        _readConnectionString = configuration.GetConnectionString("Read")
            ?? _writeConnectionString;
    }

    public async Task<IDbConnection> CreateWriteConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_writeConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public async Task<IDbConnection> CreateReadConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_readConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
