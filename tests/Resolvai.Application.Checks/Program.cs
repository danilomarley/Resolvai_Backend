using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Orders;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Home;
using Resolvai.Application.DTOs.Orders;
using Resolvai.Application.Services;
using Resolvai.Domain.Enums;

var userId = Guid.NewGuid();
var orderId = Guid.NewGuid();
var queries = new FakeQueries();
using var cancellation = new CancellationTokenSource();
var token = cancellation.Token;
var service = new OrderService(queries, new FakeUser(true, userId));

queries.Order = new(orderId, "Pedido", "Descrição", OrderStatus.Pending, DateTime.UtcNow, null);
var order = await service.GetByIdAsync(orderId, token);
Check(order == queries.Order && queries.LastOrderId == orderId, "Retorna o pedido consultado");
Check(queries.LastUserId == userId && queries.LastToken == token, "Encaminha usuário do token e cancelamento");

queries.Order = null; // O repositório oculta pedidos inexistentes ou de outro usuário.
await Throws<NotFoundException>(() => service.GetByIdAsync(orderId));
var summary = await service.GetSummaryAsync(token);
Check(summary.Orders.Total == 0 && summary.RecentOrders.Count == 0, "Home sem pedidos");
Check(queries.LastUserId == userId && queries.LastToken == token, "Resumo usa usuário do token e cancelamento");

foreach (var user in new[] { new FakeUser(false, userId), new FakeUser(true, null), new FakeUser(true, Guid.Empty) })
{
    var callsBefore = queries.Calls;
    var deniedService = new OrderService(queries, user);
    await Throws<UnauthorizedException>(() => deniedService.GetByIdAsync(orderId));
    await Throws<UnauthorizedException>(() => deniedService.GetSummaryAsync());
    Check(queries.Calls == callsBefore, "Não consulta banco sem identidade válida");
}

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

sealed record FakeUser(bool IsAuthenticated, Guid? Id) : ICurrentUser
{
    public string? Email => null;
    public UserRole? Role => null;
}

sealed class FakeQueries : IOrderQueries
{
    public OrderResponse? Order { get; set; }
    public Guid LastOrderId { get; private set; }
    public Guid LastUserId { get; private set; }
    public CancellationToken LastToken { get; private set; }
    public int Calls { get; private set; }

    public Task<OrderResponse?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        LastOrderId = id;
        RecordCall(userId, cancellationToken);
        return Task.FromResult(Order);
    }

    public Task<HomeSummaryResponse> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        RecordCall(userId, cancellationToken);
        return Task.FromResult(new HomeSummaryResponse(new(0, 0, 0, 0, 0), []));
    }

    private void RecordCall(Guid userId, CancellationToken token)
    {
        Calls++;
        LastUserId = userId;
        LastToken = token;
    }
}
