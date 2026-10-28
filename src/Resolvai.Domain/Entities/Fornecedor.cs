using Resolvai.Domain.Common;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Exceptions;
using Resolvai.Domain.ValueObjects;

namespace Resolvai.Domain.Entities;

public sealed class Fornecedor : Entity<Guid>, IAggregateRoot
{
    private static readonly HashSet<string> UfsValidas =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    private Fornecedor(
        Guid idUsuario,
        TipoPessoa tipoPessoa,
        string nomeExibicao,
        string? cnpj,
        string? bio,
        string cidadeAtuacao,
        string ufAtuacao,
        int raioAtendimentoKm,
        string? registroProfissional,
        DateTime createdAt,
        DateTime? updatedAt)
        : base(idUsuario)
    {
        TipoPessoa = tipoPessoa;
        NomeExibicao = nomeExibicao;
        Cnpj = cnpj;
        Bio = bio;
        CidadeAtuacao = cidadeAtuacao;
        UfAtuacao = ufAtuacao;
        RaioAtendimentoKm = raioAtendimentoKm;
        RegistroProfissional = registroProfissional;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid IdUsuario => Id;

    public TipoPessoa TipoPessoa { get; }

    public string NomeExibicao { get; }

    public string? Cnpj { get; }

    public string? Bio { get; }

    public string CidadeAtuacao { get; }

    public string UfAtuacao { get; }

    public int RaioAtendimentoKm { get; }

    public string? RegistroProfissional { get; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; }

    /// <summary>
    /// Cria o perfil profissional vinculado a um usuário com papel Prestador.
    /// O CPF de pessoa física permanece na entidade User.
    /// </summary>
    public static Fornecedor Register(
        Guid idUsuario,
        TipoPessoa tipoPessoa,
        string nomeExibicao,
        string? cnpj,
        string cidadeAtuacao,
        string ufAtuacao,
        int raioAtendimentoKm = 20,
        string? bio = null,
        string? registroProfissional = null)
    {
        if (idUsuario == Guid.Empty)
        {
            throw new DomainException("O identificador do usuário é obrigatório.");
        }

        if (tipoPessoa is not (TipoPessoa.PF or TipoPessoa.PJ))
        {
            throw new DomainException("O tipo de pessoa deve ser PF ou PJ.");
        }

        if (string.IsNullOrWhiteSpace(nomeExibicao))
        {
            throw new DomainException("O nome de exibição é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(cidadeAtuacao))
        {
            throw new DomainException("A cidade de atuação é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(ufAtuacao))
        {
            throw new DomainException("A UF de atuação é obrigatória.");
        }

        var ufNormalizada = ufAtuacao.Trim().ToUpperInvariant();

        if (!UfsValidas.Contains(ufNormalizada))
        {
            throw new DomainException("UF de atuação inválida.");
        }

        if (raioAtendimentoKm is < 1 or > 200)
        {
            throw new DomainException("O raio de atendimento deve estar entre 1 e 200 km.");
        }

        var bioNormalizada = NormalizeOptional(bio);

        if (bioNormalizada?.Length > 360)
        {
            throw new DomainException("A bio pode ter até 360 caracteres.");
        }

        var registroNormalizado = NormalizeOptional(registroProfissional);

        if (registroNormalizado?.Length > 80)
        {
            throw new DomainException("O registro profissional pode ter até 80 caracteres.");
        }

        if (tipoPessoa == TipoPessoa.PF && !string.IsNullOrWhiteSpace(cnpj))
        {
            throw new DomainException("Pessoa física não pode informar CNPJ.");
        }

        string? cnpjNormalizado = null;

        if (tipoPessoa == TipoPessoa.PJ)
        {
            if (!DocumentoValidator.IsCnpjValido(cnpj))
            {
                throw new DomainException("CNPJ inválido.");
            }

            cnpjNormalizado = new string(cnpj!.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        }

        return new Fornecedor(
            idUsuario,
            tipoPessoa,
            nomeExibicao.Trim(),
            cnpjNormalizado,
            bioNormalizada,
            cidadeAtuacao.Trim(),
            ufNormalizada,
            raioAtendimentoKm,
            registroNormalizado,
            createdAt: DateTime.UtcNow,
            updatedAt: null);
    }

    /// <summary>
    /// Reidrata um fornecedor já persistido. Uso exclusivo da camada de persistência.
    /// </summary>
    public static Fornecedor Restore(
        Guid idUsuario,
        TipoPessoa tipoPessoa,
        string nomeExibicao,
        string? cnpj,
        string? bio,
        string cidadeAtuacao,
        string ufAtuacao,
        int raioAtendimentoKm,
        string? registroProfissional,
        DateTime createdAt,
        DateTime? updatedAt)
        => new(
            idUsuario,
            tipoPessoa,
            nomeExibicao,
            cnpj,
            bio,
            cidadeAtuacao,
            ufAtuacao,
            raioAtendimentoKm,
            registroProfissional,
            createdAt,
            updatedAt);

    public string? NormalizeCpf(string? cpf)
    {
        if (TipoPessoa == TipoPessoa.PF)
        {
            if (!DocumentoValidator.IsCpfValido(cpf))
            {
                throw new DomainException("CPF inválido.");
            }

            return new string(cpf!.Where(char.IsAsciiDigit).ToArray());
        }

        if (!string.IsNullOrWhiteSpace(cpf))
        {
            throw new DomainException("Pessoa jurídica não pode informar CPF.");
        }

        return null;
    }

    public void ValidateRegistration(User user, Contato contato)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(contato);

        if ((TipoPessoa == TipoPessoa.PF && !DocumentoValidator.IsCpfValido(user.Cpf))
            || (TipoPessoa == TipoPessoa.PJ && user.Cpf is not null))
        {
            throw new DomainException("Documento incompatível com o tipo de pessoa do fornecedor.");
        }

        if (!contato.Principal || contato.Tipo is not ("telefone" or "whatsapp")
            || !Telefone.TryCreate(contato.Valor, out var telefone) || telefone.Value != contato.Valor)
        {
            throw new DomainException("O fornecedor deve informar um telefone principal válido.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
