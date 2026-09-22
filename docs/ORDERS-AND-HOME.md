# Pedidos e resumo da Home

Execute `db/scripts/003_create_orders.sql` após os scripts 001 e 002 antes de usar as rotas. O script é uma migração para executar uma única vez. A conexão do backend deve usar o proprietário da tabela ou um papel de servidor com BYPASSRLS e permissão de leitura; não use esse acesso no frontend. O acesso direto de `anon` e `authenticated` à tabela está bloqueado.

## Rotas

| Método | Rota | Resposta |
| --- | --- | --- |
| GET | `/api/v1/orders/{id}` | Detalhes de um pedido do usuário autenticado |
| GET | `/api/v1/home/summary` | Totais por status e os 5 pedidos mais recentes do usuário |

Ambas exigem `Authorization: Bearer <accessToken>` obtido no login. Todos os papéis, inclusive Admin, consultam apenas seus próprios pedidos. O identificador do usuário vem do token, nunca de parâmetros enviados pelo cliente. Usuários inativos ou tokens inválidos recebem 401. Pedido inexistente ou de outro usuário retorna 404. Um identificador que não seja UUID não corresponde à rota e retorna 404.

O detalhe retorna `id`, `title`, `description`, `status`, `createdAt` e `updatedAt`. O último campo é omitido quando nulo, conforme a configuração geral da API. Status possíveis: `Pending`, `InProgress`, `Completed`, `Cancelled`.

Exemplo de Home sem pedidos:

```json
{
  "orders": {
    "total": 0,
    "pending": 0,
    "inProgress": 0,
    "completed": 0,
    "cancelled": 0
  },
  "recentOrders": []
}
```

Cada item de `recentOrders` contém os mesmos campos do detalhe, exceto `description`. A ordenação é por `createdAt` decrescente, com desempate por `id`. Os totais consideram todos os pedidos do usuário, independentemente do limite de cinco itens. Não há filtro de período. Datas são armazenadas como `timestamptz`.

As consultas usam parâmetros, timeout e cancelamento. O índice começa pelo usuário e segue a ordenação da lista. Os totais são agregados no PostgreSQL, sem carregar todos os pedidos em memória. Uma transação curta de leitura com snapshot consistente mantém lista e totais alinhados. As respostas HTTP não devem ser armazenadas em cache.

Esta entrega disponibiliza leitura. A criação e atualização de pedidos não fazem parte destas rotas. Até existir esse fluxo, os dados devem ser inseridos por um processo confiável no backend/banco; não há rota pública de escrita.

## Swagger e validação

Com a API em execução, acesse `/swagger` (local: `http://localhost:5172/swagger`). O documento completo, incluindo autenticação, usuários, pedidos e Home, está em `/openapi/v1.json`. Faça login, copie `accessToken` e use **Authorize** no Swagger. As novas operações incluem descrições, tipos de resposta e indicação de Bearer.

Exemplos de requisição estão em `src/Resolvai.Api/Resolvai.Api.http`.

Verificações com banco de desenvolvimento:

1. Sem token, ambas as rotas devem retornar 401.
2. Sem pedidos, a Home deve retornar os quatro contadores e o total zerados, com lista vazia.
3. Com pedidos de dois usuários, cada Home deve contar e listar somente os pedidos do próprio usuário.
4. Consultar pedido de outro usuário ou UUID inexistente deve retornar 404.
5. Com mais de cinco pedidos, a lista deve conter cinco itens e os totais devem considerar todos.
6. Verificar cada status nos totais e o desempate de pedidos com a mesma data de criação.

Os testes de serviço podem ser executados com `dotnet run --project tests/Resolvai.Application.Checks` usando o SDK .NET 10. Eles não acessam o banco; os cenários SQL/HTTP acima exigem a API configurada e a migração aplicada.
