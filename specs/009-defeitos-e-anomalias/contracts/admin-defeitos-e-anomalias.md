# Contrato: administração de defeitos e anomalias

Porta de administração, sem autenticação (RN71). Autor em `x-sim-autor`. Erros: `{ "erros": [...] }`.

## Defeitos
São regras de cenário ([contrato da 007](../../007-comportamento-e-cenarios/contracts/admin-cenarios.md)) com efeito `defeito`:

```json
{ "instituicao": "banco-azul", "endpoint": "accountsGetAccountsAccountIdTransactions",
  "efeito": { "tipo": "defeito", "defeito": "campo-obrigatorio-ausente", "campo": "transactionAmount" },
  "frequenciaPct": 10, "semente": 42 }
```

Recusa com 422: nome fora do catálogo, `campo` em defeito que não é de campo, `quantidade` fora de `dado-muda-na-paginacao` ou igual a zero.

`GET /admin/cenarios/defeitos`: 200 com o catálogo (`nome`, `descricao`).

## Anomalias
| Método e rota | Resposta |
|---|---|
| `GET /admin/cenarios/anomalias` | 200 com o catálogo (`nome`, `descricao`) |
| `GET /admin/cenarios/anomalias/{banco}` | 200 com `[{ "nome", "ligada" }]`; 404 banco inexistente |
| `POST /admin/cenarios/anomalias/{banco}/{nome}/ligar` e `/desligar` | 200 com `{ "nome", "ligada" }`; 404 banco ou nome inexistente |

## Instituições
`GET /admin/instituicoes` e `GET /admin/instituicoes/{endereco}` ganham `anomaliasLigadas`: lista de nomes, vazia por padrão.

## Respostas oficiais
- Defeito: corpo alterado conforme RN117, sem indicação (RN112).
- `revogado-com-403`: `403` com `code` `FORBIDDEN` no lugar do 401 do portão.
- `chave-recusada-com-422`: `422` com `code` `PARAMETRO_INVALIDO`.
- `interacao-diferente`: `x-fapi-interaction-id` diferente do enviado.
