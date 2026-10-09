namespace Resolvai.Application.DTOs.Common;

public sealed record PaginaResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalItems,
    long TotalPages)
{
    public static PaginaResponse<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalItems)
        => new(items, page, pageSize, totalItems, (totalItems + pageSize - 1) / pageSize);
}
