# Spec 008: divergências e perguntas

**Respondidas em 2026-11-10.** Aprovada a opção A de todas as perguntas. Decisões na seção Clarifications da [spec](spec.md). A 009 usa RN116 a RN119.

Conferência de `docs/` e de `consentimento-no-painel.feature` contra os contratos `consents-3.3.1.yml` e `resources-3.1.0.yml` em `.specs/7bdb9c1a6c4c265d7aabcdbc662f94c61718135e/contracts/expansion/snapshots/`. Os números de linha são desses arquivos, de `docs/` e de `src/` no commit `087cc0f`.

## Divergências

### D1. O contrato sugere rejeitar o consentimento quando a alçada vence; RN98 só muda os recursos

`regras-de-negocio.md:130` (RN98): recusa ou fim do prazo levam **os recursos** a `UNAVAILABLE`, e o consentimento segue `AUTHORISED`. Em `consents-3.3.1.yml:1017`, o exemplo de `rejection.reason.additionalInformation` é "Tempo de confirmação da múltipla alçada excedido.", o que sugere um consentimento `REJECTED` (provavelmente com `CONSENT_EXPIRED`, `:998`). É só um exemplo, não uma regra; a descrição de Recursos (`resources-3.1.0.yml:21`) liga `PENDING_AUTHORISATION` à alçada dupla e não fala do consentimento. Ver Q3.

### D2. O estado do recurso hoje é um só, para todos os consentimentos

O estado fica gravado no recurso (`Massa/Contratos/Modelos.cs:47-62`) e vale em qualquer consentimento que o inclua. `PENDING_AUTHORISATION` da alçada é do **consentimento**: outro consentimento da mesma empresa, sem alçada, sobre a mesma conta, não pode ficar pendente. O contrato diz que o status considera "não apenas o consentimento vinculado mas também a disponibilidade do recurso" (`resources-3.1.0.yml:11`). Ver Q2.

### D3. Não existe front-end no repositório

`docs/08-front-end/README.md` define `front/`, mas a pasta não existe, e a fundação do front (Webpack, Jest, Clean Architecture, o CRUD de bancos como modelo) está na linha 010 de `sequencia-de-specs.md:18`. `Ganchos.cs:27` manda os cenários `@tela` para a 010. Os cenários de `consentimento-no-painel.feature` não têm `@tela`. A spec 004 já tratou "painel" como "operação de administração equivalente" (`specs/004-consentimentos-e-recursos/spec.md:229`). Ver Q1.

## Divergência menor, a corrigir em `docs/`

- **D4.** A linha 010 de `sequencia-de-specs.md:18` cita "telas de bancos, massa e cenários", sem consentimentos, mas UI10, UI13 e UI14 (`interface.md:16-22`) são telas de consentimento. Proposta (se Q1 = A): incluir "consentimentos" na linha 010.
- **D5.** `modelo-de-dominio.md:54-58` tem a classe `Aprovacao` (aprovador, decisão, prazo). Proposta: manter, e o consentimento passa a guardar as decisões (Q7).

## Conferido, sem divergência

| Afirmação | Evidência |
|---|---|
| `DEPENDE_MULTIPLA_ALCADA` na renovação | `consents-3.3.1.yml:370`, `:1249`; já implementado em `Ciclo.cs:39-42` |
| `PENDING_AUTHORISATION` é estado oficial de recurso | `resources-3.1.0.yml:21`, `:176` |
| Sem `expirationDateTime`, prazo indeterminado (RN85) | `consents-3.3.1.yml:971-972` |
| Grupos de permissões e códigos de RN83 e RN84 | `Permissoes.cs:20-40`, já usados por `POST /consents` |
| O contrato não fixa prazo máximo do consentimento nem da alçada | sem ocorrência em `consents-3.3.1.yml`; os 15 dias vêm de RN98 |

## Cenários no escopo

Entram os 10 cenários de `consentimento-no-painel.feature` e "Renovação com múltiplas alçadas" (`consentimento-api.feature:103-107`), todos hoje com `@spec-008`.

## Números de RN (para o merge com a 009)

A 008 usa só **RN96, RN97 e RN99**, as lacunas da seção "Criação no painel" de `regras-de-negocio.md`. A 009 deve evitar esses três números. Em arquivos compartilhados (`regras-de-negocio.md`, `casos-de-uso/README.md`, `Program.cs`), as mudanças da 008 são pequenas e localizadas.

## Perguntas

Respostas no formato `Q1: A`. Recomendação sempre na opção A.

### Q1 (D3). O que a 008 entrega como "painel"?

- **A (recomendada):** a API de administração que o painel vai usar: opções do menu (permissões PF ou PJ, contas e cartões já marcados, receptoras), criação, consulta, aprovação e recusa. Os cenários rodam contra essa API. As telas UI10, UI13 e UI14 ficam na 010, que passa a citar consentimentos (D4).
- **B:** criar `front/` agora, com a fundação do guia de front-end e as telas de consentimento, e testes Playwright.

**Motivo:** a fundação do front e o CRUD-modelo de bancos são da 010; fazer só a tela de consentimento antes inverte a ordem e dobra o escopo. A 004 já usou "painel = administração".

### Q2 (D2). Como o recurso fica pendente só naquele consentimento?

- **A (recomendada):** nova RN97: o estado do recurso **no consentimento** é calculado na leitura, sem gravar nada no recurso. Alçada recusada ou vencida: `UNAVAILABLE`. Alçada pendente: `PENDING_AUTHORISATION`, salvo recurso já `UNAVAILABLE` no banco. Sem alçada ou depois da última aprovação: o estado gravado. Uma regra de cenário de estado (RN108) continua por cima de tudo. Vale em Recursos e nas APIs de produto (RN104).
- **B:** gravar `PENDING_AUTHORISATION` nos recursos ao criar e `AVAILABLE` ou `UNAVAILABLE` na decisão, com um processo em segundo plano para o prazo.

