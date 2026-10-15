using System.Text.Json;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Pedidos;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Application.Services.Interfaces;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Exceptions;
using Resolvai.Domain.Repositories;

namespace Resolvai.Application.Services;

public sealed class PedidoService(
    IPedidoQueries pedidos,
    IPedidoRepository pedidoRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser) : IPedidoService
{
    public async Task<PedidoResponse> CriarAsync(CriarPedidoRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Cpf))
        {
            throw new DomainException("Finalize o cadastro antes de criar um pedido.");
        }

        if (user.Role != UserRole.Cliente)
        {
            throw new DomainException("Apenas clientes podem criar pedidos.");
        }

        var id = Guid.NewGuid();
        await pedidoRepository.AddAsync(id, userId, MontarEscopo(request), request.Fotos, cancellationToken);

        return await pedidos.GetByIdAsync(id, userId, cancellationToken)
            ?? throw new NotFoundException("Pedido", id);
    }

    // Os limites de página são validados no binding da rota (DataAnnotations de ListarPedidosQuery).
    public Task<PaginaResponse<PedidoResumoResponse>> ListAsync(ListarPedidosQuery query, CancellationToken cancellationToken = default)
        => pedidos.ListAsync(GetUserId(), query.Status, query.Page, query.PageSize, cancellationToken);

    public async Task<PedidoResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await pedidos.GetByIdAsync(id, GetUserId(), cancellationToken)
            ?? throw new NotFoundException("Pedido", id);

    public Task<ResumoHomeResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        => pedidos.GetSummaryAsync(GetUserId(), cancellationToken);

    private static string MontarEscopo(CriarPedidoRequest request)
    {
        var escopo = new Dictionary<string, string>();

        void Adicionar(string chave, string? valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                escopo[chave] = valor.Trim();
            }
        }

        Adicionar("titulo", request.Titulo);
        Adicionar("categoria", request.Categoria);
        Adicionar("descricao", request.Descricao);
        Adicionar("localizacao", request.Localizacao);
        Adicionar("detalhes", request.Detalhes);
        Adicionar("urgencia", request.Urgencia);
        Adicionar("especificacoes", request.Especificacoes);

        return JsonSerializer.Serialize(escopo);
    }

    private Guid GetUserId()
        => currentUser.IsAuthenticated && currentUser.Id is Guid id && id != Guid.Empty
            ? id
            : throw new UnauthorizedException("Token sem identificação de usuário.");
}
