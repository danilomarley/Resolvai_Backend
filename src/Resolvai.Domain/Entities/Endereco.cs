using Resolvai.Domain.Common;
using Resolvai.Domain.Exceptions;

namespace Resolvai.Domain.Entities;

public sealed class Endereco : Entity<Guid>
{
    private Endereco(
        Guid id,
        Guid idUsuario,
        string logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        string cidade,
        string estado,
        string cep,
        string? apelido,
        decimal? latitude,
        decimal? longitude,
        bool principal)
        : base(id)
    {
        IdUsuario = idUsuario;
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cidade = cidade;
        Estado = estado;
        Cep = cep;
        Apelido = apelido;
        Latitude = latitude;
        Longitude = longitude;
        Principal = principal;
    }

    public Guid IdUsuario { get; }

    public string Logradouro { get; }

    public string? Numero { get; }

    public string? Complemento { get; }

    public string? Bairro { get; }

    public string Cidade { get; }

    public string Estado { get; }

    public string Cep { get; }

    public string? Apelido { get; }

    public decimal? Latitude { get; }

    public decimal? Longitude { get; }

    public bool Principal { get; }

    public static Endereco Register(
        Guid idUsuario,
        string logradouro,
        string? numero,
        string? complemento,
        string? bairro,
        string cidade,
        string estado,
        string cep,
        string? apelido,
        decimal? latitude,
        decimal? longitude,
        bool principal)
    {
        if (string.IsNullOrWhiteSpace(logradouro))
        {
            throw new DomainException("O logradouro é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(cidade))
        {
            throw new DomainException("A cidade é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(estado))
        {
            throw new DomainException("O estado é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(cep))
        {
            throw new DomainException("O CEP é obrigatório.");
        }

        return new Endereco(
            Guid.NewGuid(),
            idUsuario,
            logradouro.Trim(),
            numero?.Trim(),
            complemento?.Trim(),
            bairro?.Trim(),
            cidade.Trim(),
            estado.Trim(),
            cep.Trim(),
            apelido?.Trim(),
            latitude,
            longitude,
            principal);
    }
}
