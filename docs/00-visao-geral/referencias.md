# Referências

## <a id="ordem-das-fontes"></a>Ordem das fontes
Quando duas fontes divergem, vale esta ordem. Uma divergência que afete comportamento vira pergunta em aberto, nunca uma escolha silenciosa.

1. Regulação: Resolução Conjunta nº 1 e instruções normativas do Banco Central
2. Manuais incorporados por essas normas (APIs, Escopo, Experiência, Segurança, Monitoramento)
3. Specs OpenAPI oficiais fixadas em `open-finance-specs`
4. Orientações e tabelas do portal do Open Finance
5. Decisões deste projeto (premissas, regras de produto e ADRs)

A POC é só estudo e não entra nessa ordem.

## Fontes usadas

As páginas foram lidas no acervo preservado em `leonidasleme/openfinance` (`docs/regulatory`, captura de 30/10/2026). Página publicada no portal não prova vigência para uma instituição específica.

| ID | Fonte | Usada para |
|---|---|---|
| <a id="f01"></a>F01 | [IN BCB 615/2025 — Manual de APIs 7.0](https://www.bcb.gov.br/content/estabilidadefinanceira/especialnor/IN615.pdf) | Limites, desempenho, disponibilidade, padrões |
| <a id="f02"></a>F02 | [Referência de endpoints: frequência, SLA, timeout, TPM, TPS e limite](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/17957025) | [Tabela de endpoints](../01-requisitos/tabela-de-endpoints.md) |
| <a id="f03"></a>F03 | [Limites de tráfego](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/17989722) | TPM, faixas e TPS |
| <a id="f04"></a>F04 | [Limites operacionais](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/17924220) | Contagem mensal e `pagination-key` |
| <a id="f05"></a>F05 | [Paginação](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/17377470) | `page`, `page-size`, `links`, `meta` |
| <a id="f06"></a>F06 | [Orientações — Consentimento](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/219480491) | Estados, permissões, renovação, erros |
| <a id="f07"></a>F07 | [Orientações — Recursos](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/219512943) | Estados, polling, 202 |
| <a id="f08"></a>F08 | [Orientações — Contas](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/1739096252) | Comportamento por estado, `transactionId`, contraparte |
| <a id="f09"></a>F09 | [Orientações — Dados Cadastrais](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/287965185) | Dados cadastrais e Recursos |
| <a id="f10"></a>F10 | [Guia de Experiência v.26 — Compartilhamento PF via hybrid flow](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/2219246394) | Etapas da jornada e requisitos `REQ.DC` |
| <a id="f11"></a>F11 | [Guia de Experiência v.24 — Compartilhamento PJ via hybrid flow](https://openfinancebrasil.atlassian.net/wiki/spaces/OF/pages/2147649715) | Jornada PJ |
| <a id="f12"></a>F12 | Specs OpenAPI em `open-finance-specs` (Accounts 2.5.1, Consents 3.3.1, Resources 3.1.0 e outras) | Contratos, parâmetros, cabeçalhos |
| <a id="f13"></a>F13 | Catálogo de regras `docs/regulatory/catalog/rules` (fichas `BR-*`) | Índice das regras; todas as fichas estão com revisão humana pendente |

## Material interno
- POC: `src/OpenFinance.FakeProxy` e `docs/transmissora-simulada.md` no repositório OpenToken
- Especificação da plataforma: `docs/` em `leonidasleme/openfinance`
- Método de documentação: repositório `app-transporte-escolar`
