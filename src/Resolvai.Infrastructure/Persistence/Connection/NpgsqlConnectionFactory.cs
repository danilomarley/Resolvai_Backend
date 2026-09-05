using System.Data.Common;
using Microsoft.Extensions.Options;
using Npgsql;
using Resolvai.Infrastructure.Options;

namespace Resolvai.Infrastructure.Persistence.Connection;

/// <summary>
/// Registrada como singleton: o que vive por toda a aplicação é o <see cref="NpgsqlDataSource"/>
/// (que carrega o pool de conexões), nunca uma <see cref="NpgsqlConnection"/> aberta, já que
/// uma conexão não pode ser compartilhada entre requisições concorrentes.
/// </summary>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(IOptions<DatabaseOptions> options)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
        {
            throw new InvalidOperationException(
                "A connection string do PostgreSQL não foi configurada. " +
                "Defina Database:ConnectionString (user-secrets) ou ConnectionStrings__Postgres (variável de ambiente).");
        }

        CommandTimeoutSeconds = settings.CommandTimeoutSeconds;
        _dataSource = new NpgsqlDataSourceBuilder(settings.ConnectionString).Build();
    }

    public int CommandTimeoutSeconds { get; }

    public async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        => await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

    public ValueTask DisposeAsync() => _dataSource.DisposeAsync();
}
