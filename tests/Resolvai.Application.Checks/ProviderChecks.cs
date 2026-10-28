using System.ComponentModel.DataAnnotations;
using Resolvai.Application.Common.Exceptions;
using Resolvai.Application.Contracts.Fornecedores;
using Resolvai.Application.Contracts.Security;
using Resolvai.Application.DTOs.Auth;
using Resolvai.Application.DTOs.Fornecedores;
using Resolvai.Application.DTOs.Users;
using Resolvai.Application.Services;
using Resolvai.Domain.Entities;
using Resolvai.Domain.Enums;
using Resolvai.Domain.Exceptions;
using Resolvai.Domain.Repositories;
using Resolvai.Domain.ValueObjects;

internal static class ProviderChecks
{
    // Vetores sintéticos de validação; não representam cadastros reais.
    private const string CpfDeTeste = "12345678909";
    private const string CnpjDeTeste = "12ABC34501DE35";

    public static async Task RunAsync()
    {
        CheckDocumentsAndEntity();
        CheckProviderDomainValidation();
        CheckRequestValidation();
        await CheckRegistrationAsync();
        await CheckRejectedRegistrationsAsync();
        await CheckOwnerAccessAsync();
        await CheckCancellationAndRetryAsync();
        await CheckClientCompatibilityAsync();
        await CheckClientCompletionAsync();
    }

