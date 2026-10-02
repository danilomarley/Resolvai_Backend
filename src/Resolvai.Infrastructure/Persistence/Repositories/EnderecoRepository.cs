using Dapper;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Repositories;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class EnderecoRepository(IDbConnectionFactory connectionFactory) : IEnderecoRepository
{
    public async Task AddAsync(Endereco endereco, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into enderecos
                (id, id_usuario, apelido, logradouro, numero, complemento, bairro, cidade, estado, cep, latitude, longitude, principal)
            values
                (@Id, @IdUsuario, @Apelido, @Logradouro, @Numero, @Complemento, @Bairro, @Cidade, @Estado, @Cep, @Latitude, @Longitude, @Principal);
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var parameters = new
        {
            endereco.Id,
            endereco.IdUsuario,
            endereco.Apelido,
            endereco.Logradouro,
            endereco.Numero,
            endereco.Complemento,
            endereco.Bairro,
            endereco.Cidade,
            endereco.Estado,
            endereco.Cep,
            endereco.Latitude,
            endereco.Longitude,
            endereco.Principal
        };

        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, commandTimeout: connectionFactory.CommandTimeoutSeconds, cancellationToken: cancellationToken));
    }
}
