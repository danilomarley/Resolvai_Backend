using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Domain.Entities;

namespace Resolvai.Application.Mappings;

public static class FornecedorMapper
{
    public static FornecedorResponse ToResponse(this Fornecedor fornecedor, IReadOnlyList<string> especialidades) => new(
        fornecedor.IdUsuario,
        fornecedor.TipoPessoa.ToString(),
        fornecedor.NomeExibicao,
        fornecedor.Cnpj,
        fornecedor.Bio,
        fornecedor.CidadeAtuacao,
        fornecedor.UfAtuacao,
        fornecedor.RaioAtendimentoKm,
        fornecedor.RegistroProfissional,
        especialidades,
        fornecedor.CreatedAt,
        fornecedor.UpdatedAt);
}