**Motivo:** A não afeta outros consentimentos (RN49, RN107), não precisa de processo agendado, e o prazo de 15 dias funciona com o relógio do banco (princípios VI e X). B quebra o outro consentimento da mesma empresa e esbarra em RN101 (`PENDING_AUTHORISATION` só como estado inicial).

### Q3 (D1). O que acontece com o consentimento quando a alçada é recusada ou vence?

- **A (recomendada):** RN98 literal: o consentimento segue `AUTHORISED`, e só os recursos vão para `UNAVAILABLE`. O token do consentimento continua sendo emitido; as APIs de produto respondem 403 `STATUS_RESOURCE_UNAVAILABLE` no acesso direto e lista vazia. A renovação segue com 422 `DEPENDE_MULTIPLA_ALCADA`.
- **B:** além dos recursos, rejeitar o consentimento: recusa grava `USER` e `CUSTOMER_MANUALLY_REJECTED`; prazo grava `ASPSP` e `CONSENT_EXPIRED`, com "Tempo de confirmação da múltipla alçada excedido." (a 004 não grava `additionalInformation` hoje).

**Motivo:** `docs/` é a fonte da verdade e RN98 já decide; o exemplo do contrato não é regra. A deixa a receptora testar o 403 de recurso indisponível, que é o caso que RN98 quer simular.

### Q4. Como o pedido de criação informa permissões e origens?

- **A (recomendada):** nova RN96: o pedido traz permissões individuais, validadas como em `POST /consents` (RN83, RN84, RN08), com `RESOURCES_READ` acrescentada sempre. Contas e cartões vêm por lista de `resourceId`: lista ausente vale como "todas marcadas"; lista vazia, com o grupo pedido, é recusada (RN95). Na seleção, só entram contas e cartões do cliente naquele banco; outro id é recusado. Crédito, investimento, câmbio e dados cadastrais compartilham tudo o que o cliente tem no banco.
- **B:** o pedido traz nomes de grupos prontos (ex.: "contas-saldos"), sem permissões soltas.

**Motivo:** o cenário "Grupo de permissões incompleto" exige poder pedir um grupo incompleto e ser recusado; B não deixa nem montar esse pedido. "Ausente = todas" segue "todas as opções vêm marcadas".

### Q5. Como se informa o prazo?

- **A (recomendada):** em meses, de 1 a 12, contados do relógio do banco (RN73); sem prazo, indeterminado (RN85).
- **B:** data e hora futura livre, como `expirationDateTime` da API.
- **C:** os dois.

**Motivo:** é o que o cenário usa ("prazo de 12 meses") e o que um menu oferece. Quem quer data exata usa `POST /consents`.

### Q6. A receptora é obrigatória?

- **A (recomendada):** sim. O consentimento do painel fica ligado a ela, como o criado por API: só ela pede token e consulta (RN120).
- **B:** opcional; sem receptora, serve a qualquer receptora cadastrada, como o de lote.

**Motivo:** UC10 passo 2: "escolhe a instituição e a receptora".

### Q7. Como funcionam as alçadas?

- **A (recomendada):** nova RN99: só para cliente PJ (PF é recusado), com 1 a 10 aprovações pendentes. Cada aprovação desconta uma; a mesma pessoa pode aprovar mais de uma vez, como no cenário. Uma recusa basta. O prazo é de 15 dias corridos desde a criação, pelo relógio do banco, fim excluído. Aprovar ou recusar sem pendência, depois de recusa, depois do prazo ou com consentimento `REJECTED` responde 409. Cada decisão grava autor (`x-sim-autor`), decisão e hora no consentimento e vai para o histórico (RN71).
- **B:** permitir alçadas também em PF.

**Motivo:** RN98 diz "na PJ". O teto de 10 só evita valor absurdo; o contrato não fixa número.

### Q8. E se o cliente não tem a origem que a permissão cobre?

- **A (recomendada):** "pelo menos uma" (RN95) só vale quando há opção: cliente sem conta no banco pode pedir contas, e nada de conta é compartilhado, como na API (RN102). Cliente sem nenhum produto naquele banco é recusado (pré-condição de UC10).
- **B:** recusar sempre que um grupo de contas ou cartões não tiver origem selecionada, mesmo sem opção.

**Motivo:** a API já aceita esse caso; B impediria testar a lista vazia de Recursos pelo painel.

### Q9. Como a criação recusada responde?

- **A (recomendada):** 422 no formato da administração (`{ "erros": [...] }`), com os códigos oficiais quando existem (`COMBINACAO_PERMISSOES_INCORRETA`, `PERMISSOES_PJ_INCORRETAS`, `INFORMACOES_PJ_NAO_INFORMADAS`, `PERMISSAO_PF_PJ_EM_CONJUNTO`, `SEM_PERMISSOES_FUNCIONAIS_RESTANTES`) e códigos próprios para o resto (`SEM_ORIGEM_SELECIONADA`, `ORIGEM_INVALIDA`, `CLIENTE_SEM_PRODUTO`, `PRAZO_INVALIDO`, `ALCADA_INVALIDA`, `RECEPTORA_INEXISTENTE`). Cliente ou banco inexistente: 404.
- **B:** mensagens livres em português.

**Motivo:** a tela da 010 decide o que mostrar a partir de um código estável, e os códigos oficiais evitam regra duplicada.
