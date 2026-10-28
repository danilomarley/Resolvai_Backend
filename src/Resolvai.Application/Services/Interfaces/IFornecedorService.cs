using Resolvai.Application.DTOs.Fornecedores;

namespace Resolvai.Application.Services.Interfaces;

public interface IFornecedorService
{
    Task<FornecedorResponse> RegisterAsync(
        RegisterFornecedorRequest request,
        CancellationToken cancellationToken = default);

    Task<FornecedorResponse> GetCurrentAsync(CancellationToken cancellationToken = default);
}
