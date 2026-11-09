# Contrato: administração de instituições (porta 8081)

Sem autenticação (RN71). Cabeçalho opcional `x-sim-autor` nas escritas; padrão `operador`.

| Método e caminho | Resposta |
|---|---|
| `GET /admin/perfis-de-comportamento` | 200, os 5 perfis |
| `GET /admin/instituicoes` | 200, lista com `endereco`, `nome`, `perfil`, `situacao` (UI02) |
| `POST /admin/instituicoes` | 201; 409 nome repetido; 422 inválido |
| `GET /admin/instituicoes/{endereco}` | 200 com `sla` e `avisos`; 404 |
| `PATCH /admin/instituicoes/{endereco}` | 200 com `sla` e `avisos`; 404; 422 |
| `DELETE /admin/instituicoes/{endereco}` | 200 com `resultado`: `apagada` ou `inativada`; 404 |
| `POST /admin/instituicoes/{endereco}/reativar` | 200; 404 |
| `GET /admin/instituicoes/{endereco}/historico` | 200, do mais novo ao mais antigo; 404 |

## POST
```json
{ "nome": "Banco Azul", "perfil": "lento", "inspiradoEmBancoReal": false, "publico": "ambos", "produtos": ["contas"], "marca": "Azul" }
```
Só `nome` é obrigatório.

## PATCH (JSON Merge Patch)
Campos aceitos: `nome`, `marca`, `publico`, `produtos`, `comportamento`, `limites`. Outro campo responde 422. Mudar o nome não muda o endereço (RN07).
```json
{ "comportamento": { "taxaDeErroPct": 5 }, "limites": { "qcaPorReceptora": { "org-1": 3000000 } } }
```

## GET de uma instituição
Traz os campos do [modelo](../data-model.md) e:
```json
{ "sla": [ { "classe": "alta", "slaMs": 1500, "p95Ms": 2500, "dentroDoSla": false } ],
  "avisos": [ { "campo": "capacidadeTps", "oficial": 300, "configurado": 100 } ] }
```

Erros: `{ "erros": ["mensagem"] }`.

## Instituição inativa nas APIs Open Finance
503 no envelope oficial, sem explicar o motivo (RN06, RN18).
