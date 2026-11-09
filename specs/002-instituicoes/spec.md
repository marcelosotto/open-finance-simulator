# Feature Specification: Instituições simuladas

**Feature Branch**: `claude/002-instituicoes-b8feb2` (feature `002-instituicoes`)

**Created**: 2026-11-09

**Status**: Clarified

**Input**: User description: "Instituições simuladas: UC01, UC02 e UC03, com os bancos prontos, endereço por banco e histórico. Usar docs/ e instituicoes.feature. Nome curto: instituicoes"

**Origem**: UC01, UC02, UC03 · RN01 a RN13, RN20 a RN37 (valores de configuração), RN60 a RN66, RN71 · RF01, RF02, RF03 · UI02 a UI05 (dados que as telas vão usar) · PR01, PR02, PR07 · ADR-003, ADR-007, ADR-008, ADR-009 · `docs/03-design/perfis-de-comportamento.md` · `docs/00-visao-geral/personas.md` · `docs/01-requisitos/gherkin/instituicoes.feature`

## Clarifications

### Session 2026-11-09

- Q: Qual status usa um banco fora do ar, e o que fazer com o 503 do perfil `instavel`? → A: Banco inativo responde 503. O 503 sai dos pesos do `instavel` e fica só para "fora do ar" (RN06, RN13, RN16).
- Q: A 002 aplica latência e erro sorteados nas chamadas? → A: Não. A 002 entrega configuração, resolução pelo endereço e "fora do ar"; o sorteio é da 007.
- Q: Como o histórico registra o autor, sem login? → A: Campo opcional `autor`, informado por quem chama a administração, com padrão `operador` (RN05, RN71).
- Q: Como tratar cenários que dependem de specs futuras? → A: RN08 fica com `@spec-004`; RN06 roda com um dublê do cadastro de clientes.
- Q: Como se forma o endereço? → A: Do nome, sem o prefixo do ambiente, em minúsculas e sem acento; fixo após a criação; colisão conta como nome repetido (RN07).
- Q: O que acontece ao apagar ou reativar? → A: Sem clientes, apaga; banco pronto só inativa; reativar é permitido e entra no histórico (RN06).
- Q: A 002 permite criar perfis próprios? → A: Não; só os 5 prontos, em leitura.
- Q: Onde fica o QCA sobrescrito por receptora? → A: Na instituição, por `organisationId` da receptora, sem conferir se ela existe; a 004 faz a ligação.
- Q: Como os perfis viram tempo típico, variação e cauda? → A: Típico = p50; variação = p95 − p50; cauda = % acima de 15 s; as classes escalam 1; 1; 1,33; 2,67.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Encontrar os bancos prontos e chamar cada um pelo endereço (Priority: P1)

Ao subir o ambiente, Diego já encontra os 5 bancos de perfil e os 11 de nome real. Ana troca só a URL base e chega ao banco escolhido, sem cabeçalho próprio do simulador.

**Why this priority**: sem banco cadastrado, nenhuma outra spec tem onde responder. Substitui o resolvedor provisório da spec 001.

**Independent Test**: subir um ambiente vazio, listar as instituições e chamar um caminho oficial com o endereço de um banco existente e com um endereço inexistente.

**Acceptance Scenarios**:

1. **Given** um ambiente recém-criado, **When** Diego lista as instituições, **Then** existem "Banco Estável", "Banco Lento", "Banco Instável", "Banco Timeout", "Banco Restritivo" e os 11 de nome real (RN02).
2. **Given** a marca `hml`, `dev` ou vazia, **When** o ambiente nasce, **Then** o banco de nome real se chama `HML-Itaú`, `DEV-Itaú` ou `Itaú` (RN09).
3. **Given** um ambiente que já subiu e teve um banco editado, **When** ele sobe de novo, **Then** a edição continua e nenhum banco é duplicado.
4. **Given** o endereço do "Banco Lento", **When** Ana chama um caminho oficial, **Then** a chamada é atribuída ao "Banco Lento", sem cabeçalho próprio exigido (RN07).
5. **Given** um endereço que não é de nenhum banco, **When** Ana chama, **Then** a resposta é 404 no envelope de erro oficial.

