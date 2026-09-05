# Organização do projeto — Resolvai.Api

A solution segue **Clean Architecture** em quatro camadas. A regra de ouro: **dependências apontam para dentro** (Api → Application/Infrastructure; Infrastructure → Application/Domain; Application → Domain; Domain não depende de ninguém).

```
Resolvai.Api.slnx
├── db/                  → scripts SQL do banco
├── docs/                → documentação
├── src/
│   ├── Resolvai.Domain
│   ├── Resolvai.Application
│   ├── Resolvai.Infrastructure
│   └── Resolvai.Api
├── Directory.Build.props
└── Directory.Packages.props
```

```
                    ┌─────────────────────┐
                    │    Resolvai.Api     │  HTTP, Swagger, middleware
                    └──────────┬──────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                                 ▼
   ┌─────────────────────┐           ┌──────────────────────┐
   │ Resolvai.Application│           │Resolvai.Infrastructure│
   │  casos de uso, DTOs │◄──────────│  DB, Supabase, I/O    │
   └──────────┬──────────┘           └──────────┬───────────┘
              │                                 │
              └────────────────┬────────────────┘
                               ▼
                    ┌─────────────────────┐
                    │   Resolvai.Domain   │  entidades e regras
                    └─────────────────────┘
```

---

## Raiz do repositório

| Item | O que fica aqui |
|------|-----------------|
| `Resolvai.Api.slnx` | Solution (lista dos projetos) |
| `Directory.Build.props` | TFM, nullable, regras compartilhadas de build |
| `Directory.Packages.props` | Versões centralizadas dos pacotes NuGet |
| `db/` | Scripts SQL versionados (schema, migrações manuais) |
| `docs/` | Documentação do time (build, arquitetura, etc.) |
| `src/` | Todo o código-fonte dos projetos |

**Não colocar** código de negócio, controllers ou conexão com banco na raiz.

---

## `db/scripts/`

Scripts SQL executados no **SQL Editor do Supabase** (ou ferramenta equivalente).

| Colocar | Não colocar |
|---------|-------------|
| `CREATE TABLE`, índices, constraints | Lógica de aplicação em C# |
| Alterações de schema numeradas (`001_`, `002_`, …) | Connection strings ou secrets |
| Comentários de bootstrap (ex.: promover Admin) | Seeds com dados sensíveis de produção |

Convenção: prefixo numérico na ordem de execução (`001_create_users.sql`, `002_drop_password_hash.sql`).

---

## `docs/`

Documentação humana do projeto (`BUILD-AND-RUN.md`, este arquivo, ADRs, etc.).

---

## `Resolvai.Domain` — núcleo do domínio

**Sem dependências** de ASP.NET, Dapper, Supabase ou HTTP. Só regras de negócio puras.

```
Resolvai.Domain/
├── Common/          → Entity<T>, IAggregateRoot
├── Entities/        → agregados (ex.: User)
├── ValueObjects/    → Email, etc.
├── Enums/           → UserRole, status de domínio
├── Exceptions/      → DomainException, InvalidEmailException
└── Repositories/    → apenas interfaces (ex.: IUserRepository)
```

| Colocar aqui | Não colocar aqui |
|--------------|------------------|
| Entidades e invariantes (`User.Register`, `Activate`) | Controllers, DTOs de API |
| Value objects e enums de negócio | SQL, Npgsql, Dapper |
| Interfaces de repositório | Cliente Supabase / HttpClient |
| Exceções de domínio | `appsettings`, Options |

**Onde criar algo novo**

- Nova entidade → `Entities/`
- Validação encapsulada (e-mail, CPF, …) → `ValueObjects/`
- Contrato de persistência → `Repositories/INomeRepository.cs`
- Erro de regra → `Exceptions/`

---

## `Resolvai.Application` — casos de uso

Orquestra o domínio. Conhece o Domain; **não** conhece detalhes de PostgreSQL/Supabase (só contratos).

```
Resolvai.Application/
├── Common/Exceptions/     → NotFound, Conflict, Unauthorized (camada app)
├── Contracts/             → portas para infra (ISupabaseAuthClient, ICurrentUser)
├── DTOs/
│   ├── Auth/              → LoginRequest, LoginResponse, RegisterRequest
│   └── Users/             → CreateUserRequest, UserResponse, …
├── Mappings/              → Entity → DTO (ex.: UserMapper)
├── Services/
│   ├── Interfaces/        → IAuthService, IUserService
│   ├── AuthService.cs
│   └── UserService.cs
└── DependencyInjection.cs → registro dos services de aplicação
```

| Colocar aqui | Não colocar aqui |
|--------------|------------------|
| Services / use cases | Controllers (`[HttpGet]`, etc.) |
| DTOs de entrada/saída da API | SQL cru ou connection strings |
| Interfaces que a Infrastructure implementa | Middleware ASP.NET |
| Mapeamento Entity ↔ DTO | Validação de JWT / pipeline HTTP |
| Exceções de aplicação (404, 409, 401) | Classes `Options` ligadas a config |

**Onde criar algo novo**

