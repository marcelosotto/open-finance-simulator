# Feature Specification: Defeitos e anomalias

**Feature Branch**: `claude/009-defeitos-e-anomalias-44a40c`

**Created**: 2026-11-10

**Status**: Aprovada

**Input**: User description: "Defeitos e anomalias: UC22. Usar docs/ e defeitos-e-anomalias.feature. Nome curto: defeitos-e-anomalias"

**Origem em `docs/`**: [UC22](../../docs/01-requisitos/casos-de-uso/README.md#uc22), [RN110](../../docs/01-requisitos/regras-de-negocio.md#rn110) a [RN115](../../docs/01-requisitos/regras-de-negocio.md#rn115), [RN14](../../docs/01-requisitos/regras-de-negocio.md#rn14), [RN15](../../docs/01-requisitos/regras-de-negocio.md#rn15), [RN17](../../docs/01-requisitos/regras-de-negocio.md#rn17), [RN27](../../docs/01-requisitos/regras-de-negocio.md#rn27), [RN36](../../docs/01-requisitos/regras-de-negocio.md#rn36), [RN37](../../docs/01-requisitos/regras-de-negocio.md#rn37), [RN38](../../docs/01-requisitos/regras-de-negocio.md#rn38), [RN39](../../docs/01-requisitos/regras-de-negocio.md#rn39), [RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54), [RN88](../../docs/01-requisitos/regras-de-negocio.md#rn88), [PR02](../../docs/00-visao-geral/premissas.md#pr02), [PR04](../../docs/00-visao-geral/premissas.md#pr04), [RNF12](../../docs/01-requisitos/requisitos.md#rnf12), [modelo de dados](../../docs/04-dados/modelo-de-dados.md), [ADR-008](../../docs/05-adr/ADR-008-pipeline-de-avaliacao.md). Base: spec [007](../007-comportamento-e-cenarios/spec.md). Cenários Gherkin: [defeitos-e-anomalias.feature](../../docs/01-requisitos/gherkin/defeitos-e-anomalias.feature) e "Anomalia de chave com 422" de [paginacao.feature](../../docs/01-requisitos/gherkin/paginacao.feature).

## Clarifications

### Session 2026-11-10

Marcelo aprovou a opção A de Q1 a Q13 ("siga as recomendações"), repassada pela sessão controladora. Decisões registradas em `docs/`: RN110 a RN115 reescritas, RN116 e RN117 novas, notas em RN36, RN37 e RN88, UC22, modelo de dados, perfis e a linha da 010 em `sequencia-de-specs.md`. Evidências em [perguntas.md](perguntas.md).

- Q1: defeito → efeito `defeito` de regra de cenário; disputa só com defeitos.
- Q2: age só em 2XX com corpo JSON; sorteio em toda chamada que chega à etapa.
- Q3: conta na cota; não muda chave nem links; defeito antes da falha de transporte.
- Q4: `campo` opcional, critério fixo, só o primeiro item.
- Q5: `transactionId` copiado no segundo item; `totalRecords` + 1; data dois anos antes.
- Q6: `valor-incoerente` com `currency` `USD`.
- Q7: RN115 é o defeito `dado-muda-na-paginacao`, só na página seguinte.
- Q8: anomalia por banco, ligada ou desligada, com API em `/admin/cenarios/anomalias`.
- Q9: todo 401 do portão vira 403 `FORBIDDEN`, no host.
- Q10: chave ruim → 422 `PARAMETRO_INVALIDO`, antes da cota.
- Q11: outro UUID em toda resposta em que o portão devolveria o recebido.
- Q12: `anomaliasLigadas` na lista e no detalhe de instituições; aviso visual na 010.
- Q13: mudanças em `docs/` como propostas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Defeitos de dado (Priority: P1)

Como QA, quero que o banco devolva dado ruim de propósito, com taxa e alcance, para ver se a receptora rejeita o registro ou a resposta (RN110, RN111, RN112).

**Why this priority**: é o núcleo de UC22. Uma receptora que só recebe dado bom nunca prova a validação.

**Independent Test**: criar uma regra de defeito com 100% de frequência, consultar transações e conferir o efeito no corpo.

**Acceptance Scenarios**:

1. **Given** nenhuma regra de defeito, **When** Ana faz 1.000 consultas no Banco Estável, **Then** todas validam contra a spec.
2. **Given** o defeito `campo-obrigatorio-ausente` com 100%, **When** Ana consulta transações, **Then** o primeiro item vem sem `transactionAmount`.
3. **Given** o defeito `formato-invalido`, **Then** a data do primeiro item sai fora do formato.
4. **Given** o defeito `enum-invalido`, **Then** `type` do primeiro item sai fora da lista.
5. **Given** o defeito `transaction-id-repetido`, **Then** o segundo item repete o `transactionId` do primeiro.
6. **Given** o defeito `total-records-incoerente`, **Then** `totalRecords` difere da quantidade real.
7. **Given** o defeito `data-fora-da-janela`, **Then** a data do primeiro item fica fora do período.
8. **Given** um defeito com 10% e semente fixa, **When** Bruno roda 1.000 consultas duas vezes, reiniciando a sequência, **Then** as duas rodadas afetam as mesmas chamadas.

---

### User Story 2 - Anomalias de protocolo (Priority: P1)

Como QA, quero ligar no banco um desvio nomeado de regra oficial, para ver se a receptora tolera ou denuncia o desvio (RN113, RN114).

**Why this priority**: bancos reais desobedecem o manual, e a receptora precisa reagir.

**Independent Test**: ligar a anomalia no banco e provocar a situação.

**Acceptance Scenarios**:

1. **Given** `revogado-com-403` ligada no Banco Azul, **When** o consentimento é revogado e a receptora consulta, **Then** o status é 403.
2. **Given** `chave-recusada-com-422` ligada, **When** a receptora pede a página seguinte com chave expirada, **Then** o status é 422.
3. **Given** `interacao-diferente` ligada, **When** a receptora faz uma chamada válida, **Then** o `x-fapi-interaction-id` da resposta difere do enviado.
4. **Given** o Banco Azul com uma anomalia ligada, **When** Diego lista os bancos, **Then** o Banco Azul traz o aviso de anomalias ligadas.

---

### User Story 3 - Dado que muda durante a paginação (Priority: P2)

Como QA, quero que registros apareçam ou sumam entre as páginas, para ver se a receptora percebe a mudança de `totalRecords` (RN115).

**Why this priority**: é um caso raro, e RN37 garante a estabilidade por padrão.

**Independent Test**: ligar o defeito e ler as páginas com a mesma chave.

**Acceptance Scenarios**:

1. **Given** uma regra que adiciona registros entre páginas, **When** Ana lê todas as páginas, **Then** `totalRecords` muda entre a primeira e as seguintes.

### Edge Cases

- Defeito em resposta sem corpo (202, 204) ou sem o campo-alvo: a resposta sai normal (Q2, Q4).
- Defeito e erro da etapa (status, timeout, janela, sorteio do perfil): sai o erro, sem defeito, mas a sequência da semente avança (Q2).
- Defeito e falha de transporte na mesma chamada: o defeito é aplicado antes, e a falha corrompe o corpo já alterado (Q3).
- Resposta com defeito conta na cota; página seguinte não conta (RN27, Q3).
- O defeito não muda a chave nem os links (Q3).
- `dado-muda-na-paginacao` na primeira página ou com chave inválida: não age (Q7).
- `chave-recusada-com-422` sem chave ou com chave válida: não age (Q10).
- `revogado-com-403` não muda o 401 OAuth de `POST /token` (Q9).
- Anomalia em banco inexistente ou nome fora do catálogo: 404 na administração (Q8).
- Relógio do banco alterado: `data-fora-da-janela` usa o instante da chamada (RN73).

## Requirements *(mandatory)*

### Functional Requirements

**Defeitos**

- **FR-001**: Uma regra de cenário MUST aceitar o efeito `defeito`, com o nome do catálogo e, quando couber, `campo` ou `quantidade` (Q1, Q4, Q7).
- **FR-002**: O catálogo MUST ter `campo-obrigatorio-ausente`, `formato-invalido`, `enum-invalido`, `valor-incoerente`, `transaction-id-repetido`, `total-records-incoerente`, `data-fora-da-janela` e `dado-muda-na-paginacao` (RN110, RN115, Q6).
- **FR-003**: Regras de defeito MUST disputar só entre si, pela ordem de RN15, e valer junto com a regra de comportamento vencedora (Q1).
- **FR-004**: A frequência do defeito MUST ser sorteada em toda chamada que chega à etapa, com a sequência da semente (RN54), e o defeito MUST agir só na resposta 2XX com corpo JSON (Q2).
- **FR-005**: Os defeitos de campo MUST seguir os critérios de Q4 e Q5, sobre o primeiro item da página ou o objeto `data`.
- **FR-006**: Sem regra de defeito, nenhuma resposta MUST mudar (RN111).
- **FR-007**: A resposta MUST NOT indicar o defeito (RN112).
- **FR-008**: A resposta com defeito MUST contar na cota como 2XX, e o defeito MUST NOT mudar a chave ou os links (RN27, Q3).
- **FR-009**: `dado-muda-na-paginacao` MUST agir só na página seguinte, acrescentando ou tirando itens e ajustando `totalRecords` e `totalPages`, com resultado válido no schema (RN115, Q7).

**Anomalias**

- **FR-010**: Cada instituição MUST ter as anomalias do catálogo (`revogado-com-403`, `chave-recusada-com-422`, `interacao-diferente`), todas desligadas ao nascer (RN113, RN114, Q8).
- **FR-011**: A administração MUST listar o catálogo, mostrar o estado por banco e ligar ou desligar cada anomalia, com histórico (PR02, Q8).
- **FR-012**: `revogado-com-403` MUST trocar todo 401 do portão de segurança por 403 `FORBIDDEN` (Q9).
- **FR-013**: `chave-recusada-com-422` MUST responder 422 `PARAMETRO_INVALIDO` para chave inválida ou expirada, antes da cota (RN36, RN39, Q10).
- **FR-014**: `interacao-diferente` MUST devolver outro UUID v4 em `x-fapi-interaction-id` sempre que o portão devolveria o recebido (RN38, Q11).
- **FR-015**: A lista e o detalhe de instituições MUST trazer `anomaliasLigadas` (RN114, Q12).

**Geral**

- **FR-016**: Regras de defeito e anomalias MUST valer em até 5 s, pelo cache das regras, sem regredir as correções da 007 (RNF12).
- **FR-017**: O novo comportamento MUST ir para `docs/` antes do código (Q13), e os cenários do escopo MUST rodar sem `@spec-009` (ADR-009).

### Key Entities

- **Regra de defeito**: regra de cenário (spec 007) com efeito `defeito`, nome do catálogo, `campo` ou `quantidade`.
- **Anomalias da instituição**: o conjunto de anomalias ligadas num banco.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Os 7 cenários do escopo (14 casos com os exemplos) passam no CI, sem etiqueta.
- **SC-002**: Sem regra de defeito e sem anomalia, 100% das respostas validam contra o schema oficial.
- **SC-003**: A mesma regra de defeito, com a mesma semente, afeta as mesmas posições da sequência em 100% das repetições.
- **SC-004**: Cobertura de 100% no domínio e 80% no total (RNF08).

## Assumptions

- O defeito é aplicado ao corpo já montado, na saída da chamada, sem mudar a montagem dos dados.
- Uma regra de defeito pertence, como as demais, a no máximo um pacote.
- O aviso visual de RN114 é da spec 010.
