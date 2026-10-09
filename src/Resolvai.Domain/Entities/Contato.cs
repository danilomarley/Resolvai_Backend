using Resolvai.Domain.Common;
using Resolvai.Domain.Exceptions;

namespace Resolvai.Domain.Entities;

public sealed class Contato : Entity<Guid>
{
    private static readonly string[] TiposValidos = ["telefone", "whatsapp", "email_alt"];

    private Contato(Guid id, Guid idUsuario, string tipo, string valor, bool principal)
        : base(id)
    {
        IdUsuario = idUsuario;
        Tipo = tipo;
        Valor = valor;
        Principal = principal;
    }

    public Guid IdUsuario { get; }

    public string Tipo { get; }

    public string Valor { get; }

    public bool Principal { get; }

    public static Contato Register(Guid idUsuario, string tipo, string valor, bool principal)
    {
        if (string.IsNullOrWhiteSpace(tipo) || !TiposValidos.Contains(tipo))
        {
            throw new DomainException("O tipo de contato deve ser 'telefone', 'whatsapp' ou 'email_alt'.");
        }

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("O valor do contato é obrigatório.");
        }

        return new Contato(Guid.NewGuid(), idUsuario, tipo, valor.Trim(), principal);
    }
}
