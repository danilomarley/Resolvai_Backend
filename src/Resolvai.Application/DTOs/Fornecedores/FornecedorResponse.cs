namespace Resolvai.Application.DTOs.Fornecedores;

public sealed record FornecedorResponse(
    Guid IdUsuario,
    string TipoPessoa,
    string NomeExibicao,
    string? Cnpj,
    string? Bio,
    string CidadeAtuacao,
    string UfAtuacao,
    int RaioAtendimentoKm,
    string? RegistroProfissional,
    IReadOnlyList<string> Especialidades,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
