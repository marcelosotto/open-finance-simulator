# Contratos próprios da spec 004

As rotas de Consentimentos e Recursos seguem `consents-3.3.1` e `resources-3.1.0`, sem escrita à mão.

## `POST /{banco}/token` (APIs Open Finance)
`application/x-www-form-urlencoded`: `grant_type=client_credentials`, `client_id`, `client_secret`, `scope`.

| `scope` | Resultado |
|---|---|
| `consents` | Token da receptora |
| `consent:{consentId}` | Token do consentimento (pode vir com outros escopos, que são ignorados) |

200: `{ "access_token", "token_type": "Bearer", "expires_in": 900, "scope" }`.

| Situação | Resposta |
|---|---|
| Banco inexistente / inativo | 404 / 503 |
| Campo ausente, `grant_type` errado, `scope` desconhecido | 400 `invalid_request` |
| Credencial inválida | 401 `invalid_client` |
| Consentimento inexistente, de outro banco, de outra receptora ou fora de `AUTHORISED` | 400 `invalid_scope` |

## Administração (`x-sim-autor` opcional)
| Rota | Corpo | Resposta |
|---|---|---|
| `POST /admin/receptoras` | `{ nome, organisationId? }` | 201 com credenciais e `credencialDeTeste: true`; 422 sem nome; 409 repetida |
| `GET /admin/receptoras` | — | Lista |
| `GET /admin/receptoras/{organisationId}` | — | 200 ou 404 |
| `POST /admin/consentimentos/{consentId}/revogar` | `{ motivo? }`; `motivo: "fraude"` grava `ASPSP` | 200; 404; 409 se já `REJECTED` |
| `PUT /admin/recursos/{resourceId}/estado` | `{ estado }` | 200; 404; 422 transição proibida |
| `POST /admin/clientes/{documento}/produtos` | `{ banco, produto, estado? }` | 201; 404; 422 cliente virtual, produto ou estado inválido |
