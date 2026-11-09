# Spec 005: divergências e perguntas

**Respondidas em 2026-11-09.** Aprovadas as recomendações, salvo Q3 (o mínimo é dado por família) e Q7 (lançamentos futuros só em Contas e até 30 dias). Decisões na seção Clarifications da [spec](spec.md).

Conferência de `docs/` contra os contratos em `.specs/7bdb9c1a6c4c265d7aabcdbc662f94c61718135e/contracts/expansion/snapshots/` (os 13 arquivos de dados conferem com os `sha256` de `specs.lock.json`). Os números de linha são desses arquivos.

Os 69 endpoints de `tabela-de-endpoints.md` batem, um a um, com os caminhos dos 13 contratos. A RN38 confere (`accounts-2.5.1.yaml:1381`), e os padrões de ID aceitam os prefixos de RN62 (`accounts-2.5.1.yaml:1388`, `transactionId` em `:926`).

## BLOQUEIO: divergências entre docs e contratos

### D1. RN33 e `paginacao.feature` exigem totais que as transações não têm

RN33 diz que `meta.totalRecords` e `meta.totalPages` são obrigatórios. O cenário "Lista vazia" (`paginacao.feature:42-47`) consulta **transações** e confere esses campos. Nas 14 operações de transações, `meta` exige só `requestDateTime`, e os links não têm `last`:

| Contrato | Meta (só `requestDateTime`) | Links (`self`, `first`, `prev`, `next`) |
|---|---|---|
| `accounts-2.5.1.yaml` | `MetaOnlyRequestDateTime` `:1243` | `TransactionsLinks` `:1189` |
| `credit-cards-accounts-2.4.0.yaml` | `MetaOnlyRequestDateTime` `:1520` | `TransactionsLinks` `:1652` |
| `bank-fixed-incomes-1.1.0.yml` | `BankFixedIncomesTransactionsMeta` `:798` | `BankFixedIncomeTransactionsLinks` `:847` |
| `credit-fixed-incomes-1.1.0.yml` | `CreditFixedIncomesMetaTransactions` `:786` | `CreditFixedIncomesTransactionsLinks` `:732` |
| `variable-incomes-1.3.0.yml` | `MetaSingle` `:852` | `VariableIncomesTransactionsLinks` `:925` |
| `treasure-titles-1.1.0.yml` | `TreasureTitlesMetaTransaction` `:809` | `TreasureTitlesTransactionsLinks` `:432` |
| `funds-1.1.0.yml` | `MetaSingle` `:964` | `FundsTransactionsLinks` `:1037` |

Transações de fatura são diferentes: usam `Links` e `Meta` completos (`credit-cards-accounts-2.4.0.yaml:2027-2029`).

### D2. RN41 generaliza a janela futura, que só Contas declara

RN41 aceita "12 meses no futuro" em `/transactions` e `/transactions-current`. Só Contas diz isso (`accounts-2.5.1.yaml:265`, `:320`, `:1433`). Cartão: "últimos 12 meses" (`credit-cards-accounts-2.4.0.yaml:367`) e "últimos 7 dias" (`:425`), com D-6 em `:1781`. Investimentos: "últimos 12 meses" (`bank-fixed-incomes-1.1.0.yml:190`, `variable-incomes-1.3.0.yml:232`) e D-6 (`bank-fixed-incomes-1.1.0.yml:1203`, `credit-fixed-incomes-1.1.0.yml:244`, `funds-1.1.0.yml:260`).

### D3. RN31 impõe mínimo de 25, e 7 contratos declaram `minimum: 1`

RN31 entrega 25 quando se pede 5 e cita F05, do portal (nível 4 da ordem das fontes). Declaram `minimum: 1` (nível 3): `customers-2.3.0.yaml:2173`, `accounts-2.5.1.yaml:1480`, `credit-cards-accounts-2.4.0.yaml:1810`, `loans-2.6.0.yaml:1531`, `financings-2.4.0.yml:1451`, `unarranged-accounts-overdraft-2.5.0.yml:1434`, `invoice-financings-2.4.0.yml:1462`. Declaram `minimum: 25`: `bank-fixed-incomes-1.1.0.yml:1141`, `credit-fixed-incomes-1.1.0.yml:1499`, `variable-incomes-1.3.0.yml:1376`, `treasure-titles-1.1.0.yml:1170`, `funds-1.1.0.yml:1374`, `exchanges-1.1.0.yaml:948`.

F05 não está no repositório, então não confirmei o texto dela.

### D4. RN42 não vale como escrita em faturas

RN42 faz `from*`/`to*` andarem juntos e assume o dia atual sem datas. Em `/bills`, `fromDueDate` e `toDueDate` só dizem "Data inicial/final de filtragem", sem par nem padrão (`credit-cards-accounts-2.4.0.yaml:1740`, `:1823`). Em `/bills/{billId}/transactions`, a data exige o par (`:1753`) e não assume o dia atual (compare com `:1769`).

## Perguntas

### Q1. Como corrigir D1?
**Recomendo:** RN33 passa a valer "quando o contrato declara os totais"; `meta` das 14 operações leva só `requestDateTime`; `paginacao.feature:42-47` passa a consultar uma lista com totais (contas) e um novo cenário confere que transações vazias respondem 200 sem totais. Página além do total, em transações, continua 422 `PAGE_NOT_FOUND`.
**Motivo:** o princípio VII manda toda resposta validar contra o contrato, e o nível 3 vence decisão de projeto.

