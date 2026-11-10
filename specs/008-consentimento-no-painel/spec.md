# Feature Specification: Consentimento no painel

**Feature Branch**: `claude/008-consentimento-no-painel-997cad`

**Created**: 2026-11-10

**Status**: Aprovada

**Input**: User description: "Consentimento no painel: UC10 e UC11, com menu de criação e múltiplas alçadas. Usar docs/ e consentimento-no-painel.feature. Nome curto: consentimento-no-painel"

**Origem em `docs/`**: [UC10](../../docs/01-requisitos/casos-de-uso/README.md#uc10), [UC11](../../docs/01-requisitos/casos-de-uso/README.md#uc11), [RF09](../../docs/01-requisitos/requisitos.md#rf09), [RN08](../../docs/01-requisitos/regras-de-negocio.md#rn08), [RN49](../../docs/01-requisitos/regras-de-negocio.md#rn49), [RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71), [RN73](../../docs/01-requisitos/regras-de-negocio.md#rn73), [RN80](../../docs/01-requisitos/regras-de-negocio.md#rn80), [RN83](../../docs/01-requisitos/regras-de-negocio.md#rn83) a [RN85](../../docs/01-requisitos/regras-de-negocio.md#rn85), [RN87](../../docs/01-requisitos/regras-de-negocio.md#rn87), [RN95](../../docs/01-requisitos/regras-de-negocio.md#rn95), [RN98](../../docs/01-requisitos/regras-de-negocio.md#rn98), [RN100](../../docs/01-requisitos/regras-de-negocio.md#rn100) a [RN108](../../docs/01-requisitos/regras-de-negocio.md#rn108), [RN120](../../docs/01-requisitos/regras-de-negocio.md#rn120), [UI10](../../docs/01-requisitos/interface.md#ui10), [UI13](../../docs/01-requisitos/interface.md#ui13), [UI14](../../docs/01-requisitos/interface.md#ui14), sequências [01](../../docs/03-design/sequencia/01-consentimento-no-painel.md) e [04](../../docs/03-design/sequencia/04-multiplas-alcadas.md). Contratos oficiais: `consents-3.3.1` e `resources-3.1.0`, fixados em `specs.lock.json`. Cenários Gherkin: [consentimento-no-painel.feature](../../docs/01-requisitos/gherkin/consentimento-no-painel.feature) e "Renovação com múltiplas alçadas" de [consentimento-api.feature](../../docs/01-requisitos/gherkin/consentimento-api.feature).

## Clarifications

### Session 2026-11-10

Marcelo aprovou a opção A de Q1 a Q9 ("siga as recomendações"), repassada pela sessão controladora. Decisões registradas em `docs/` (RN95, nova RN96, nova RN97, RN98, nova RN99, UC10, UC11, linha 010 de `sequencia-de-specs.md`, modelo de domínio e modelo de dados). Evidências em [perguntas.md](perguntas.md). Cada (Qn) nos requisitos aponta a decisão.

- Q1: painel → API de administração; telas UI10, UI13 e UI14 na 010.
- Q2: recurso pendente → estado no consentimento calculado na leitura (RN97).
- Q3: alçada recusada ou vencida → consentimento segue `AUTHORISED`; só os recursos vão para `UNAVAILABLE`.
- Q4: pedido → permissões soltas validadas como na API, `RESOURCES_READ` sempre; seleção ausente = todas, vazia = recusa (RN96).
- Q5: prazo → meses de 1 a 12; sem prazo, indeterminado.
- Q6: receptora → obrigatória, ligada ao consentimento.
- Q7: alçadas → só PJ, 1 a 10, 15 dias, 409 fora de hora, decisões gravadas (RN99).
- Q8: origem → "pelo menos uma" só quando há opção; cliente sem produto no banco é recusado (RN95).
- Q9: erros → 422 da administração com códigos oficiais e próprios.
- RNs da 008: RN96, RN97 e RN99. A 009 usa RN116 a RN119.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar consentimento pelo menu (Priority: P1)

Como QA, desenvolvedor ou analista, quero criar um consentimento escolhendo cliente, instituição, receptora, permissões, origens e prazo, para ter dados prontos sem passar por login no banco ([UC10](../../docs/01-requisitos/casos-de-uso/README.md#uc10), RN80, RN95).

**Why this priority**: é o objetivo da spec; sem isso a receptora não tem consentimento pronto para testar.

**Independent Test**: criar Mariana com conta, cartão e empréstimo no Banco Estável, criar um consentimento pelo painel para a receptora "Coletor" e conferir estado, permissões, recursos e validade; depois pedir token com a receptora e consultar Recursos.

**Acceptance Scenarios**:

1. **Given** Mariana com conta e cartão no Banco Estável, **When** Bruno escolhe Mariana, o banco, a receptora "Coletor", contas e cartões com prazo de 12 meses e confirma, **Then** o consentimento nasce `AUTHORISED`, vence em 12 meses pelo relógio do banco, e a conta e o cartão aparecem em Recursos como `AVAILABLE`.
2. **Given** um pedido sem prazo, **When** Bruno confirma, **Then** o consentimento não tem data final (RN85).
3. **Given** permissões de contas e a lista de contas vazia, **When** Bruno confirma, **Then** nada é criado (RN95).
4. **Given** permissões de operações de crédito, **When** Bruno confirma, **Then** todas as operações de crédito do cliente naquele banco são compartilhadas, sem seleção (RN95).
5. **Given** um grupo incompleto de contas, **When** Bruno confirma, **Then** nada é criado, com `COMBINACAO_PERMISSOES_INCORRETA` (RN83).
6. **Given** um consentimento criado para "Coletor", **When** outra receptora pede token para ele, **Then** o pedido é recusado (RN120, Q6).

---

### User Story 2 - Ver só as opções que valem para o cliente (Priority: P1)

Como QA, quero que o menu mostre só as permissões do tipo do cliente e as contas e cartões dele, já marcados, para não montar um pedido impossível ([UI10](../../docs/01-requisitos/interface.md#ui10), [UI13](../../docs/01-requisitos/interface.md#ui13), RN84, RN95, RN08).

**Why this priority**: é o que torna o menu diferente da API.

**Independent Test**: pedir as opções para Mariana (PF) e para a Indústria Aurora (PJ) no Banco Estável e conferir grupos e origens.

**Acceptance Scenarios**:

1. **Given** o cliente PF Mariana, **When** Bruno abre as permissões, **Then** só aparecem permissões PF, sem `CUSTOMERS_BUSINESS_*`.
2. **Given** um cliente PJ, **When** Bruno abre as permissões, **Then** só aparecem permissões PJ.
3. **Given** Mariana com uma conta e um cartão, **When** Bruno abre as origens, **Then** a conta e o cartão aparecem marcados, e crédito aparece sem seleção.
4. **Given** um banco que não oferece cartões, **When** Bruno abre as permissões, **Then** o grupo de cartões não aparece (RN08).

---

### User Story 3 - Múltiplas alçadas na PJ (Priority: P1)

Como QA, quero criar o consentimento de uma empresa que exige várias aprovações e aprovar ou recusar no painel, para testar como a receptora trata recursos pendentes ([UC11](../../docs/01-requisitos/casos-de-uso/README.md#uc11), RN98, RN107).

**Why this priority**: `PENDING_AUTHORISATION` e `DEPENDE_MULTIPLA_ALCADA` só podem ser testados assim.

**Independent Test**: criar o consentimento da Indústria Aurora com duas aprovações pendentes, consultar Recursos, aprovar duas vezes, consultar de novo; repetir com recusa e com o relógio do banco 16 dias à frente.

**Acceptance Scenarios**:

1. **Given** um consentimento da Indústria Aurora com duas aprovações pendentes, **When** a receptora consulta Recursos, **Then** os recursos estão em `PENDING_AUTHORISATION`, e o acesso direto à conta responde 403 `STATUS_RESOURCE_PENDING_AUTHORISATION`.
2. **Given** o mesmo consentimento, **When** Bruno aprova duas vezes no painel, **Then** os recursos vão para `AVAILABLE`.
3. **Given** um consentimento com aprovação pendente, **When** Bruno recusa no painel, **Then** os recursos vão para `UNAVAILABLE`, e o consentimento segue `AUTHORISED` (Q3).
4. **Given** um consentimento com aprovação pendente há 16 dias pelo relógio do banco, **Then** os recursos estão em `UNAVAILABLE`, sem nenhuma ação.
5. **Given** um consentimento do Comércio Sol sem alçadas, **When** é criado, **Then** os recursos nascem `AVAILABLE`.
6. **Given** um consentimento da Indústria Aurora com duas aprovações pendentes, **When** a receptora tenta renovar, **Then** a resposta é 422 `DEPENDE_MULTIPLA_ALCADA` (RN87).
7. **Given** um consentimento já recusado ou sem pendência, **When** Bruno aprova, **Then** a resposta é 409 e nada muda (Q7).
8. **Given** a mesma conta da Indústria Aurora em outro consentimento sem alçada, **When** a receptora consulta Recursos desse outro, **Then** a conta está `AVAILABLE` (RN107, Q2).

### Edge Cases

- Cliente sem nenhum produto no banco escolhido: recusado (pré-condição de UC10, Q8).
- Cliente com permissão de contas e sem conta no banco: aceito, sem conta compartilhada (Q8).
- Conta ou cartão de outro cliente, de outro banco, ou id de outro tipo na seleção: recusado com `ORIGEM_INVALIDA` (Q4).
- Alçada pedida para cliente PF, ou fora de 1 a 10: recusada com `ALCADA_INVALIDA` (Q7).
- Prazo fora de 1 a 12 meses: recusado com `PRAZO_INVALIDO` (Q5).
- Receptora inexistente: recusada com `RECEPTORA_INEXISTENTE` (Q6).
- Recurso já `UNAVAILABLE` no banco durante a alçada pendente: continua `UNAVAILABLE` (Q2).
- Regra de cenário de estado (RN108) ativa sobre um recurso com alçada pendente: a regra vence enquanto durar.
- Relógio do banco antes da criação: o prazo da alçada não vence (tempo negativo conta como zero, RN73).
- Duas aprovações ao mesmo tempo na última pendência: só uma conta; a outra recebe 409.
- Banco inativo: a criação pela administração é aceita; as APIs Open Finance do banco seguem em 503 (RN06).
- Consentimento revogado (RN86) com alçada pendente: aprovar ou recusar responde 409; as APIs respondem 401 (RN88).

## Requirements *(mandatory)*

### Functional Requirements

**Opções do menu**

- **FR-001**: A administração MUST devolver, para um cliente e um banco, os grupos oficiais de permissões que cabem no tipo do cliente (só PF ou só PJ), sem os grupos de contas e cartões que o banco não oferece (RN08, RN84).
- **FR-002**: As opções MUST listar as contas e os cartões do cliente naquele banco, todos marcados, e indicar que os demais produtos compartilham tudo, sem seleção (RN95).
- **FR-003**: As opções MUST listar as receptoras cadastradas. Cliente ou banco inexistente MUST responder 404, e cliente sem produto no banco MUST ser indicado (Q8).

**Criação**

- **FR-004**: A criação MUST receber cliente, banco, receptora, permissões, seleção opcional de contas e cartões, prazo opcional em meses e alçadas opcionais, e acrescentar `RESOURCES_READ` (Q4).
- **FR-005**: As permissões MUST ser validadas como em `POST /consents`: grupos completos (RN83), PF e PJ conforme o tipo do cliente (RN84), produtos de seleção individual não oferecidos retirados, e recusa sem permissão funcional restante (RN08). Os erros MUST usar os códigos oficiais (Q9).
- **FR-006**: Seleção ausente MUST valer como todas as contas (ou cartões) do cliente no banco. Seleção vazia com o grupo pedido e o cliente com opção MUST ser recusada com `SEM_ORIGEM_SELECIONADA`. Id que não é conta ou cartão do cliente naquele banco MUST ser recusado com `ORIGEM_INVALIDA` (RN95, Q4, Q8).
- **FR-007**: Os demais produtos cobertos pelas permissões MUST entrar todos no consentimento (RN95).
- **FR-008**: O consentimento MUST nascer `AUTHORISED`, persistido, marcado (RN60 a RN66), com a hora de criação e a validade pelo relógio do banco; prazo de N meses vence N meses depois da criação; sem prazo, sem data final (RN73, RN80, RN85, Q5).
- **FR-009**: O consentimento MUST ficar ligado à receptora escolhida, com as mesmas regras do criado por API: só ela pede token e consulta (RN120, Q6).
- **FR-010**: A criação MUST ir para o histórico com o autor informado, e responder com o consentimento criado e os recursos com o estado no consentimento (RN71).
- **FR-011**: Pedido inválido MUST responder 422 no formato da administração, com todos os códigos aplicáveis e sem criar nada (Q9).

**Múltiplas alçadas**

- **FR-012**: A criação MUST aceitar, só para cliente PJ, de 1 a 10 aprovações pendentes; caso contrário, `ALCADA_INVALIDA` (RN98, Q7).
- **FR-013**: O estado de cada recurso **no consentimento** MUST ser calculado na leitura: alçada recusada ou vencida → `UNAVAILABLE`; alçada pendente → `PENDING_AUTHORISATION`, salvo recurso já `UNAVAILABLE`; senão, o estado gravado. Uma regra de cenário de estado MUST valer por cima. Nada MUST ser gravado no recurso (RN104, RN107, RN108, Q2).
- **FR-014**: O estado de FR-013 MUST valer em Recursos e em todas as APIs de produto: lista sem o recurso e 403 `STATUS_RESOURCE_<estado>` no acesso direto (RN104). Outros consentimentos com o mesmo recurso MUST NOT ser afetados.
- **FR-015**: Aprovar MUST descontar uma pendência; recusar MUST encerrar a alçada como recusada. Sem pendência, alçada recusada, prazo vencido ou consentimento `REJECTED` MUST responder 409 sem mudar nada. Escritas concorrentes MUST contar uma só vez (Q7).
- **FR-016**: O prazo da alçada MUST ser de 15 dias corridos desde a criação, pelo relógio do banco, fim excluído, e vencer sem nenhuma ação (RN73, RN98).
- **FR-017**: Cada decisão MUST gravar autor, decisão e hora no consentimento, e ir para o histórico (RN71, Q7).
- **FR-018**: Com alçada pendente, recusada ou vencida, o consentimento MUST seguir `AUTHORISED`, e a renovação MUST responder 422 `DEPENDE_MULTIPLA_ALCADA` (RN87, RN98, Q3).

**Consulta**

- **FR-019**: A administração MUST consultar um consentimento com situação da alçada (pendentes, recusada, vencida, prazo), decisões e recursos com o estado no consentimento (UI14).

**Geral**

- **FR-020**: O novo comportamento MUST ser registrado em `docs/` antes do código (RN96, RN97, RN99), e os 11 cenários do escopo MUST rodar sem a etiqueta `@spec-008` (ADR-009).
- **FR-021**: As respostas das APIs Open Finance para consentimentos do painel MUST validar contra o schema oficial (RNF01).

### Key Entities

- **Consentimento**: já existe desde a 004; ganha a situação da alçada (pendentes, recusada) e as decisões.
- **Decisão de alçada**: autor, decisão (aprovada ou recusada) e hora (classe `Aprovacao` do modelo de domínio).
- **Opções do menu**: grupos de permissões, contas e cartões marcados, produtos agrupados e receptoras, calculados, nunca gravados.
- **Estado do recurso no consentimento**: calculado do estado gravado, da alçada e da regra de cenário.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos 11 cenários do escopo passam no CI, sem etiqueta.
- **SC-002**: Um consentimento criado pelo painel fica pronto para a receptora pedir token na primeira tentativa, sem nenhuma outra configuração.
- **SC-003**: Nenhum pedido recusado deixa consentimento gravado (0 em 100% dos casos de erro testados).
- **SC-004**: Um recurso com alçada pendente em um consentimento continua `AVAILABLE` em 100% das consultas por outro consentimento sem alçada.
- **SC-005**: Com o relógio do banco 16 dias à frente, 100% dos recursos com alçada pendente aparecem `UNAVAILABLE`, sem processo agendado.
- **SC-006**: 100% das respostas de Recursos e das APIs de produto para consentimentos do painel validam contra o schema oficial, e a cobertura fica em 100% no domínio e 80% no total (RNF08).

## Assumptions

- Sem front-end nesta spec: o "painel" é a API de administração, e as telas UI10, UI13 e UI14 ficam na 010 (Q1).
- O cliente é escolhido pelo documento, como no resto da administração; a lista de clientes para escolher é da tela.
- Em PJ, o consentimento do painel não grava `loggedUser`; a renovação PJ já aceita qualquer CPF de cliente simulado com o CNPJ (RN87).
- Cliente virtual pode receber consentimento pelo painel, persistido sobre os recursos virtuais, como na API (RN80).
- Revogar pelo painel já existe desde a 004 (RN86) e não muda.
- Consentimentos de lote e do mundo virtual seguem sem alçada.
- O banco inativo não impede a criação pela administração.
