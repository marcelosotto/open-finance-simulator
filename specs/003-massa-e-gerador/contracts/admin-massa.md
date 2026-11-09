# Contrato: administração da massa (porta 8081)

Sem autenticação (RN71). Cabeçalho opcional `x-sim-autor` nas escritas; padrão `operador`. Erros: `{ "erros": ["mensagem"] }`, com 404, 409 ou 422.

| Método e caminho | Resposta |
|---|---|
| `GET /admin/massa/perfis` | 200, prontos e criados |
| `PUT /admin/massa/perfis/{nome}` | 200; 422 inválido ou pronto |
| `DELETE /admin/massa/perfis/{nome}` | 204; 404; 409 pronto ou em uso |
| `GET /admin/massa/distribuicoes` | 200 |
| `PUT /admin/massa/distribuicoes/{nome}` | 200; 422 |
| `DELETE /admin/massa/distribuicoes/{nome}` | 204; 404 |
| `POST /admin/massa/clientes` | 201; 422 |
| `GET /admin/massa/clientes/{documento}` | 200 com recursos e consentimentos; 404 |
| `POST /admin/massa/lotes` | 202; 422 |
| `GET /admin/massa/lotes` e `/{id}` | 200; 404 |
| `POST /admin/massa/lotes/{id}/cancelar` | 202; 404; 409 já terminado |
| `GET /admin/massa/mundo-virtual` | 200; 404 não definido |
| `PUT /admin/massa/mundo-virtual` | 200; 409 falta confirmar; 422 |
| `GET /admin/massa/mundo-virtual/itens?inicio=0&quantidade=1000&banco=` | 200; 404 não definido |
| `POST /admin/massa/ajustes` | 201; 404 alvo inexistente; 422 |
| `GET /admin/massa/consentimentos/{consentId}` | 200; 404 |
| `GET /admin/massa/recursos/{id}/transacoes?de=&ate=` | 200 com `transacoes` e `saldo`; 404 |
| `POST /admin/massa/limpezas` | 200 com o que será ou foi apagado; 404; 409 lote em andamento |
| `GET /admin/massa/historico` | 200, do mais novo ao mais antigo |

## Lote
```json
{ "quantidade": 100, "distribuicaoNome": "minha", "consentimentosPorCliente": 1, "permissoes": ["ACCOUNTS_READ"], "semente": 42, "historicoMeses": 12 }
```
Só `quantidade` é obrigatória. No lugar de `distribuicaoNome`, aceita `distribuicao` com o objeto.

## Distribuição
```json
{ "bancos": { "banco-estavel": 60, "banco-lento": 40 }, "perfis": { "cliente-comum": 100 }, "produtos": { "contas": 90 }, "pjPct": 10 }
```

## Cliente
```json
{ "tipo": "PF", "perfil": "cliente-comum", "bancos": ["banco-estavel"], "nome": "Mariana Alves", "semente": 7 }
```
`documento` não é um campo aceito.

## Mundo virtual
```json
{ "total": 10000000, "semente": 42, "distribuicao": { }, "confirmar": false }
```

## Ajuste
```json
{ "tipo": "transacao", "recursoId": "hml-p-r0x0", "dia": "2026-11-01", "posicao": 0, "valor": 90.00 }
{ "tipo": "consentimento", "consentId": "urn:hml:v-c0x0", "estado": "REJECTED" }
```

## Limpeza
```json
{ "alvo": "lote", "id": "lote-0", "confirmar": false }
```
`alvo`: `lote`, `instituicao` (com `id`), `ajustes-mundo` ou `tudo`. Sem `confirmar: true`, só mostra o que será apagado.

## Porta para as specs 004 e 005
`IConsultaDeMassa`: cliente por documento, consentimento e recurso por ID (persistidos ou virtuais, já com ajustes), transações por dia e saldo.
