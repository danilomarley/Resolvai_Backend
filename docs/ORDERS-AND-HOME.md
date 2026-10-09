# Pedidos e resumo da Home

As rotas leem as tabelas `public.pedidos`, `public.propostas` e `public.fotos_pedido` (scripts 006 a 008) e `public.users` (nome do prestador). Antes de usá-las, aplique os scripts de `db/scripts` em ordem numérica, incluindo `011_create_ix_pedidos_cliente_criacao.sql` (índice da listagem).

O antigo `003_create_orders.sql` foi removido: as rotas não usam mais `public.orders`. Se essa tabela foi criada em algum banco, ela pode ser descartada pelo responsável depois de conferir que não há dados a migrar.

A conexão do backend deve usar o proprietário das tabelas ou um papel de servidor com permissão de leitura. Não use esse acesso no frontend.

## Código

As classes desta funcionalidade seguem o padrão em português do domínio (`Contato`, `Endereco`). Os nomes das propriedades e dos valores de enum continuam em inglês porque formam o contrato JSON já consumido pelo front.

| Camada | Classes |
| --- | --- |
| Domain | `StatusPedido`, `StatusProposta` |
| Application | `IPedidoService`/`PedidoService`, `IPedidoQueries`, `ListarPedidosQuery`, `PedidoResumoResponse`, `PedidoResponse`, `FotoPedidoResponse`, `PropostaResponse`, `ResumoHomeResponse`, `ContagemPedidosResponse`, `ContagemPropostasResponse`, `PaginaResponse<T>` |
| Infrastructure | `PedidoQueries` |
| Api | `PedidosController` (`/api/v1/orders`), `HomeController` (`/api/v1/home`) |

## Rotas

| Método | Rota | Resposta | Issue |
| --- | --- | --- | --- |
| GET | `/api/v1/orders?status=&page=1&pageSize=10` | Lista paginada dos pedidos do usuário (Seus Pedidos) | Resolvai_Cliente#49 |
| GET | `/api/v1/orders/{id}` | Detalhe de um pedido com escopo, fotos e propostas | Resolvai_Cliente#49 |
| GET | `/api/v1/home/summary` | Dados dos cards e os 5 pedidos mais recentes | Resolvai_Cliente#50 |

Todas exigem `Authorization: Bearer <accessToken>` obtido no login. Todos os papéis, inclusive Admin, consultam apenas os pedidos em que são o cliente (`pedidos.id_cliente`). O identificador do usuário vem do token, nunca de parâmetros enviados pelo cliente. Usuários inativos ou tokens inválidos recebem 401. As respostas HTTP não devem ser armazenadas em cache (`Cache-Control: no-store`). Campos nulos são omitidos, conforme a configuração geral da API.

### Status

| Banco (`pedidos.status`) | API |
| --- | --- |
| `aberto` | `Pending` |
| `em_andamento` | `InProgress` |
| `concluido` | `Completed` |
| `cancelado` | `Cancelled` |

| Banco (`propostas.status`) | API |
| --- | --- |
| `enviada` | `Sent` |
| `visualizada` | `Viewed` |
| `escolhida` | `Chosen` |

### Conteúdo esperado em `pedidos.escopo`

