# Cadastro de Prestador — issue #2

O backend cadastra contas Cliente e Prestador separadas, com e-mails e UUIDs distintos. O cadastro profissional aceita PF ou PJ e não exige aprovação manual. Login do frontend, recuperação de senha, edição de perfil, portfólio e avaliações permanecem fora desta implementação.

## Rotas e contratos

| Método | Rota | Acesso | Resultado |
| --- | --- | --- | --- |
| POST | `/api/v1/auth/register-provider` | Público | 201 com `FornecedorResponse` e `Location` para o próprio perfil |
| GET | `/api/v1/fornecedores/me` | Bearer, papel Prestador | 200 com o perfil profissional do dono do token |
| GET | `/api/v1/especialidades` | Público | Lista ativa de objetos `codigo` e `nome` |

O cadastro Cliente mantém `POST /api/v1/auth/register`, `GET /api/v1/users/me` e `POST /api/v1/users/me/complete-registration`. A finalização Cliente não pode ser usada para atribuir CPF a uma conta Prestador PJ. O login existente só aceita o perfil local cujo UUID coincide com o usuário autenticado; não há mais fallback de identidade por e-mail.

O corpo de cadastro profissional usa os seguintes campos JSON:

| Campo | Regra |
| --- | --- |
| `name`, `email`, `password` | Obrigatórios; nome até 200, e-mail até 320, senha de 8 a 128 caracteres |
| `tipoPessoa` | `PF` ou `PJ` |
| `nomeExibicao` | Obrigatório, até 200 caracteres |
| `cpf` | Obrigatório para PF; não informar para PJ |
| `cnpj` | Obrigatório para PJ, numérico ou alfanumérico; não informar para PF |
| `telefone` | Obrigatório, DDD e 10 ou 11 dígitos; persistido como contato principal `whatsapp` |
| `cidadeAtuacao`, `ufAtuacao` | Obrigatórios; cidade até 120 caracteres e UF válida |
| `raioAtendimentoKm` | Opcional, padrão 20; inteiro de 1 a 200 |
| `bio`, `registroProfissional` | Opcionais, até 360 e 80 caracteres |
| `especialidades` | De 1 a 100 códigos ativos; repetições são removidas |

`FornecedorResponse` contém `idUsuario`, `tipoPessoa`, `nomeExibicao`, `cnpj`, `bio`, `cidadeAtuacao`, `ufAtuacao`, `raioAtendimentoKm`, `registroProfissional`, `especialidades`, `createdAt` e `updatedAt`. Valores nulos são omitidos pela configuração da API. POST e GET retornam a mesma representação profissional, sem tokens ou senha. Dados básicos do usuário continuam disponíveis em `/api/v1/users/me`.

Respostas esperadas: 400 para dados inválidos; 409 para duplicidades ou retomada sem comprovação de posse; 401 sem autenticação; 403 para outro papel em `/fornecedores/me`; 404 para perfil profissional inexistente; 503 quando a conclusão do cadastro não pôde ser confirmada. Respostas de perfil e cadastro profissional usam `Cache-Control: no-store`.

## Persistência e pré-requisitos

`FornecedorService` valida os dados antes de chamar Auth. `FornecedorRepository` insere `users`, `fornecedores`, `contatos` e `fornecedor_especialidades` em uma única transação Dapper. Especialidades são conferidas novamente com `FOR SHARE` dentro da transação; falhas ou cancelamento desfazem as gravações locais. Constraints únicas protegem concorrência e são traduzidas em 409 sem documentos/e-mails nas mensagens.

Os scripts 012 e 013 já foram aplicados em produção e estão versionados como referência. Não reaplicá-los nesse ambiente. O script 014 troca a unicidade global de CPF por `(cpf, role)`, preservando múltiplos valores nulos para PJ. Ele deve ser validado em ambiente isolado; qualquer execução de produção exige autorização específica. Antes dessa migração, o mesmo CPF entre Cliente e Prestador ainda pode ser rejeitado pelo banco.