---

### User Story 2 - Cadastrar uma instituição (Priority: P1)

Diego cria um banco informando só o nome, e opcionalmente perfil, público, produtos e marca. O sistema gera `organisationId` e endereço.

**Why this priority**: é o objetivo central do UC01.

**Independent Test**: criar um banco só com o nome, abri-lo e chamar o endereço dele.

**Acceptance Scenarios**:

1. **Given** nenhum banco "Banco Azul", **When** Diego o cria só com o nome, **Then** ele existe com o perfil `estavel`, `organisationId` e endereço próprios (RN03).
2. **Given** a marca `hml`, **When** Diego cria um banco inspirado no "Itaú", **Then** o nome é `HML-Itaú` (RN09).
3. **Given** o "Banco Azul" existe, **When** Diego cria outro com o mesmo nome, **Then** o cadastro é recusado (UC01).
4. **Given** o "Banco Azul", **When** Diego o abre, **Then** vê público, produtos, marca e `organisationId` (RN01).
5. **Given** um banco que não oferece contas, **When** outra spec pergunta se ele oferece o produto, **Then** a resposta é "não" (RN08).

---

### User Story 3 - Configurar comportamento (Priority: P2)

Diego ajusta latência por classe de frequência, taxa de erro e pesos por status de um banco, sem mudar o perfil nem os outros bancos. O valor salvo vale na próxima chamada.

**Why this priority**: entrega o UC02 e é a base da spec 007.

**Independent Test**: copiar um perfil para um banco, alterar valores e confirmar que o perfil e os outros bancos ficaram iguais.

**Acceptance Scenarios**:

1. **Given** o "Banco Azul" usa o perfil `lento`, **When** Diego muda a latência dele, **Then** o perfil `lento` e os outros bancos com esse perfil não mudam (RN04).
2. **Given** a taxa de erro do "Banco Azul", **When** Diego a muda para 5%, **Then** a próxima chamada usa 5%, sem reiniciar (RN05).
3. **Given** uma taxa fora de 0 a 100%, **When** Diego salva, **Then** é recusado (UC02).
4. **Given** um status que nenhuma spec declara, como 418, ou 429, 423 ou 529, **When** Diego o configura, **Then** é recusado (RN13, UC02).
5. **Given** a configuração de um banco, **When** ela é consultada, **Then** cada classe de frequência mostra o SLA oficial e se o valor fica dentro ou fora dele (RN11, UI04).

---

### User Story 4 - Configurar limites e paginação (Priority: P2)

Diego vê os valores oficiais já preenchidos, informa o QCA, liga ou desliga o limite operacional e ajusta TPS, `page-size` máximo e validade da chave.

**Why this priority**: entrega o UC03 e prepara as specs 005 e 006.

**Independent Test**: abrir um banco novo, conferir os valores oficiais, alterar abaixo do oficial e ler o aviso.

**Acceptance Scenarios**:

1. **Given** um banco novo, **When** Diego abre os limites, **Then** vê os valores oficiais de TPM, limite operacional, capacidade (300 TPS), `page-size` máximo (1.000) e validade da chave (60 min) (RN21, RN26, RN29, RN31, RN35).
2. **Given** um banco, **When** Diego informa o QCA, **Then** o valor vale para toda receptora e define a faixa do TPM da alta frequência; sem valor, vale a primeira faixa (RN21).
3. **Given** um valor abaixo do oficial, **When** Diego salva, **Then** o sistema aceita e devolve um aviso (RN26, UI05).
4. **Given** o valor do banco, **When** Diego sobrescreve o QCA para uma receptora, **Then** só essa receptora usa o novo valor (RN21).
5. **Given** o "Banco Restritivo", **When** Diego o abre, **Then** TPM e limite operacional estão na metade do oficial, com `page-size` máximo 200 e chave de 10 min.

---

### User Story 5 - Histórico e inativação (Priority: P3)

