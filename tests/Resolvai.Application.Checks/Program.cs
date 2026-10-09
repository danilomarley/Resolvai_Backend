using System.ComponentModel.DataAnnotations;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Pedidos;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Common;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Pedidos;
using Resolvai.Application.Services;
using Resolvai.Domain.Enums;

var userId = Guid.NewGuid();
var pedidoId = Guid.NewGuid();
var queries = new FakeQueries();
using var cancellation = new CancellationTokenSource();
var token = cancellation.Token;
var service = new PedidoService(queries, new FakeUser(true, userId));

queries.Pedido = new(pedidoId, "Pedido", null, "Descrição", null, StatusPedido.Pending, DateTime.UtcNow, null, null, [], []);
var pedido = await service.GetByIdAsync(pedidoId, token);
Check(pedido == queries.Pedido && queries.LastPedidoId == pedidoId, "Retorna o pedido consultado");
Check(queries.LastUserId == userId && queries.LastToken == token, "Encaminha usuário do token e cancelamento");

queries.Pedido = null; // O repositório oculta pedidos inexistentes ou de outro usuário.
await Throws<NotFoundException>(() => service.GetByIdAsync(pedidoId));
var resumo = await service.GetSummaryAsync(token);
Check(resumo.Orders.Total == 0 && resumo.Proposals.Total == 0 && resumo.RecentOrders.Count == 0, "Home sem pedidos");
Check(queries.LastUserId == userId && queries.LastToken == token, "Resumo usa usuário do token e cancelamento");

var pagina = await service.ListAsync(new ListarPedidosQuery { Status = StatusPedido.InProgress, Page = 3, PageSize = 20 }, token);
Check(pagina == queries.Pagina, "Retorna a página consultada");
Check(queries.LastStatus == StatusPedido.InProgress && queries.LastPage == 3 && queries.LastPageSize == 20,
    "Encaminha filtro e paginação");
Check(queries.LastUserId == userId && queries.LastToken == token, "Lista usa usuário do token e cancelamento");
await service.ListAsync(new ListarPedidosQuery());
Check(queries.LastStatus is null && queries.LastPage == 1 && queries.LastPageSize == 10, "Lista usa padrões sem filtro");

foreach (var user in new[] { new FakeUser(false, userId), new FakeUser(true, null), new FakeUser(true, Guid.Empty) })
{
    var callsBefore = queries.Calls;
    var deniedService = new PedidoService(queries, user);
    await Throws<UnauthorizedException>(() => deniedService.GetByIdAsync(pedidoId));
    await Throws<UnauthorizedException>(() => deniedService.GetSummaryAsync());
    await Throws<UnauthorizedException>(() => deniedService.ListAsync(new ListarPedidosQuery()));
    Check(queries.Calls == callsBefore, "Não consulta banco sem identidade válida");
}

Check(IsValid(new ListarPedidosQuery()), "Parâmetros padrão da lista são válidos");
Check(IsValid(new ListarPedidosQuery { Page = int.MaxValue, PageSize = ListarPedidosQuery.MaxPageSize }), "Aceita limites máximos");
Check(!IsValid(new ListarPedidosQuery { Page = 0 }), "Rejeita página zero");
Check(!IsValid(new ListarPedidosQuery { PageSize = 0 }), "Rejeita tamanho de página zero");
Check(!IsValid(new ListarPedidosQuery { PageSize = ListarPedidosQuery.MaxPageSize + 1 }), "Rejeita página acima do limite");
Check(!IsValid(new ListarPedidosQuery { Status = (StatusPedido)99 }), "Rejeita status inexistente");

Check(PaginaResponse<int>.Create([], 1, 10, 0).TotalPages == 0, "Sem itens não há páginas");
Check(PaginaResponse<int>.Create([], 1, 10, 20).TotalPages == 2, "Total múltiplo do tamanho da página");
Check(PaginaResponse<int>.Create([], 1, 10, 21).TotalPages == 3, "Página parcial conta como página");

Console.WriteLine("Todos os testes de serviço passaram.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new Exception($"Era esperada a exceção {typeof(T).Name}.");
}

static bool IsValid(object value)
    => Validator.TryValidateObject(value, new ValidationContext(value), null, validateAllProperties: true);

sealed record FakeUser(bool IsAuthenticated, Guid? Id) : ICurrentUser
{
    public string? Email => null;
    public UserRole? Role => null;
}

sealed class FakeQueries : IPedidoQueries
{
    public PedidoResponse? Pedido { get; set; }
    public PaginaResponse<PedidoResumoResponse> Pagina { get; } = PaginaResponse<PedidoResumoResponse>.Create([], 1, 10, 0);
    public Guid LastPedidoId { get; private set; }
    public StatusPedido? LastStatus { get; private set; }
    public int LastPage { get; private set; }
    public int LastPageSize { get; private set; }
    public Guid LastUserId { get; private set; }
    public CancellationToken LastToken { get; private set; }
    public int Calls { get; private set; }

    public Task<PaginaResponse<PedidoResumoResponse>> ListAsync(
        Guid userId, StatusPedido? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        (LastStatus, LastPage, LastPageSize) = (status, page, pageSize);
        RecordCall(userId, cancellationToken);
        return Task.FromResult(Pagina);
    }

    public Task<PedidoResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        LastPedidoId = id;
        RecordCall(userId, cancellationToken);
        return Task.FromResult(Pedido);
    }

    public Task<ResumoHomeResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RecordCall(userId, cancellationToken);
        return Task.FromResult(new ResumoHomeResponse(new(0, 0, 0, 0, 0, 0, 0), new(0, 0, 0), []));
    }

    private void RecordCall(Guid userId, CancellationToken token)
    {
        Calls++;
        LastUserId = userId;
        LastToken = token;
    }
}
