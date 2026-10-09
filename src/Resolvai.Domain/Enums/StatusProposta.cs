namespace Resolvai.Domain.Enums;

/// <summary>
/// Persistido como texto na coluna propostas.status (enviada/visualizada/escolhida).
/// Os membros ficam em inglês porque são o valor exposto no JSON da API.
/// </summary>
public enum StatusProposta
{
    Sent,
    Viewed,
    Chosen
}