As tabelas profissionais mantêm RLS e acesso direto de `anon`/`authenticated` revogado. A API usa o repositório privilegiado e restringe a consulta ao UUID do token e ao perfil local ativo. Nenhuma role vem de metadados editáveis pelo usuário.

## Auth e recuperação de cadastro

Supabase Auth e PostgreSQL não compartilham transação. Portanto, falha, timeout ou perda de resposta ainda podem deixar uma conta externa sem perfil local; não existe garantia de atomicidade distribuída.

Cada signup envia um identificador aleatório de tentativa. O cliente só trata a resposta como criação nova quando a correlação retornada confere e há uma identidade associada. O identificador serve apenas para distinguir a resposta daquela operação e nunca é usado para autorização. Isso evita adotar o UUID fictício de uma resposta ofuscada ou uma conta não confirmada criada em outra tentativa.

Quando a conta externa já existe, a retomada exige autenticação com a senha enviada e usa somente o UUID e o e-mail retornados nessa autenticação. O backend não redefine senha, não altera uma conta existente por e-mail e não apaga contas automaticamente. Se o e-mail ainda não estiver confirmado, o usuário deve confirmar o e-mail e reenviar os mesmos dados com suas credenciais originais. A política atual de confirmação do Supabase permanece inalterada.

Depois de iniciar o signup, o cadastro usa cancelamento interno limitado a 60 segundos, independente da desconexão HTTP. Uma falha de persistência devolve 503 com orientação para repetir a operação. Se o perfil já tiver sido confirmado no banco e só a resposta tiver sido perdida, a repetição recebe 409; a conta pode entrar normalmente pelo fluxo de login existente. Duplicidades de documento devem ser resolvidas pelo usuário antes de repetir.

Em conciliação administrativa, verificar a correspondência entre os UUIDs de Auth e perfil local; nunca recuperar por simples igualdade de e-mail, exportar dados pessoais para logs ou excluir contas sem análise e autorização. A confirmação de e-mail é distinta de aprovação da plataforma.

## Verificação segura

Com SDK .NET 10:

```powershell
dotnet build Resolvai.Api.slnx
dotnet run --project tests/Resolvai.Application.Checks
```

Os checks usam repositórios falsos e `HttpMessageHandler` simulado: não leem configuração da API, não acessam PostgreSQL nem fazem HTTP real. Cobrem PF/PJ, CPF/CNPJ, limites, especialidades, duplicidades, acesso por dono, cancelamento, retomada, respostas ofuscadas e regressão do Cliente.

A validação de SQL e rollback real deve ocorrer em PostgreSQL/Supabase isolado, com dados sintéticos: PF e PJ válidos; mesmo CPF entre papéis após 014; CPF repetido no mesmo papel; CNPJ repetido; especialidade desativada entre pré-validação e transação; falha em cada uma das quatro tabelas; tentativas concorrentes. Conferir também 401/403/404 e `Location` em execução local da API configurada exclusivamente para esse ambiente.

Para o frontend futuro, configurar `VITE_API_URL` e `Cors:AllowedOrigins` somente no ambiente apropriado, sem editar ou versionar credenciais reais. Não usar o banco de produção como ambiente de testes.

### Resultado da verificação local

- Solution compilada com SDK portátil .NET 10.0.403: zero erros e zero avisos.
- Checks de serviço, domínio, regressão Cliente e Auth HTTP simulado aprovados.
- API iniciada em localhost com content root vazio e configuração sintética, sem carregar os appsettings reais: rotas novas e Cliente presentes no OpenAPI; consultas privadas sem token retornam 401; cadastros Cliente e Prestador com corpo vazio retornam 400.
- Não foram executados scripts SQL, cadastros reais ou chamadas ao Supabase. A integração Dapper/constraints e o rollback real ainda precisam ser verificados em um PostgreSQL isolado antes da liberação.