    private static void CheckDocumentsAndEntity()
    {
        Check(DocumentoValidator.IsCpfValido(CpfDeTeste), "Aceita CPF sintético com dígitos verificadores válidos");
        Check(DocumentoValidator.IsCpfValido("123.456.789-09"), "Aceita máscara de CPF");
        Check(!DocumentoValidator.IsCpfValido("12345678908"), "Rejeita CPF com dígito verificador incorreto");
        Check(!DocumentoValidator.IsCpfValido("11111111111"), "Rejeita sequência repetida no CPF");
        Check(!DocumentoValidator.IsCpfValido(null), "Rejeita CPF ausente");
        Check(!DocumentoValidator.IsCpfValido("\u0661\u0662\u0663\u0664\u0665\u0666\u0667\u0668\u0669" + "09"),
            "Rejeita CPF com algarismos fora de ASCII");
        Check(!DocumentoValidator.IsCpfValido("abc" + CpfDeTeste), "Não descarta letras para aceitar um CPF inválido");
        Check(DocumentoValidator.IsCnpjValido(CnpjDeTeste), "Aceita vetor sintético de CNPJ alfanumérico");
        Check(DocumentoValidator.IsCnpjValido("12.abc.345/01de-35"), "Aceita máscara e normalização de CNPJ");
        Check(DocumentoValidator.IsCnpjValido("11222333000181"), "Mantém CNPJ numérico compatível");
        Check(!DocumentoValidator.IsCnpjValido("12ABC34501DE34"), "Rejeita dígito verificador incorreto no CNPJ");
        Check(!DocumentoValidator.IsCnpjValido("12ABC34501DE3X"), "CNPJ exige dígitos verificadores numéricos");
        Check(!DocumentoValidator.IsCnpjValido("00000000000000"), "Rejeita sequência repetida no CNPJ");
        Check(!DocumentoValidator.IsCnpjValido(null), "Rejeita CNPJ ausente");

        var id = Guid.NewGuid();
        var pf = Fornecedor.Register(id, TipoPessoa.PF, "  Perfil de teste  ", null, " Cidade de teste ", " ce ");
        Check(pf.Id == id && pf.IdUsuario == id, "Fornecedor usa a identidade do usuário");
        Check(pf.NomeExibicao == "Perfil de teste" && pf.CidadeAtuacao == "Cidade de teste" && pf.UfAtuacao == "CE",
            "Normaliza dados profissionais");
        Check(pf.Cnpj is null && pf.RaioAtendimentoKm == 20 && pf.UpdatedAt is null, "PF usa raio padrão e CNPJ nulo");
        var pj = Fornecedor.Register(id, TipoPessoa.PJ, "Perfil de teste", "12.abc.345/01de-35", "Cidade de teste", "CE",
            bio: "  ", registroProfissional: "  ");
        Check(pj.Cnpj == CnpjDeTeste && pj.Bio is null && pj.RegistroProfissional is null,
            "Normaliza CNPJ e campos opcionais em branco");

        Throws<DomainException>(() => Fornecedor.Register(Guid.Empty, TipoPessoa.PF, "Nome", null, "Cidade", "CE"));
        Throws<DomainException>(() => Fornecedor.Register(id, (TipoPessoa)99, "Nome", null, "Cidade", "CE"));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, " ", null, "Cidade", "CE"));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, " ", "CE"));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "XX"));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "CE", 0));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "CE", 201));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "CE", bio: new string('x', 361)));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "CE", registroProfissional: new string('x', 81)));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PF, "Nome", "---", "Cidade", "CE"));
        Throws<DomainException>(() => Fornecedor.Register(id, TipoPessoa.PJ, "Nome", null, "Cidade", "CE"));
        _ = Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "CE", 1, new string('x', 360), new string('x', 80));
        _ = Fornecedor.Register(id, TipoPessoa.PF, "Nome", null, "Cidade", "CE", 200);

        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var restored = Fornecedor.Restore(id, TipoPessoa.PF, "Persistido", null, null, "Cidade", "CE", 50, null, createdAt, updatedAt);
        Check(restored.CreatedAt == createdAt && restored.UpdatedAt == updatedAt && restored.RaioAtendimentoKm == 50,
            "Restore preserva estado e datas persistidos");
    }

    private static void CheckProviderDomainValidation()
    {
        var request = Request();
        var id = Guid.NewGuid();
        var pf = Fornecedor.Register(id, TipoPessoa.PF, request.NomeExibicao, null,
            request.CidadeAtuacao, request.UfAtuacao);
        var pj = Fornecedor.Register(id, TipoPessoa.PJ, request.NomeExibicao, CnpjDeTeste,
            request.CidadeAtuacao, request.UfAtuacao);
        var phoneDigits = new string(request.Telefone.Where(char.IsAsciiDigit).ToArray());
        var phoneMask = $"({phoneDigits[..2]}) {phoneDigits[2..^4]}-{phoneDigits[^4..]}";
        var cpfMask = $"{CpfDeTeste[..3]}.{CpfDeTeste[3..6]}.{CpfDeTeste[6..9]}-{CpfDeTeste[9..]}";

        Check(Telefone.Create(phoneMask).Value == phoneDigits, "Domínio normaliza a máscara de telefone na entrada");
        Check(!Telefone.TryCreate("\t" + phoneMask, out _), "Domínio não descarta caracteres de controle do telefone");
        Check(pf.NormalizeCpf(cpfMask) == CpfDeTeste, "Domínio normaliza CPF válido sem perder a validação original");
        Throws<DomainException>(() => pf.NormalizeCpf("abc" + CpfDeTeste));
        Check(pj.NormalizeCpf(" \t ") is null, "Entrada PJ sem CPF é normalizada para nulo");
        Throws<DomainException>(() => pj.NormalizeCpf(CpfDeTeste));

        var user = User.Register(id, request.Name, Email.Create(request.Email), UserRole.Prestador);
        var contact = Contato.Register(id, "whatsapp", phoneDigits, principal: true);
        Throws<DomainException>(() => pf.ValidateRegistration(user, contact));
        pj.ValidateRegistration(user, contact);
        user.CompleteRegistration(CpfDeTeste);
        pf.ValidateRegistration(user, contact);
        Throws<DomainException>(() => pj.ValidateRegistration(user, contact));

        var invalidCpf = CpfDeTeste[..^1] + (CpfDeTeste[^1] == '0' ? '1' : '0');
        var invalidUser = User.Restore(id, request.Name, user.Email, UserRole.Prestador,
            true, user.CreatedAt, null, invalidCpf);
        Throws<DomainException>(() => pf.ValidateRegistration(invalidUser, contact));
        var emptyCpfUser = User.Restore(id, request.Name, user.Email, UserRole.Prestador,
            true, user.CreatedAt, null, string.Empty);
        Throws<DomainException>(() => pj.ValidateRegistration(emptyCpfUser, contact));

        foreach (var value in new[] { phoneMask, "x" + phoneDigits, "\u0661" + phoneDigits[1..] })
        {
            var invalidContact = Contato.Register(id, "whatsapp", value, principal: true);
            Throws<DomainException>(() => pf.ValidateRegistration(user, invalidContact));
        }

        Throws<DomainException>(() => pf.ValidateRegistration(user,
            Contato.Register(id, "whatsapp", phoneDigits, principal: false)));
    }

    private static void CheckRequestValidation()
    {
        Check(IsValid(Request()), "DTO aceita cadastro PF completo");
        Check(!IsValid(Request() with { Password = "curta" }), "DTO rejeita senha curta");
        Check(!IsValid(Request() with { TipoPessoa = "Admin" }), "DTO rejeita tipo de pessoa desconhecido");
        Check(!IsValid(Request() with { RaioAtendimentoKm = 201 }), "DTO rejeita raio acima do limite");
        Check(!IsValid(Request() with { Especialidades = [] }), "DTO exige ao menos uma especialidade");
        Check(!IsValid(Request() with { Cnpj = new string('x', 19) }), "DTO limita tamanho do CNPJ recebido");
    }

    private static async Task CheckRegistrationAsync()
    {
        var fixture = new Fixture();
        var response = await fixture.Service.RegisterAsync(Request() with { Especialidades = ["hidraulica", "hidraulica"] });
        Check(response.IdUsuario == fixture.Auth.Id && response.TipoPessoa == "PF", "Cadastro retorna o perfil criado");
        Check(fixture.Providers.SavedUser?.Role == UserRole.Prestador && fixture.Providers.SavedUser.Cpf == CpfDeTeste,
            "PF persiste usuário Prestador com CPF normalizado");
        Check(fixture.Providers.SavedContact?.IdUsuario == fixture.Auth.Id && fixture.Providers.SavedContact.Principal,
            "Telefone principal pertence ao usuário criado");
        Check(fixture.Providers.SavedSpecialties.SequenceEqual(["hidraulica"]), "Remove especialidades repetidas antes de persistir");
        Check(fixture.Users.LastCpfRole == UserRole.Prestador, "Consulta duplicidade de CPF no papel Prestador");

        fixture = new Fixture();
        response = await fixture.Service.RegisterAsync(Request() with { TipoPessoa = "PJ", Cpf = null, Cnpj = CnpjDeTeste });
        Check(response.TipoPessoa == "PJ" && response.Cnpj == CnpjDeTeste && fixture.Providers.SavedUser?.Cpf is null,
            "PJ persiste CNPJ profissional e deixa CPF do usuário nulo");
        Check(fixture.Users.CpfCalls == 0, "PJ não consulta CPF inexistente");

        fixture = new Fixture();
        var phoneDigits = new string(Request().Telefone.Where(char.IsAsciiDigit).ToArray());
        var phoneMask = $"({phoneDigits[..2]}) {phoneDigits[2..^4]}-{phoneDigits[^4..]}";
        await fixture.Service.RegisterAsync(Request() with { Telefone = phoneMask });
        Check(fixture.Providers.SavedContact?.Valor == phoneDigits,
            "Cadastro aceita máscara na entrada e persiste somente os dígitos do telefone");
    }

    private static async Task CheckRejectedRegistrationsAsync()
    {
        var invalid = new[]
        {
            Request() with { Name = " " },
            Request() with { Email = "invalido" },
            Request() with { Password = "curta" },
            Request() with { TipoPessoa = "Outro" },
            Request() with { Cpf = "12345678908" },
            Request() with { Cnpj = CnpjDeTeste },
            Request() with { TipoPessoa = "PJ", Cpf = null, Cnpj = null },
            Request() with { TipoPessoa = "PJ", Cnpj = CnpjDeTeste },
            Request() with { Telefone = "123" },
            Request() with { Telefone = "\t" + Request().Telefone },
            Request() with { Telefone = "x" + Request().Telefone },
            Request() with { Telefone = "\u0661" + Request().Telefone[1..] },
            Request() with { CidadeAtuacao = " " },
            Request() with { UfAtuacao = "XX" },
            Request() with { RaioAtendimentoKm = 201 },
            Request() with { Bio = new string('x', 361) },
            Request() with { RegistroProfissional = new string('x', 81) },
            Request() with { Especialidades = [] },
            Request() with { Especialidades = ["inativa"] },
            Request() with { Especialidades = [null!] }
        };

        foreach (var request in invalid)
        {
            var fixture = new Fixture();
            await ThrowsAsync<DomainException>(() => fixture.Service.RegisterAsync(request));
            Check(fixture.Auth.SignUpCalls == 0 && fixture.Providers.AddCalls == 0,
                "Dados inválidos não criam conta externa nem perfil local");
        }

        var duplicate = new Fixture();
        duplicate.Users.EmailExists = true;
        await ThrowsAsync<ConflictException>(() => duplicate.Service.RegisterAsync(Request()));
        Check(duplicate.Auth.SignUpCalls == 0, "E-mail local duplicado é rejeitado antes do Auth");

        duplicate = new Fixture();
        duplicate.Users.CpfExists = true;
        await ThrowsAsync<ConflictException>(() => duplicate.Service.RegisterAsync(Request()));
        Check(duplicate.Auth.SignUpCalls == 0 && duplicate.Users.LastCpfRole == UserRole.Prestador,
            "CPF repetido no mesmo papel é rejeitado antes do Auth");

        duplicate = new Fixture();
        duplicate.Providers.CnpjExists = true;
        await ThrowsAsync<ConflictException>(() => duplicate.Service.RegisterAsync(Request() with { TipoPessoa = "PJ", Cpf = null, Cnpj = CnpjDeTeste }));
        Check(duplicate.Auth.SignUpCalls == 0, "CNPJ repetido é rejeitado antes do Auth");
    }

    private static async Task CheckOwnerAccessAsync()
    {
        var id = Guid.NewGuid();
        foreach (var current in new[]
        {
            new Current(false, id, UserRole.Prestador),
            new Current(true, null, UserRole.Prestador),
            new Current(true, Guid.Empty, UserRole.Prestador),
            new Current(true, id, UserRole.Cliente),
            new Current(true, id, null)
        })
        {
            var fixture = new Fixture(current);
            await ThrowsAsync<UnauthorizedException>(() => fixture.Service.GetCurrentAsync());
            Check(fixture.Providers.ReadCalls == 0, "Sem identidade Prestador válida não lê perfil profissional");
        }

        var owner = new Fixture(new Current(true, id, UserRole.Prestador));
        owner.Users.LocalUser = User.Register(id, "Prestador de teste", Email.Create("prestador@example.invalid"), UserRole.Prestador);
        owner.Providers.Stored = Fornecedor.Register(id, TipoPessoa.PF, "Perfil", null, "Cidade", "CE");
        owner.Providers.SavedSpecialties = ["hidraulica"];
        var response = await owner.Service.GetCurrentAsync();
        Check(response.IdUsuario == id && owner.Providers.LastReadId == id, "Consulta apenas o perfil do usuário do token");

        owner.Users.LocalUser.Deactivate();
        await ThrowsAsync<UnauthorizedException>(() => owner.Service.GetCurrentAsync());
        owner.Users.LocalUser = null;
        await ThrowsAsync<UnauthorizedException>(() => owner.Service.GetCurrentAsync());

        owner.Users.LocalUser = User.Register(id, "Cliente de teste", Email.Create("cliente@example.invalid"), UserRole.Cliente);
        await ThrowsAsync<UnauthorizedException>(() => owner.Service.GetCurrentAsync());
        owner.Users.LocalUser = User.Register(id, "Prestador de teste", Email.Create("prestador@example.invalid"), UserRole.Prestador);
        owner.Providers.Stored = null;
        await ThrowsAsync<NotFoundException>(() => owner.Service.GetCurrentAsync());
    }

    private static async Task CheckCancellationAndRetryAsync()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var fixture = new Fixture();
        await ThrowsAsync<OperationCanceledException>(() => fixture.Service.RegisterAsync(Request(), canceled.Token));
        Check(fixture.Auth.SignUpCalls == 0, "Cancelamento prévio não inicia criação externa");

        using var disconnected = new CancellationTokenSource();
        fixture = new Fixture();
        fixture.Auth.OnSignUp = disconnected.Cancel;
        await fixture.Service.RegisterAsync(Request(), disconnected.Token);
        Check(fixture.Providers.SavedUser is not null && !fixture.Providers.LastWriteToken.IsCancellationRequested,
            "Desconexão após iniciar Auth não cancela a persistência local");

        fixture = new Fixture();
        fixture.Providers.FailNextWrite = true;
        await ThrowsAsync<RegistrationPendingException>(() => fixture.Service.RegisterAsync(Request()));
        Check(fixture.Providers.SavedUser is null, "Falha simulada não confirma perfil local");
        await fixture.Service.RegisterAsync(Request());
        Check(fixture.Auth.SignUpCalls == 2 && fixture.Providers.AttemptedIds.All(id => id == fixture.Auth.Id),
            "Nova tentativa pode persistir a mesma identidade Auth recuperada");

        fixture = new Fixture();
        fixture.Providers.RejectNextWrite = true;
        await ThrowsAsync<DomainException>(() => fixture.Service.RegisterAsync(Request()));
        Check(fixture.Providers.SavedUser is null, "Regra de domínio do repositório permanece erro de validação");

        fixture = new Fixture();
        fixture.Auth.TimeoutOnSignUp = true;
        await ThrowsAsync<RegistrationPendingException>(() => fixture.Service.RegisterAsync(Request()));
        Check(fixture.Providers.AddCalls == 0, "Timeout de resultado externo incerto não inicia perfil com UUID desconhecido");
    }

    private static async Task CheckClientCompatibilityAsync()
    {
        var fixture = new Fixture();
        var authService = new AuthService(fixture.Users, fixture.Auth);
        var registered = await authService.RegisterAsync(new RegisterRequest
        {
            Name = "Cliente de teste",
            Email = "cliente@example.invalid",
            Password = "SenhaSintetica!123"
        });
        Check(registered.Role == nameof(UserRole.Cliente) && fixture.Users.LocalUser?.Id == fixture.Auth.Id,
            "Cadastro público Cliente mantém papel e UUID Auth");

        fixture.Users.LocalUser = User.Register(Guid.NewGuid(), "Outro perfil", Email.Create("prestador@example.invalid"), UserRole.Cliente);
        await ThrowsAsync<UnauthorizedException>(() => authService.LoginAsync(new LoginRequest
        {
            Email = "prestador@example.invalid",
            Password = "SenhaSintetica!123"
        }));
        Check(fixture.Users.GetByEmailCalls == 0, "Login não escolhe perfil de outro UUID por e-mail");

        fixture.Users.LocalUser = User.Register(fixture.Auth.Id, "Cliente de teste", Email.Create("prestador@example.invalid"), UserRole.Cliente);
        var session = await authService.LoginAsync(new LoginRequest
        {
            Email = "prestador@example.invalid",
            Password = "SenhaSintetica!123"
        });
        Check(session.User.Id == fixture.Auth.Id && session.User.Role == nameof(UserRole.Cliente),
            "Login continua aceitando perfil Cliente com UUID correto");
    }

    private static async Task CheckClientCompletionAsync()
    {
        var id = Guid.NewGuid();
        var fixture = new Fixture();
        var addresses = new Addresses();
        var contacts = new Contacts();
        fixture.Users.LocalUser = User.Register(id, "Cliente de teste", Email.Create("cliente@example.invalid"), UserRole.Cliente);
        var service = new UserService(fixture.Users, addresses, contacts, fixture.Auth, new Current(true, id, UserRole.Cliente));
        var response = await service.CompleteRegistrationAsync(CompletionRequest());
        Check(response.Role == nameof(UserRole.Cliente) && response.Cpf == CpfDeTeste
            && fixture.Users.LastCpfRole == UserRole.Cliente,
            "Finalização Cliente consulta CPF no próprio papel e preserva contrato");
        Check(addresses.Saved?.IdUsuario == id && addresses.Saved.Principal && addresses.Saved.Cep == "00000000"
            && contacts.Saved?.IdUsuario == id && contacts.Saved.Principal,
            "Finalização Cliente grava endereço e contato principais para o dono do token");

        fixture = new Fixture();
        addresses = new Addresses();
        contacts = new Contacts();
        fixture.Users.LocalUser = User.Register(id, "Cliente de teste", Email.Create("cliente@example.invalid"), UserRole.Cliente);
        fixture.Users.CpfExists = true;
        service = new UserService(fixture.Users, addresses, contacts, fixture.Auth, new Current(true, id, UserRole.Cliente));
        await ThrowsAsync<ConflictException>(() => service.CompleteRegistrationAsync(CompletionRequest()));
        Check(fixture.Users.LastCpfRole == UserRole.Cliente && fixture.Users.UpdateCalls == 0
            && addresses.Saved is null && contacts.Saved is null,
            "CPF já usado por Cliente rejeita finalização sem novas gravações");

        fixture = new Fixture();
        addresses = new Addresses();
        contacts = new Contacts();
        fixture.Users.LocalUser = User.Register(id, "Conta PJ de teste", Email.Create("empresa@example.invalid"), UserRole.Prestador);
        service = new UserService(fixture.Users, addresses, contacts, fixture.Auth, new Current(true, id, UserRole.Prestador));
        await ThrowsAsync<DomainException>(() => service.CompleteRegistrationAsync(CompletionRequest()));
        Check(fixture.Users.LocalUser.Cpf is null && fixture.Users.CpfCalls == 0 && fixture.Users.UpdateCalls == 0
            && addresses.Saved is null && contacts.Saved is null,
            "Prestador PJ não adquire CPF pela finalização exclusiva de Cliente");
    }

    private static CompleteRegistrationRequest CompletionRequest() => new()
    {
        Name = "Cliente de teste",
        Cpf = CpfDeTeste,
        Endereco = new EnderecoRequest
        {
            Logradouro = "Rua de teste",
            Numero = "0",
            Bairro = "Bairro de teste",
            Cidade = "Cidade de teste",
            Estado = "ce",
            Cep = "00000-000"
        },
        Contato = new ContatoRequest { Tipo = "whatsapp", Valor = "00000000000" }
    };

    private static RegisterFornecedorRequest Request() => new()
    {
        Name = "Prestador de teste",
        Email = "prestador@example.invalid",
        Password = "SenhaSintetica!123",
        TipoPessoa = "PF",
        NomeExibicao = "Perfil de teste",
        Cpf = CpfDeTeste,
        Telefone = "(00) 00000-0000",
        CidadeAtuacao = "Cidade de teste",
        UfAtuacao = "CE",
        Especialidades = ["hidraulica"]
    };

    private static bool IsValid(object value)
        => Validator.TryValidateObject(value, new ValidationContext(value), null, validateAllProperties: true);

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Era esperada a exceção {typeof(T).Name}.");
    }

    private static async Task ThrowsAsync<T>(Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { return; }
        throw new Exception($"Era esperada a exceção {typeof(T).Name}.");
    }

    private sealed class Fixture
    {
        public Users Users { get; } = new();
        public Providers Providers { get; } = new();
        public Auth Auth { get; } = new();
        public FornecedorService Service { get; }

        public Fixture(ICurrentUser? current = null)
        {
            Service = new FornecedorService(Users, Providers, new Specialties(), Auth,
                current ?? new Current(false, null, null));
        }
    }

    private sealed record Current(bool IsAuthenticated, Guid? Id, UserRole? Role) : ICurrentUser
    {
        public string? Email => "prestador@example.invalid";
    }

    private sealed class Users : IUserRepository
    {
        public User? LocalUser { get; set; }
        public bool EmailExists { get; set; }
        public bool CpfExists { get; set; }
        public int CpfCalls { get; private set; }
        public int GetByEmailCalls { get; private set; }
        public int UpdateCalls { get; private set; }
        public UserRole? LastCpfRole { get; private set; }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalUser?.Id == id ? LocalUser : null);

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            GetByEmailCalls++;
            return Task.FromResult(LocalUser);
        }

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
            => Task.FromResult(EmailExists);

        public Task<bool> ExistsByCpfAsync(string cpf, UserRole role, Guid excludingUserId, CancellationToken cancellationToken = default)
        {
            CpfCalls++;
            LastCpfRole = role;
            return Task.FromResult(CpfExists);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<User>>(LocalUser is null ? [] : [LocalUser]);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            LocalUser = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LocalUser = user;
            return Task.CompletedTask;
        }
    }

    private sealed class Addresses : IEnderecoRepository
    {
        public Endereco? Saved { get; private set; }

        public Task AddAsync(Endereco endereco, CancellationToken cancellationToken = default)
        {
            Saved = endereco;
            return Task.CompletedTask;
        }
    }

    private sealed class Contacts : IContatoRepository
    {
        public Contato? Saved { get; private set; }

        public Task AddAsync(Contato contato, CancellationToken cancellationToken = default)
        {
            Saved = contato;
            return Task.CompletedTask;
        }
    }

    private sealed class Providers : IFornecedorRepository
    {
        public bool CnpjExists { get; set; }
        public bool FailNextWrite { get; set; }
        public bool RejectNextWrite { get; set; }
        public int AddCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public Guid LastReadId { get; private set; }
        public User? SavedUser { get; private set; }
        public Fornecedor? Stored { get; set; }
        public Contato? SavedContact { get; private set; }
        public IReadOnlyList<string> SavedSpecialties { get; set; } = [];
        public List<Guid> AttemptedIds { get; } = [];
        public CancellationToken LastWriteToken { get; private set; }

        public Task<bool> ExistsByCnpjAsync(string cnpj, CancellationToken cancellationToken = default)
            => Task.FromResult(CnpjExists);

        public Task AddCompleteAsync(User user, Fornecedor fornecedor, Contato contato,
            IReadOnlyList<string> especialidades, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddCalls++;
            LastWriteToken = cancellationToken;
            AttemptedIds.Add(user.Id);
            if (RejectNextWrite)
            {
                RejectNextWrite = false;
                throw new DomainException("Especialidade inativada durante a operação simulada.");
            }

            if (FailNextWrite)
            {
                FailNextWrite = false;
                throw new InvalidOperationException("Falha local simulada.");
            }

            SavedUser = user;
            Stored = fornecedor;
            SavedContact = contato;
            SavedSpecialties = especialidades.ToArray();
            return Task.CompletedTask;
        }

        public Task<Fornecedor?> GetByUsuarioIdAsync(Guid idUsuario, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            LastReadId = idUsuario;
            return Task.FromResult(Stored?.IdUsuario == idUsuario ? Stored : null);
        }

        public Task<IReadOnlyList<string>> GetEspecialidadesAsync(Guid idUsuario, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            LastReadId = idUsuario;
            return Task.FromResult(SavedSpecialties);
        }
    }

    private sealed class Specialties : IEspecialidadeQueries
    {
        public Task<IReadOnlyList<EspecialidadeResponse>> ListActiveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EspecialidadeResponse>>([new("hidraulica", "Hidráulica")]);
    }

    private sealed class Auth : ISupabaseAuthClient
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int SignUpCalls { get; private set; }
        public Action? OnSignUp { get; set; }
        public bool TimeoutOnSignUp { get; set; }

        public Task<SupabaseSession> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new SupabaseSession("token-de-teste", "Bearer", DateTimeOffset.UtcNow.AddMinutes(1), null, new(Id, email)));

        public Task<SupabaseAuthUser> SignUpAsync(string email, string password, string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SignUpCalls++;
            if (TimeoutOnSignUp)
            {
                throw new TaskCanceledException("Timeout externo simulado.");
            }

            OnSignUp?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new SupabaseAuthUser(Id, email));
        }

        public Task<SupabaseAuthUser> AdminCreateUserAsync(string email, string password, string name, CancellationToken cancellationToken = default)
            => Task.FromResult(new SupabaseAuthUser(Id, email));
    }
}
