# Contrato: administração do painel de consentimentos

Endereço de administração, sem autenticação (RN71). Autor opcional em `x-sim-autor`. Erro: `{ "erros": ["CODIGO", ...] }`.

## `GET /admin/consentimentos/opcoes?cliente={documento}&banco={endereco}`
- 200: `{ cliente, tipo, banco, semProduto, grupos: [{ nome, permissoes[] }], contas: [{ resourceId, marcada }], cartoes: [{ resourceId, marcada }], agrupados: [tipo], receptoras: [{ organisationId, nome }] }`.
- 404: cliente ou banco inexistente. 422 `PEDIDO_INVALIDO`: parâmetro faltando.

## `POST /admin/consentimentos`
Corpo: `{ cliente, banco, receptora, permissoes[], contas?: [resourceId], cartoes?: [resourceId], prazoMeses?: 1..12, aprovacoesPendentes?: 1..10 }`.
- 201: a visão do consentimento (abaixo).
- 404: cliente ou banco inexistente.
- 422, com todos os códigos aplicáveis: `PEDIDO_INVALIDO`, `COMBINACAO_PERMISSOES_INCORRETA`, `PERMISSOES_PJ_INCORRETAS`, `INFORMACOES_PJ_NAO_INFORMADAS`, `PERMISSAO_PF_PJ_EM_CONJUNTO`, `SEM_PERMISSOES_FUNCIONAIS_RESTANTES`, `SEM_ORIGEM_SELECIONADA`, `ORIGEM_INVALIDA`, `CLIENTE_SEM_PRODUTO`, `PRAZO_INVALIDO`, `ALCADA_INVALIDA`, `RECEPTORA_INEXISTENTE`.
- Seleção de contas ou cartões sem a permissão do grupo correspondente: `ORIGEM_INVALIDA`.

## `GET /admin/consentimentos/{consentId}`
- 200: `{ consentId, cliente, banco, receptora, estado, permissoes, criadoEm, expiraEm?, rejeitadoPor?, razao?, alcada: { situacao, aprovacoesPendentes, prazo? }, decisoes: [{ autor, decisao, em }], recursos: [{ resourceId, tipo, estado }] }`. `estado` e `recursos[].estado` são os efetivos (vencimento, RN97).
- 404: inexistente.

## `POST /admin/consentimentos/{consentId}/aprovar` e `/recusar`
- 200: a visão depois da decisão.
- 404: inexistente.
- 409: sem pendência, já recusada, vencida, consentimento `REJECTED` ou escrita concorrente esgotada.
