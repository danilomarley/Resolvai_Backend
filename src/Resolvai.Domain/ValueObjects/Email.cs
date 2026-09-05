using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using Resolvai.Domain.Exceptions;

namespace Resolvai.Domain.ValueObjects;

/// <summary>
/// E-mail sempre normalizado em minúsculas, para casar com a coluna citext do PostgreSQL.
/// </summary>
public sealed record Email
{
    public const int MaxLength = 320;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? value)
    {
        if (!TryCreate(value, out var email))
        {
            throw new InvalidEmailException(value);
        }

        return email;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Email? email)
    {
        email = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength || !MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized)
        {
            return false;
        }

        email = new Email(normalized);
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
