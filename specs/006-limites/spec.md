# Feature Specification: Limites

**Feature Branch**: `claude/cool-hawking-9037b3`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "Limites: UC18 a UC20, com TPM, limite operacional e capacidade em Redis. Usar docs/ e limites.feature. Nome curto: limites"

**Origem em `docs/`**: [UC18](../../docs/01-requisitos/casos-de-uso/README.md#uc18), [UC19](../../docs/01-requisitos/casos-de-uso/README.md#uc19), [UC20](../../docs/01-requisitos/casos-de-uso/README.md#uc20), [RF14](../../docs/01-requisitos/requisitos.md#rf14), [RN20](../../docs/01-requisitos/regras-de-negocio.md#rn20) a [RN30](../../docs/01-requisitos/regras-de-negocio.md#rn30), [RN35](../../docs/01-requisitos/regras-de-negocio.md#rn35), [RN36](../../docs/01-requisitos/regras-de-negocio.md#rn36), [RN39](../../docs/01-requisitos/regras-de-negocio.md#rn39), [RN73](../../docs/01-requisitos/regras-de-negocio.md#rn73), [PA10](../../docs/01-requisitos/regras-de-negocio.md#pa10), [PA11](../../docs/01-requisitos/regras-de-negocio.md#pa11), [RNF02](../../docs/01-requisitos/requisitos.md#rnf02) a [RNF04](../../docs/01-requisitos/requisitos.md#rnf04), [RNF12](../../docs/01-requisitos/requisitos.md#rnf12), [tabela de endpoints](../../docs/01-requisitos/tabela-de-endpoints.md), [sequência 02](../../docs/03-design/sequencia/02-chamada-de-dados.md), [sequência 03](../../docs/03-design/sequencia/03-coleta-paginada.md), [ADR-003](../../docs/05-adr/ADR-003-mongodb-e-redis.md), [ADR-008](../../docs/05-adr/ADR-008-pipeline-de-avaliacao.md). Contratos oficiais: os 15 fixados em `specs.lock.json`. Cenários Gherkin: [limites.feature](../../docs/01-requisitos/gherkin/limites.feature) e os dois cenários `@spec-006` de [paginacao.feature](../../docs/01-requisitos/gherkin/paginacao.feature) ("Chave dentro da validade", linha 77, e "Chave recusada conta na cota", linha 96).

## Clarifications

### Session 2026-11-09

Decisões aprovadas por Marcelo em 2026-11-09 e registradas em `docs/` (RN23, RN25, RN28 a RN30, RN36, RN39, UC19, tabela de endpoints, ADR-008, sequência 02, C3 e `limites.feature`). Evidências em [perguntas.md](perguntas.md).

- Q1: Qual o alcance da capacidade? → A: Por instituição, somando todos os endpoints.
- Q2: Quem produz 429? → A: Entre os limites, só o TPM; regra de cenário também pode; concorrência global não é simulada.
- Q3: Em quais rotas vale a capacidade? → A: Nas 75 operações dos contratos; token e administração não contam.
- Q4: Onde entra o 403 de recurso? → A: Depois de capacidade e tráfego, em etapa própria de acesso ao recurso; `PAGE_NOT_FOUND` é parâmetro, antes da cota.
- Q5: Cota esgotada na paginação? → A: Chave válida responde 200 sem conferir a cota; chave recusada responde 423.
- Q6: O que é o objeto? → A: O último parâmetro do caminho; sem parâmetro, o consentimento.
- Q7: Fuso do mês? → A: UTC.
- Q8: Contadores indisponíveis? → A: A chamada segue como dentro dos limites, e a falha vai só para a observabilidade.
- Q9: Cota sob concorrência? → A: Confere antes e conta depois do 2XX; pode ultrapassar em poucas unidades.
- Q10: Códigos das recusas? → A: `TOO_MANY_REQUESTS`, `LOCKED` e `SITE_IS_OVERLOADED`.
- Q11: Cenário do 500? → A: Fica na 006, com o 500 provocado pelo teste.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Receber 429 ao passar do TPM (Priority: P1)

Como receptora, quero que cada endpoint recuse o excesso de chamadas por minuto, como uma instituição real, para ajustar meu consumo antes de produção ([UC18](../../docs/01-requisitos/casos-de-uso/README.md#uc18), RN20 a RN24).

**Why this priority**: é o limite que qualquer coletor encontra primeiro, e a persona Ana quer ver o 429 no meio da paginação.

**Independent Test**: fazer TPM + 1 chamadas a um endpoint no mesmo minuto e conferir o 429 na última, o endpoint vizinho livre e o contador zerado na virada.

**Acceptance Scenarios**:

1. **Given** um endpoint de TPM 1000, **When** chegam 1001 chamadas no mesmo minuto, **Then** a última recebe 429 com `Retry-After`.
2. **Given** `/accounts` no limite, **When** a receptora chama `/accounts/{accountId}` no mesmo minuto, **Then** não recebe 429.
3. **Given** o limite estourado às 10h25m59s, **When** a receptora chama às 10h26m00s, **Then** é atendida.
4. **Given** uma receptora com QCA configurado, **When** chama um endpoint de alta frequência, **Then** o TPM é o da faixa: 1.000.000 → 2.500; 2.000.000 → 5.000; 3.000.000 → 8.000; 6.000.000 → 10.000; 8.000.000 → 12.000.
5. **Given** um banco com QCA de 7.000.000, **When** uma receptora sem valor próprio chama, **Then** vale a faixa de 7.000.000; **When** uma receptora com QCA próprio de 500.000 chama, **Then** o TPM é 2.500.
6. **Given** 5000 chamadas a `/resources` no mesmo minuto, **When** são atendidas, **Then** nenhuma recebe 429.

---

### User Story 2 - Receber 423 ao esgotar a cota do mês (Priority: P1)

Como receptora, quero que a cota mensal seja contada como nas regras oficiais, inclusive na paginação, para não gastar cota à toa ([UC19](../../docs/01-requisitos/casos-de-uso/README.md#uc19), RN25 a RN28, RN36).

**Why this priority**: a cota define como a receptora desenha a coleta, e é o principal erro da POC (`confronto-com-a-poc.md`).

**Independent Test**: com o limite ligado, chamar `/accounts/{accountId}` nove vezes no mês e conferir o 423; repetir com 5XX, 429 e coleta paginada, conferindo a cota.

**Acceptance Scenarios**:

1. **Given** o limite ligado, **When** chega a chamada 8 do mês a `/accounts/{accountId}` do mesmo recurso e cliente, **Then** responde 200; **When** chega a 9, **Then** responde 423.
2. **Given** o limite ligado, **When** chega a chamada 420 a `/accounts/{accountId}/balances`, **Then** responde 200; **When** chega a 421, **Then** responde 423.
3. **Given** uma resposta 500 ou 429, **When** ela termina, **Then** a cota não muda.
4. **Given** uma consulta de 3 páginas, **When** a receptora lê as 3, **Then** a cota sobe 1.
5. **Given** uma chave emitida há 59 minutos, **When** a receptora pede a página seguinte, **Then** a cota não muda.
6. **Given** uma chave expirada ou inválida, **When** a receptora pede a página seguinte, **Then** responde 200, traz nova chave e a cota sobe 1.
7. **Given** a conta "A" com a cota de saldos esgotada, **When** a receptora consulta o saldo da conta "B", **Then** responde 200.
8. **Given** um banco com limite de baixa frequência 5, **When** chega a 6ª chamada do mês, **Then** responde 423, e a configuração avisa que o valor está abaixo do oficial.

---

### User Story 3 - Receber 529 ao passar da capacidade (Priority: P2)

Como receptora, quero que a instituição recuse o excesso de requisições por segundo, para testar a reação a uma instituição sobrecarregada ([UC20](../../docs/01-requisitos/casos-de-uso/README.md#uc20), RN29).

**Why this priority**: acontece só sob carga alta, depois dos limites por endpoint.

**Independent Test**: enviar capacidade + 1 chamadas no mesmo segundo e conferir o 529 na última.

**Acceptance Scenarios**:

1. **Given** um banco de capacidade 300 TPS, **When** chegam 301 chamadas no mesmo segundo, **Then** a última recebe 529.

---

### User Story 4 - Limites valem no ambiente todo e na ordem oficial (Priority: P1)

Como operador, quero que os limites valham sobre o total de várias instâncias e na ordem fixa da avaliação, para que o resultado não dependa de qual instância atendeu (RN30, RN39, RNF04).

**Why this priority**: sem isso, 429 e 423 mudam conforme a implantação.

**Independent Test**: dividir chamadas entre duas instâncias com os mesmos contadores e conferir o total; chamar com consentimento revogado acima do TPM.

**Acceptance Scenarios**:

1. **Given** duas instâncias, **When** 1001 chamadas se dividem entre elas no mesmo minuto, **Then** o limite vale sobre o total.
2. **Given** um endpoint acima do TPM, **When** a receptora chama com consentimento revogado, **Then** responde 401.

### Edge Cases

- Chamada recusada em segurança (400, 401, 403 do portão): não conta em capacidade, TPM nem cota, porque RN39 a recusa antes.
- Chamada recusada por capacidade (529): não conta no TPM.
- Chamada recusada por parâmetro (422): conta no TPM, porque RN39 põe o tráfego antes, e não na cota, porque não é 2XX (RN27).
- Instituição inativa (503) ou inexistente (404): recusada antes de qualquer contador, como hoje.
- Endpoint sem `pagination-key` no contrato: toda chamada 2XX conta (RN27).
- `page=2` sem chave: é consulta nova e conta; o que define "página seguinte" é a chave válida, como diz o contrato (`accounts-2.5.1.yaml:1458`).
- Instituição com limite operacional desligado: não conta nem responde 423 (RN25).
- Mudança de limite no meio do mês ou do minuto: vale na próxima chamada, sem zerar contadores (RN05).
- Virada do mês em UTC: a cota recomeça (Q7).
- Relógio da instituição alterado: mês pela hora simulada; TPM e capacidade pela hora real (RN73). O relógio por instituição é entregue na spec 007.
- Cota esgotada: chave válida responde 200; chave recusada responde 423 (Q5).
- Acima do TPM, recurso de outro cliente responde 429 (Q4).
- Contadores indisponíveis: a chamada segue normal (Q8).

## Requirements *(mandatory)*

### Functional Requirements

**Ordem**

- **FR-001**: As etapas MUST seguir RN39: cabeçalhos e segurança do token (400, 401, 403), capacidade (529), tráfego (429), acesso ao recurso (403), parâmetros (422, inclusive `PAGE_NOT_FOUND`), limite operacional (423) e resposta, sem caminho paralelo (ADR-008, Q4).

**Tráfego (UC18)**

- **FR-002**: O TPM MUST contar por instituição, endpoint e receptora autenticada (RN20).
- **FR-003**: O TPM MUST seguir a classe do endpoint e a configuração da instituição; na alta sem valor fixo, a faixa de QCA do banco ou o QCA próprio da receptora (RN21, RN26).
- **FR-004**: A janela MUST ser o minuto cheio e MUST seguir a hora real, injetada nos testes (RN22, RN73, princípio VI).
- **FR-005**: Passar do TPM MUST responder 429, com `Retry-After` quando ligado na instituição (RN23, PA11).
- **FR-006**: Token, Consentimentos e Recursos MUST NOT ter limite de tráfego (RN24).

**Limite operacional (UC19)**

- **FR-007**: Com o limite ligado, a cota MUST contar por mês, endpoint, objeto, cliente e receptora, com mês em UTC (RN25, Q7). O objeto é o último parâmetro do caminho; sem parâmetro, o consentimento (Q6).
- **FR-008**: A cota MUST valer só nos 69 endpoints de dados: Consentimentos e Recursos não têm esse limite (RN28; nenhum dos dois contratos declara 423).
- **FR-009**: Valores por classe, com 420 para saldos e limites da conta, e configuráveis por instituição, mesmo abaixo do oficial, com aviso na configuração (RN26).
- **FR-010**: Passar da cota MUST responder 423 (RN28).
- **FR-011**: Só 2XX MUST contar, depois da resposta (RN27, ADR-008). Chamadas simultâneas podem ultrapassar a cota em poucas unidades (Q9).
- **FR-012**: Chamada com `pagination-key` válida MUST NOT contar (RN27). Chamada com chave inválida ou expirada MUST contar, além de gerar nova chave e responder com sucesso (RN36).
- **FR-013**: O resultado da chave MUST ficar disponível à etapa de cota, sem gravar a chave (RN37). Com a cota esgotada, chave válida MUST responder 200, e chave recusada MUST responder 423, sem nova chave (RN36, UC19, Q5).

**Capacidade (UC20)**

- **FR-014**: A capacidade MUST contar requisições por segundo cheio, por instituição, somando todos os endpoints, pela hora real (RN29, RN73, UC20, Q1).
- **FR-015**: Passar da capacidade configurada MUST responder 529 nas 75 operações cujo contrato o declara; token e administração não contam (RN29, Q3).

**Contadores e respostas**

- **FR-016**: Os contadores MUST ser compartilhados entre instâncias (RN30, RNF04), em Redis (ADR-003), e o ambiente Docker Compose MUST incluí-lo (RNF11). Com os contadores indisponíveis, a chamada MUST seguir como dentro dos limites, e a falha MUST aparecer só na observabilidade (RN30, Q8).
- **FR-017**: As respostas 429, 423 e 529 MUST validar contra o schema de erro do contrato de cada operação (RNF01), com `code` `TOO_MANY_REQUESTS`, `LOCKED` e `SITE_IS_OVERLOADED` (Q10), e MUST NOT explicar por que saíram assim (RN18).
- **FR-018**: 429, 423 e 529 MUST NOT sair do sorteio de erros (RN13). Nesta spec, o 429 sai só do TPM; a regra de cenário entra na 007 (RN23, Q2).
- **FR-019**: Mudar limites MUST valer nas próximas chamadas, sem reiniciar (RN05, RNF12).
- **FR-020**: O simulador MUST NOT guardar registro das chamadas: os contadores guardam só totais (RN18, princípio V).

**Cenários**

- **FR-021**: Os cenários de `limites.feature` e os dois `@spec-006` de `paginacao.feature` MUST rodar no CI, sem a etiqueta, ao fim desta spec (ADR-009). O 500 do cenário de erro é provocado pelo teste (Q11).

### Key Entities *(include if feature involves data)*

- **Contador de capacidade**: instituição e segundo; só total.
- **Contador de tráfego**: instituição, endpoint, receptora e minuto; só total.
- **Contador de cota**: instituição, mês, endpoint, objeto, cliente e receptora; só total.
- **Limites da instituição**: TPM, QCA, cota, capacidade e `Retry-After`, já existentes desde a spec 002.
- **Resultado da chave**: ausente, válida ou recusada; calculado na chamada.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cenários de `limites.feature` e os dois `@spec-006` de `paginacao.feature` passam no CI, sem etiqueta.
- **SC-002**: Nos 69 endpoints de dados, o limite aplicado é igual ao da tabela oficial em 100% dos casos.
- **SC-003**: Com várias instâncias, o total atendido em um minuto nunca passa do TPM configurado.
- **SC-004**: Uma coleta paginada dentro da validade da chave consome exatamente 1 da cota, em 100% das execuções.
- **SC-005**: As respostas 429, 423 e 529 validam contra o contrato em 100% das operações que as declaram.
- **SC-006**: Com limites ativos, o simulador segue dentro das metas de RNF02 (300 requisições por segundo por instância) e RNF03 (10 ms no p95).
- **SC-007**: Nenhum teste depende de hora real, e a cobertura fica em 100% no domínio e 80% no total.

## Assumptions

- As specs 001 a 005 entregam rotas, instituições com os limites, token, portão e a chave assinada. A 005 não expõe o resultado da chave; esta spec passa a expor.
- `avisos` já sai em `GET` e `PATCH /admin/instituicoes/{endereco}` (spec 002). O passo "a tela deve avisar" de `limites.feature:118` confere esse campo; a tela em si (UI05) é da spec 010.
- A origem é sempre a receptora do token: toda rota limitada exige autenticação, e por RN39 a recusa vem antes da contagem. A origem por IP de RN20 não tem uso nesta versão.
- O `Retry-After` envia o valor configurado (`RetryAfterSeg`, 60 por padrão), só no 429. Nenhum dos 15 contratos declara esse cabeçalho, e um cabeçalho a mais não contraria o schema.
- "Endpoint" é a operação do contrato: `/accounts` de Contas e de Cartão são diferentes.
- Valor fixo de TPM na alta (como no Banco Restritivo) vence a faixa de QCA, conforme `Limites.TpmDaAlta`.
- Redis perdido zera os contadores, o que é aceito (ADR-003).
- As chamadas dos cenários de TPM são distribuídas nos segundos do minuto, para a capacidade não recusar antes.
- Os valores numéricos de RN20 a RN29 vêm das fontes F01, F03 e F04, que não estão no repositório; os contratos não os trazem.