Toda alteração fica no histórico, com data, antes e depois. Um banco com clientes não é apagado: é inativado e passa a responder como fora do ar.

**Why this priority**: dá rastreabilidade, e depende das histórias anteriores.

**Independent Test**: mudar um valor e ler o histórico; inativar um banco e chamar o endereço dele.

**Acceptance Scenarios**:

1. **Given** uma mudança de configuração, **When** Diego lê o histórico do banco, **Then** vê data, autor, valor antigo e novo (RN05, RN71).
2. **Given** o "Banco Azul" tem clientes, **When** Diego tenta apagá-lo, **Then** o banco é inativado (RN06).
3. **Given** um banco inativo, **When** Ana chama o endereço dele, **Then** a resposta é a de fora do ar, sem explicar o motivo (RN06, RN18).

---

### Edge Cases

- Dois nomes que geram o mesmo endereço (`Banco Azul` e `banco azul`): tratados como nome repetido.
- Marca do ambiente alterada entre subidas: os bancos prontos existentes mantêm o nome gravado (RN64).
- Peso negativo em um status: recusado.
- Todos os pesos zerados com taxa de erro acima de zero: recusado, porque não haveria status a sortear.
- Valor negativo ou zero em limite, `page-size` ou validade da chave: recusado.
- Duas edições simultâneas do mesmo banco: a última vence, e as duas entram no histórico.
- Nome sem letra ou número, ou que gere um endereço acima de 64 caracteres: recusado.

## Requirements *(mandatory)*

### Functional Requirements

**Bancos prontos e endereço**

- **FR-001**: O sistema MUST criar, ao subir, os 5 bancos de perfil e os 11 de nome real que faltarem, sem sobrescrever os existentes (RN02, RF02).
- **FR-002**: Bancos de nome real MUST usar o perfil `estavel` e o QCA da tabela de personas (RN09).
- **FR-003**: O nome de banco inspirado em banco real MUST levar a marca do ambiente em maiúsculas com hífen na frente; com a marca vazia, sem prefixo (RN09).
- **FR-004**: A instituição chamada MUST ser identificada só pelo endereço, sem cabeçalho próprio (RN07, ADR-007). O cadastro MUST substituir o resolvedor provisório da spec 001.
- **FR-005**: Endereço que não pertence a nenhuma instituição MUST responder 404 no envelope oficial.
- **FR-006**: O endereço MUST sair do nome, sem o prefixo do ambiente, em minúsculas e sem acento, e MUST NOT mudar depois da criação (RN07).

**Cadastro**

- **FR-007**: Criar uma instituição MUST exigir só o nome; sem perfil, vale `estavel` (RN03).
- **FR-008**: Público, produtos e marca MUST ter padrão e poder ser ajustados (RN01, UC01).
- **FR-009**: O sistema MUST gerar `organisationId` marcado com ambiente e natureza persistido (RN01, RN62).
- **FR-010**: Nome repetido MUST ser recusado (UC01).
- **FR-011**: O sistema MUST informar a outras specs quais produtos cada instituição oferece (RN08).
- **FR-012**: Os dados persistidos MUST sobreviver a uma reinicialização (RN51).

**Perfis e comportamento**

- **FR-013**: O sistema MUST oferecer os 5 perfis de `perfis-de-comportamento.md`, sem nenhuma anomalia ligada (RN114).
- **FR-014**: Aplicar um perfil MUST copiar os valores para a instituição; mudar a cópia não altera o modelo nem outras instituições (RN04).
- **FR-015**: O operador MUST poder ajustar por instituição a latência por classe de frequência, a taxa de erro e os pesos por status (UC02, RN10, RN13). Não há latência por endpoint (RN10).
- **FR-016**: Taxa de erro fora de 0 a 100% MUST ser recusada (UC02).
- **FR-017**: Status sem declaração em nenhuma spec travada, e 429, 423 e 529, MUST ser recusados (RN13, UC02).
- **FR-018**: A consulta de comportamento MUST mostrar, por classe, o SLA oficial e se o valor fica dentro ou fora (RN11, UI04).
- **FR-019**: A configuração salva MUST valer na próxima chamada, sem reiniciar (RN05): cada chamada lê a configuração em vigor. O sorteio de latência e erro fica para a spec 007.

