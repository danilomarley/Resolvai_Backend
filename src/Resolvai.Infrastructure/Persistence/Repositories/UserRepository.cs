using Dapper;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;
using Resolvai.Infrastructure.Persistence.Connection;

namespace Resolvai.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(IDbConnectionFactory connectionFactory) : IUserRepository
{
    private const string SelectColumns = """
        select id         as "Id",
               name       as "Name",
               email      as "EmailAddress",
               role       as "Role",
               is_active  as "IsActive",
               created_at as "CreatedAt",
               updated_at as "UpdatedAt"
        from users
        """;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(
            cancellationToken
        );

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            Command($"{SelectColumns} where id = @Id;", new { Id = id }, cancellationToken)
        );

        return row?.ToDomain();
    }

    public async Task<User?> GetByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(
            cancellationToken
        );

        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(
            Command(
                $"{SelectColumns} where email = @Email;",
                new { Email = email },
                cancellationToken
            )
        );

        return row?.ToDomain();
    }

    public async Task<bool> ExistsByEmailAsync(
        Email email,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(
            cancellationToken
        );

        return await connection.ExecuteScalarAsync<bool>(
            Command(
                "select exists (select 1 from users where email = @Email);",
                new { Email = email },
                cancellationToken
            )
        );
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(
            cancellationToken
        );

        var rows = await connection.QueryAsync<UserRow>(
            Command(
                $"{SelectColumns} order by created_at desc;",
                parameters: null,
                cancellationToken
            )
        );

        return rows.Select(row => row.ToDomain()).ToList();
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            insert into users (id, name, email, role, is_active, created_at, updated_at)
            values (@Id, @Name, @Email, @Role, @IsActive, @CreatedAt, @UpdatedAt);
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(
            cancellationToken
        );

        await connection.ExecuteAsync(Command(sql, ToParameters(user), cancellationToken));
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = """
            update users
               set name       = @Name,
                   email      = @Email,
                   role       = @Role,
                   is_active  = @IsActive,
                   updated_at = @UpdatedAt
             where id = @Id;
            """;

        await using var connection = await connectionFactory.CreateConnectionAsync(
            cancellationToken
        );

        await connection.ExecuteAsync(Command(sql, ToParameters(user), cancellationToken));
    }

    private CommandDefinition Command(
        string sql,
        object? parameters,
        CancellationToken cancellationToken
    ) =>
        new(
            sql,
            parameters,
            commandTimeout: connectionFactory.CommandTimeoutSeconds,
            cancellationToken: cancellationToken
        );

    private static object ToParameters(User user) =>
        new
        {
            user.Id,
            user.Name,
            user.Email,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
        };

    private sealed record UserRow(
        Guid Id,
        string Name,
        string EmailAddress,
        string Role,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
    )
    {
        public User ToDomain() =>
            User.Restore(
                Id,
                Name,
                Email.Create(EmailAddress),
                Enum.Parse<UserRole>(Role, ignoreCase: true),
                IsActive,
                CreatedAt,
                UpdatedAt
            );
    }
}
