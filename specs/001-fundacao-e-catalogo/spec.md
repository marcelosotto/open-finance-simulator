# Feature Specification: Fundação e catálogo

**Feature Branch**: `001-fundacao-e-catalogo`

**Created**: 2026-11-05

**Status**: Draft

**Input**: User description: "Fundação e catálogo: monólito modular, CI com cobertura, leitura das specs de open-finance-specs por commit, rotas geradas, tabela oficial de endpoints e marcação de ambiente. Usar docs/ e marcacao.feature. Nome curto: fundacao-e-catalogo"

**Origem em `docs/`**: [UC24](../../docs/01-requisitos/casos-de-uso/README.md#uc24), [UC23](../../docs/01-requisitos/casos-de-uso/README.md#uc23), [RF17](../../docs/01-requisitos/requisitos.md#rf17), [RF18](../../docs/01-requisitos/requisitos.md#rf18), [RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60) a [RN66](../../docs/01-requisitos/regras-de-negocio.md#rn66), [RN70](../../docs/01-requisitos/regras-de-negocio.md#rn70), [RN72](../../docs/01-requisitos/regras-de-negocio.md#rn72), [RNF01](../../docs/01-requisitos/requisitos.md#rnf01), [RNF07](../../docs/01-requisitos/requisitos.md#rnf07), [RNF08](../../docs/01-requisitos/requisitos.md#rnf08), [RNF10](../../docs/01-requisitos/requisitos.md#rnf10), [RNF11](../../docs/01-requisitos/requisitos.md#rnf11), [PR01](../../docs/00-visao-geral/premissas.md#pr01), [PR03](../../docs/00-visao-geral/premissas.md#pr03), [PR06](../../docs/00-visao-geral/premissas.md#pr06), [PR07](../../docs/00-visao-geral/premissas.md#pr07), [PA23](../../docs/01-requisitos/regras-de-negocio.md#pa23), [ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md), [ADR-005](../../docs/05-adr/ADR-005-marcacao-em-tres-camadas.md), [ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md), [ADR-009](../../docs/05-adr/ADR-009-gherkin-reqnroll.md), [tabela oficial de endpoints](../../docs/01-requisitos/tabela-de-endpoints.md), [estratégia de testes](../../docs/06-testes/estrategia-de-testes.md). Cenários Gherkin: [marcacao.feature](../../docs/01-requisitos/gherkin/marcacao.feature).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catálogo das APIs a partir das specs fixadas (Priority: P1)

Como desenvolvedor, quero que o simulador conheça as APIs Open Finance (consentimento, recursos e dados) lendo as specs oficiais de `open-finance-specs` na versão fixada por commit, e publique cada operação no endereço de produção sob o prefixo da instituição, sem nenhuma rota escrita à mão ([PR06](../../docs/00-visao-geral/premissas.md#pr06), [ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md), [ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md)).

**Why this priority**: tudo o que vem depois (instituições, massa, consentimentos, dados, limites) se pendura nas operações do catálogo. Sem ele, nenhuma outra spec tem onde morar.

**Independent Test**: subir o simulador com o commit inicial (`7bdb9c1a6c4c265d7aabcdbc662f94c61718135e`) e conferir que cada operação das specs fixadas responde no caminho oficial, com o prefixo de uma instituição, e que um caminho fora das specs não responde como operação Open Finance.

**Acceptance Scenarios**:

1. **Given** o arquivo de travamento aponta para o commit inicial e os hashes conferem, **When** o simulador sobe, **Then** toda operação das specs fixadas está disponível em `/{banco}/open-banking/{api}/{versão}/…`, com o mesmo método e caminho da spec.
2. **Given** o simulador no ar, **When** a receptora chama um caminho que não existe nas specs fixadas, **Then** recebe a resposta oficial de recurso não encontrado, sem rota inventada.
3. **Given** o catálogo carregado, **When** alguém consulta a lista de operações conhecidas, **Then** cada operação traz API, versão, método, caminho e o schema de resposta oficial.
4. **Given** o simulador empacotado, **When** sobe sem acesso à rede, **Then** carrega as mesmas specs, porque elas vão dentro da imagem.

---

### User Story 2 - Atualizar a versão das specs com conferência no CI (Priority: P1)

Como desenvolvedor, quero trocar o commit em `specs.lock.json`, abrir um PR e ver o CI conferir os hashes e listar as operações novas, alteradas e removidas, para adotar uma versão oficial nova com uma mudança pequena e revisável ([UC24](../../docs/01-requisitos/casos-de-uso/README.md#uc24), [RNF07](../../docs/01-requisitos/requisitos.md#rnf07)).

**Why this priority**: fixar por commit só vale se a troca for segura. Um hash divergente que passa sem aviso esconde spec adulterada ou incompleta.

**Independent Test**: abrir um PR que muda o commit e conferir o relatório de diferenças; abrir outro com um hash adulterado e conferir que o build falha.

**Acceptance Scenarios**:

1. **Given** um PR que muda o commit e os hashes conferem, **When** o CI roda, **Then** mostra as operações novas, alteradas e removidas em relação ao commit anterior.
2. **Given** um arquivo de spec cujo SHA-256 difere do registrado, **When** o CI roda, **Then** o build falha e nomeia o arquivo divergente.
3. **Given** uma versão nova sem regra nova, **When** o PR é aplicado, **Then** nenhuma regra de negócio precisa mudar ([RNF07](../../docs/01-requisitos/requisitos.md#rnf07)).

---

### User Story 3 - Tabela oficial de frequência e limites por endpoint (Priority: P1)

Como QA, quero que cada endpoint do catálogo traga a classe de frequência, o SLA, o TPM e o limite mensal da [tabela oficial](../../docs/01-requisitos/tabela-de-endpoints.md), e que um teste no CI quebre se algum valor divergir, para que nenhuma instituição simulada nasça com limite diferente do oficial ([PR01](../../docs/00-visao-geral/premissas.md#pr01)).

**Why this priority**: as specs não trazem frequência nem limites ([ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md)). Os limites (spec 006) e o comportamento (spec 007) dependem desses valores. A POC errava a classe de 25 de 69 endpoints ([confronto com a POC](../../docs/01-requisitos/confronto-com-a-poc.md)).

**Independent Test**: rodar o teste da tabela oficial e conferir que ele compara cada endpoint; alterar um valor de propósito e conferir que o teste falha.

**Acceptance Scenarios**:

1. **Given** o arquivo de classes com a tabela oficial, **When** o teste da tabela roda, **Then** confere classe, SLA, TPM e limite mensal de cada endpoint listado na tabela.
2. **Given** um endpoint com classe diferente da tabela, **When** o teste roda, **Then** falha e aponta o endpoint e o valor divergente.
3. **Given** uma operação do catálogo sem linha no arquivo de classes, ou uma linha sem operação no catálogo, **When** o CI roda, **Then** o build falha e nomeia a operação.
4. **Given** endpoints com o mesmo caminho em APIs diferentes (por exemplo, `GET /accounts` em Contas e em Cartão de Crédito), **When** a classe é consultada, **Then** cada API tem seus próprios valores.

---

### User Story 4 - Marcação de ambiente e natureza (Priority: P1)

Como equipe, quero reconhecer um dado simulado em qualquer lugar, mesmo depois de copiado para outro sistema, pela marca em três lugares: cabeçalho da resposta, prefixo dos identificadores e texto de campos livres ([UC23](../../docs/01-requisitos/casos-de-uso/README.md#uc23), [RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60), [ADR-005](../../docs/05-adr/ADR-005-marcacao-em-tres-camadas.md)).

**Why this priority**: é princípio da constituição (IV) e deve existir antes de qualquer dado ser gerado nas specs seguintes.

**Independent Test**: rodar os cenários de [marcacao.feature](../../docs/01-requisitos/gherkin/marcacao.feature) (exceto o `@tela`) com dados de teste, em `dev` e em `hml`.

**Acceptance Scenarios** (de `marcacao.feature`):

1. **Given** um ambiente `dev` e um recurso persistido, **When** Ana consulta o recurso, **Then** `x-sim-environment` é `dev` e `x-sim-data-nature` é `persistido` ([RN61](../../docs/01-requisitos/regras-de-negocio.md#rn61)). O mesmo vale para `hml` e `virtual`.
2. **Given** um ambiente `hml`, **When** é criado um recurso persistido ou virtual, **Then** o ID começa com `hml-p-` ou `hml-v-` e segue o padrão oficial `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$` ([RN62](../../docs/01-requisitos/regras-de-negocio.md#rn62)).
3. **Given** um ambiente `hml`, **When** é criado um consentimento persistido ou virtual, **Then** o `consentId` começa com `urn:hml:p-` ou `urn:hml:v-` e segue o padrão oficial ([RN62](../../docs/01-requisitos/regras-de-negocio.md#rn62)).
4. **Given** um ambiente `hml`, **When** Ana consulta uma transação, **Then** `transactionName` começa com `[HML]` e respeita o tamanho oficial do campo ([RN63](../../docs/01-requisitos/regras-de-negocio.md#rn63)).
5. **Given** uma resposta marcada, **When** Ana a valida contra a spec oficial, **Then** a resposta é válida ([RN61](../../docs/01-requisitos/regras-de-negocio.md#rn61)).
6. **Given** um cliente criado em `dev`, **When** a massa é copiada para `hml`, **Then** o cliente continua marcado como `dev` ([RN64](../../docs/01-requisitos/regras-de-negocio.md#rn64)).
7. **Given** uma lista com dado persistido e dado virtual, **When** Ana consulta a lista, **Then** `x-sim-data-nature` é `misto` ([RN65](../../docs/01-requisitos/regras-de-negocio.md#rn65)).
8. **Given** qualquer instituição, **When** Diego tenta desligar a marcação, **Then** a opção não existe ([RN66](../../docs/01-requisitos/regras-de-negocio.md#rn66)).
9. **Given** um ambiente `hml`, **When** Ana consulta uma transação, **Then** a marca aparece no cabeçalho, no identificador e no texto ([RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60)).
10. **Given** a marca de ambiente vazia (produção), **When** Ana consulta uma transação persistida, **Then** `x-sim-environment` não é enviado, `x-sim-data-nature` é `persistido`, o ID começa com `p-` e `transactionName` sai sem marca ([RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60), [RN70](../../docs/01-requisitos/regras-de-negocio.md#rn70)).
11. **Given** a marca de ambiente vazia, **When** é criado um consentimento virtual, **Then** o `consentId` começa com `urn:v:` e segue o padrão oficial ([RN62](../../docs/01-requisitos/regras-de-negocio.md#rn62)).

Nos cenários, "ambiente `hml`" quer dizer "marca de ambiente configurada como `hml`"; o mesmo vale para qualquer outro valor, como `stage`.

---

### User Story 5 - Fundação: módulos, execução local e portões de CI (Priority: P2)

Como desenvolvedor, quero um monólito modular que sobe por inteiro com um comando local e um CI que bloqueia o merge quando falha formatação, análise estática, testes, cenários Gherkin, cobertura, conformidade com as specs ou a tabela oficial ([ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md), [RNF08](../../docs/01-requisitos/requisitos.md#rnf08), [RNF11](../../docs/01-requisitos/requisitos.md#rnf11)).

**Why this priority**: é a base técnica das histórias acima, mas sozinha não entrega valor de simulação.

**Independent Test**: clonar o repositório numa máquina limpa, subir o ambiente com um comando e abrir um PR que derruba a cobertura do domínio abaixo de 100%, conferindo que o CI bloqueia.

**Acceptance Scenarios**:

1. **Given** uma máquina com o ambiente de contêineres instalado, **When** o desenvolvedor sobe o ambiente com um comando, **Then** a API Open Finance, a administração e as dependências ficam no ar ([RNF11](../../docs/01-requisitos/requisitos.md#rnf11)).
2. **Given** um PR com cobertura do domínio abaixo de 100% ou total abaixo de 80%, **When** o CI roda, **Then** o merge é bloqueado ([RNF08](../../docs/01-requisitos/requisitos.md#rnf08)).
3. **Given** um cenário de `marcacao.feature` sem implementação ou falhando, **When** o CI roda, **Then** o build falha.
4. **Given** um módulo que usa por dentro outro módulo sem passar pela fronteira pública dele, **When** o CI roda, **Then** a análise estática aponta a violação.

---

### Edge Cases

- **Arquivo de travamento ausente ou malformado:** o build falha antes de empacotar, com mensagem que nomeia o problema.
- **Commit inexistente no repositório de specs:** o build falha; nada é empacotado.
- **Arquivo novo na spec sem hash registrado, ou hash registrado sem arquivo:** tratado como divergência; o build falha.
- **Mesmo caminho em APIs diferentes** (`/accounts` em Contas e Cartão; `/contracts` em quatro famílias de crédito): cada API fica no seu próprio prefixo de versão e com sua própria classe.
- **Prefixo de ID que estoura o tamanho oficial:** o identificador gerado continua dentro de 100 caracteres e do padrão oficial; a parte aleatória encolhe, nunca a marca.
- **Texto marcado que estoura o tamanho oficial:** o texto original é cortado para caber com a marca `[HML] `/`[DEV] ` na frente; a marca nunca é cortada.
- **Resposta sem dado** (lista vazia, erro): leva o cabeçalho de ambiente; a natureza segue a regra decidida no plano, sem mudar o corpo oficial.
- **Marca de ambiente vazia (produção):** nada é concatenado; ver FR-019.
- **Marca de ambiente com outro valor (por exemplo, `stage`):** é usada como veio, sem regra; ver FR-019.
- **Spec com operação que o simulador ainda não implementa:** ver FR-006.

## Requirements *(mandatory)*

### Functional Requirements

**Catálogo de specs**

- **FR-001**: O sistema MUST ler as specs oficiais de `open-finance-specs` no commit registrado no arquivo de travamento `specs.lock.json`, começando pelo commit `7bdb9c1a6c4c265d7aabcdbc662f94c61718135e` ([ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md)).
- **FR-002**: O build MUST conferir o SHA-256 de cada arquivo de spec contra o registrado e falhar, nomeando o arquivo, se algum divergir, faltar ou sobrar ([UC24](../../docs/01-requisitos/casos-de-uso/README.md#uc24)).
- **FR-003**: As specs MUST ir empacotadas na imagem, para que o simulador suba sem acesso à rede ([ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md)).
- **FR-004**: O catálogo MUST cobrir as APIs de consentimento, recursos e todas as famílias de dados da [tabela oficial](../../docs/01-requisitos/tabela-de-endpoints.md) ([PA06](../../docs/01-requisitos/regras-de-negocio.md#pa06)).
- **FR-005**: Toda operação do catálogo MUST ser publicada a partir da spec, com o mesmo método e caminho, sob o prefixo da instituição: `/{banco}/open-banking/{api}/{versão}/…`. Nenhuma rota oficial pode ser escrita à mão ([PR06](../../docs/00-visao-geral/premissas.md#pr06), [ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md)).
- **FR-006**: Enquanto o comportamento de uma operação não for entregue pela spec que cuida dela, a operação publicada MUST responder 501 (não implementado) no formato de erro oficial da API, com os cabeçalhos de marcação. Ela não finge comportamento nem devolve o exemplo da spec. A validação de contrato e marcação de cada operação entra junto com a spec que implementa a operação (decisão de Marcelo em 2026-11-05).
- **FR-007**: O catálogo MUST expor, para cada operação, API, versão, método, caminho, parâmetros, cabeçalhos exigidos e schema de resposta, para uso das specs seguintes e da validação de contrato.
- **FR-008**: Caminho fora do catálogo MUST receber a resposta oficial de recurso não encontrado.
- **FR-009**: O CI MUST, num PR que troca o commit, listar as operações novas, alteradas e removidas em relação ao commit anterior ([UC24](../../docs/01-requisitos/casos-de-uso/README.md#uc24)).
- **FR-010**: Trocar o commit MUST NOT exigir mudança de regra de negócio, salvo quando a versão nova traz regra nova ([RNF07](../../docs/01-requisitos/requisitos.md#rnf07)).

**Tabela oficial**

- **FR-011**: O sistema MUST manter um arquivo próprio com classe de frequência, SLA, TPM e limite mensal de cada endpoint, copiado da [tabela oficial](../../docs/01-requisitos/tabela-de-endpoints.md), além do timeout (15 s) e do TPS (300) comuns a todos ([ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md)).
- **FR-012**: O arquivo MUST distinguir endpoints de mesmo caminho em APIs diferentes.
- **FR-013**: Endpoints com TPM "Faixa QCA" MUST ficar marcados como dependentes dos consentimentos ativos ([RN21](../../docs/01-requisitos/regras-de-negocio.md#rn21)); o cálculo da faixa fica para a spec de limites.
- **FR-014**: Um teste MUST comparar cada linha do arquivo com a tabela oficial e falhar no primeiro valor divergente, nomeando endpoint e campo.
- **FR-015**: O CI MUST falhar se houver operação do catálogo sem linha no arquivo, ou linha sem operação no catálogo.

**Marcação**

- **FR-016**: Toda resposta das APIs Open Finance MUST trazer `x-sim-environment` com a marca do ambiente (omitido quando a marca está vazia) e, quando houver dado, `x-sim-data-nature` com `persistido`, `virtual` ou `misto`, sem mudar o corpo oficial ([RN61](../../docs/01-requisitos/regras-de-negocio.md#rn61), [RN65](../../docs/01-requisitos/regras-de-negocio.md#rn65)).
- **FR-017**: Todo identificador gerado MUST começar com `{marca}-p-` ou `{marca}-v-`, e todo `consentId` com `urn:{marca}:p-` ou `urn:{marca}:v-`, sempre dentro do padrão e do tamanho oficiais. Com a marca vazia, fica só a natureza: `p-…`/`v-…` e `urn:p:…`/`urn:v:…` ([RN62](../../docs/01-requisitos/regras-de-negocio.md#rn62)).
- **FR-018**: `transactionName` e o nome do cliente MUST começar com a marca em maiúsculas entre colchetes (`[HML]`, `[DEV]`, `[STAGE]`…), dentro do tamanho oficial do campo; com a marca vazia, o texto sai sem marca ([RN63](../../docs/01-requisitos/regras-de-negocio.md#rn63), [PA23](../../docs/01-requisitos/regras-de-negocio.md#pa23)).
- **FR-019**: A marca do ambiente MUST vir de uma configuração da implantação, com valor livre (`dev`, `hml`, `stage` ou outro), sem lista fechada nem validação de regra: o sistema só concatena o valor nos três lugares (cabeçalho, prefixo dos IDs e texto). Com a configuração **vazia** (produção), nada é concatenado: os IDs saem sem prefixo de ambiente, o texto sai sem marca e o cabeçalho `x-sim-environment` não é enviado ([RN70](../../docs/01-requisitos/regras-de-negocio.md#rn70), decisão de Marcelo em 2026-11-05).
- **FR-020**: A marca MUST ser gravada na criação do dado e nunca mudar, inclusive quando a massa é copiada para outro ambiente ([RN64](../../docs/01-requisitos/regras-de-negocio.md#rn64)).
- **FR-021**: Nenhuma configuração de instituição, perfil ou cenário MUST permitir desligar ou alterar a marca ([RN66](../../docs/01-requisitos/regras-de-negocio.md#rn66)).
- **FR-022**: Um teste MUST percorrer as operações publicadas e falhar se algum ID ou resposta vier sem marca ([estratégia de testes](../../docs/06-testes/estrategia-de-testes.md)).
- **FR-023**: Resposta marcada, com perfil `estavel` e sem defeitos, MUST validar contra o schema oficial ([RNF01](../../docs/01-requisitos/requisitos.md#rnf01)).

**Fundação**

- **FR-024**: O sistema MUST ser um monólito modular, com módulos de fronteira explícita, de modo que API Open Finance, administração e worker possam rodar juntos agora e separados depois ([ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md)).
- **FR-025**: As APIs de administração MUST ficar em endereço separado das APIs Open Finance ([RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71), [PR07](../../docs/00-visao-geral/premissas.md#pr07)).
- **FR-026**: O ambiente completo MUST subir localmente com um único comando ([RNF11](../../docs/01-requisitos/requisitos.md#rnf11)).
- **FR-027**: O CI MUST bloquear o merge quando falhar formatação, análise estática, testes, cenários Gherkin, cobertura (100% no domínio e 80% no total), conformidade com as specs ou o teste da tabela oficial ([RNF08](../../docs/01-requisitos/requisitos.md#rnf08)).
- **FR-028**: Os cenários de `marcacao.feature` (exceto `@tela`) MUST existir como testes de aceitação antes da implementação e rodar no CI ([ADR-009](../../docs/05-adr/ADR-009-gherkin-reqnroll.md)).
- **FR-029**: Relógio e semente MUST ser injetáveis desde a fundação, para que nenhum teste dependa de hora real ou de sorteio sem semente ([PR05](../../docs/00-visao-geral/premissas.md#pr05)).

### Key Entities

- **Travamento das specs**: commit fixado do repositório de specs e o SHA-256 de cada arquivo usado.
- **Operação do catálogo**: API, versão, método, caminho, parâmetros, cabeçalhos e schema de resposta, lidos da spec fixada.
- **Classe do endpoint**: API, endpoint, frequência, SLA, TPM (valor fixo ou "Faixa QCA") e limite mensal, da tabela oficial; mais timeout e TPS comuns.
- **Marca**: marca do ambiente (valor livre da configuração, vazio em produção) e natureza (`persistido`/`virtual`), gravados na criação do dado e imutáveis; a resposta agrega a natureza como `persistido`, `virtual` ou `misto`.
- **Diferença entre versões**: operações novas, alteradas e removidas entre dois commits.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das operações das specs fixadas respondem no caminho oficial sob o prefixo de uma instituição, e 0 rotas oficiais existem fora do que as specs geram.
- **SC-002**: 100% dos endpoints da tabela oficial têm classe, SLA, TPM e limite mensal iguais aos da tabela; uma única divergência proposital quebra o CI.
- **SC-003**: Um hash adulterado em qualquer arquivo de spec quebra o build em 100% das tentativas.
- **SC-004**: Trocar a versão das specs se resume a mudar o arquivo de travamento e revisar o relatório de diferenças, sem alteração de regra de negócio quando a versão não traz regra nova.
- **SC-005**: 100% das respostas e identificadores percorridos pelo teste de marcação trazem a marca. Das operações que já têm comportamento, 100% das respostas com perfil `estavel` validam contra o schema oficial. As operações ainda em 501 entram nessa conta quando a spec delas for entregue.
- **SC-006**: Todos os cenários não `@tela` de `marcacao.feature` passam no CI.
- **SC-007**: Um desenvolvedor novo sobe o ambiente completo numa máquina limpa com um comando, em menos de 10 minutos.
- **SC-008**: Nenhum PR entra com cobertura abaixo de 100% no domínio ou 80% no total.

## Assumptions

- A decisão do FR-019 (marca configurável e vazia em produção) já está em `docs/` (RN09, RN60 a RN66, RN70, RN72, PA05, ADR-005, `marcacao.feature`) e na constituição 2.0.0, princípio IV.
- Com a marca vazia, a natureza continua aparecendo no ID e no cabeçalho `x-sim-data-nature`.
- O sistema não confere o valor da marca. Valor com caractere fora de letras, números e hífen deixa os IDs fora do padrão oficial; é responsabilidade de quem configura.
- O escopo das APIs é o da primeira versão: consentimento, recursos e dados ([PA06](../../docs/01-requisitos/regras-de-negocio.md#pa06)). Pagamentos e demais famílias ficam fora.
- O comportamento real das operações (consentimento, recursos, dados, paginação, limites) chega nas specs 004 a 007. Esta spec entrega o catálogo, as rotas e a marcação como mecanismo, testados com dados de teste.
- O cadastro de instituições é da spec 002. Aqui as rotas já carregam o prefixo `{banco}`, e os testes usam uma instituição de teste.
- O cenário `@tela` de `marcacao.feature` ([RN72](../../docs/01-requisitos/regras-de-negocio.md#rn72), ambiente em destaque) fica para a spec 010 (back-office).
- O cenário de cópia de massa entre ambientes ([RN64](../../docs/01-requisitos/regras-de-negocio.md#rn64)) é provado no nível do dado marcado; a cópia em si, como operação, chega com a massa (spec 003).
- O arquivo de classes é mantido à mão a partir da tabela oficial; quando a tabela oficial muda, o arquivo muda ([ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md)).
- Segurança em modo simples e demais itens da stack seguem a constituição; esta spec não decide a autenticação das APIs Open Finance (spec 004).
- Observabilidade da saúde do simulador ([RNF09](../../docs/01-requisitos/requisitos.md#rnf09)) entra como base mínima na fundação; o simulador não guarda registro das chamadas ([RN18](../../docs/01-requisitos/regras-de-negocio.md#rn18)).
