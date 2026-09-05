namespace Resolvai.Application.Common.Exceptions;

public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' não foi encontrado.");
