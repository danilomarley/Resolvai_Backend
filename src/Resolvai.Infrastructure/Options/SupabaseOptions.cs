namespace Resolvai.Infrastructure.Options;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    /// <summary>
    /// URL do projeto, ex.: https://xxxx.supabase.co
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// anon/public key — login e signup públicos.
    /// </summary>
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>
    /// service_role key — criação administrativa de usuários. Nunca exponha no cliente.
    /// </summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT Secret do projeto (Settings → API). Usado para validar access tokens.
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>
    /// Audience padrão dos tokens do GoTrue. Em geral: authenticated.
    /// </summary>
    public string Audience { get; set; } = "authenticated";

    public string AuthIssuer => $"{Url.TrimEnd('/')}/auth/v1";
}
