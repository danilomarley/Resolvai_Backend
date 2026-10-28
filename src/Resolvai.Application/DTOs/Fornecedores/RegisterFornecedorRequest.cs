using System.ComponentModel.DataAnnotations;

namespace Resolvai.Application.DTOs.Fornecedores;

public sealed record RegisterFornecedorRequest
{
    [Required]
    [MaxLength(200)]
    public required string Name { get; init; }

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public required string Email { get; init; }

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public required string Password { get; init; }

    [Required]
    [RegularExpression("^(PF|PJ)$")]
    public required string TipoPessoa { get; init; }

    [Required]
    [MaxLength(200)]
    public required string NomeExibicao { get; init; }

    [MaxLength(14)]
    public string? Cpf { get; init; }

    [MaxLength(18)]
    public string? Cnpj { get; init; }

    [Required]
    [MaxLength(20)]
    public required string Telefone { get; init; }

    [Required]
    [MaxLength(120)]
    public required string CidadeAtuacao { get; init; }

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public required string UfAtuacao { get; init; }

    [Range(1, 200)]
    public int RaioAtendimentoKm { get; init; } = 20;

    [MaxLength(360)]
    public string? Bio { get; init; }

    [MaxLength(80)]
    public string? RegistroProfissional { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required List<string> Especialidades { get; init; }
}
