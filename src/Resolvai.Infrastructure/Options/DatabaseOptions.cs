namespace Resolvai.Infrastructure.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Connection string do PostgreSQL do Supabase (Settings → Database).
    /// Nunca versionada: user-secrets em dev; Database__ConnectionString ou ConnectionStrings__Supabase nos demais ambientes.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = 30;
}
