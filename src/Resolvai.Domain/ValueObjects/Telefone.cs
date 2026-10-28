using System.Diagnostics.CodeAnalysis;
using Resolvai.Domain.Exceptions;

namespace Resolvai.Domain.ValueObjects;

public sealed record Telefone
{
    private Telefone(string value) => Value = value;

    public string Value { get; }

    public static Telefone Create(string? value)
    {
        if (!TryCreate(value, out var telefone))
        {
            throw new DomainException("Telefone deve conter DDD e 10 ou 11 dígitos.");
        }

        return telefone;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out Telefone? telefone)
    {
        telefone = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = new string(value.Where(char.IsAsciiDigit).ToArray());

        if (normalized.Length is not (10 or 11)
            || value.Any(c => !char.IsAsciiDigit(c) && c is not (' ' or '(' or ')' or '-' or '+' or '.')))
        {
            return false;
        }

        telefone = new Telefone(normalized);
        return true;
    }
}
