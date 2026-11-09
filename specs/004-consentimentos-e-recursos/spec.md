# Feature Specification: Consentimentos e recursos

**Feature Branch**: `claude/004-consentimentos-e-recursos-8f1b3e`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "Consentimentos e recursos: modo simples de segurança, UC09, UC12 a UC15. Usar docs/ e os .feature consentimento-api, recursos e seguranca. Nome curto: consentimentos-e-recursos"

**Origem em `docs/`**: [UC25](../../docs/01-requisitos/casos-de-uso/README.md#uc25), [UC09](../../docs/01-requisitos/casos-de-uso/README.md#uc09), [UC12](../../docs/01-requisitos/casos-de-uso/README.md#uc12), [UC13](../../docs/01-requisitos/casos-de-uso/README.md#uc13), [UC14](../../docs/01-requisitos/casos-de-uso/README.md#uc14), [UC15](../../docs/01-requisitos/casos-de-uso/README.md#uc15), [RF08](../../docs/01-requisitos/requisitos.md#rf08), [RF10](../../docs/01-requisitos/requisitos.md#rf10), [RF11](../../docs/01-requisitos/requisitos.md#rf11), [RF19](../../docs/01-requisitos/requisitos.md#rf19), [RN08](../../docs/01-requisitos/regras-de-negocio.md#rn08), [RN24](../../docs/01-requisitos/regras-de-negocio.md#rn24), [RN28](../../docs/01-requisitos/regras-de-negocio.md#rn28), [RN38](../../docs/01-requisitos/regras-de-negocio.md#rn38), [RN39](../../docs/01-requisitos/regras-de-negocio.md#rn39), [RN47](../../docs/01-requisitos/regras-de-negocio.md#rn47), [RN49](../../docs/01-requisitos/regras-de-negocio.md#rn49), [RN53](../../docs/01-requisitos/regras-de-negocio.md#rn53), [RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71), [RN73](../../docs/01-requisitos/regras-de-negocio.md#rn73), [RN80](../../docs/01-requisitos/regras-de-negocio.md#rn80), [RN82](../../docs/01-requisitos/regras-de-negocio.md#rn82) a [RN88](../../docs/01-requisitos/regras-de-negocio.md#rn88), [RN100](../../docs/01-requisitos/regras-de-negocio.md#rn100) a [RN108](../../docs/01-requisitos/regras-de-negocio.md#rn108), [RN120](../../docs/01-requisitos/regras-de-negocio.md#rn120), [RN122](../../docs/01-requisitos/regras-de-negocio.md#rn122), [RN123](../../docs/01-requisitos/regras-de-negocio.md#rn123), [RNF01](../../docs/01-requisitos/requisitos.md#rnf01), [RNF10](../../docs/01-requisitos/requisitos.md#rnf10), [ADR-006](../../docs/05-adr/ADR-006-modos-de-seguranca.md), [ADR-008](../../docs/05-adr/ADR-008-pipeline-de-avaliacao.md), [modelo de dados](../../docs/04-dados/modelo-de-dados.md). Contratos oficiais: `consents-3.3.1` e `resources-3.1.0`, fixados em `specs.lock.json`. Cenários Gherkin: [consentimento-api.feature](../../docs/01-requisitos/gherkin/consentimento-api.feature), [recursos.feature](../../docs/01-requisitos/gherkin/recursos.feature), [seguranca.feature](../../docs/01-requisitos/gherkin/seguranca.feature); e os passos pendentes da spec 003 em [massa-e-semente.feature](../../docs/01-requisitos/gherkin/massa-e-semente.feature).

## Clarifications

### Session 2026-11-09

Perguntas Q1 a Q9 enviadas a Marcelo em um único lote, aguardando resposta. Enquanto isso, os requisitos marcados com `[Qn]` trazem a recomendação, sem valor de decisão. Três deles têm marcador `[NEEDS CLARIFICATION]`, porque mudam o escopo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Autenticar a receptora (Priority: P1)

Como receptora, quero me identificar com credenciais de teste e receber um token, para chamar o simulador pelo mesmo caminho de produção ([UC25](../../docs/01-requisitos/casos-de-uso/README.md#uc25), RN120, RN122, RN123, ADR-006).

**Why this priority**: toda chamada protegida depende dessa identidade.

**Independent Test**: cadastrar uma receptora, pedir token para um consentimento autorizado, chamar `GET /resources` com ele e repetir sem token e com senha errada.

**Acceptance Scenarios**:

1. **Given** uma chamada sem token, **When** a receptora consulta contas, **Then** a resposta é 401.
2. **Given** um token de consentimento só com contas, **When** a receptora consulta cartões, **Then** a resposta é 403.
3. **Given** um consentimento revogado, **When** a receptora pede um token para ele, **Then** o pedido é recusado.
4. **Given** uma senha errada, **When** a receptora pede um token, **Then** o pedido é recusado.
5. **Given** uma credencial emitida pelo simulador, **When** ela é consultada, **Then** está identificada como credencial de teste.
6. **Given** uma chamada de administração sem usuário, **When** ela é feita no endereço de administração, **Then** é atendida; **When** é feita pelo endereço das APIs Open Finance, **Then** a resposta é 404.

---

### User Story 2 - Criar consentimento por API (Priority: P1)

Como receptora, quero criar um consentimento informando usuário, permissões e validade, para seguir direto para o token ([UC09](../../docs/01-requisitos/casos-de-uso/README.md#uc09), RN08, RN80, RN83, RN84, RN85).

**Why this priority**: é a porta de entrada do fluxo oficial.

**Independent Test**: criar consentimentos válidos e inválidos para um cliente simulado e conferir status, código de erro, permissões devolvidas e recursos compartilhados.

**Acceptance Scenarios**:

1. **Given** Mariana com conta e cartão no Banco Estável, **When** a receptora cria um consentimento de contas, **Then** responde 201 em `AUTHORISED`, e só a conta é compartilhada.
2. **Given** um CPF que não é de cliente simulado, **When** a receptora cria o consentimento, **Then** responde 422 `ERRO_NAO_MAPEADO`.
3. **Given** um banco que oferece contas e um grupo incompleto de contas, **When** a receptora cria, **Then** responde 422 `COMBINACAO_PERMISSOES_INCORRETA`.
4. **Given** um banco que não oferece contas, **When** a receptora pede só contas, **Then** responde 422 `SEM_PERMISSOES_FUNCIONAIS_RESTANTES`; **When** pede contas e cartões, **Then** responde 201 sem as permissões de contas.
5. **Given** um banco que não oferece crédito, **When** a receptora pede operações de crédito, **Then** responde 201 com as permissões de crédito.
6. **Given** permissões PJ sem dados PJ, dados PJ com permissões PF, ou PF e PJ juntas, **When** a receptora cria, **Then** responde 422 com `INFORMACOES_PJ_NAO_INFORMADAS`, `PERMISSOES_PJ_INCORRETAS` ou `PERMISSAO_PF_PJ_EM_CONJUNTO`, respectivamente.
7. **Given** um pedido sem `expirationDateTime`, **When** o consentimento é criado, **Then** não tem data final.

---

### User Story 3 - Consultar e revogar consentimento (Priority: P1)

Como receptora, QA ou desenvolvedor, quero consultar um consentimento e revogá-lo, para testar o que acontece depois da revogação ([UC12](../../docs/01-requisitos/casos-de-uso/README.md#uc12), RN49, RN82, RN86, RN88).

**Why this priority**: o 401 pós-revogação é o principal caso de erro que a receptora precisa tratar.

**Independent Test**: criar dois consentimentos do mesmo cliente, revogar um, conferir `REJECTED` com autor e 401 no revogado, e o outro segue válido.

**Acceptance Scenarios**:

1. **Given** um consentimento autorizado, **When** a receptora chama `DELETE`, **Then** responde 204, o estado é `REJECTED` e as APIs respondem 401 para ele.
2. **Given** um consentimento autorizado, **When** ele é revogado pela administração, **Then** a rejeição registra quem revogou.
3. **Given** dois consentimentos de Mariana no Banco Estável, **When** um é revogado, **Then** o outro continua válido.
4. **Given** um consentimento inexistente, revogado ou expirado, **When** a receptora consulta recursos, contas ou saldos, **Then** a resposta é 401.
5. **Given** um consentimento virtual revogado por ajuste, **When** a receptora chama as APIs, **Then** a resposta é 401.

---

### User Story 4 - Consultar recursos (Priority: P1)

Como receptora, quero saber o que posso consumir e em que estado está, para chamar só o que está disponível ([UC14](../../docs/01-requisitos/casos-de-uso/README.md#uc14), RN47, RN100, RN102, RN103, RN107).

**Why this priority**: entrega o primeiro dado consumível e fecha o ciclo com os IDs gerados na 003.

**Independent Test**: consultar recursos de um consentimento persistido e de um virtual, durante e depois da preparação da instituição.

**Acceptance Scenarios**:

1. **Given** duas contas, uma em `TEMPORARILY_UNAVAILABLE`, **When** a receptora consulta recursos, **Then** as duas aparecem, cada uma com seu estado.
2. **Given** um banco que leva 2 minutos para preparar, **When** a receptora consulta depois de 1 minuto, **Then** responde 202 sem corpo.
3. **Given** uma preparação acima de 5 minutos, **When** a receptora consulta depois do limite, **Then** responde 504.
4. **Given** um consentimento só com permissões cadastrais, **When** a receptora consulta, **Then** responde 200 com lista vazia.
5. **Given** um ID gerado por lote ou pelo mundo virtual, **When** a receptora consulta com um token dele, **Then** recebe os recursos.

---

### User Story 5 - Renovar consentimento (Priority: P2)

Como receptora, quero renovar um consentimento sem nova jornada e consultar o histórico das renovações ([UC13](../../docs/01-requisitos/casos-de-uso/README.md#uc13), RN87).

**Why this priority**: importa em consentimentos longos, mas o fluxo principal funciona sem ela.

**Independent Test**: renovar um consentimento válido, conferir a nova validade e `/extensions`, e provocar cada erro de RN87.

**Acceptance Scenarios**:

1. **Given** um consentimento que vence em 30 dias, **When** a receptora renova por mais 12 meses, **Then** a validade muda e a extensão aparece em `/extensions`.
2. **Given** uma data menor que a validade atual, **When** a receptora renova, **Then** responde 422 `DATA_EXPIRACAO_INVALIDA`.
3. **Given** um consentimento em `REJECTED`, **When** a receptora renova, **Then** responde 422 `ESTADO_CONSENTIMENTO_INVALIDO`.
4. **Given** um consentimento com múltiplas alçadas, **When** a receptora renova, **Then** responde 422 `DEPENDE_MULTIPLA_ALCADA`.

---

### User Story 6 - Alterar recursos (Priority: P2)

Como operador ou QA, quero mudar o estado de um recurso ou adicionar um produto, para simular bloqueio, encerramento ou produto novo ([UC15](../../docs/01-requisitos/casos-de-uso/README.md#uc15), RN101, RN106, RN108).

**Why this priority**: amplia os casos de teste, mas depende dos recursos já consultáveis.

**Independent Test**: percorrer as transições aceitas e recusadas pela administração e conferir o estado em `GET /resources` na chamada seguinte.

**Acceptance Scenarios**:

1. **Given** um recurso em `PENDING_AUTHORISATION`, **When** o operador muda para `AVAILABLE`, **Then** a mudança é aceita.
2. **Given** um recurso em `AVAILABLE`, **When** o operador muda para `PENDING_AUTHORISATION`, **Then** é recusada; **When** muda para `UNAVAILABLE`, **Then** é aceita.
3. **Given** um recurso em `UNAVAILABLE`, **When** o operador muda para `AVAILABLE`, **Then** é recusada, exceto em Fundos e Renda Variável.
4. **Given** um consentimento com operações de crédito, **When** o operador adiciona um empréstimo ao cliente, **Then** o empréstimo aparece em Recursos.
5. **Given** um recurso virtual, **When** o operador muda o estado, **Then** a mudança vira um ajuste e vale na chamada seguinte.

### Edge Cases

- Permissão que não existe nos agrupamentos oficiais: 400, como manda o contrato oficial.
- `DELETE` em consentimento já `REJECTED`: 422 com o código do contrato oficial.
- `expirationDateTime` no passado na criação: 422 `DATA_EXPIRACAO_INVALIDA`.
- Renovação de consentimento com prazo indeterminado para uma data determinada: 422 `DATA_EXPIRACAO_INVALIDA`, pois a validade não pode diminuir.
- Consentimento vencido ao ser consultado: o estado passa a `REJECTED` na leitura, usando o relógio injetado.
- Consentimento apagado por limpeza com token ainda válido: 401 em Recursos e nas APIs de produto. Esta é a forma de alcançar o caso "inexistente" de RN88, já que não se emite token para consentimento que não existe.
- Cliente simulado sem produto coberto naquele banco: 201, e Recursos responde 200 com lista vazia (RN102).
- Duração de preparação acima de 5 minutos: 202 até o limite, e depois 504, sem nunca terminar. Com a duração padrão 0, não há 202.
- Mudar um recurso para o estado em que já está: aceito, sem histórico.
- Instituição inativa: 503 antes de qualquer avaliação de token (RN06).

## Requirements *(mandatory)*

### Functional Requirements

**Receptora e token (modo simples)**

- **FR-001**: O operador MUST poder cadastrar uma receptora pela administração, e o simulador MUST gerar usuário e senha de teste (RN120). `[Q3]` Nome obrigatório; `organisationId` gerado se não for informado; sem receptora pronta ao subir o ambiente.
- **FR-002**: Toda credencial emitida MUST estar identificada como de teste e não servir fora do simulador (RN123).
- **FR-003**: A receptora MUST obter o token informando usuário, senha e `consentId`; só consentimento `AUTHORISED` recebe token, e credencial inválida é recusada (UC25, RN120). [NEEDS CLARIFICATION: Q1 — `POST /consents` e as demais chamadas sem consentimento pedem um token `client_credentials` no contrato oficial, e UC25 só prevê token por consentimento. Como a receptora se autentica nessas chamadas?]
- **FR-004**: O token MUST ligar a receptora, o consentimento, o cliente e os escopos das permissões concedidas; a identidade MUST vir do token, nunca de cabeçalho próprio (RN122, princípio VIII).
- **FR-005**: O token MUST deixar de valer quando o consentimento deixa de estar `AUTHORISED`, efeito verificado na própria chamada (RN88, RN122). `[Q2]` Validade de 900 segundos, pelo relógio injetado.
- **FR-006**: `[Q4]` Consentimento criado por uma receptora só aceita token, consulta e revogação dela; gerado por lote ou mundo virtual, aceita qualquer receptora cadastrada. Token usado em outro banco responde 401.

**Portão das APIs protegidas**

- **FR-007**: Toda rota oficial protegida MUST avaliar `x-fapi-interaction-id`, token e permissão antes de qualquer efeito de negócio, na ordem de RN39: 400 (RN38), 401 sem token ou com consentimento sem validade (RN88), 403 quando falta permissão (RN122). As etapas de capacidade, tráfego, limite operacional e cenário entram nas specs 006 e 007.
- **FR-008**: [NEEDS CLARIFICATION: Q7 — o portão vale já para as rotas de produto, que seguem em 501 até a spec 005, e quais cenários de `recursos.feature` e `consentimento-api.feature` passam para as specs 005, 007 e 008?]

**Consentimentos**

- **FR-009**: `POST /consents` MUST criar o consentimento em `AUTHORISED` e responder 201, com todos os produtos do cliente naquele banco cobertos pelas permissões (RN80, UC09).
- **FR-010**: Documento que não é de cliente simulado MUST responder 422 `ERRO_NAO_MAPEADO` (RN80, RNF10). `[Q6]` Em pedido PJ, o cliente é o CNPJ de `businessEntity`, e `loggedUser` MUST ser o CPF de um cliente simulado PF.
- **FR-011**: O sistema MUST validar os agrupamentos da tabela do contrato oficial, removendo produto individual não oferecido e mantendo os agrupados (Operações de Crédito, Investimentos e Câmbio), com os erros 422 de RN83, e MUST aplicar os erros de RN84.
- **FR-012**: Sem `expirationDateTime`, o prazo MUST ser indeterminado (RN85). Com a data, MUST ser futura.
- **FR-013**: `GET /consents/{consentId}` MUST devolver o consentimento, inclusive `REJECTED`, com o objeto `rejection` oficial.
- **FR-014**: `DELETE /consents/{consentId}` MUST levar o consentimento a `REJECTED` e responder 204; em `REJECTED`, 422 (RN86, contrato oficial).
- **FR-015**: A administração MUST permitir revogar qualquer consentimento, registrando o autor (RN71, RN82, RN86).
- **FR-016**: `REJECTED` MUST ser final e registrar quem rejeitou e a razão (RN82). `[Q5]` `DELETE` → `TPP` com `CUSTOMER_MANUALLY_REVOKED`; administração → `USER` com `CUSTOMER_MANUALLY_REVOKED`; vencimento → `ASPSP` com `CONSENT_MAX_DATE_REACHED`; fraude → `ASPSP` com `INTERNAL_SECURITY_REASON`.
- **FR-017**: Ao vencer, o consentimento MUST ser tratado como `REJECTED` em toda leitura, usando o relógio injetado (RN73, RN82).
- **FR-018**: Revogar um consentimento MUST NOT afetar outro do mesmo cliente (RN49).

**Renovação**

- **FR-019**: `POST /consents/{consentId}/extends` MUST aceitar só `AUTHORISED`, mudar só validade e finalidade, e aplicar os erros 422 de RN87. A mudança MUST aparecer em `GET /consents/{consentId}`.
- **FR-020**: `GET /consents/{consentId}/extensions` MUST listar as renovações feitas.
- **FR-021**: `[Q6]` Em PF, só o CPF criador renova; em PJ, qualquer CPF de cliente simulado com o CNPJ do consentimento. Documento diferente responde 422 `ERRO_NAO_MAPEADO`.
- **FR-022**: `[Q8]` Consentimento que exija múltiplas alçadas MUST responder 422 `DEPENDE_MULTIPLA_ALCADA` na renovação; o valor nasce na spec 008 (UC11).

**Recursos**

- **FR-023**: `GET /resources` MUST listar todo recurso selecionado, em qualquer estado, com tipo e estado, e sem Dados Cadastrais; sem recursos, 200 com lista vazia (RN47, RN100, RN102, RN107).
- **FR-024**: Enquanto a instituição prepara, a resposta MUST ser 202 sem corpo, até a duração configurada na instituição, limitada a 5 minutos depois da criação do consentimento; passado o limite, 504 (RN103).
- **FR-025**: A administração MUST permitir mudar o estado de um recurso, recusando as transições proibidas por RN101, e a mudança MUST valer na próxima chamada (RN108). Em recurso virtual, a mudança MUST virar ajuste (RN53).
- **FR-026**: A administração MUST permitir adicionar um produto ao cliente em um banco, com estado inicial opcional, padrão `AVAILABLE`. Em seleção agrupada, o produto MUST entrar nos consentimentos vigentes (RN106). `[Q9]` Adicionar produto a cliente virtual é recusado.
- **FR-027**: [NEEDS CLARIFICATION: Q9 — `DELETE` e `/extends` em consentimento virtual gravam ajuste, embora RN53 diga que ajustes nascem na administração? `POST /consents` para um cliente virtual é aceito?]

**Transversais**

- **FR-028**: Toda resposta MUST validar contra o schema oficial da versão fixada, com perfil `estavel` e sem defeitos (RNF01), e levar a marcação (RN60 a RN66).
- **FR-029**: Toda alteração por administração MUST gravar data, autor, antes e depois (RN71). As chamadas às APIs oficiais MUST NOT ser registradas (RN18).
- **FR-030**: Os passos pendentes de `massa-e-semente.feature` ("as chamadas com ele devem responder 401" e "os IDs devem funcionar nas APIs") MUST passar a chamar as APIs reais.

### Key Entities *(include if feature involves data)*

- **Receptora**: nome, `organisationId` e credenciais de teste; origem dos limites por receptora.
- **Token**: receptora, consentimento, cliente, escopos e validade.
- **Consentimento**: criado na 003, ganha receptora opcional, permissões finais, rejeição (autor e razão), extensões e aprovações pendentes.
- **Extensão**: nova validade, validade anterior, usuário e data.
- **Recurso**: criado na 003, com estado, transições e ligação a consentimentos.
- **Ajuste**: alteração sobre consentimento ou recurso virtual (003).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Todos os cenários `@spec-004` passam no CI, exceto os que forem movidos para outra spec com aprovação (Q7).
- **SC-002**: 100% das respostas de Consentimentos e Recursos, com perfil `estavel`, validam contra os contratos oficiais fixados.
- **SC-003**: Um consentimento revogado passa a responder 401 já na chamada seguinte, em 100% das tentativas.
- **SC-004**: Uma mudança de estado de recurso aparece na chamada seguinte em 100% das tentativas, persistido ou virtual.
- **SC-005**: A receptora percorre criar, autenticar, consultar recursos, renovar e revogar trocando só endereço e credenciais.
- **SC-006**: Nenhum teste depende de hora real, e a cobertura fica em 100% no domínio e 80% no total.

## Assumptions

- As specs 001 a 003 entregam, respectivamente, rotas e marcação, instituições com produtos e `preparacaoDeRecursosSeg`, e a porta de consulta da massa.
- O `Retry-After`, o TPM, a capacidade e o relógio por instituição entram nas specs 006 e 007. Consentimentos e Recursos não têm limite por origem nem limite operacional (RN24, RN28).
- O modo FAPI e a jornada com redirecionamento continuam adiados (RN120, ADR-006).
- O painel e as telas ficam nas specs 008 e 010. Aqui, "painel" significa uma operação de administração equivalente.
- Mudanças em `docs/` decorrentes das respostas entram antes do plano, pelo princípio I.
