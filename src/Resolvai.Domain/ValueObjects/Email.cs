namespace Resolvai.Domain.ValueObjects;

/// <summary>
/// Armazena o e-mail normalizado. A Application valida a entrada antes de criar o objeto.
/// </summary>
public sealed record Email
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string value) => new(value.Trim().ToLowerInvariant());

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
