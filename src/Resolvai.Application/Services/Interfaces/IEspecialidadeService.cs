using Resolvai.Application.DTOs.Fornecedores;

namespace Resolvai.Application.Services.Interfaces;

public interface IEspecialidadeService
{
    Task<IReadOnlyList<EspecialidadeResponse>> ListActiveAsync(CancellationToken cancellationToken = default);
}
