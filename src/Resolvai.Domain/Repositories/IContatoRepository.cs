using Resolvai.Domain.Entities;

namespace Resolvai.Domain.Repositories;

public interface IContatoRepository
{
    Task AddAsync(Contato contato, CancellationToken cancellationToken = default);
}