### Q2. Como corrigir D2?
**Recomendo:** só Contas aceita 12 meses no futuro. Cartão e investimentos aceitam até hoje, e data futura responde 422.
**Motivo:** é o que cada contrato declara. Alternativa: aceitar futuro em todas, ao custo de RN41 ficar mais larga que o contrato.

### Q3. O mínimo de 25 vale nas 7 APIs com `minimum: 1`?
**Recomendo:** manter RN31 em todas as famílias. Entregar 25 quando se pede de 1 a 24 não invalida a resposta, e pedir abaixo de 25 onde o contrato exige 25 também entrega 25, sem 422.
**Motivo:** é o que `paginacao.feature:17-21` aprova, e uma regra única é mais simples. Porém, se F05 não sustentar RN31 nas 7 APIs, a alternativa é respeitar o `page-size` pedido nelas.

### Q4. Como corrigir D4?
**Recomendo:** RN42 ganha a exceção de faturas. `/bills` aceita datas isoladas e, sem datas, devolve as faturas dos últimos 12 meses. `/bills/{billId}/transactions` exige o par e, sem datas, devolve a fatura inteira.
**Motivo:** o contrato não assume o dia atual nesses dois filtros, e o dia atual esconderia quase toda fatura.

### Q5. Até onde vai o dado de cada família?
Hoje a massa só calcula transação e saldo de conta (`IConsultaDeMassa`). RN46 cobra coerência apenas em contas e cartão.
**Recomendo:** coerência completa em contas e cartão. Nas demais famílias, dado determinístico, válido no schema e coerente só dentro da resposta. Ordem por data em todas as listas de transações (RN43; contrato explícito só em Contas e Cartão: `accounts-2.5.1.yaml:265`, `credit-cards-accounts-2.4.0.yaml:368`).
**Motivo:** princípio X; RN46 não pede mais que isso. A alternativa, coerência de parcelas, saldos e cotas em todas as famílias, multiplica o trabalho sem regra que a exija.

### Q6. Para onde vão os cenários que dependem de outras specs?
**Recomendo:** `@spec-006` em "Chave dentro da validade" (`paginacao.feature:64-67`) e no passo de cota do esquema RN36 (`:75`), separando a parte de nova chave e 200, que fica na 005; `@spec-009` em "Anomalia de chave com 422" (`:83-87`).
**Motivo:** a cota nasce na 006 e as anomalias na 009 (UC22); é a convenção `@spec-NNN`.

### Q7. Como a transação muda de estado (RN44)?
**Recomendo:** derivar do relógio injetado: data futura é `LANCAMENTO_FUTURO`; hoje, nos tipos com imutabilidade em D+1, `TRANSACAO_PROCESSANDO`, com ID provisório; demais casos, `TRANSACAO_EFETIVADA`, com ID definitivo. Tabela de imutabilidade: `accounts-2.5.1.yaml:47-88`; enum: `:1064-1066`.
**Motivo:** não pede API nova e é repetível (princípio VI). Implica gerar lançamentos futuros (RN59 fala só do histórico, e o domínio da massa ainda não os gera), o que precisa de confirmação. Alternativa: mudança só por ajuste de administração.

### Q8. O que a chave de paginação congela?
**Recomendo:** a chave guarda a consulta e o instante de emissão. Ajustes feitos depois não aparecem nas páginas seguintes (RN37). Chave com filtros diferentes dos originais é inválida (RN36). A chave não é gravada, e a validade conta desde a primeira emissão.
**Motivo:** RN37 pede lista estável, e o dado virtual não é gravado (RN52).

### Q9. Quais códigos para parâmetro inválido, e como tratar `reserved-balances`?
Os contratos não listam códigos (`code` é texto livre: `accounts-2.5.1.yaml:1358-1362`).
**Recomendo:** 422 `PARAMETRO_INVALIDO` para `page` menor que 1, `page-size` menor que 1 ou não inteiro, data malformada e `from*` depois de `to*`; 422 `PARAMETRO_NAO_INFORMADO` para par incompleto; janela excedida como 422 `PARAMETRO_INVALIDO`. `reserved-balances` declara `page` e `page-size`, sem `pagination-key` (`accounts-2.5.1.yaml:418`), e devolve objeto único (`:1293`): sempre uma página, sem chave.
**Motivo:** RN39 define 422 sem fixar o código. Registro proposto: nova linha em RN42.

### Q10. RN104 e RN105 valem em todas as famílias?
Essas regras citam F08 (Contas); `STATUS_RESOURCE_*` e `RESOURCE_FORBIDDEN` não aparecem em nenhum dos 13 contratos.
**Recomendo:** sim, em todas as famílias com recurso (todas, menos Dados Cadastrais).
**Motivo:** PA08 manda seguir a orientação de cada API, mas não há orientação das demais no repositório; uma regra única evita inventar variação por família.

## Pendências da spec 004 (aprovadas)

- RN33: `consents-3.3.1.yml:333-353` lista 200, 400, 401, 403, 404, 405, 406, 429, 500, 504 e 529 em `/extensions`, sem 422. Confirmado; o registro entra com as respostas.
- `MudarEstado` (`src/Modulos/Simulador.Consentimentos/Http/Rotas.cs:604`) hoje grava sem conferir versão.
