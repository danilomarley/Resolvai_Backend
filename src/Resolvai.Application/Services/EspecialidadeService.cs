using Resolvai.Application.Contracts.Fornecedores;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Application.Services.Interfaces;

namespace Resolvai.Application.Services;

public sealed class EspecialidadeService(IEspecialidadeQueries especialidadeQueries) : IEspecialidadeService
{
    public Task<IReadOnlyList<EspecialidadeResponse>> ListActiveAsync(CancellationToken cancellationToken = default)
        => especialidadeQueries.ListActiveAsync(cancellationToken);
}
