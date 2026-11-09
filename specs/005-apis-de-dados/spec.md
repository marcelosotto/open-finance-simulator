# Feature Specification: APIs de dados

**Feature Branch**: `claude/005-apis-de-dados-874b2a`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "APIs de dados: UC16 e UC17, com todas as famílias de dados e paginação oficial. Usar docs/ e os .feature dados-e-transacoes e paginacao. Nome curto: apis-de-dados"

**Origem em `docs/`**: [UC16](../../docs/01-requisitos/casos-de-uso/README.md#uc16), [UC17](../../docs/01-requisitos/casos-de-uso/README.md#uc17), [RF12](../../docs/01-requisitos/requisitos.md#rf12), [RF13](../../docs/01-requisitos/requisitos.md#rf13), [RN31](../../docs/01-requisitos/regras-de-negocio.md#rn31) a [RN47](../../docs/01-requisitos/regras-de-negocio.md#rn47), [RN51](../../docs/01-requisitos/regras-de-negocio.md#rn51) a [RN55](../../docs/01-requisitos/regras-de-negocio.md#rn55), [RN59](../../docs/01-requisitos/regras-de-negocio.md#rn59) a [RN66](../../docs/01-requisitos/regras-de-negocio.md#rn66), [RN88](../../docs/01-requisitos/regras-de-negocio.md#rn88), [RN104](../../docs/01-requisitos/regras-de-negocio.md#rn104), [RN105](../../docs/01-requisitos/regras-de-negocio.md#rn105), [PA09](../../docs/01-requisitos/regras-de-negocio.md#pa09), [PA21](../../docs/01-requisitos/regras-de-negocio.md#pa21), [tabela de endpoints](../../docs/01-requisitos/tabela-de-endpoints.md), [sequência 02](../../docs/03-design/sequencia/02-chamada-de-dados.md), [sequência 03](../../docs/03-design/sequencia/03-coleta-paginada.md), [ADR-008](../../docs/05-adr/ADR-008-pipeline-de-avaliacao.md). Contratos oficiais: as 13 famílias de dados fixadas em `specs.lock.json` (de `customers-2.3.0` a `exchanges-1.1.0`). Cenários Gherkin: [dados-e-transacoes.feature](../../docs/01-requisitos/gherkin/dados-e-transacoes.feature), [paginacao.feature](../../docs/01-requisitos/gherkin/paginacao.feature) e os três cenários `@spec-005` de [recursos.feature](../../docs/01-requisitos/gherkin/recursos.feature).

## Clarifications

### Session 2026-11-09

Decisões aprovadas por Marcelo em 2026-11-09 e registradas em `docs/` (RN31, RN33, RN37, RN41 a RN44, RN46, RN59, RN104, RN105, `paginacao.feature`, `dados-e-transacoes.feature`, `recursos.feature`). Evidências em [perguntas.md](perguntas.md).

- Q1: Como tratar os totais das transações? → A: RN33 vale quando o contrato os declara; as 14 operações de transações não trazem totais.
- Q2: Quem aceita data futura? → A: Só Contas.
- Q3: O mínimo de 25 vale em todas as famílias? → A: Não. O mínimo é dado por família, lido do contrato: 25 nas 6 famílias (e Consentimentos e Recursos) com `minimum: 25`; o `page-size` pedido nas 7 com `minimum: 1`.
- Q4: Como ficam as datas de faturas? → A: `/bills` aceita datas isoladas e, sem datas, traz 12 meses; `/bills/{billId}/transactions` exige o par e, sem datas, traz a fatura inteira.
- Q5: Até onde vai o dado? → A: Coerência completa em contas e cartão; nas demais, determinístico, válido no schema e coerente dentro da resposta.
- Q6: Para onde vão os cenários de outras specs? → A: Cota em `@spec-006`; anomalia em `@spec-009`.
- Q7: Como a transação muda de estado? → A: Pelo relógio injetado. Lançamentos futuros só em Contas e só em 30 dias; a consulta aceita 12 meses, e além do dia 30 vem vazia, sem cálculo. Futuro não entra no saldo. Nada é gravado.
- Q8: O que a chave congela? → A: Consulta e instante; ajustes posteriores não aparecem; filtros diferentes invalidam a chave.
- Q9: Quais códigos de parâmetro? → A: `PARAMETRO_INVALIDO` e `PARAMETRO_NAO_INFORMADO`; `reserved-balances` em página única, sem chave.
- Q10: RN104 e RN105 em todas as famílias? → A: Sim, nas famílias com recurso.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar contas e transações (Priority: P1)

Como receptora, quero consultar contas, saldos e transações com as mesmas regras de uma instituição real, para desenvolver sobre dados realistas ([UC16](../../docs/01-requisitos/casos-de-uso/README.md#uc16), RN38, RN40 a RN46).

**Why this priority**: é o uso mais frequente do simulador e fecha o ciclo com o consentimento e os recursos da spec 004.

**Independent Test**: com um consentimento autorizado, consultar saldo e transações de uma conta, somar as transações efetivadas ao saldo inicial e validar as respostas contra o contrato fixado.

**Acceptance Scenarios**:

1. **Given** um consentimento autorizado, **When** a receptora consulta contas sem `x-fapi-interaction-id`, **Then** responde 400 com um identificador gerado; **When** envia um válido, **Then** recebe o mesmo valor.
2. **Given** uma conta, **When** a receptora consulta o saldo, **Then** a resposta valida contra o contrato fixado.
3. **Given** `transactions-current`, **When** a consulta vai de D-6 até hoje, **Then** responde 200; **When** vai de D-7 até hoje, **Then** responde 422.
4. **Given** `transactions`, **When** a consulta vai de D-365 até hoje, **Then** responde 200; **When** vai de D-400 até hoje, **Then** responde 422.
5. **Given** só a data inicial, **When** a receptora consulta transações, **Then** responde 422; **Given** nenhuma data, **Then** vêm só as transações de hoje.
6. **Given** várias transações, **When** a receptora consulta, **Then** vêm da mais recente para a mais antiga.
7. **Given** uma transação em processamento, **When** ela é efetivada, **Then** só o identificador definitivo aparece.
8. **Given** uma transação PIX, **When** a receptora consulta, **Then** a contraparte vem preenchida.
9. **Given** as transações efetivadas, **When** somadas ao saldo inicial, **Then** dão o saldo informado.

---

### User Story 2 - Paginar listas (Priority: P1)

Como receptora, quero a paginação oficial, para ler listas grandes sem gastar a cota ([UC17](../../docs/01-requisitos/casos-de-uso/README.md#uc17), RN31 a RN37).

**Why this priority**: toda coleta real depende dela, e os erros de paginação são os que a receptora mais precisa tratar.

**Independent Test**: percorrer uma lista de 47 registros até a última página, conferindo tamanhos, `links` e `meta`; repetir com chave expirada e inválida.

**Acceptance Scenarios**:

1. **Given** uma lista, **When** a receptora não informa `page` e `page-size`, **Then** recebe até 25 registros.
2. **Given** 47 registros, **When** pede `page-size` 5, **Then** nas famílias com mínimo de 25 a primeira página tem 25 e a segunda 22, e nas demais a primeira tem 5 (Q3).
3. **Given** 3 páginas, **When** pede a página 9, **Then** responde 422 `PAGE_NOT_FOUND`.
4. **Given** `page-size` 5000, **When** a receptora pede, **Then** recebe até 1000 e o `self` mostra 1000.
5. **Given** um banco com máximo de 800, **When** a receptora pede a página 2 com 1000, **Then** recebe os itens 801 a 1600.
6. **Given** uma lista paginada cujo contrato traz totais e nenhum registro, **When** a receptora consulta, **Then** responde 200 com `meta.totalRecords` e `meta.totalPages` iguais a 0 (Q1).
7. **Given** 3 páginas, **When** pede a primeira, **Then** `links` tem `self`, `next` e `last`, sem `first` e `prev`; **When** pede a última, **Then** tem `self`, `first` e `prev`, sem `next` e `last` (nas transações, sem `last`, como manda o contrato: Q1).
8. **Given** uma chave expirada ou inválida, **When** pede a página seguinte, **Then** responde 200 e os links trazem nova chave.
9. **Given** 3 páginas, **When** lê todas com a mesma chave, **Then** nenhum registro se repete ou falta.

---

### User Story 3 - Consultar todas as famílias (Priority: P2)

Como receptora, quero consultar dados cadastrais, cartão, crédito, investimentos e câmbio, para cobrir todas as APIs da fase de dados ([UC16](../../docs/01-requisitos/casos-de-uso/README.md#uc16), RN40, RN47, [tabela de endpoints](../../docs/01-requisitos/tabela-de-endpoints.md)).

**Why this priority**: amplia a cobertura sobre o fluxo já provado nas contas.

**Independent Test**: para cada um dos 69 endpoints da tabela, chamar com um consentimento que cubra a família e validar a resposta contra o contrato fixado.

**Acceptance Scenarios**:

1. **Given** um consentimento com todas as permissões, **When** a receptora chama cada endpoint de dados, **Then** recebe 200 e a resposta valida contra o contrato.
2. **Given** um consentimento só com permissões cadastrais, **When** a receptora consulta recursos, **Then** responde 200 com lista vazia, e os dados cadastrais respondem 200.
3. **Given** o mesmo consentimento e a mesma semente, **When** a consulta é repetida, **Then** o conteúdo é igual.

---

### User Story 4 - Respeitar o estado do recurso (Priority: P1)

Como receptora, quero que recursos indisponíveis e de terceiros sejam recusados como numa instituição real, para tratar esses casos ([RN88](../../docs/01-requisitos/regras-de-negocio.md#rn88), RN104, RN105).

**Why this priority**: são os cenários que a spec 004 deixou para quando as rotas de produto existissem.

**Independent Test**: variar o estado de um recurso e consultar a lista e o acesso direto.

**Acceptance Scenarios**:

1. **Given** uma conta `AVAILABLE`, **When** a receptora lista e acessa, **Then** ela aparece e responde 200.
2. **Given** uma conta em `TEMPORARILY_UNAVAILABLE`, `UNAVAILABLE` ou `PENDING_AUTHORISATION`, **When** a receptora lista, **Then** ela não aparece; **When** acessa, **Then** responde 403 com `STATUS_RESOURCE_<estado>`.
3. **Given** uma conta de outro cliente ou fora do consentimento, **When** a receptora acessa, **Then** responde 403 `RESOURCE_FORBIDDEN`.

---

### User Story 5 - Fechar as pendências da spec 004 (Priority: P3)

Como operador, quero que mudanças concorrentes de estado de recurso não se percam, e que a exceção de `/extensions` esteja documentada (RN33, RN108, commit `b996c91`).

**Why this priority**: corrige um risco pequeno; não bloqueia o fluxo principal.

**Independent Test**: disparar duas mudanças de estado concorrentes sobre o mesmo recurso e conferir que nenhuma se perde sem aviso.

**Acceptance Scenarios**:

1. **Given** duas mudanças concorrentes no mesmo recurso, **When** ambas são enviadas, **Then** o estado final corresponde a uma delas, e a outra recebe recusa ou é aplicada sobre o valor novo, sem sobrescrita cega.
2. **Given** a RN33, **When** é lida, **Then** registra a exceção intencional de `GET /extensions`.

### Edge Cases

- `page` abaixo de 1, `page-size` não inteiro ou abaixo de 1, data malformada ou `from*` depois de `to*`: 422 (Q9).
- Pedido da primeira página sem registros: 200, nunca `PAGE_NOT_FOUND` (RN33).
- `accounts/{id}/reserved-balances` declara `page` e `page-size`, sem `pagination-key`, e devolve objeto único: sempre uma página, sem chave nos links (Q9).
- Endpoints sem paginação no contrato (ex.: `contracts/{id}/payments`): ignoram `page` e `page-size`.
- Faturas e transações de fatura sem datas: regra de RN42 não se aplica como escrita (Q4).
- Chave válida enviada com filtros diferentes dos que a geraram: tratada como inválida, com RN36 (Q8).
- Consentimento revogado no meio da coleta: 401 na página seguinte (RN88, sequência 03).
- Página pedida além do total em transações, onde o total não é exposto: 422 `PAGE_NOT_FOUND`, pois o sistema conhece o total (Q1).

## Requirements *(mandatory)*

### Functional Requirements

**Rotas e portão**

- **FR-001**: Os 69 endpoints de dados da tabela oficial MUST deixar de responder 501 e devolver dados, nas rotas geradas do contrato fixado (RF12, princípio VII).
- **FR-002**: Toda rota de dados MUST seguir a ordem de RN39. As etapas de capacidade, tráfego, limite operacional e cenário continuam nas specs 006 e 007, sem mudar o portão da spec 004 (400, 401, 403).
- **FR-003**: Sem `x-fapi-interaction-id` válido, a resposta MUST ser 400 com identificador gerado; com valor válido, MUST devolver o mesmo (RN38).

**Conteúdo**

- **FR-004**: Com perfil `estavel` e sem defeitos, toda resposta MUST validar contra o schema da versão fixada (RN40, princípio VII).
- **FR-005**: O conteúdo MUST sair da semente, da mesma forma para massa persistida e virtual, sem gravar item a item, com ajustes por cima (RN51 a RN54).
- **FR-006**: Saldo e extrato MUST ser coerentes; em cartão, limite usado e faturas MUST bater com as transações do ciclo (RN46). Nas demais famílias, o dado é determinístico, válido no schema e coerente dentro da resposta (Q5).
- **FR-007**: Toda resposta MUST levar a marcação de ambiente e natureza: cabeçalhos, prefixo dos IDs e texto marcado (RN60 a RN66).
- **FR-008**: A contraparte MUST seguir a tabela oficial por tipo e sentido da transação (RN45).
- **FR-009**: Transações MUST vir da mais recente para a mais antiga (RN43), em todas as famílias (Q5).
- **FR-010**: Uma transação MUST passar pelos estados de RN44, derivados do relógio injetado, e identificador provisório e definitivo MUST NOT aparecer juntos. Lançamentos futuros existem só em Contas, até 30 dias; além disso a lista vem vazia, sem cálculo, e eles não entram no saldo (RN44, RN46, RN59, Q7).

**Filtros de data**

- **FR-011**: `/transactions` e `/transactions-current` MUST aplicar as janelas oficiais de cada família e responder 422 fora delas (RN41, Q2).
- **FR-012**: `from*` e `to*` MUST andar juntos onde o contrato exige, com 422 se vier só um; sem nenhum dos dois, vale o dia atual (RN42), exceto em faturas (Q4).

**Paginação**

- **FR-013**: `page` começa em 1 e `page-size` vale 25 por padrão (RN31). Acima do máximo, MUST aplicar o máximo e mostrá-lo no `self` (PA21, RN32). O mínimo é o `minimum` do `page-size` no contrato da família; abaixo dele, MUST aplicar o mínimo (RN31, Q3).
- **FR-014**: O máximo da instituição MUST valer no cálculo da página (RN32).
- **FR-015**: Sem registros, `meta` MUST trazer totais zerados nos contratos que têm totais. Nos 14 endpoints de transações que declaram só `requestDateTime`, `meta` MUST NOT trazer totais (Q1).
- **FR-016**: Página além do total MUST responder 422 `PAGE_NOT_FOUND`, exceto a primeira (RN33).
- **FR-017**: `links` MUST seguir RN34 e o schema do endpoint.
- **FR-018**: Toda lista paginada com `pagination-key` no contrato MUST emitir chave nos links, válida por 60 minutos pelo relógio injetado, e configurável pela instituição (RN35).
- **FR-019**: Chave inválida ou expirada MUST gerar nova chave e responder com sucesso (RN36). O efeito na cota entra na spec 006 (Q6).
- **FR-020**: A lista MUST ser consistente durante a paginação de uma mesma chave (RN37, Q8).

**Estado do recurso**

- **FR-021**: Recurso fora de `AVAILABLE` MUST NOT aparecer na lista e MUST responder 403 no acesso direto, com `STATUS_RESOURCE_<estado>` (RN104).
- **FR-022**: Recurso de outro cliente, inexistente ou fora do consentimento MUST responder 403 `RESOURCE_FORBIDDEN` (RN105). FR-021 e FR-022 valem em todas as famílias com recurso (Q10).
- **FR-023**: Dados Cadastrais MUST NOT aparecer em Recursos (RN47).

**Pendências da spec 004**

- **FR-024**: A RN33 MUST registrar como exceção intencional que `GET /consents/{consentId}/extensions` responde 422 `PAGE_NOT_FOUND`, embora `consents-3.3.1` não declare 422 nessa rota.
- **FR-025**: `PUT /admin/recursos/{resourceId}/estado` MUST usar escrita condicional por versão, como as demais operações (commit `b996c91`), com teste de corrida.

**Transversais**

- **FR-026**: O simulador MUST NOT explicar na resposta por que ela saiu assim nem registrar as chamadas (RN18).
- **FR-027**: Os cenários que dependem de limite operacional ou de anomalia MUST ficar etiquetados com a spec que os entrega (Q6).

### Key Entities *(include if feature involves data)*

- **Produto por família**: conta, cartão, contrato de crédito, investimento e operação de câmbio, derivados do recurso e da semente.
- **Transação**: identificador, data, valor, direção, tipo, estado e contraparte; calculada, com ajustes.
- **Fatura**: ciclo de cartão, com transações e limite usado.
- **Dados cadastrais**: identificação, qualificação e relacionamento do cliente.
- **Chave de paginação**: consulta de origem, emissão e validade.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Todos os cenários `@spec-005` passam no CI, exceto os movidos para outras specs (Q6).
- **SC-002**: 100% dos 69 endpoints de dados, com perfil `estavel`, respondem de forma válida contra o contrato fixado.
- **SC-003**: A mesma consulta, com a mesma semente, massa e relógio, dá o mesmo resultado em 100% das repetições.
- **SC-004**: Uma coleta paginada percorre toda a lista sem repetir nem perder registros em 100% das execuções.
- **SC-005**: Uma receptora consome qualquer família trocando só endereço e credenciais.
- **SC-006**: Nenhum teste depende de hora real, e a cobertura fica em 100% no domínio e 80% no total.

## Assumptions

- As specs 001 a 004 entregam rotas, marcação, instituições (com `PageSizeMaximo` e validade da chave), massa, token e portão.
- TPM, limite operacional e capacidade entram na spec 006; relógio por instituição e cenários na 007; anomalias e defeitos na 009.
- `PAGE_NOT_FOUND`, `STATUS_RESOURCE_*` e `RESOURCE_FORBIDDEN` não aparecem em nenhum dos contratos de dados; o campo `code` é livre, então usá-los não contraria o schema.
- Mudanças em `docs/` decorrentes das respostas entram antes do plano, pelo princípio I.
