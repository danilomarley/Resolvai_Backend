using System.ComponentModel.DataAnnotations;

namespace Resolvai.Application.DTOs.Pedidos;

public sealed record CriarPedidoRequest
{
    [MaxLength(90)]
    public string? Titulo { get; init; }

    [Required]
    [MaxLength(80)]
    public required string Categoria { get; init; }

    [Required]
    [MinLength(10)]
    [MaxLength(2000)]
    public required string Descricao { get; init; }

    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public required string Localizacao { get; init; }

    [MaxLength(2000)]
    public string? Detalhes { get; init; }

    [MaxLength(40)]
    public string? Urgencia { get; init; }

    [MaxLength(2000)]
    public string? Especificacoes { get; init; }

    [MaxLength(4)]
    public IReadOnlyList<NovaFotoPedidoRequest> Fotos { get; init; } = [];
}

public sealed record NovaFotoPedidoRequest
{
    [Required]
    [Url]
    [MaxLength(2048)]
    public required string Url { get; init; }

    [MaxLength(200)]
    public string? Descricao { get; init; }
}
