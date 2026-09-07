# Organização do projeto — Resolvai.Api

A solução mantém quatro projetos. Na Application, os arquivos são agrupados por funcionalidade: quem trabalha com usuários começa pela pasta `Users`; quem trabalha com login ou cadastro público começa por `Auth`.

## Estrutura

```text
Resolvai.Api.slnx
├── src/
│   ├── Resolvai.Api/             # HTTP, autenticação, Swagger e respostas de erro
│   ├── Resolvai.Application/     # Operações e validações da aplicação
│   │   ├── Auth/
│   │   │   ├── AuthService.cs
│   │   │   ├── IAuthService.cs
│   │   │   ├── DTOs/             # LoginRequest, LoginResponse, RegisterRequest
│   │   │   └── Validators/       # LoginRequestValidator, RegisterRequestValidator
│   │   ├── Users/
│   │   │   ├── UserService.cs
│   │   │   ├── IUserService.cs
│   │   │   ├── UserMapper.cs
│   │   │   ├── DTOs/             # CreateUserRequest, SetUserStatusRequest, UserResponse
│   │   │   └── Validators/       # CreateUserRequestValidator, UserValidator
│   │   ├── Common/
│   │   │   ├── Exceptions/       # NotFound, Conflict, Unauthorized
│   │   │   └── Validation/       # Regras compartilhadas de nome, e-mail e senha
│   │   ├── Contracts/Security/   # Acesso ao Supabase e ao usuário da requisição
│   │   └── DependencyInjection.cs
│   ├── Resolvai.Domain/          # User, Email, UserRole e interface do repositório
│   └── Resolvai.Infrastructure/  # SQL/Dapper, conexão PostgreSQL e cliente Supabase
├── tests/Resolvai.Api.Tests/     # Validação e testes HTTP com dependências em memória
├── db/scripts/                  # Scripts SQL numerados na ordem de execução
├── docs/
├── Directory.Build.props        # Configuração compartilhada de compilação
├── Directory.Packages.props     # Versões dos pacotes NuGet
├── dotnet-tools.json             # Versão local do CSharpier
└── .csharpierrc.json             # Padrão de formatação
```

## Caminho de uma requisição

No cadastro público:

1. `AuthController.Register` recebe o JSON como `RegisterRequest`.
2. `AuthService.RegisterAsync` chama `RegisterRequestValidator` com `ValidateAndThrowAsync`.
3. O validator verifica nome, e-mail e senha antes de consultar o banco ou chamar o Supabase.
4. O service consulta se o e-mail já existe e solicita o cadastro ao `ISupabaseAuthClient`.
5. O service cria o perfil `User` com papel `Viewer` e executa `UserValidator`, incluindo a validação do identificador recebido.
6. `IUserRepository.AddAsync` grava o perfil. A implementação `UserRepository`, na Infrastructure, contém o SQL.
7. `UserMapper.ToResponse` converte o perfil no DTO de resposta.
8. O controller devolve HTTP 201.

Os arquivos `DependencyInjection.cs` ligam interfaces às implementações. Por exemplo, `IUserService` é atendido por `UserService`. Os validators também são registrados explicitamente nesse arquivo.

## Responsabilidade de cada projeto

- **Api:** controllers, rotas, autorização, configuração HTTP e tradução de exceções. Não contém SQL ou regras de validação de campos.
- **Application:** serviços, DTOs, validators e mapeamentos agrupados por funcionalidade. Depende do Domain e de contratos para acessar recursos externos.
- **Domain:** representação e alterações do estado do negócio. `User` mantém operações como ativar/desativar; `Email` normaliza o texto. As validações desses objetos foram movidas para a Application.
- **Infrastructure:** implementações de repositórios com Dapper, conexão Npgsql, configuração e comunicação HTTP com o Supabase.

Dependências: Api referencia Application/Infrastructure; Infrastructure referencia Application/Domain; Application referencia Domain. Domain não referencia os demais projetos nem FluentValidation.

## Validação com FluentValidation

