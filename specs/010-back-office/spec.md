# Feature Specification: Back-office

**Feature Branch**: `claude/010-back-office-d94942`

**Created**: 2026-11-10

**Status**: Aprovada

**Input**: User description: "Back-office: telas de bancos, massa e cenários. Usar docs/, interface.md e docs/08-front-end/README.md. Nome curto: back-office"

**Origem em `docs/`**: linha 010 de [sequência de specs](../../docs/07-gerenciamento/sequencia-de-specs.md); telas [UI01](../../docs/01-requisitos/interface.md#ui01) a [UI07](../../docs/01-requisitos/interface.md#ui07), [UI10](../../docs/01-requisitos/interface.md#ui10), [UI13](../../docs/01-requisitos/interface.md#ui13), [UI14](../../docs/01-requisitos/interface.md#ui14) e [UI16](../../docs/01-requisitos/interface.md#ui16); regras [RN03](../../docs/01-requisitos/regras-de-negocio.md#rn03), [RN11](../../docs/01-requisitos/regras-de-negocio.md#rn11), [RN15](../../docs/01-requisitos/regras-de-negocio.md#rn15), [RN26](../../docs/01-requisitos/regras-de-negocio.md#rn26), [RN56](../../docs/01-requisitos/regras-de-negocio.md#rn56), [RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71), [RN72](../../docs/01-requisitos/regras-de-negocio.md#rn72), [RN80](../../docs/01-requisitos/regras-de-negocio.md#rn80), [RN95](../../docs/01-requisitos/regras-de-negocio.md#rn95) a [RN99](../../docs/01-requisitos/regras-de-negocio.md#rn99), [RN113](../../docs/01-requisitos/regras-de-negocio.md#rn113), [RN114](../../docs/01-requisitos/regras-de-negocio.md#rn114), [PR03](../../docs/00-visao-geral/premissas.md#pr03); [ADR-010](../../docs/05-adr/ADR-010-front-end-react.md) e [guia de front-end](../../docs/08-front-end/README.md). Casos de uso atendidos pela tela: [UC01](../../docs/01-requisitos/casos-de-uso/README.md#uc01) a [UC03](../../docs/01-requisitos/casos-de-uso/README.md#uc03), [UC05](../../docs/01-requisitos/casos-de-uso/README.md#uc05) a [UC08](../../docs/01-requisitos/casos-de-uso/README.md#uc08), [UC10](../../docs/01-requisitos/casos-de-uso/README.md#uc10), [UC11](../../docs/01-requisitos/casos-de-uso/README.md#uc11), [UC21](../../docs/01-requisitos/casos-de-uso/README.md#uc21), [UC22](../../docs/01-requisitos/casos-de-uso/README.md#uc22). Cenários Gherkin: [consentimento-no-painel.feature](../../docs/01-requisitos/gherkin/consentimento-no-painel.feature) (no navegador) e "Ambiente visível nas telas" (`@tela`) de [marcacao.feature](../../docs/01-requisitos/gherkin/marcacao.feature). APIs consumidas: os contratos de administração das specs [002](../002-instituicoes/contracts/admin-instituicoes.md), [003](../003-massa-e-gerador/contracts/admin-massa.md), [004](../004-consentimentos-e-recursos/contracts/token-e-admin.md), [007](../007-comportamento-e-cenarios/contracts/admin-cenarios.md), [008](../008-consentimento-no-painel/contracts/admin-painel.md) e [009](../009-defeitos-e-anomalias/contracts/admin-defeitos-e-anomalias.md).

## Clarifications

### Session 2026-11-10

Marcelo aprovou a opção A de Q1 a Q10 ("Siga as recomendações"), repassada pela sessão controladora. Evidências em [perguntas.md](perguntas.md). Registradas em `docs/`: UI01 e UI06 em `interface.md`, variáveis de ambiente, Husky e CI no guia de front-end. Os marcadores **(Qn)** abaixo apontam para a decisão.

- Q1: bancos, consentimento e receptoras (P1); massa e cenários (P2). Ficam fora: mundo virtual, ajustes, transações de recurso, estado de recurso, produto de cliente e histórico da massa.
- Q2: marca lida de `GET /admin/ambiente` (rota nova); sem `SIM_AMBIENTE`; cor fixa para `dev`, `hml`, `stage` e neutra para o resto.
- Q3: o simulador serve o build do front na porta de administração, na mesma origem; sem CORS e sem contêiner novo.
- Q4: job `front` no CI; aqui, pela imagem `node:22`; Husky configurado, mas sem ativar `core.hooksPath`.
- Q5: cria com nome e perfil e abre a edição preenchida pelo servidor.
- Q6: grupos abríveis, permissões marcadas e desmarcáveis; `RESOURCES_READ` fixa.
- Q7: busca por `consentId`, logo depois de criar e pela tela do cliente; sem rota nova.
- Q8: campo "Seu nome" no topo, guardado no navegador, enviado em `x-sim-autor`.
- Q9: rota nova `GET /admin/cenarios/regras/vencedora`, com `SelecaoDeRegras`; frequência não entra.
- Q10: Playwright em `front/e2e/` contra o Compose, com job próprio no CI.

### Session 2026-11-10 (usabilidade)

Marcelo travou no menu de consentimento ao digitar um documento que não era de um cliente da massa e pediu melhorias de uso: "Sim, incluir tudo", repassado pela sessão controladora. Registrado em `docs/`: [RN124](../../docs/01-requisitos/regras-de-negocio.md#rn124) (e a exceção em RN120), UI10, UI17, UI18 e UI19 em `interface.md`, e o [guia de uso](../../docs/08-front-end/guia-de-uso.md).

- Q11: cada tela tem duas ou três linhas de ajuda no topo, com link para Primeiros passos (UI17).
- Q12: o cliente é escolhido numa lista dos clientes do banco (UI19), com a rota nova `GET /admin/massa/clientes?banco=`; banco sem clientes leva à tela de lotes; 404 vira mensagem do que faltou e de como resolver.
- Q13: página Primeiros passos como página inicial, com o que já foi feito marcado (rota `GET /admin/massa/resumo`) e a tabela "tela → para que serve" (UI18), repetida no guia de uso.
- Q14: receptora padrão `XP`, configurável e idempotente, com `organisationId` fictício (RN124); o menu só pede receptora quando há mais de uma.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastro de bancos (Priority: P1)

Como Diego, quero listar, criar, ajustar, apagar e reativar bancos simulados numa tela, sem montar JSON à mão, e ver na lista quando um banco tem anomalia ligada (UC01 a UC03, UI02 a UI05, RN114).

**Why this priority**: é o primeiro CRUD completo e o modelo das outras telas (guia de front-end, regra 10). Sem banco não há massa, consentimento nem cenário.

**Independent Test**: com o simulador no ar, abrir a tela de bancos, criar o "Banco Azul" só com nome e perfil, mudar a taxa de erro e conferir o aviso de SLA e o histórico.

**Acceptance Scenarios**:

1. **Given** os bancos prontos, **When** Diego abre a lista, **Then** vê cada banco com nome, endereço, perfil e situação (UI02).
2. **Given** o Banco Instável com `revogado-com-403` ligada, **When** Diego abre a lista, **Then** a linha do banco mostra um aviso com o nome da anomalia; os bancos sem anomalia não mostram aviso (RN114).
3. **Given** a tela de novo banco, **When** Diego informa só nome e perfil e confirma, **Then** o banco é criado e os demais campos aparecem preenchidos e editáveis (UI03, RN03) **(Q5)**.
4. **Given** um nome já usado, **When** Diego confirma, **Then** a tela mostra a recusa da API sem perder o que foi digitado.
5. **Given** um banco com p95 configurado acima do SLA, **When** Diego abre o comportamento, **Then** cada classe mostra o SLA oficial e se o valor fica dentro ou fora (UI04, RN11).
6. **Given** um limite abaixo do oficial, **When** Diego salva, **Then** o valor é aceito e a tela avisa, com o oficial ao lado (UI05, RN26).
7. **Given** um banco, **When** Diego pede para apagar, **Then** a tela pede confirmação e informa se ele foi apagado ou inativado; um banco inativo pode ser reativado.
8. **Given** um banco, **When** Diego abre o histórico, **Then** vê as mudanças da mais nova para a mais antiga, com data, autor, antes e depois (RN71).
9. **Given** um banco, **When** Diego liga ou desliga uma anomalia na tela do banco, **Then** o aviso da lista acompanha a mudança (RN113, RN114).
10. **Given** um banco, **When** Diego fixa ou acelera o relógio, ou volta ao relógio real, **Then** a tela mostra a situação do relógio do banco (UC28).

---

### User Story 2 - Consentimento no painel (Priority: P1)

Como Bruno ou Carla, quero criar um consentimento por um menu em passos e aprovar ou recusar as alçadas, para ter dados prontos para os testes sem chamar a API Open Finance (UC10, UC11, UI10, UI13, UI14).

**Why this priority**: a API da 008 já existe e só falta a tela; os cenários de `consentimento-no-painel.feature` são de navegador.

**Independent Test**: com a massa pronta, criar pelo menu um consentimento da Mariana no Banco Estável, depois um da Indústria Aurora com duas alçadas, e aprovar as duas.

**Acceptance Scenarios**:

1. **Given** a cliente Mariana e o Banco Estável, **When** Bruno percorre os passos cliente, banco e receptora, permissões e prazo, e confirma, **Then** a tela mostra o consentimento em `AUTHORISED`, com os recursos (UI10, RN80).
2. **Given** a Mariana com contas e cartões no banco, **When** Bruno chega à seleção de origem, **Then** contas e cartões aparecem todos marcados, só os de produto que o banco ainda oferece (UI13, RN95, RN96).
3. **Given** a seleção de origem, **When** Bruno desmarca todas as contas e tenta confirmar, **Then** o consentimento não é criado e a tela explica o motivo (RN95).
4. **Given** um cliente sem conta no banco, **When** Bruno abre a seleção de origem, **Then** a seção de contas não aparece.
5. **Given** o pedido de crédito, investimento, câmbio ou cadastro, **Then** esses itens aparecem só como grupos, sem seleção (UI13).
6. **Given** a cliente PF Mariana, **When** Bruno abre as permissões, **Then** só aparecem grupos PF (RN84).
7. **Given** um grupo de contas incompleto, **When** Bruno confirma, **Then** o consentimento não é criado e a tela mostra o código oficial (RN83) **(Q6)**.
8. **Given** o prazo vazio, **When** Bruno confirma, **Then** o consentimento não tem data final (RN85).
9. **Given** um consentimento da Indústria Aurora com duas aprovações pendentes, **When** Bruno aprova duas vezes, **Then** a tela mostra zero pendências e os recursos em `AVAILABLE` (UI14, RN98).
10. **Given** uma aprovação pendente, **When** Bruno recusa, **Then** os recursos aparecem em `UNAVAILABLE` e os botões somem (RN98, RN99).
11. **Given** um consentimento com alçada vencida, recusada ou `REJECTED`, **When** Bruno o abre, **Then** a tela não oferece aprovar nem recusar, e mostra os estados da visão admin (RN97, RN99).
12. **Given** um `consentId`, **When** Bruno o procura, **Then** abre a tela do consentimento **(Q7)**.

---

### User Story 3 - Massa (Priority: P2)

Como Diego, quero montar a distribuição, gerar e acompanhar lotes, criar um cliente avulso e limpar o ambiente pela tela (UC05 a UC08, UI07).

**Why this priority**: hoje a massa só é criada por JSON; a tela tira esse passo de quem não é desenvolvedor.

**Independent Test**: criar uma distribuição que soma 100%, gerar um lote pequeno, acompanhar até terminar e abrir um cliente do lote.

**Acceptance Scenarios**:

1. **Given** a tela de distribuição, **When** as porcentagens de bancos ou de perfis não somam 100%, **Then** a tela mostra a soma e bloqueia salvar; produtos têm porcentagem própria, sem soma (UI07, RN56).
2. **Given** uma distribuição, **When** Diego gera um lote, **Then** vê o andamento até o fim e pode cancelar enquanto ele roda.
3. **Given** um lote terminado, **When** Diego abre os clientes, **Then** vê a lista e chega ao cliente com recursos e consentimentos.
4. **Given** os perfis de dados, **Then** a tela lista prontos e criados; os prontos não podem ser alterados nem apagados.
5. **Given** um pedido de limpeza, **When** Diego pede a prévia, **Then** vê o que será apagado e só então confirma.

---

### User Story 4 - Cenários, defeitos e anomalias (Priority: P2)

Como Bruno, quero criar, ligar e desligar regras de cenário e pacotes, e ver qual regra vence num conflito (UC21, UC22, UI06, RN15).

**Why this priority**: é como os testes repetíveis são montados; sem a tela, Bruno depende de JSON.

**Independent Test**: criar uma regra de latência no banco e uma de status no endpoint do mesmo banco, e conferir na tela qual vence.

**Acceptance Scenarios**:

1. **Given** regras ativas, **When** Bruno abre a lista, **Then** vê as regras por alcance (recurso, cliente, endpoint, instituição) com efeito, frequência, período e pacote (UI06).
2. **Given** duas regras que valem para a mesma chamada, **Then** a tela mostra qual vence, pela ordem de RN15, com as exceções de estado e defeito **(Q9)**.
3. **Given** a tela de nova regra, **When** Bruno escolhe o efeito `defeito`, **Then** só aparecem os nomes do catálogo e o campo-alvo opcional (RN111).
4. **Given** um pacote com regras, **When** Bruno tenta apagar, **Then** a tela mostra a recusa; ligar e desligar o pacote vale para todas as regras dele.

---

### User Story 5 - Marca do ambiente e faixa de simulação (Priority: P1)

Como Diego, quero ver em toda tela de qual ambiente vêm os dados e que tudo é simulado (UI01, UI16, RN72, PR03).

**Why this priority**: é invariante de toda tela e cenário `@tela` aprovado.

**Independent Test**: subir com a marca `hml`, abrir qualquer tela e conferir a marca em destaque e a faixa; subir com a marca vazia e conferir que nenhuma marca aparece.

**Acceptance Scenarios**:

1. **Given** a marca de ambiente "hml", **When** Diego abre o back-office, **Then** a marca "hml" aparece em destaque (marcacao.feature, RN72) **(Q2)**.
2. **Given** a marca vazia, **Then** nenhuma marca aparece, mas a faixa "Ambiente simulado, dados fictícios" continua (UI16).
3. **Given** marcas diferentes, **Then** cada uma tem cor própria (UI01).

### Edge Cases

- A API de administração fora do ar ou respondendo 500 (por exemplo, MongoDB fora, RN113): a tela mostra o erro e permite tentar de novo, sem tela em branco.
- Recusas com vários códigos (`{ "erros": [...] }`): todos aparecem, com texto em português para os códigos conhecidos e o próprio código para os desconhecidos.
- Duas pessoas editando o mesmo banco: vale o que a API gravar por último; a tela relê depois de salvar.
- 409 na alçada (decisão concorrente, prazo vencido entre abrir e clicar): a tela relê a visão e mostra a situação nova.
- Lista grande de clientes de lote: a tela pagina com `inicio` e `quantidade`, sem carregar tudo.
- Banco apagado enquanto a tela está aberta: 404 leva de volta à lista.
- Cliente sem nenhum produto no banco: o menu de consentimento mostra a recusa `CLIENTE_SEM_PRODUTO` antes do passo de permissões.

## Requirements *(mandatory)*

### Functional Requirements

**Geral**
- **FR-001**: O back-office MUST falar só com a API de administração, nunca com as APIs Open Finance (guia de front-end).
- **FR-002**: O back-office MUST NOT ter login nem autenticação (RN71).
- **FR-003**: Toda tela MUST mostrar a marca do ambiente em destaque, com cor por marca, e nada com a marca vazia (UI01, RN72). A marca MUST vir da configuração do ambiente do simulador, sem outra fonte (RN66) **(Q2)**.
- **FR-004**: Toda tela MUST mostrar a faixa fixa "Ambiente simulado, dados fictícios" (UI16, PR03).
- **FR-005**: As escritas MUST enviar o autor informado no cabeçalho `x-sim-autor` (RN05, RN71) **(Q8)**.
- **FR-006**: Recusas da API MUST aparecer na tela com todos os códigos devolvidos, sem perder o que foi digitado.
- **FR-007**: A tela MUST NOT recriar regra de negócio do simulador: valida só formato e faixas declaradas nos contratos (obrigatórios, números, somas de UI07, prazo de 1 a 12, alçadas de 1 a 10), e o resto vem da recusa da API (constituição, princípio I).

**Bancos**
- **FR-010**: Listar bancos com nome, endereço, perfil, situação e aviso de anomalias ligadas, com os nomes (UI02, RN114).
- **FR-011**: Criar banco pedindo nome e perfil; os demais campos MUST aparecer preenchidos com o padrão e poder ser ajustados (UI03, RN03) **(Q5)**.
- **FR-012**: Editar nome, marca, público, produtos, comportamento e limites; mostrar SLA oficial e dentro/fora por classe (UI04) e o oficial ao lado de cada limite, com aviso quando abaixo (UI05).
- **FR-013**: Apagar com confirmação, informando o resultado (`apagada` ou `inativada`); reativar banco inativo.
- **FR-014**: Mostrar o histórico do banco, do mais novo ao mais antigo.
- **FR-015**: Ligar e desligar as anomalias do catálogo no banco (RN113).
- **FR-016**: Fixar, acelerar e devolver ao real o relógio do banco (UC28).

**Consentimento**
- **FR-020**: Menu em passos: cliente; banco e receptora; permissões e seleção de origem; prazo e alçadas; confirmação (UI10).
- **FR-021**: As opções do menu MUST vir da API (`/admin/consentimentos/opcoes`): grupos do tipo do cliente, contas e cartões já marcados, agrupados sem seleção e receptoras (UI13, RN95, RN96).
- **FR-022**: Com opção de conta ou cartão, a tela MUST impedir confirmar com o grupo pedido e nenhuma origem marcada; sem opção, a seção não aparece (UI13).
- **FR-023**: Alçadas só para cliente PJ, de 1 a 10 (RN99); prazo opcional de 1 a 12 meses (RN96).
- **FR-024**: A tela do consentimento MUST mostrar estado, permissões, datas, recursos com o estado da visão admin, situação da alçada, pendências e decisões, e oferecer aprovar e recusar só quando houver pendência válida (UI14, RN97 a RN99).
- **FR-025**: Revogar o consentimento pela tela (contrato da 004) **(Q1)**.

**Massa** **(Q1)**
- **FR-030**: Listar perfis de dados; criar, alterar e apagar só os criados.
- **FR-031**: Criar, alterar e apagar distribuições, com a soma de bancos e de perfis visível e salvar bloqueado fora de 100% (UI07, RN56).
- **FR-032**: Gerar lote, acompanhar o andamento, cancelar e listar os clientes do lote em páginas.
- **FR-033**: Criar cliente avulso e abrir um cliente com recursos e consentimentos.
- **FR-034**: Limpeza com prévia e confirmação.
- **FR-035**: Listar e criar receptoras, porque o menu de consentimento exige uma.

**Cenários** **(Q1)**
- **FR-040**: Criar, editar, ligar, desligar e apagar regras de cenário com os efeitos do contrato, incluindo `defeito` com o catálogo (RN111).
- **FR-041**: Criar, ligar, desligar e apagar pacotes.
- **FR-042**: Listar regras por alcance e mostrar a vencedora em conflito (UI06, RN15) **(Q9)**.

**Construção e qualidade**
- **FR-050**: O código segue o [guia de front-end](../../docs/08-front-end/README.md): stack, camadas, Either, injeção de dependência, estado, estilo e testes, com 100% de cobertura no `core` e 80% no total.
- **FR-051**: Os portões do front (lint, tipos, testes com cobertura e build de produção) MUST rodar no CI **(Q4)**.
- **FR-052**: O back-office MUST subir no Docker Compose junto com o simulador (RNF11) **(Q3)**.
- **FR-053**: Os cenários de `consentimento-no-painel.feature` MUST ser provados no navegador (estratégia de testes) **(Q10)**.

### Key Entities

- **Instituição (visão da tela)**: endereço, nome, perfil, situação, anomalias ligadas, comportamento com SLA, limites com avisos, relógio.
- **Opções do menu de consentimento**: tipo do cliente, grupos de permissões, contas e cartões com marcação, agrupados, receptoras.
- **Consentimento (visão admin)**: estado efetivo, permissões, datas, alçada (situação, pendências, prazo), decisões, recursos com estado.
- **Distribuição, perfil de dados, lote, cliente**: como nos contratos da 003.
- **Regra de cenário e pacote**: alcance, efeito, frequência, período, semente, ligada, pacote.
- **Autor**: nome livre de quem opera a tela, sem login.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Diego cria um banco só com nome e perfil em menos de 30 segundos, sem escrever JSON.
- **SC-002**: Carla cria um consentimento PF pelo menu em menos de 1 minuto, e um PJ com duas alçadas aprovadas em menos de 2 minutos.
- **SC-003**: 100% das telas mostram a faixa de simulação, e a marca do ambiente sempre que ela não é vazia.
- **SC-004**: Toda anomalia ligada aparece na lista de bancos na próxima leitura da lista.
- **SC-005**: Todos os cenários de `consentimento-no-painel.feature` e o cenário `@tela` de `marcacao.feature` passam no navegador.
- **SC-006**: Cobertura do front de 100% no `core` e pelo menos 80% no total, travada no CI.
- **SC-007**: Nenhuma recusa da API deixa a tela em branco ou perde o que foi digitado.

## Assumptions

- As APIs de administração das specs 002 a 009 já existem e não mudam de contrato, exceto o que as respostas de [perguntas.md](perguntas.md) pedirem (por exemplo, a leitura da marca e a vencedora de conflito).
- Uma pessoa por vez opera a tela; não há edição concorrente com bloqueio.
- Datas aparecem no fuso do navegador, com o valor da API disponível ao passar o mouse.
- Textos só em `pt-BR` (guia de front-end).
- Fora do escopo: login, CRUD de usuário, telas das APIs Open Finance, modo FAPI e os itens adiados.
- Os invariantes das specs 007 a 009 (cache de regras ≤5 s, chave `v2.`, corte da RN37, cota da RN27, visão admin da RN97, anomalias da RN113 e RN114) não mudam: a tela só lê e escreve pela administração.
