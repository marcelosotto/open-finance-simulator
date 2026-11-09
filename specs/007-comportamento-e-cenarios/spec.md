# Feature Specification: Comportamento e cenários

**Feature Branch**: `claude/007-comportamento-e-cenarios-629767`

**Created**: 2026-11-09

**Status**: Aprovada

**Input**: User description: "Comportamento e cenários: UC04, UC21 e UC28, com perfis, timeout, indisponibilidade e relógio. Usar docs/ e comportamento.feature. Nome curto: comportamento-e-cenarios"

**Origem em `docs/`**: [UC04](../../docs/01-requisitos/casos-de-uso/README.md#uc04), [UC21](../../docs/01-requisitos/casos-de-uso/README.md#uc21), [UC28](../../docs/01-requisitos/casos-de-uso/README.md#uc28), [RF04](../../docs/01-requisitos/requisitos.md#rf04), [RF15](../../docs/01-requisitos/requisitos.md#rf15), [RF22](../../docs/01-requisitos/requisitos.md#rf22), [RN10](../../docs/01-requisitos/regras-de-negocio.md#rn10) a [RN19](../../docs/01-requisitos/regras-de-negocio.md#rn19), [RN27](../../docs/01-requisitos/regras-de-negocio.md#rn27), [RN37](../../docs/01-requisitos/regras-de-negocio.md#rn37), [RN39](../../docs/01-requisitos/regras-de-negocio.md#rn39), [RN44](../../docs/01-requisitos/regras-de-negocio.md#rn44), [RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54), [RN73](../../docs/01-requisitos/regras-de-negocio.md#rn73), [RN104](../../docs/01-requisitos/regras-de-negocio.md#rn104), [RN108](../../docs/01-requisitos/regras-de-negocio.md#rn108), [PA19](../../docs/01-requisitos/regras-de-negocio.md#pa19), [PA20](../../docs/01-requisitos/regras-de-negocio.md#pa20), [PR05](../../docs/00-visao-geral/premissas.md#pr05), [RNF01](../../docs/01-requisitos/requisitos.md#rnf01), [RNF03](../../docs/01-requisitos/requisitos.md#rnf03), [RNF05](../../docs/01-requisitos/requisitos.md#rnf05), [RNF12](../../docs/01-requisitos/requisitos.md#rnf12), [perfis de comportamento](../../docs/03-design/perfis-de-comportamento.md), [ADR-008](../../docs/05-adr/ADR-008-pipeline-de-avaliacao.md). Contratos oficiais: os 15 fixados em `specs.lock.json`. Cenários Gherkin: [comportamento.feature](../../docs/01-requisitos/gherkin/comportamento.feature) e "Bloqueio programado" de [recursos.feature](../../docs/01-requisitos/gherkin/recursos.feature).

## Clarifications

### Session 2026-11-09

Marcelo aprovou a opção A de Q1 a Q18, repassada pela sessão controladora. Decisões registradas em `docs/` (RN10, RN12 a RN17, RN39, RN54, RN73, RN108, nova RN109, UC04, UC21, UC28, perfis e modelo de dados). Evidências em [perguntas.md](perguntas.md). Cada (Qn) nos requisitos aponta a decisão.

- Q1: recusa nos pesos → status não declarado, abaixo de 400, 429, 423, 529 e 503.
- Q2: regra de estado → sobrepõe na leitura, sem gravar; ressalva em RN39.
- Q3: indisponibilidade → na etapa de regra de cenário (RN39 literal).
- Q4: status de regra → 200, erros declarados e 503; endpoint que não declara não é afetado, salvo 503.
- Q5: chamada lenta → cauda e todo 504 simulado esperam 15 s reais; regra pode mudar a duração.
- Q6: `recursoIndisponivelPct` → sorteio por chamada, 403 no acesso direto (RN109).
- Q7: escritas da administração → relógio do banco quando entram em resposta ou comparação; histórico, lotes e massa na hora real.
- Q8: vencedora → uma só, substitui só o que define.
- Q9: frequência sem disparo → segue o perfil.
- Q10: semente → sequência por regra nos contadores.
- Q11: período → instantes UTC, [início, fim); janela do perfil em ciclos a partir de 00:00 UTC.
- Q12: instituição → opcional, com ao menos um filtro.
- Q13: transporte na cota → só a conexão cortada não conta.
- Q14: códigos → nome HTTP em inglês; enums de Consentimentos respeitados.
- Q15: latência → só quem chega à etapa.
- Q16: acelerado → continua da hora simulada; fator em (0, 100.000].
- Q17: relógio que volta → nada é corrigido; decorrido negativo conta como zero.
- Q18: fora → defeitos e RN115 na 009; tela na 010.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Perfis que se comportam como descritos (Priority: P1)

Como QA, quero que cada banco responda com a latência e os erros do seu perfil, para ver como o sistema reage a uma instituição lenta ou instável ([RN10](../../docs/01-requisitos/regras-de-negocio.md#rn10) a [RN13](../../docs/01-requisitos/regras-de-negocio.md#rn13)).

**Why this priority**: sem isso, nenhuma instituição simulada difere de outra, e o perfil é só configuração.

**Independent Test**: fazer chamadas a bancos de perfis diferentes, com sorteio repetível, e conferir distribuição de tempo e de status.

**Acceptance Scenarios**:

1. **Given** 10.000 chamadas ao endpoint de saldos do Banco Estável, **When** terminam, **Then** o p95 fica abaixo de 1.500 ms.
2. **Given** 10.000 chamadas ao endpoint de saldos do Banco Lento, **When** terminam, **Then** o p95 fica acima de 1.500 ms e todas terminam.
3. **Given** a classe alta com tempo típico de 3 s, **When** a receptora consulta o saldo, **Then** a resposta leva cerca de 3 s, e os endpoints da classe baixa não mudam.
4. **Given** pesos de um banco que incluem 429, **When** o operador salva, **Then** a configuração é recusada.
5. **Given** um banco com taxa de erro acima de zero, **When** uma chamada é sorteada, **Then** o status sai dos pesos, entre os que aquele endpoint declara, com corpo válido no contrato.

---

### User Story 2 - Timeout e indisponibilidade (Priority: P1)

Como QA, quero provocar timeout, fora do ar e falha de transporte, para provar a reação do sistema a essas falhas ([UC21](../../docs/01-requisitos/casos-de-uso/README.md#uc21), [RN12](../../docs/01-requisitos/regras-de-negocio.md#rn12), [RN16](../../docs/01-requisitos/regras-de-negocio.md#rn16), [RN17](../../docs/01-requisitos/regras-de-negocio.md#rn17)).

**Why this priority**: é o caso mais difícil de reproduzir contra um banco real.

**Independent Test**: com relógio injetado, conferir que a resposta só termina após 15 s e que a janela vale só dentro do período.

**Acceptance Scenarios**:

1. **Given** uma chamada ao Banco Timeout afetada pelo timeout, **When** ela termina, **Then** só terminou depois de 15 s e tem status 504.
2. **Given** uma janela do Banco Estável das 10h às 10h30, **When** chega uma chamada às 10h10, **Then** recebe 503; **When** chega às 10h31, **Then** a resposta é normal.
3. **Given** uma regra de transporte para o endpoint de contas, **When** a receptora consulta, **Then** observa conexão cortada, corpo truncado ou JSON inválido, conforme a regra.
4. **Given** uma regra que força 500, **When** a receptora consulta, **Then** o corpo segue o contrato e não diz qual regra agiu.

---

### User Story 3 - Regras de cenário e pacotes (Priority: P1)

Como QA, quero programar uma falha para um cliente, recurso ou endpoint, de forma repetível, para rodar o mesmo teste todo dia ([UC04](../../docs/01-requisitos/casos-de-uso/README.md#uc04), [RN14](../../docs/01-requisitos/regras-de-negocio.md#rn14), [RN15](../../docs/01-requisitos/regras-de-negocio.md#rn15), [RN19](../../docs/01-requisitos/regras-de-negocio.md#rn19), [RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54)).

**Why this priority**: é o que torna um teste específico possível, além do comportamento médio do perfil.

**Independent Test**: salvar regras conflitantes e conferir qual vence; ligar um pacote e conferir que suas regras valem.

**Acceptance Scenarios**:

1. **Given** a regra "Mariana sempre recebe 500 em saldos", **When** Mariana é consultada duas vezes, **Then** as duas respostas têm status 500.
2. **Given** "Banco Estável responde 503" e "Mariana responde 200", **When** Mariana é consultada, **Then** o status é 200.
3. **Given** "Mariana responde 503" e "Mariana responde 500 em saldos", **When** o saldo é consultado, **Then** o status é 500.
4. **Given** "Mariana responde 503" e, criada depois, "Mariana responde 500", **When** Mariana é consultada, **Then** o status é 500.
5. **Given** o pacote "Black Friday" com duas regras, **When** Bruno liga o pacote, **Then** as duas regras valem.
6. **Given** uma regra que bloqueia a conta às 10h, **When** Ana consulta a conta às 10h01, **Then** recebe 403.

---

### User Story 4 - Relógio por instituição (Priority: P2)

Como QA, quero fixar ou acelerar o relógio de um banco, para testar expiração e virada do mês sem esperar ([UC28](../../docs/01-requisitos/casos-de-uso/README.md#uc28), [RN73](../../docs/01-requisitos/regras-de-negocio.md#rn73)).

**Why this priority**: depende das respostas e das regras já existirem, mas destrava os testes dependentes de tempo.

**Independent Test**: fixar o relógio de um banco e conferir datas, cabeçalho e os demais bancos inalterados.

**Acceptance Scenarios**:

1. **Given** o relógio do Banco Azul fixado em 2026-12-31T23:59:50Z, **When** Ana consulta o saldo, **Then** a resposta usa a data simulada e `x-sim-clock` a mostra.
2. **Given** o relógio do Banco Azul acelerado, **When** Ana faz chamadas dentro de um minuto real, **Then** o TPM conta pela hora real.
3. **Given** o relógio fixado, **When** Bruno volta ao relógio real, **Then** `x-sim-clock` não é enviado.

### Edge Cases

- Peso de status não declarado pelo endpoint chamado: fica fora do sorteio daquela chamada; sem nenhum declarado, não há erro (RN13).
- Regra com status não declarado pelo endpoint: não age nele, salvo o 503 (Q4).
- 422 forçado em Consentimentos: `code` restrito ao enum de cada operação (Q14).
- Chamada recusada antes da etapa de comportamento: sem latência simulada (Q15).
- Erro simulado, timeout e conexão cortada não contam na cota (RN27, Q13).
- Regra de frequência parcial que não dispara: vale o perfil (Q9).
- Relógio volta para trás: nada é corrigido; tempo decorrido negativo conta como zero (Q17).
- Cliente desiste durante a espera: a chamada é cancelada, sem contar na cota.
- Transação com relógio simulado: continua limitada ao instante simulado da consulta (RN44), um só instante por chamada.

## Requirements *(mandatory)*

### Functional Requirements

**Perfis**

- **FR-001**: Toda chamada que chega à etapa de comportamento MUST sofrer a latência da classe do endpoint: tempo típico, variação e cauda lenta (RN10, Q15).
- **FR-002**: Toda chamada MUST sortear erro pela taxa e pelos pesos da instituição, só entre os status que o endpoint declara (RN13).
- **FR-003**: 429, 423, 529 e 503 MUST NOT sair do sorteio, e os pesos MUST recusar esses status ao salvar, além de status abaixo de 400 (RN13, FR-018 da 006, Q1).
- **FR-004**: `recursoIndisponivelPct` MUST produzir 403 `STATUS_RESOURCE_TEMPORARILY_UNAVAILABLE` no acesso direto, por sorteio na chamada (Q6).
- **FR-005**: A janela de indisponibilidade do perfil MUST responder 503, em ciclos contados a partir de 00:00 UTC, pelo relógio do banco (`perfis-de-comportamento.md`, Q11).

**Timeout e falhas**

- **FR-006**: Chamada da cauda lenta e todo 504 da etapa de comportamento MUST esperar 15 s reais e terminar em 504; uma regra pode informar outra duração (RN12, UC21, Q5). O 504 de preparação de Recursos (RN103) não muda.
- **FR-007**: A indisponibilidade MUST responder 503, 504 ou cortar a conexão, conforme configurado, para as chamadas do alcance que chegam à etapa (RN16, Q3).
- **FR-008**: O sistema MUST simular conexão cortada, corpo truncado e JSON inválido (RN17). Só conexão cortada deixa de contar na cota (Q13).

**Regras de cenário**

- **FR-009**: A administração MUST criar, listar, alterar, apagar, ligar e desligar regras, com alcance (instituição, endpoint, cliente, recurso, combináveis, ao menos um), efeito, frequência, período e semente (RN14, UC04, Q12). Efeitos: latência, status, timeout, indisponibilidade, falha de transporte e mudança de estado (Q18).
- **FR-010**: Com várias regras valendo, MUST vencer uma só: recurso, cliente, endpoint e instituição, nessa ordem; depois, mais filtros; depois, a mais recente (RN15).
- **FR-011**: A regra vencedora MUST substituir só o que define, e o resto segue o perfil (Q8).
- **FR-012**: Uma regra com status MUST aceitar 200 (segue o fluxo normal), os erros declarados por alguma spec e 503. O 429 MUST usar o `Retry-After` do banco; 429, 423 e 529 usam os códigos de RN23, RN28 e RN29 (RN13, Q2 da 006, Q4, Q14).
- **FR-013**: Regra com mudança de estado MUST valer como estado do recurso enquanto durar o período, em toda rota e na etapa de acesso, sem gravar (RN104, RN108, Q2).
- **FR-014**: Uma regra com semente MUST dar o mesmo resultado na mesma posição da sequência, mesmo com várias instâncias (RN54, RNF04, Q10).
- **FR-015**: O período MUST usar instantes em UTC, início incluído e fim excluído, pelo relógio do banco chamado (RN73, Q11).
- **FR-016**: Um pacote MUST agrupar regras com nome, e ligar ou desligar todas de uma vez; dentro dele vale FR-010 (RN19).
- **FR-017**: A etapa MUST rodar na ordem de RN39, antes de qualquer efeito gravado da chamada.
- **FR-018**: A resposta MUST NOT dizer por que saiu assim, e o corpo de erro MUST seguir o contrato do endpoint, salvo 503, que usa o envelope comum (RN18, Q14).

**Relógio**

- **FR-019**: O operador MUST fixar uma data ou definir um fator de aceleração por instituição e voltar ao relógio real; a mudança MUST ir para o histórico e valer em até 5 s (RN05, RN73, RNF12, Q16).
- **FR-020**: O relógio do banco MUST valer para datas das respostas, expirações (token, chave, consentimento, preparação de Recursos), cota mensal e horário das regras, com um único instante por chamada (RN44, RN73, RN25).
- **FR-021**: TPM, capacidade, latência e os 15 s MUST seguir a hora real (RN73, Q16).
- **FR-022**: Com relógio alterado, toda resposta das rotas oficiais do banco MUST trazer `x-sim-clock` com a hora simulada em ISO-8601 UTC; com relógio real, MUST NOT trazer (RN73).
- **FR-023**: Escritas da administração ligadas a um banco e usadas em resposta ou comparação MUST usar o relógio do banco; histórico, lotes e criação de massa MUST usar a hora real (RN37, Q7).

**Geral**

- **FR-024**: A etapa de comportamento MUST reaproveitar a instituição já lida na chamada e respeitar RNF03. Configuração de regras e relógio MUST valer em até 5 s (RNF12).
- **FR-025**: O novo comportamento MUST ser registrado em `docs/` antes do código, e os 16 cenários de `comportamento.feature` e "Bloqueio programado" MUST rodar sem a etiqueta `@spec-007` (ADR-009).

### Key Entities

- **Regra de cenário**: alcance, efeito, frequência, período, semente, ligada ou desligada, pacote opcional.
- **Pacote de cenário**: nome e regras, ligados de uma vez.
- **Relógio da instituição**: real, fixado (instante) ou acelerado (fator, a partir de uma base).
- **Comportamento da instituição**: já existe desde a spec 002 e passa a ser aplicado.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos 17 cenários do escopo passam no CI, sem etiqueta.
- **SC-002**: Em 10.000 chamadas sorteadas, a taxa de erro de cada perfil fica a até 20% do valor configurado, e o p95 de cada perfil a até 10%.
- **SC-003**: A mesma regra, com a mesma semente e o mesmo relógio, dá a mesma sequência em 100% das repetições.
- **SC-004**: O simulador acrescenta até 10 ms no p95 além da latência configurada (RNF03).
- **SC-005**: Com perfil `estavel` e sem regras, 100% das respostas validam contra o schema oficial (RNF01), inclusive os erros sorteados.
- **SC-006**: Com relógio alterado em um banco, nenhum outro banco muda de comportamento.
- **SC-007**: Nenhum teste depende de hora real ou de sorteio sem semente, e a cobertura fica em 100% no domínio e 80% no total (RNF08).

## Assumptions

- Uma janela de indisponibilidade de RN16 é uma regra de cenário com efeito de indisponibilidade.
- A distribuição da latência é livre, desde que p50 fique no tempo típico e p95 no típico mais a variação.
- O alcance por recurso compara com o recurso do caminho da chamada. Rota sem recurso no caminho ou sem cliente no token (como `POST /consents`) não casa com esse filtro.
- O alcance por endpoint usa a operação oficial. `POST /token` e a administração ficam fora de regras e perfis.
- Cliente e recurso de uma regra não são validados ao salvar, porque podem ser virtuais.
- Uma regra pertence a no máximo um pacote.
- Sem back-office nesta spec: UI06 fica na 010, e defeitos e RN115 na 009 (Q18).