- `Auth/Validators/LoginRequestValidator`: e-mail obrigatório e válido; senha obrigatória com até 128 caracteres. O login não exige o mínimo de oito caracteres usado na criação de senhas.
- `Auth/Validators/RegisterRequestValidator`: nome obrigatório de até 200 caracteres, e-mail válido de até 320 e senha de 8 a 128 caracteres.
- `Users/Validators/CreateUserRequestValidator`: mesmas regras de cadastro, mais um papel pertencente ao enum `UserRole`.
- `Users/Validators/UserValidator`: perfil válido antes de salvar, incluindo identificador não vazio, nome, e-mail e papel. Também é chamado na atualização do status.
- `Common/Validation/UserValidationRules`: implementação única das regras de nome, e-mail e nova senha, reutilizada pelos validators.

Os DTOs não usam mais DataAnnotations. `User` e `Email` não repetem essas regras. Ao adicionar operações que criem ou alterem esses objetos, valide na Application antes de persistir. A normalização (`Trim` e e-mail em minúsculas) permanece nos objetos; não substitui a validação.

Os serviços chamam os validators explicitamente, inclusive quando usados fora de um controller. Não utilizamos a integração automática legada `FluentValidation.AspNetCore`.

Regras que dependem do banco ou da sessão, como e-mail duplicado e usuário inativo, permanecem nos serviços. As verificações de configuração, token e resposta do Supabase permanecem nos respectivos adaptadores.

`SetUserStatusRequest` aceita tanto `true` quanto `false`, portanto não usa `NotEmpty` no booleano. O `required` do C# mantém a obrigatoriedade de presença no JSON. Campos ausentes, JSON malformado e tipos incompatíveis continuam sendo tratados pelo ASP.NET; erros de conteúdo são tratados pelo FluentValidation. A validação implícita de referências não nulas do MVC está desativada para evitar duplicidade.

`GlobalExceptionHandler` converte `ValidationException` em HTTP 400 (`application/problem+json`) com mensagens agrupadas por campo:

```json
{
  "title": "Um ou mais campos são inválidos.",
  "status": 400,
  "instance": "POST /api/v1/auth/register",
  "errors": {
    "Email": ["Informe um e-mail válido."]
  }
}
```

As restrições de tamanho e formato agora estão nos validators; sua remoção dos atributos também remove a origem desses metadados automáticos no OpenAPI. O Swagger continua disponível, mas não deduz regras do FluentValidation.

## Adicionar uma funcionalidade

Para uma funcionalidade de ocorrências:

1. Criar a entidade em `Domain/Entities` e a interface do repositório em `Domain/Repositories`.
2. Criar `Application/Occurrences`, com serviço e interface, `DTOs`, `Validators` e mapper se necessário.
3. Registrar o serviço e cada `IValidator<T>` em `Application/DependencyInjection.cs`.
4. Chamar `ValidateAndThrowAsync` no início das operações que recebem entradas, antes de qualquer efeito externo.
5. Implementar o repositório em `Infrastructure/Persistence/Repositories` e registrá-lo em `Infrastructure/DependencyInjection.cs`.
6. Criar o script em `db/scripts` e o controller em `Api/Controllers/V1`.
7. Adicionar exemplos HTTP e testes para entradas válidas e inválidas.

## Formatação e testes

Na raiz do repositório:

```powershell
dotnet tool restore
dotnet csharpier format .
dotnet csharpier check .
dotnet test Resolvai.Api.slnx
```

O manifesto fixa a versão do CSharpier para a equipe. O comando `format` ajusta C# e XML; `check` verifica sem alterar arquivos. A configuração usa quatro espaços, limite de 100 colunas e quebras de linha LF.

Os testes HTTP usam controllers, serviços e tratamento de erros reais com banco, Supabase e autenticação substituídos em memória. Não exigem credenciais nem exercitam a integração real com o Supabase.

Para executar a API, consulte [BUILD-AND-RUN.md](BUILD-AND-RUN.md).