- Novo caso de uso → `Services/` + interface em `Services/Interfaces/`
- Contrato para um adaptador externo → `Contracts/` (por área, ex.: `Contracts/Security/`)
- Request/Response → `DTOs/<Área>/`
- Registro no DI → `DependencyInjection.cs`

---

## `Resolvai.Infrastructure` — detalhes técnicos

Implementa os contratos da Application/Domain: banco, Auth externo, etc.

```
Resolvai.Infrastructure/
├── Options/                 → DatabaseOptions, SupabaseOptions
├── Persistence/
│   ├── Connection/          → NpgsqlConnectionFactory
│   ├── Repositories/        → UserRepository (Dapper)
│   └── TypeHandlers/        → handlers Dapper (Email, …)
├── Security/                → SupabaseAuthClient
└── DependencyInjection.cs   → Bind options, AddHttpClient, repositórios
```

| Colocar aqui | Não colocar aqui |
|--------------|------------------|
| Repositórios com SQL (Dapper) | Regras de negócio (ficar no Domain) |
| Clientes HTTP (Supabase Auth) | Controllers |
| `IOptions<T>` / bind de configuração | DTOs de API (ficar na Application) |
| Type handlers, factories de conexão | Lógica de “quando criar usuário” (service) |

**Onde criar algo novo**

- Novo repositório → `Persistence/Repositories/` (implementa interface do Domain)
- Nova integração externa → pasta por preocupação (`Security/`, `Email/`, `Storage/`, …)
- Nova seção de config → `Options/` + bind em `DependencyInjection.cs`

---

## `Resolvai.Api` — host HTTP

Camada de entrada: HTTP, autenticação do pipeline, Swagger, serialização.

```
Resolvai.Api/
├── Controllers/
│   ├── ApiControllerBase.cs
│   └── V1/                  → AuthController, UsersController
├── Extensions/              → AddSupabaseAuthentication, OpenAPI/Swagger
├── Middlewares/             → GlobalExceptionHandler
├── Security/                → CurrentUser (adapta HttpContext → ICurrentUser)
├── Properties/              → launchSettings.json
├── Program.cs
├── appsettings*.json
└── Resolvai.Api.http        → requests de exemplo
```

| Colocar aqui | Não colocar aqui |
|--------------|------------------|
| Controllers finos (só HTTP ↔ service) | SQL ou acesso a banco |
| Middleware e exception handler | Regras de domínio |
| Extensões de DI do host (JWT, OpenAPI) | Implementação de repositório |
| `appsettings`, `launchSettings` | Casos de uso longos (mover para Application) |

**Onde criar algo novo**

- Novo endpoint → `Controllers/V1/` (ou `V2/` se versionar)
- Novo middleware → `Middlewares/`
- Wiring de autenticação/docs → `Extensions/`
- Adaptador de claims/contexto HTTP → `Security/`

Controllers devem permanecer **finos**: validação de modelo + chamar `IXxxService` + retornar status HTTP.

---

## Onde colocar X? (atalho)

| Quero adicionar… | Onde |
|------------------|------|
| Tabela / coluna no banco | `db/scripts/00N_….sql` |
| Entidade ou regra de negócio | `Resolvai.Domain` |
| Interface de repositório | `Domain/Repositories/` |
| Implementação SQL (Dapper) | `Infrastructure/Persistence/Repositories/` |
| Caso de uso (login, criar user) | `Application/Services/` |
| DTO de request/response | `Application/DTOs/<área>/` |
| Cliente Supabase / API externa | `Infrastructure/` (+ contrato em `Application/Contracts/`) |
| Opção de `appsettings` | `Infrastructure/Options/` + chave em `appsettings.json` |
| Endpoint REST | `Api/Controllers/V1/` |
| Tratamento global de erro HTTP | `Api/Middlewares/` |
| Guia / ADR | `docs/` |
| Pacote NuGet e versão | `Directory.Packages.props` (+ `PackageReference` no `.csproj`) |

---

## Convenções rápidas

1. **Domain** não referencia Application, Infrastructure nem Api.
2. **Application** não referencia Infrastructure nem Api (só contratos; infra implementa).
3. **Api** orquestra o host: chama `AddApplication()` e `AddInfrastructure()`.
4. Segredos (`ConnectionString`, keys do Supabase) → **user-secrets** / variáveis de ambiente, nunca commitados.
5. Versionamento de API HTTP por pasta (`Controllers/V1/`), não misturar versões no mesmo controller sem necessidade.
6. Um service de aplicação por capacidade (`AuthService`, `UserService`); evitar “God service”.

---

## Exemplo: nova feature “Ocorrências”

Fluxo sugerido de pastas:

1. `Domain/Entities/Occurrence.cs` + `Domain/Repositories/IOccurrenceRepository.cs`
2. `db/scripts/003_create_occurrences.sql`
3. `Application/DTOs/Occurrences/` + `IOccurrenceService` / `OccurrenceService`
4. `Infrastructure/Persistence/Repositories/OccurrenceRepository.cs`
5. `Api/Controllers/V1/OccurrencesController.cs`
6. Registrar no `DependencyInjection` de Application e Infrastructure
