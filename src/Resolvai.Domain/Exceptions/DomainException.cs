namespace Resolvai.Domain.Exceptions;

/// <summary>
/// Violação de uma regra ou invariante do domínio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
