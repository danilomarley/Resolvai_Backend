namespace Resolvai.Domain.Exceptions;

public sealed class InvalidEmailException(string? value)
    : DomainException($"O e-mail informado é inválido: '{value}'.");
