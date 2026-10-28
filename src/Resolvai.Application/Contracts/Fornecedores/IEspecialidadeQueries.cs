using Resolvai.Application.DTOs.Fornecedores;

namespace Resolvai.Application.Contracts.Fornecedores;

public interface IEspecialidadeQueries
{
    Task<IReadOnlyList<EspecialidadeResponse>> ListActiveAsync(CancellationToken cancellationToken = default);
}