Título, categoria, descrição e localização não são colunas: vêm do `jsonb` `escopo`. A rota de criação de pedido (Resolvai_Cliente#51) deve gravar estas chaves:

```json
{
  "titulo": "Infiltração na laje do quarto",
  "categoria": "Impermeabilização",
  "descricao": "A umidade aparece depois de chuvas.",
  "localizacao": "Aldeota, Fortaleza",
  "detalhes": "Mancha de 1 m² no canto do quarto",
  "urgencia": "Nos próximos dias",
  "especificacoes": "Inspeção, preparação da superfície e manta"
}
```

Sem `titulo`, o título passa a ser a `categoria`; sem ambos, `"Pedido"`. Sem `descricao`, a descrição é `""`. As fotos ficam em `fotos_pedido`, não no escopo. O detalhe devolve o `escopo` completo em `scope`, inclusive chaves extras.

## GET /api/v1/orders

Parâmetros de query, todos opcionais:

| Parâmetro | Padrão | Regra |
| --- | --- | --- |
| `status` | todos | `Pending`, `InProgress`, `Completed` ou `Cancelled` |
| `page` | 1 | inteiro ≥ 1 |
| `pageSize` | 10 | inteiro entre 1 e 50 |

Valores inválidos retornam 400 (`ValidationProblemDetails`). Uma página além da última retorna `items` vazio com os totais preenchidos. A ordenação é por `createdAt` decrescente, com desempate por `id`.

```json
{
  "items": [
    {
      "id": "10000000-0000-0000-0000-000000000002",
      "title": "Reparo hidráulico no banheiro",
      "category": "Hidráulica",
      "status": "InProgress",
      "createdAt": "2026-10-06T14:00:00Z",
      "updatedAt": "2026-10-08T14:00:00Z",
      "proposalsCount": 2,
      "chosenProposalValue": 1100.50
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 1,
  "totalPages": 1
}
```

`proposalsCount` conta todas as propostas recebidas pelo pedido. `chosenProposalValue` é o valor da proposta com status `escolhida` e é omitido enquanto nenhuma foi escolhida.

## GET /api/v1/orders/{id}

Retorna os campos da listagem, exceto os dois de propostas, e acrescenta:

| Campo | Origem |
| --- | --- |
| `description` | `escopo.descricao` (sempre presente, `""` se ausente) |
| `location` | `escopo.localizacao` |
| `scope` | `escopo` completo, como JSON |
| `photos[]` | `fotos_pedido`: `id`, `url` (valor da coluna `foto`, URL ou caminho no storage), `description` |
| `proposals[]` | `propostas` + `users`: `id`, `providerId`, `providerName`, `status`, `value`, `deadline`, `description`, `warranty` |

As propostas vêm com a escolhida primeiro, depois por valor crescente. Consultar o detalhe não marca propostas como visualizadas. Pedido inexistente ou de outro usuário retorna 404, assim como um identificador que não seja UUID.

## GET /api/v1/home/summary

Mantém o contrato anterior (`orders.total/pending/inProgress/completed/cancelled` e `recentOrders`) e acrescenta os dados dos cards:

```json
{
  "orders": {
    "total": 8,
    "pending": 4,
    "inProgress": 2,
    "completed": 1,
    "cancelled": 1,
    "active": 6,
    "createdLast7Days": 5
  },
  "proposals": {
    "total": 6,
    "awaitingDecision": 3,
    "unviewed": 2
  },
  "recentOrders": []
}
```

| Campo | Significado |
| --- | --- |
| `orders.active` | `pending + inProgress` (card "Pedidos ativos") |
| `orders.createdLast7Days` | pedidos criados nos últimos 7 dias, pelo relógio do banco ("novos nesta semana") |
| `proposals.total` | todas as propostas recebidas nos pedidos do usuário |
| `proposals.awaitingDecision` | propostas em pedidos `Pending`, que ainda aguardam escolha ("Propostas recebidas") |
| `proposals.unviewed` | dessas, as que estão com status `enviada` ("propostas para conferir") |

Cada item de `recentOrders` tem o mesmo formato dos itens de `GET /api/v1/orders`, e a lista é limitada a 5. Os totais consideram todos os pedidos do usuário, independentemente desse limite. Sem pedidos, todos os contadores são zero e a lista vem vazia.

O card "Sua avaliação" do protótipo não tem fonte de dados: não há tabela de avaliações.

## Consultas

As consultas usam parâmetros, timeout e cancelamento. Cada rota executa suas leituras numa transação curta `REPEATABLE READ`, então contadores, página e listas relacionadas vêm do mesmo snapshot. Os totais são agregados no PostgreSQL, sem carregar os pedidos em memória. O índice `ix_pedidos_cliente_criacao` começa pelo cliente e segue a ordenação da listagem.

Estas rotas só leem. A criação de pedidos é tratada na Resolvai_Cliente#51.

## Swagger e validação

Com a API em execução, acesse `/swagger` (local: `http://localhost:5172/swagger`). O documento completo está em `/openapi/v1.json`. Faça login, copie `accessToken` e use **Authorize** no Swagger. Exemplos de requisição estão em `src/Resolvai.Api/Resolvai.Api.http`.

Os testes de serviço rodam com `dotnet run --project tests/Resolvai.Application.Checks` (SDK .NET 10). Eles não acessam o banco. Com o banco de desenvolvimento, verifique:

1. Sem token ou com usuário inativo, as três rotas retornam 401.
2. Sem pedidos, a Home retorna contadores zerados e lista vazia.
3. Com pedidos de dois clientes, cada um vê apenas os próprios na lista, no detalhe e nos totais.
4. Pedido de outro cliente ou UUID inexistente retorna 404.
5. Com mais de cinco pedidos, `recentOrders` tem cinco itens e os totais consideram todos; a lista paginada respeita `page`/`pageSize`/`totalPages`.
6. Cada status aparece no contador e no filtro correspondentes; pedidos com a mesma data de criação são desempatados por `id`.
7. `page=0`, `pageSize=51` e `status` inexistente retornam 400.
