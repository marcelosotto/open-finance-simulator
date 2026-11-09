# Comportamento das APIs de dados

Rotas e schemas vêm dos 13 contratos em `specs.lock.json`.

| Situação | Resposta |
|---|---|
| Recurso de outro cliente, inexistente ou fora do consentimento | 403 `RESOURCE_FORBIDDEN` |
| Recurso fora de `AVAILABLE`, acesso direto | 403 `STATUS_RESOURCE_<estado>` |
| Parâmetro ou janela inválidos | 422 `PARAMETRO_INVALIDO` |
| Par de datas incompleto | 422 `PARAMETRO_NAO_INFORMADO` |
| Página além do total | 422 `PAGE_NOT_FOUND` |
| Chave inválida ou expirada | 200, com nova chave nos links |

`PUT /admin/recursos/{resourceId}/estado` responde 409 quando outra chamada grava primeiro e as tentativas se esgotam.
