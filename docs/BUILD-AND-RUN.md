# Build e Run — Resolvai.Api

Guia rápido para compilar e executar a API no **Visual Studio** e no **VS Code**.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Conta/projeto no [Supabase](https://supabase.com) (banco + Auth)
- Scripts SQL em `db/scripts/` já executados no SQL Editor do Supabase

Confirme o SDK:

```powershell
dotnet --version
```

## Configuração (obrigatória antes do Run)

A API valida `Database` e `Supabase` na inicialização. Em Development, use **user-secrets** (não versionar chaves no `appsettings.json`).

```powershell
cd src\Resolvai.Api

dotnet user-secrets set "Database:ConnectionString" "Host=db.<ref>.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<senha>;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Supabase:Url" "https://<ref>.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "<anon-key>"
dotnet user-secrets set "Supabase:ServiceRoleKey" "<service-role-key>"
dotnet user-secrets set "Supabase:JwtSecret" "<jwt-secret>"
```

Os valores ficam em **Project Settings → API** e **Database** no painel do Supabase.

---

## Visual Studio

### Abrir e restaurar

1. Abra `Resolvai.Api.slnx` (File → Open → Project/Solution).
2. Aguarde o restore automático dos pacotes NuGet (ou clique com o botão direito na solution → **Restore NuGet Packages**).

### Build

- Menu **Build → Build Solution** (atalho: `Ctrl+Shift+B`)
- Ou botão direito na solution → **Build**

### Run / Debug

1. No seletor de perfil (barra superior), escolha:
   - **https** — `https://localhost:7099` (e também `http://localhost:5172`)
   - **http** — `http://localhost:5172`
2. Pressione **F5** (com debug) ou **Ctrl+F5** (sem debug).
3. O browser deve abrir em `/swagger`.

### Parar

- **Shift+F5** ou botão Stop na barra de debug.

---

## VS Code

### Extensões recomendadas

- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) (inclui C# e suporte a solution)
- Ou, no mínimo, a extensão **C#** (`ms-dotnettools.csharp`)

### Abrir o projeto

1. **File → Open Folder…** e selecione a pasta raiz do repositório (onde está `Resolvai.Api.slnx`).
2. Se o C# Dev Kit pedir, escolha a solution `Resolvai.Api.slnx`.

### Build pelo terminal

```powershell
dotnet build Resolvai.Api.slnx
```

### Run pelo terminal

```powershell
dotnet run --project src\Resolvai.Api\Resolvai.Api.csproj --launch-profile https
```

Para só HTTP:

```powershell
dotnet run --project src\Resolvai.Api\Resolvai.Api.csproj --launch-profile http
```

Depois abra no browser:

- Swagger: [http://localhost:5172/swagger](http://localhost:5172/swagger)
- Com HTTPS: [https://localhost:7099/swagger](https://localhost:7099/swagger)

### Run / Debug pela UI (C# Dev Kit)

1. Abra a view **Run and Debug** (`Ctrl+Shift+D`).
2. Selecione o projeto **Resolvai.Api** / perfil **https** ou **http**.
3. Pressione **F5**.

Se não aparecer nenhum perfil, use o comando da Command Palette (`Ctrl+Shift+P`):

- **.NET: Generate Assets for Build and Debug**

Isso cria `.vscode/launch.json` e `.vscode/tasks.json` com base na solution.

### Certificado de desenvolvimento (HTTPS)

Na primeira vez com o perfil **https**, se o browser reclamar do certificado:

```powershell
dotnet dev-certs https --trust
```

---

## URLs úteis

| Recurso | URL |
|--------|-----|
| Swagger UI | `/swagger` |
| OpenAPI JSON | `/openapi/v1.json` |
| HTTP local | `http://localhost:5172` |
| HTTPS local | `https://localhost:7099` |

---

## Problemas comuns

| Sintoma | O que verificar |
|--------|------------------|
| Falha na inicialização com mensagem de `Database:ConnectionString` ou `Supabase:*` | User-secrets / variáveis de ambiente não configurados |
| `401` nas rotas protegidas | Login via `POST /api/v1/auth/login` e uso do `accessToken` no Bearer |
| Build falha com TFM `net10.0` | Instalar o .NET 10 SDK e reiniciar o IDE |
| Porta em uso | Encerrar outro processo na `5172`/`7099` ou alterar `launchSettings.json` |
