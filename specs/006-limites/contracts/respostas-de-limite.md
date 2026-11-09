# Respostas de limite

Corpo: envelope oficial `errors[]` e `meta`, como os demais erros (`accounts-2.5.1.yaml:1342-1373`).

| Status | `code` | `title` | `detail` | Cabeçalho |
|---|---|---|---|---|
| 529 | `SITE_IS_OVERLOADED` | Site sobrecarregado | A operação foi recusada porque o limite de requisições por segundo foi atingido. | — |
| 429 | `TOO_MANY_REQUESTS` | Muitas requisições | A operação foi recusada porque muitas solicitações foram feitas em um determinado período. | `Retry-After`, se ligado |
| 423 | `LOCKED` | Recurso bloqueado | A operação foi recusada porque o limite de consultas foi atingido. | — |
