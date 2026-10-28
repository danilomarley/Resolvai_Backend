using Resolvai.Domain.Entities;

namespace Resolvai.Domain.Repositories;

public interface IFornecedorRepository
{
    Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persiste usuário, perfil profissional, contato e especialidades numa única transação.
    /// A criação da conta no Auth é responsabilidade do serviço de aplicação.
    /// </summary>
    Task AddCompleteAsync(
        User user,
        Fornecedor fornecedor,
        Contato contato,
        IReadOnlyList<string> especialidades,
        CancellationToken cancellationToken = default);

    Task<Fornecedor?> GetByUsuarioIdAsync(Guid idUsuario, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetEspecialidadesAsync(Guid idUsuario, CancellationToken cancellationToken = default);
}