**Limites e paginação**

- **FR-020**: Todo banco MUST nascer com os valores oficiais, salvo os do perfil `restritivo` (RN26, PR01).
- **FR-021**: O operador MUST poder ajustar QCA, TPM por classe, limite operacional (ligado/desligado e valores), TPS, `page-size` máximo, validade da chave, preparação de Recursos e `Retry-After` (UC03, RN21, RN23, RN25, RN26, RN29, RN32, RN35, RN103).
- **FR-022**: O QCA MUST poder ser sobrescrito por receptora (RN21).
- **FR-023**: Valor abaixo do oficial MUST ser aceito, com aviso na resposta (RN26, UI05).
- **FR-024**: A contagem e a aplicação dos limites ficam para as specs 005 e 006.

**Histórico e inativação**

- **FR-025**: Toda alteração de instituição MUST gravar data, autor informado (padrão `operador`), antes e depois (RN05, RN71).
- **FR-026**: A administração MUST ficar no endereço separado e sem autenticação (RN71, PR07).
- **FR-027**: Pedir para apagar MUST inativar instituição com clientes ou pronta, e apagar as demais (RN06). Até a spec 003, o cadastro de clientes é uma porta sem clientes.
- **FR-028**: Instituição inativa MUST responder 503 no envelope oficial, sem explicar o motivo (RN06, RN18).
- **FR-029**: Instituição inativa MUST poder ser reativada, com registro no histórico (RN06).

### Key Entities

- **Instituição**: identificador, endereço, nome, `organisationId`, marca, público, produtos, situação (ativa ou inativa), origem (pronta ou criada) e as cópias de comportamento e limites.
- **Perfil de comportamento**: modelo nomeado com latência por classe, taxa de erro, pesos por status, janelas, chance de recurso indisponível e valores de limites.
- **Configuração de comportamento**: cópia ajustável de uma instituição.
- **Configuração de limites**: QCA do banco, sobrescritas por receptora, TPM por classe, limite operacional, TPS, `page-size` máximo, validade da chave, `Retry-After` e preparação de Recursos.
- **Entrada de histórico**: instituição, data, autor, campo ou bloco alterado, antes e depois.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em um ambiente vazio, 100% dos 16 bancos prontos existem ao final da subida, e subir de novo não muda a contagem.
- **SC-002**: Um operador cria um banco utilizável informando um único campo.
- **SC-003**: 100% das alterações aparecem no histórico, com data, antes e depois.
- **SC-004**: Uma configuração salva vale na chamada seguinte, em 100% das vezes, sem reiniciar.
- **SC-005**: Resolver a instituição cabe no orçamento de 10 ms no p95 do caminho da chamada (RNF03).
- **SC-006**: Os cenários de `instituicoes.feature` passam, exceto os que dependerem de spec posterior e estiverem etiquetados.
- **SC-007**: A cobertura fica em 100% no domínio e em 80% no total (RNF08).

## Assumptions

- Perfis de comportamento são só os 5 prontos, em leitura. Criar perfis próprios fica fora desta spec.

- Esta spec entrega a API de administração; as telas ficam na spec 010, e as respostas já trazem os dados de UI02 a UI05.
- Padrões: público PF e PJ, todos os produtos, marca igual ao nome (campos-obrigatorios.md).
- Pesos dos perfis prontos: pesos iguais entre os "erros mais comuns" da tabela.
- Médias e baixas frequências crescem na proporção do SLA da classe (`perfis-de-comportamento.md`).
- O MongoDB entra nesta spec e sobe no Docker Compose (ADR-003); o Redis fica para a 006.
- Regras de cenário, relógio, defeitos e anomalias ficam para as specs 007 e 009.
- Valores dos perfis são estimativas (PA20).
