using Resolvai.Domain.Entities;

namespace Resolvai.Domain.Repositories;

public interface IEnderecoRepository
{
    Task AddAsync(Endereco endereco, CancellationToken cancellationToken = default);
}
