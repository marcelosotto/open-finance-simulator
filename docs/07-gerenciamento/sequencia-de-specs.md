# Sequência de specs (Spec Kit)

Relacionados: [casos de uso](../01-requisitos/casos-de-uso/README.md) · [cenários Gherkin](../01-requisitos/gherkin/) · [insumo da constituição](insumo-da-constituicao.md)

**Antes de rodar:** fechar as [perguntas em aberto](../01-requisitos/regras-de-negocio.md#perguntas-em-aberto) que bloqueiam cada spec e aprovar os casos de uso dela.

| # | Spec | Casos de uso | Cenários Gherkin | Depende de | Bloqueada por | Comando |
|---|---|---|---|---|---|---|
| 001 | `fundacao-e-catalogo` | [UC24](../01-requisitos/casos-de-uso/README.md#uc24), [UC23](../01-requisitos/casos-de-uso/README.md#uc23) | marcacao | — | PA23 | `/speckit-specify Fundação e catálogo: monólito modular, CI com cobertura, leitura das specs de open-finance-specs por commit, rotas geradas, tabela oficial de endpoints e marcação de ambiente. Usar docs/ e marcacao.feature. Nome curto: fundacao-e-catalogo` |
| 002 | `instituicoes` | [UC01](../01-requisitos/casos-de-uso/README.md#uc01), [UC02](../01-requisitos/casos-de-uso/README.md#uc02), [UC03](../01-requisitos/casos-de-uso/README.md#uc03) | instituicoes | 001 | PA10 | `/speckit-specify Instituições simuladas: UC01, UC02 e UC03, com os bancos prontos, endereço por banco e histórico. Usar docs/ e instituicoes.feature. Nome curto: instituicoes` |
| 003 | `massa-e-gerador` | [UC05](../01-requisitos/casos-de-uso/README.md#uc05) a [UC08](../01-requisitos/casos-de-uso/README.md#uc08), [UC27](../01-requisitos/casos-de-uso/README.md#uc27) | massa-e-semente | 002 | PA17, PA23 | `/speckit-specify Massa e gerador: UC05 a UC08 e UC27, com semente, mundo virtual, ajustes e lotes. Usar docs/ e massa-e-semente.feature. Nome curto: massa-e-gerador` |
| 004 | `consentimentos-e-recursos` | [UC25](../01-requisitos/casos-de-uso/README.md#uc25) (modo simples), [UC09](../01-requisitos/casos-de-uso/README.md#uc09), [UC12](../01-requisitos/casos-de-uso/README.md#uc12) a [UC15](../01-requisitos/casos-de-uso/README.md#uc15) | consentimento-api, recursos, seguranca | 003 | PA08 | `/speckit-specify Consentimentos e recursos: modo simples de segurança, UC09, UC12 a UC15. Usar docs/ e os .feature consentimento-api, recursos e seguranca. Nome curto: consentimentos-e-recursos` |
| 005 | `apis-de-dados` | [UC16](../01-requisitos/casos-de-uso/README.md#uc16), [UC17](../01-requisitos/casos-de-uso/README.md#uc17) | dados-e-transacoes, paginacao | 004 | PA09, PA13 | `/speckit-specify APIs de dados: UC16 e UC17, com todas as famílias de dados e paginação oficial. Usar docs/ e os .feature dados-e-transacoes e paginacao. Nome curto: apis-de-dados` |
| 006 | `limites` | [UC18](../01-requisitos/casos-de-uso/README.md#uc18) a [UC20](../01-requisitos/casos-de-uso/README.md#uc20) | limites | 005 | PA11 | `/speckit-specify Limites: UC18 a UC20, com TPM, limite operacional e capacidade em Redis. Usar docs/ e limites.feature. Nome curto: limites` |
| 007 | `comportamento-e-cenarios` | [UC04](../01-requisitos/casos-de-uso/README.md#uc04), [UC21](../01-requisitos/casos-de-uso/README.md#uc21), [UC28](../01-requisitos/casos-de-uso/README.md#uc28) | comportamento | 006 | PA19 | `/speckit-specify Comportamento e cenários: UC04, UC21 e UC28, com perfis, timeout, indisponibilidade e relógio. Usar docs/ e comportamento.feature. Nome curto: comportamento-e-cenarios` |
| 008 | `jornada-de-consentimento` | [UC10](../01-requisitos/casos-de-uso/README.md#uc10), [UC11](../01-requisitos/casos-de-uso/README.md#uc11) | jornada-de-consentimento | 004 | PA18 | `/speckit-specify Jornada de consentimento: UC10 e UC11, com telas do banco, receptora de demonstração e múltiplas alçadas. Usar docs/ e jornada-de-consentimento.feature. Nome curto: jornada-de-consentimento` |
| 009 | `defeitos-e-anomalias` | [UC22](../01-requisitos/casos-de-uso/README.md#uc22) | defeitos-e-anomalias | 007 | — | `/speckit-specify Defeitos e anomalias: UC22. Usar docs/ e defeitos-e-anomalias.feature. Nome curto: defeitos-e-anomalias` |
| 010 | `back-office` | [UC26](../01-requisitos/casos-de-uso/README.md#uc26) e telas de configuração | — | 009 | PA14 | `/speckit-specify Back-office: telas de bancos, massa, cenários e UC26. Usar docs/ e interface.md. Nome curto: back-office` |
| 011 | `seguranca-fapi` | [UC25](../01-requisitos/casos-de-uso/README.md#uc25) (modo FAPI) | seguranca (`@fapi`) | 008 | PA02, PA12 | `/speckit-specify Segurança FAPI: modo FAPI do UC25. Usar docs/ e os cenários @fapi. Nome curto: seguranca-fapi` |

O simulador já entrega valor sem a 011, porque o modo simples entra na 004.

## Ciclo de cada spec
1. `/speckit-specify` da linha.
2. `/speckit-clarify`: resolver ambiguidades.
3. `/speckit-plan`: plano com o Constitution Check.
4. `/speckit-tasks`: tarefas, com testes antes da implementação.
5. `/speckit-analyze`: consistência entre spec, plano e tarefas.
6. `/speckit-implement`: implementação.
