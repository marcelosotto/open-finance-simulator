# Data Model: Consentimento no painel

## Consentimento (coleção `consentimentos`, já existe)
Campos novos, opcionais, gravados só por consentimentos do painel:

| Campo | Tipo | Regra |
|---|---|---|
| `aprovacoesPendentes` | inteiro, 0 a 10 | já existia; o painel grava de 1 a 10 em PJ (RN99) |
| `alcadaRecusada` | booleano | verdadeiro depois de uma recusa (RN98) |
| `decisoes` | lista de { `autor`, `decisao` (`aprovada` ou `recusada`), `em` } | uma por aprovação ou recusa (RN99) |

Situação da alçada, calculada:
- `sem-alcada`: nenhuma pendência e nenhuma decisão.
- `pendente`: pendências > 0, sem recusa, antes de criação + 15 dias.
- `vencida`: pendências > 0, sem recusa, a partir de criação + 15 dias.
- `recusada`: com recusa.
- `aprovada`: sem pendência, com decisões.
- `encerrada` (só na visão da administração): consentimento `REJECTED` com alçada pendente ou vencida (RN99). Em todo `REJECTED` a visão mostra zero pendências.

## Estado do recurso no consentimento (calculado, RN97)
1. Alçada `recusada` ou `vencida` → `UNAVAILABLE`.
2. Alçada `pendente` → `PENDING_AUTHORISATION`, salvo estado gravado `UNAVAILABLE`. Em consentimento `REJECTED`, pendente ou vencida conta como `encerrada` → `UNAVAILABLE`.
3. Senão → estado gravado.
4. Regra de cenário de estado (RN108), se houver, por cima, só em chamadas Open Finance; a visão da administração não a aplica.

## Opções do menu (calculadas)
Grupos do tipo do cliente, sem contas e cartões que o banco não oferece; contas e cartões do cliente no banco, marcados, só se o banco ainda oferece o produto (senão a seleção os recusa com `ORIGEM_INVALIDA`); tipos agrupados que o cliente tem no banco; receptoras.
