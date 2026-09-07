namespace Resolvai.Application.Common.Exceptions;

public sealed class UnauthorizedException(string message = "Credenciais inválidas.")
    : Exception(message);
