using System.Data.Common;

namespace Resolvai.Infrastructure.Persistence.Connection;

/// <summary>
/// Entrega conexões abertas e de vida curta. Quem chama é dono do descarte.
/// </summary>
public interface IDbConnectionFactory
{
    int CommandTimeoutSeconds { get; }

    Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
