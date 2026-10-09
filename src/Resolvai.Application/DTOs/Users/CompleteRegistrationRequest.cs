using System.ComponentModel.DataAnnotations;

namespace Resolvai.Application.DTOs.Users;

/// <summary>
/// Completa o cadastro do usuário autenticado no Supabase: CPF, endereço e contato.
/// </summary>
public sealed record CompleteRegistrationRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    [MaxLength(14)]
    [RegularExpression(@"^(\d{11}|\d{3}\.\d{3}\.\d{3}-\d{2})$", ErrorMessage = "CPF inválido.")]
    public required string Cpf { get; init; }

    [Required]
    public required EnderecoRequest Endereco { get; init; }

    [Required]
    public required ContatoRequest Contato { get; init; }
}

public sealed record EnderecoRequest
{
    [Required]
    [MaxLength(200)]
    public required string Logradouro { get; init; }

    [Required]
    [MaxLength(20)]
    public required string Numero { get; init; }

    [MaxLength(200)]
    public string? Complemento { get; init; }

    [Required]
    [MaxLength(120)]
    public required string Bairro { get; init; }

    [Required]
    [MaxLength(120)]
    public required string Cidade { get; init; }

    [Required]
    [MaxLength(2)]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "Estado inválido.")]
    public required string Estado { get; init; }

    [Required]
    [MaxLength(9)]
    [RegularExpression(@"^(\d{8}|\d{5}-\d{3})$", ErrorMessage = "CEP inválido.")]
    public required string Cep { get; init; }

    [MaxLength(60)]
    public string? Apelido { get; init; }

    public decimal? Latitude { get; init; }

    public decimal? Longitude { get; init; }
}

public sealed record ContatoRequest
{
    [Required]
    [RegularExpression("^(telefone|whatsapp|email_alt)$")]
    public required string Tipo { get; init; }

    [Required]
    [MaxLength(60)]
    public required string Valor { get; init; }
}
