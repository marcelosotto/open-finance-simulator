---

description: "Lista de tarefas da spec 001 — Fundação e catálogo"
---

# Tasks: Fundação e catálogo

**Input**: documentos de `specs/001-fundacao-e-catalogo/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: obrigatórios. O princípio IX da constituição exige testes primeiro: em cada fase, as
tarefas de teste vêm antes e MUST falhar antes da implementação.

**Organization**: tarefas agrupadas por história da spec (US1 a US5).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: história da spec (US1 a US5)

## Convenções de caminho

Estrutura do [plan.md](plan.md#source-code-repository-root):

- `src/Simulador.Host/`, `src/Simulador.Comum/`
- `src/Modulos/Simulador.Catalogo/` e `src/Modulos/Simulador.Marcacao/`, cada um com `Contratos/` (público), `Dominio/` e demais pastas (`internal`)
- `tools/Simulador.Ferramentas/`
- `tests/Simulador.Testes.Unidade/`, `tests/Simulador.Testes.Integracao/`, `tests/Simulador.Testes.Aceitacao/`

Regras para todas as tarefas:

- Nenhum teste usa `DateTime.Now`/`UtcNow` ou `new Random()` sem semente; use `FakeTimeProvider` e `IGeradorAleatorio` com semente fixa.
- Nenhum caminho oficial (`/open-banking/...`) aparece escrito em `src/` (PR06).
- Tipos de módulo fora de `Contratos` são `internal`; os projetos de teste recebem `InternalsVisibleTo`.

---

## Phase 1: Setup

**Purpose**: solução, projetos e configuração de build

- [X] T001 Criar `global.json` fixando o SDK do .NET 10 (`rollForward: latestFeature`)
- [X] T002 Criar `Directory.Build.props` com `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild true` e `LangVersion latest`
- [X] T003 Criar `Directory.Packages.props` (gerenciamento central) com `Microsoft.OpenApi` e o leitor YAML (`Microsoft.OpenApi.YamlReader`), `JsonSchema.Net`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Instrumentation.AspNetCore`, `xunit.v3`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Reqnroll.xunit.v3`, `coverlet.collector`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing`, com versões estáveis fixas
- [X] T004 [P] Criar `.editorconfig` com estilo C# (namespaces por arquivo, `var` quando óbvio, chaves obrigatórias, `dotnet_diagnostic` dos analisadores em `warning`)
- [X] T005 [P] Atualizar `.gitignore` com `bin/`, `obj/`, `TestResults/`, `.specs/`, `*.user` e `coverage*/`
- [X] T006 Criar os projetos `src/Simulador.Comum/Simulador.Comum.csproj` (classlib), `src/Modulos/Simulador.Catalogo/Simulador.Catalogo.csproj` (classlib com `FrameworkReference Microsoft.AspNetCore.App`), `src/Modulos/Simulador.Marcacao/Simulador.Marcacao.csproj` (idem), `src/Simulador.Host/Simulador.Host.csproj` (`Microsoft.NET.Sdk.Web`) e `tools/Simulador.Ferramentas/Simulador.Ferramentas.csproj` (console), com as referências: Host → Comum, Catalogo, Marcacao; Catalogo → Comum; Marcacao → Comum; Ferramentas → Catalogo
- [X] T007 Criar os projetos `tests/Simulador.Testes.Unidade/`, `tests/Simulador.Testes.Integracao/` (referencia Host, com `Microsoft.AspNetCore.Mvc.Testing`) e `tests/Simulador.Testes.Aceitacao/` (Reqnroll com `Reqnroll.xunit.v3`, referencia Host), todos com `xunit.v3` e `coverlet.collector`
- [X] T008 Adicionar `InternalsVisibleTo` para os três projetos de teste em `src/Simulador.Comum/Simulador.Comum.csproj`, `src/Modulos/Simulador.Catalogo/Simulador.Catalogo.csproj`, `src/Modulos/Simulador.Marcacao/Simulador.Marcacao.csproj` e `tools/Simulador.Ferramentas/Simulador.Ferramentas.csproj`
- [X] T009 Criar `Simulador.slnx` com todos os projetos e confirmar que `dotnet build` passa sem aviso

**Checkpoint**: solução compila vazia, sem aviso.

---

## Phase 2: Foundational (bloqueia todas as histórias)

**Purpose**: relógio, semente, envelope de erro, trava das specs e host com duas portas

### Testes primeiro

- [X] T010 [P] Teste de `GeradorAleatorioComSemente` em `tests/Simulador.Testes.Unidade/Comum/GeradorAleatorioComSementeTests.cs`: mesma semente gera a mesma sequência; sementes diferentes geram sequências diferentes; `ProximoTexto(n)` só devolve `[a-z0-9]`
- [X] T011 [P] Teste do envelope de erro em `tests/Simulador.Testes.Unidade/Comum/EnvelopeDeErroTests.cs`: serializa `{"errors":[{"code","title","detail"}],"meta":{"requestDateTime"}}` com `requestDateTime` do `FakeTimeProvider` em UTC ISO 8601 (formato de [respostas-padrao.md](contracts/respostas-padrao.md))
- [X] T012 [P] Teste de leitura da trava em `tests/Simulador.Testes.Unidade/Catalogo/TravaDasSpecsTests.cs`: aceita o exemplo válido; recusa `commit` fora de `^[0-9a-f]{40}$`, `sha256` fora de `^[0-9a-f]{64}$`, `repositorio` fora de `^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$`, lista vazia e `id` repetido ([specs-lock.schema.json](contracts/specs-lock.schema.json))
- [X] T013 [P] Teste de `ConferenciaDeHash` em `tests/Simulador.Testes.Unidade/Catalogo/ConferenciaDeHashTests.cs`: passa quando o SHA-256 dos bytes bate com a trava e com o `catalog.json`; falha nomeando o arquivo quando diverge da trava, diverge do catálogo, falta arquivo ou o contrato não existe no catálogo (FR-002)
- [X] T014 [P] Teste do host em `tests/Simulador.Testes.Integracao/Host/PortasTests.cs`: `GET /saude/vivo` responde 200 na porta de administração e 404 na porta Open Finance

### Implementação

- [X] T015 [P] Criar `IGeradorAleatorio` e `GeradorAleatorioComSemente` (sobre `Random(semente)`) em `src/Simulador.Comum/Aleatorio/`
- [X] T016 [P] Criar `EnvelopeDeErro` (modelo `ResponseError` e fábrica com `TimeProvider`) em `src/Simulador.Comum/Erros/EnvelopeDeErro.cs`
- [X] T017 [P] Criar `TravaDasSpecs` e `ContratoTravado` (campos `repositorio`, `commit`, `catalogo` com padrão `contracts/expansion/catalog.json`, `contratos[] { id, caminhoLocal, sha256 }`) com validação em `src/Modulos/Simulador.Catalogo/Dominio/TravaDasSpecs.cs` e a leitura do JSON em `src/Modulos/Simulador.Catalogo/Contratos/LeitorDeTrava.cs`
- [X] T018 Criar `ConferenciaDeHash` e `FalhaDeConferencia` (arquivo e motivo) em `src/Modulos/Simulador.Catalogo/Dominio/ConferenciaDeHash.cs`, expondo a operação por `src/Modulos/Simulador.Catalogo/Contratos/IConferenciaDeSpecs.cs`
- [X] T019 Criar `specs.lock.json` na raiz com `repositorio: marcelosotto/open-finance-specs`, `commit: 7bdb9c1a6c4c265d7aabcdbc662f94c61718135e` e os 15 contratos de [research.md R02](research.md#r02--quais-contratos-entram-no-catálogo), copiando `id`, `localPath` (como `caminhoLocal`) e `sha256` do `contracts/expansion/catalog.json` desse commit
- [X] T020 Criar o comando `specs baixar` em `tools/Simulador.Ferramentas/Comandos/BaixarSpecs.cs`: baixa `https://codeload.github.com/{repositorio}/tar.gz/{commit}` (com `Authorization` só se `SPECS_TOKEN` existir), extrai apenas o `catalog.json` e os `caminhoLocal` travados para `.specs/{commit}/`, preservando os bytes ([ferramenta-specs.md](contracts/ferramenta-specs.md))
- [X] T021 Criar o comando `specs verificar` em `tools/Simulador.Ferramentas/Comandos/VerificarSpecs.cs`, usando `IConferenciaDeSpecs`; código de saída 1 com uma linha por falha
- [X] T022 Criar o roteador de comandos em `tools/Simulador.Ferramentas/Program.cs` (`specs baixar|verificar|diferencas`, `cobertura conferir`), com ajuda e código de saída
- [X] T023 Criar `src/Simulador.Host/Program.cs`: Kestrel em 8080 (Open Finance) e 8081 (administração) por configuração `Simulador:PortaOpenFinance` e `Simulador:PortaAdministracao`; `TimeProvider.System` e `IGeradorAleatorio` registrados; `Simulador:Semente` opcional; `/saude/vivo` restrito à porta de administração (`RequireHost("*:8081")` ou filtro por `LocalPort`)
- [X] T024 Configurar OpenTelemetry (logs, métricas e traces, exportador OTLP por `OTEL_EXPORTER_OTLP_ENDPOINT`) em `src/Simulador.Host/Observabilidade.cs`, sem registrar corpo, cabeçalhos ou parâmetros das chamadas (RN18)
- [X] T025 Criar `FabricaDoSimulador` (`WebApplicationFactory<Program>`) em `tests/Simulador.Testes.Integracao/Infra/FabricaDoSimulador.cs` com `FakeTimeProvider`, semente fixa, `Simulador:MarcaDoAmbiente` configurável, pasta das specs apontando para `.specs/{commit}` e um cliente por porta; e o mesmo para a aceitação em `tests/Simulador.Testes.Aceitacao/Infra/FabricaDoSimulador.cs` (reaproveitando por link de arquivo)

**Checkpoint**: `specs baixar` e `specs verificar` passam no commit travado; o host sobe com as duas portas; T010 a T014 verdes.

---

## Phase 3: User Story 1 — Catálogo das APIs a partir das specs fixadas (P1) 🎯 MVP

**Goal**: toda operação das 15 specs travadas publicada sob `/{banco}/open-banking/{api}/{versão}/…`, sem rota escrita à mão.

**Independent Test**: subir o simulador com o commit inicial e conferir que toda operação responde 501 no caminho oficial, que um caminho fora das specs responde 404 e que `/admin/catalogo` lista as operações.

### Testes primeiro

- [X] T026 [P] [US1] Teste de `LeitorDeSpecs` em `tests/Simulador.Testes.Unidade/Catalogo/LeitorDeSpecsTests.cs`: para `accounts-2.5.1`, extrai `api = accounts` e `versao = v2` de `servers[0].url`, lista as operações com `metodo`, `caminho`, `operationId`, parâmetros, cabeçalhos exigidos e schema por código de resposta; o número de operações por contrato bate com o `catalog.json`
- [X] T027 [P] [US1] Teste de mesmo caminho em contratos diferentes em `tests/Simulador.Testes.Unidade/Catalogo/CatalogoTests.cs`: `GET /accounts` de `accounts-2.5.1` e de `credit-cards-accounts-2.4.0` geram `caminhoPublicado` diferentes; `operationId` é único no catálogo; a chave é `(contrato, metodo, caminho)`
- [X] T028 [P] [US1] Teste de rotas geradas em `tests/Simulador.Testes.Integracao/Catalogo/RotasGeradasTests.cs`: teoria sobre todas as operações do catálogo; cada uma, chamada em `/banco-teste{caminhoPublicado}` com parâmetros de caminho preenchidos, responde **501** com `code = NAO_IMPLEMENTADO` ([respostas-padrao.md](contracts/respostas-padrao.md))
- [X] T029 [P] [US1] Teste de caminho desconhecido em `tests/Simulador.Testes.Integracao/Catalogo/CaminhoDesconhecidoTests.cs`: caminho fora do catálogo e método errado em caminho existente respondem **404** com `code = NAO_ENCONTRADO`; `{banco}` fora de `^[a-z0-9-]{1,64}$` responde 404
- [X] T030 [P] [US1] Teste da administração em `tests/Simulador.Testes.Integracao/Catalogo/AdminCatalogoTests.cs`: `GET /admin/catalogo` na porta 8081 traz `commit` e uma entrada por operação com `contrato`, `api`, `versao`, `metodo`, `caminho`, `caminhoPublicado`, `operationId` e `implementada` ([admin-api.md](contracts/admin-api.md)); não existe na porta 8080; `/saude/pronto` dá 200 com specs carregadas e 503 com pasta de specs vazia ou hash divergente
- [X] T031 [P] [US1] Teste "nenhuma rota escrita à mão" em `tests/Simulador.Testes.Unidade/Arquitetura/SemRotaOficialNoCodigoTests.cs`: varre os `.cs` de `src/` e falha se algum contiver o literal `/open-banking`

### Implementação

- [X] T032 [P] [US1] Criar `OperacaoDoCatalogo` (campos de [data-model.md](data-model.md#operacaodocatalogo)) e `ICatalogo` em `src/Modulos/Simulador.Catalogo/Contratos/`
- [X] T033 [US1] Criar `LeitorDeSpecs` com `Microsoft.OpenApi` (leitor YAML) em `src/Modulos/Simulador.Catalogo/Leitura/LeitorDeSpecs.cs`: lê cada `caminhoLocal` travado da pasta `Simulador:PastaDasSpecs`, extrai `api` e `versao` do caminho de `servers[0].url` e monta as `OperacaoDoCatalogo`
- [X] T034 [US1] Criar `Catalogo` (implementa `ICatalogo`) em `src/Modulos/Simulador.Catalogo/Dominio/Catalogo.cs`: carrega na subida, roda a `ConferenciaDeHash` antes de ler e guarda o estado de prontidão; falha de conferência deixa o catálogo vazio e não pronto
- [X] T035 [P] [US1] Criar `IResolvedorDeInstituicao` em `src/Modulos/Simulador.Catalogo/Contratos/IResolvedorDeInstituicao.cs` e `ResolvedorProvisorio` (aceita `^[a-z0-9-]{1,64}$`) em `src/Modulos/Simulador.Catalogo/Rotas/ResolvedorProvisorio.cs`
- [X] T036 [P] [US1] Criar `IManipuladorDeOperacao` (chave `operationId`) em `src/Modulos/Simulador.Catalogo/Contratos/IManipuladorDeOperacao.cs`
- [X] T037 [US1] Criar `PublicadorDeRotas` em `src/Modulos/Simulador.Catalogo/Rotas/PublicadorDeRotas.cs`: para cada operação, `MapMethods("/{banco}" + caminho base + caminho, [metodo], Despachante)` só na porta Open Finance; o despachante resolve a instituição, procura o manipulador pelo `operationId` e responde 501 com `EnvelopeDeErro` se não houver
- [X] T038 [US1] Criar o fallback 404 com `EnvelopeDeErro` (`NAO_ENCONTRADO`) na porta Open Finance em `src/Modulos/Simulador.Catalogo/Rotas/RespostaNaoEncontrada.cs`
- [X] T039 [US1] Criar `GET /admin/catalogo` e `GET /saude/pronto` na porta de administração em `src/Modulos/Simulador.Catalogo/Rotas/AdminCatalogo.cs` (`implementada` = existe manipulador registrado)
- [X] T040 [US1] Expor `AddCatalogo()`/`UseCatalogo()` em `src/Modulos/Simulador.Catalogo/Contratos/ExtensoesDoCatalogo.cs` e chamá-los em `src/Simulador.Host/Program.cs`

**Checkpoint**: T026 a T031 verdes. A US1 é demonstrável sozinha (quickstart, passo 3, sem as verificações de marca).

---

## Phase 4: User Story 2 — Atualizar a versão das specs com conferência no CI (P1)

**Goal**: num PR que troca o commit, o CI confere os hashes e lista as operações novas, alteradas e removidas.

**Independent Test**: rodar `specs diferencas` entre dois locks e conferir a lista; adulterar um hash e conferir que `specs verificar` falha nomeando o arquivo.

### Testes primeiro

- [X] T041 [P] [US2] Teste de `DiferencaEntreVersoes` em `tests/Simulador.Testes.Unidade/Catalogo/DiferencaEntreVersoesTests.cs`: com dois conjuntos de operações montados em memória, detecta novas, removidas e alteradas; alteração de versão do contrato, de parâmetros, de cabeçalhos exigidos e de schema de resposta aparece em `mudancas[]`; conjuntos iguais dão lista vazia
- [X] T042 [P] [US2] Teste do comando em `tests/Simulador.Testes.Unidade/Ferramentas/DiferencasComandoTests.cs`: `specs diferencas --resumo arquivo.md` escreve o Markdown no formato de [ferramenta-specs.md](contracts/ferramenta-specs.md) (título com os dois commits curtos e as seções Novas, Alteradas e Removidas com contagem)
- [X] T043 [P] [US2] Teste de ponta a ponta da verificação em `tests/Simulador.Testes.Unidade/Ferramentas/VerificarComandoTests.cs`: com uma cópia temporária de `.specs/{commit}` e um byte alterado num YAML, `specs verificar` devolve 1 e a saída cita o `caminhoLocal`

### Implementação

- [X] T044 [US2] Criar `DiferencaEntreVersoes` em `src/Modulos/Simulador.Catalogo/Dominio/DiferencaEntreVersoes.cs` (campos de [data-model.md](data-model.md#diferencaentreversoes)); a comparação de schema usa a serialização canônica do schema da operação
- [X] T045 [US2] Criar o comando `specs diferencas --base <lock> --novo <lock> [--resumo <arquivo>]` em `tools/Simulador.Ferramentas/Comandos/DiferencasSpecs.cs`: baixa os dois commits (reaproveitando `BaixarSpecs`), lê os dois catálogos com `LeitorDeSpecs` e escreve o resultado no console e, com `--resumo`, no arquivo

**Checkpoint**: T041 a T043 verdes. O passo de CI que usa o comando entra em T079.

---

## Phase 5: User Story 3 — Tabela oficial de frequência e limites por endpoint (P1)

**Goal**: cada operação do catálogo com classe, SLA, TPM e limite mensal iguais aos da tabela oficial em `docs/`.

**Independent Test**: rodar o teste da tabela; alterar um valor de `config/tabela-de-endpoints.json` e conferir que ele falha nomeando seção, endpoint e campo.

### Testes primeiro

- [X] T046 [P] [US3] Teste de `LeitorDaTabelaOficial` em `tests/Simulador.Testes.Unidade/Catalogo/LeitorDaTabelaOficialTests.cs`: lê `docs/01-requisitos/tabela-de-endpoints.md`, separa as seções (`Consentimento`, `Recursos` e as `###` das APIs de dados), converte `Alta`/`Média-alta`/`Média`/`Baixa`, `1.500` → 1500, `Faixa QCA` → `faixa-qca`, e lê a seção `Operações fora da tabela` quando existir
- [X] T047 [P] [US3] Teste da tabela oficial em `tests/Simulador.Testes.Integracao/Tabela/TabelaOficialTests.cs`: cada linha do Markdown tem entrada igual em `config/tabela-de-endpoints.json` (casada por `secao`, `metodo` e `caminho`) e vice-versa; divergência falha com seção, endpoint, campo, esperado e encontrado (FR-014); `timeoutSegundos = 15` e `tps = 300`
- [X] T048 [P] [US3] Teste catálogo × tabela em `tests/Simulador.Testes.Integracao/Tabela/CatalogoTabelaTests.cs`: toda operação do catálogo tem entrada no arquivo ou está na seção de exclusões de `docs/`; toda entrada do arquivo tem operação no catálogo; a falha nomeia contrato e operação (FR-015)
- [X] T049 [P] [US3] Teste de validação do arquivo em `tests/Simulador.Testes.Unidade/Catalogo/TabelaDeEndpointsTests.cs`: recusa `frequencia` fora de `alta`, `media-alta`, `media`, `baixa`; `slaMs` fora de 1500, 2000, 4000; `tpm` que não seja inteiro ≥ 1, `faixa-qca` ou nulo; `limiteMensal` que não seja inteiro ≥ 1 ou nulo ([tabela-de-endpoints.schema.json](contracts/tabela-de-endpoints.schema.json))
- [X] T050 [P] [US3] Teste de operação excluída em `tests/Simulador.Testes.Integracao/Tabela/OperacaoExcluidaTests.cs`: operação listada em `Operações fora da tabela` não é publicada (responde 404) e não aparece em `/admin/catalogo`

### Implementação

- [X] T051 [P] [US3] Criar `ClasseDoEndpoint` e `TabelaDeEndpoints` (leitura e validação de `config/tabela-de-endpoints.json`) em `src/Modulos/Simulador.Catalogo/Dominio/TabelaDeEndpoints.cs`, com `ClasseDoEndpoint` exposta em `src/Modulos/Simulador.Catalogo/Contratos/ClasseDoEndpoint.cs`
- [X] T052 [US3] Criar `config/tabela-de-endpoints.json` com `fonte`, `timeoutSegundos: 15`, `tps: 300` e uma entrada por linha de `docs/01-requisitos/tabela-de-endpoints.md` (`contrato` pelo mapeamento de [research.md R02](research.md#r02--quais-contratos-entram-no-catálogo); `tpm: null` e `limiteMensal: null` em Consentimento e Recursos)
- [X] T053 [US3] Criar `LeitorDaTabelaOficial` em `tests/Simulador.Testes.Integracao/Tabela/LeitorDaTabelaOficial.cs` (código só de teste; o Markdown é a fonte da verdade)
- [X] T054 [US3] Rodar T048. Para cada operação do catálogo sem linha na tabela, **parar e pedir decisão a Marcelo**: incluir a linha em `docs/01-requisitos/tabela-de-endpoints.md` com fonte, ou registrá-la na seção nova `## Operações fora da tabela` (contrato, método, caminho e motivo). Nenhuma exclusão sem aprovação (princípio I)
- [X] T055 [US3] Ligar a tabela ao catálogo em `src/Modulos/Simulador.Catalogo/Dominio/Catalogo.cs`: cada operação recebe sua `ClasseDoEndpoint`; operações da seção de exclusões não são publicadas; `/admin/catalogo` passa a trazer `classe` ([admin-api.md](contracts/admin-api.md)); o arquivo da tabela vai para a saída do Host

**Checkpoint**: T046 a T050 verdes.

---

## Phase 6: User Story 4 — Marcação de ambiente e natureza (P1)

**Goal**: marca no cabeçalho, no prefixo dos IDs e no texto, com a marca do ambiente vinda da configuração e vazia em produção.

**Independent Test**: rodar os cenários de `marcacao.feature` (exceto `@tela`) com a marca `hml`, `dev`, `stage` e vazia.

### Testes primeiro

- [X] T056 [P] [US4] Teste de `MarcaDoAmbiente` em `tests/Simulador.Testes.Unidade/Marcacao/MarcaDoAmbienteTests.cs`: `hml` → `textual = "[HML] "`; `stage` → `"[STAGE] "`; vazio ou só espaços → `vazia = true` e `textual = ""`; nenhum valor é recusado
- [X] T057 [P] [US4] Teste de `GeradorDeIdentificador` em `tests/Simulador.Testes.Unidade/Marcacao/GeradorDeIdentificadorTests.cs`: com `hml` gera `hml-p-…` e `hml-v-…`; com marca vazia gera `p-…` e `v-…`; todo ID casa com `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$`; com marca de 95 caracteres o ID continua com até 100 caracteres e a marca inteira (só a parte aleatória encolhe); mesma semente gera os mesmos IDs
- [X] T058 [P] [US4] Teste do `consentId` em `tests/Simulador.Testes.Unidade/Marcacao/GeradorDeConsentIdTests.cs`: com `hml` gera `urn:hml:p-…` e `urn:hml:v-…`; com marca vazia gera `urn:p:…` e `urn:v:…`; todos casam com o padrão de `consentId` lido da spec `consents-3.3.1` travada
- [X] T059 [P] [US4] Teste de `MarcadorDeTexto` em `tests/Simulador.Testes.Unidade/Marcacao/MarcadorDeTextoTests.cs`: `PIX recebido` com `hml` vira `[HML] PIX recebido`; com `maxLength` menor que o total, o fim do texto original é cortado e a marca fica inteira; com marca vazia o texto não muda
- [X] T060 [P] [US4] Teste de `AgregadorDeNatureza` e `LeitorDeMarca` em `tests/Simulador.Testes.Unidade/Marcacao/NaturezaTests.cs`: todos persistidos → `persistido`; todos virtuais → `virtual`; ambos → `misto`; nenhum → sem natureza; `LeitorDeMarca` extrai marca e natureza de `dev-p-…`, `p-…`, `urn:hml:v-…` e `urn:v:…` sem usar a configuração atual (RN64)
- [X] T061 [P] [US4] Teste da varredura de marcação em `tests/Simulador.Testes.Integracao/Marcacao/VarreduraDeMarcacaoTests.cs`: com marca `hml`, toda operação do catálogo e o 404 trazem `x-sim-environment: hml` e não trazem `x-sim-data-nature`; com marca vazia, nenhuma resposta traz `x-sim-environment` (FR-016, FR-022)
- [X] T062 [P] [US4] Teste do adaptador de schema em `tests/Simulador.Testes.Integracao/Contrato/AdaptadorDeSchemaTests.cs`: `nullable: true` aceita `null`; `example` é ignorado; `$ref` para `components/schemas` é resolvido
- [X] T063 [P] [US4] Teste RN66 em `tests/Simulador.Testes.Unidade/Marcacao/MarcaSemDesligamentoTests.cs`: nenhuma rota de administração, nem tipo público de configuração fora de `Simulador:MarcaDoAmbiente`, tem propriedade ou parâmetro que mude ou desligue a marca
- [X] T064 [US4] Ligar `docs/01-requisitos/gherkin/marcacao.feature` ao projeto de aceitação por link (`<None Include="..\..\docs\01-requisitos\gherkin\marcacao.feature" Link="Features\marcacao.feature" />` com o item Reqnroll) em `tests/Simulador.Testes.Aceitacao/Simulador.Testes.Aceitacao.csproj` e excluir a etiqueta `@tela` em `tests/Simulador.Testes.Aceitacao/reqnroll.json` ou por filtro de categoria no `dotnet test`
- [X] T065 [US4] Criar o manipulador de teste da operação de transações de contas (`GET /accounts/{accountId}/transactions` de `accounts-2.5.1`) em `tests/Simulador.Testes.Aceitacao/Infra/ManipuladorDeTransacoesDeTeste.cs`: devolve uma lista com itens persistidos e/ou virtuais montados com `IMarcador`, conforme o passo do cenário, e registra a natureza de cada item
- [X] T066 [US4] Criar os passos em `tests/Simulador.Testes.Aceitacao/Passos/MarcacaoPassos.cs` para todos os cenários não `@tela` de `marcacao.feature`, incluindo "A marca não muda o contrato" (valida com o `ValidadorDeContrato` contra o schema de `accounts-2.5.1`) e "A marca nunca muda" (cria com a marca `dev`, recria o host com `hml` e relê). Confirmar que todos falham antes da implementação

### Implementação

- [X] T067 [P] [US4] Criar `MarcaDoAmbiente`, `Natureza` (`persistido`/`p`, `virtual`/`v`), `NaturezaDaResposta` e `IMarcador` em `src/Modulos/Simulador.Marcacao/Contratos/`
- [X] T068 [P] [US4] Criar `GeradorDeIdentificador` (IDs de recurso e `consentId`) em `src/Modulos/Simulador.Marcacao/Dominio/GeradorDeIdentificador.cs`, usando `IGeradorAleatorio`; ID até 100 caracteres no padrão `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$`; corta-se só a parte aleatória
- [X] T069 [P] [US4] Criar `MarcadorDeTexto` (prefixo `[{MARCA}] `, corte no fim do texto original até o `maxLength`) em `src/Modulos/Simulador.Marcacao/Dominio/MarcadorDeTexto.cs`
- [X] T070 [P] [US4] Criar `AgregadorDeNatureza` e `LeitorDeMarca` em `src/Modulos/Simulador.Marcacao/Dominio/`
- [X] T071 [US4] Criar o filtro de cabeçalhos em `src/Modulos/Simulador.Marcacao/Http/FiltroDeMarcacao.cs`: aplicado à porta Open Finance (rotas do catálogo, 501 e 404); envia `x-sim-environment` se a marca não for vazia; envia `x-sim-data-nature` a partir das naturezas registradas no `HttpContext` pelo manipulador (`IRegistroDeNatureza` em `Contratos/`), sem cabeçalho quando não há item
- [X] T072 [US4] Criar `AddMarcacao()` lendo `Simulador:MarcaDoAmbiente` uma vez na subida em `src/Modulos/Simulador.Marcacao/Contratos/ExtensoesDaMarcacao.cs` e registrar no `src/Simulador.Host/Program.cs`
- [X] T073 [US4] Criar `ValidadorDeContrato` e o adaptador OpenAPI 3.0 → JSON Schema (`nullable`, `example`, `$ref`) com `JsonSchema.Net` em `tests/Simulador.Testes.Integracao/Contrato/ValidadorDeContrato.cs`, compartilhado com a aceitação por link de arquivo

**Checkpoint**: T056 a T066 verdes; quickstart, passos 3 e 4, com cabeçalhos.

---

## Phase 7: User Story 5 — Fundação: módulos, execução local e portões de CI (P2)

**Goal**: o ambiente sobe com um comando, e o CI bloqueia o merge em qualquer portão.

**Independent Test**: subir com `docker compose up --build` numa máquina limpa; abrir um PR que derruba a cobertura do domínio e conferir que o CI bloqueia.

### Testes primeiro

- [X] T074 [P] [US5] Teste de arquitetura em `tests/Simulador.Testes.Unidade/Arquitetura/FronteirasDosModulosTests.cs`: por reflexão, nenhum tipo público de `Simulador.Catalogo` ou `Simulador.Marcacao` fora do namespace `*.Contratos`; nenhum módulo referencia outro módulo (só `Simulador.Comum`)
- [X] T075 [P] [US5] Teste do comando `cobertura conferir` em `tests/Simulador.Testes.Unidade/Ferramentas/CoberturaComandoTests.cs`: com relatórios Cobertura de exemplo, passa com domínio 100% e total ≥ 80%; falha com uma linha de `*.Dominio.*` sem cobertura (nomeando classe e linha) e com total de 79,9%

### Implementação

- [X] T076 [US5] Criar o comando `cobertura conferir --relatorios <glob> --dominio 100 --total 80 --filtro-dominio "*.Dominio.*"` em `tools/Simulador.Ferramentas/Comandos/ConferirCobertura.cs` (linhas e ramos; soma de todos os relatórios)
- [X] T077 [US5] Criar `Dockerfile` multiestágio: estágio de build roda `specs baixar` e `specs verificar` (com `SPECS_TOKEN` como segredo de build opcional), publica o Host e copia `.specs/{commit}` e `config/` para a imagem final (`mcr.microsoft.com/dotnet/aspnet:10.0`), com `Simulador__PastaDasSpecs` apontando para a pasta copiada
- [X] T078 [US5] Criar `docker-compose.yml` com o serviço `simulador` (`8080:8080`, `127.0.0.1:8081:8081`, `Simulador__MarcaDoAmbiente=${Simulador__MarcaDoAmbiente-hml}`, `OTEL_EXPORTER_OTLP_ENDPOINT`) e o serviço `otel-collector` com `deploy/otel-collector.yaml` (exportador `debug`)
- [X] T079 [US5] Criar `.github/workflows/ci.yml` em `push` e `pull_request`: SDK do `global.json`; `dotnet format --verify-no-changes`; `dotnet build -warnaserror`; `specs baixar` e `specs verificar`; `dotnet test --collect:"XPlat Code Coverage"` (excluindo `@tela`); `cobertura conferir`; em PR que altera `specs.lock.json`, `git show origin/${{ github.base_ref }}:specs.lock.json > base.lock.json` e `specs diferencas --base base.lock.json --novo specs.lock.json --resumo "$GITHUB_STEP_SUMMARY"`; `docker build` da imagem

**Checkpoint**: T074 e T075 verdes; o workflow passa num PR de teste.

---

## Phase 8: Polish & Cross-Cutting

- [X] T080 [P] Atualizar `README.md`: situação (código iniciado), como rodar (`docker compose up --build`), comandos da ferramenta e a variável `Simulador__MarcaDoAmbiente`
- [X] T081 [P] Conferir que `docs/02-arquitetura/c4/C3-componentes.md` e `docs/06-testes/estrategia-de-testes.md` continuam fiéis ao que foi construído; divergência vira pergunta a Marcelo, não ajuste silencioso
- [X] T082 Rodar o [quickstart.md](quickstart.md) inteiro, incluindo as provas de falha (hash adulterado e frequência trocada) e a subida sem rede
- [X] T083 Rodar `cobertura conferir` e completar testes até 100% em `*.Dominio` e 80% no total
- [X] T084 Revisar a conformidade com a constituição (princípios I a X) e registrar no PR

---

## Dependencies & Execution Order

### Fases

- **Setup (1)** → **Foundational (2)** → histórias.
- **US1 (3)** depende só da fase 2.
- **US2 (4)** depende da fase 2 e do `LeitorDeSpecs` (T033) da US1.
- **US3 (5)** depende da US1 (catálogo carregado).
- **US4 (6)** depende da fase 2; os cabeçalhos no 501/404 e a varredura (T061) dependem da US1.
- **US5 (7)**: T074 e T075 podem começar depois da fase 2; T077 a T079 dependem de US1 a US4 para o CI ficar verde.
- **Polish (8)** depois de tudo.

### Dentro de cada história

Testes (falhando) → tipos de `Contratos` → `Dominio` → `Rotas`/`Http` → ligação no Host.

### Ordem sugerida

```text
Fase 1 → Fase 2 → US1 → (US2 ∥ US3 ∥ US4) → US5 → Polish
```

## Parallel Example

### Fase 2 (testes)

```text
T010 GeradorAleatorioComSementeTests ∥ T011 EnvelopeDeErroTests ∥ T012 TravaDasSpecsTests ∥ T013 ConferenciaDeHashTests ∥ T014 PortasTests
```

### US4 (testes de unidade)

```text
T056 MarcaDoAmbiente ∥ T057 GeradorDeIdentificador ∥ T058 consentId ∥ T059 MarcadorDeTexto ∥ T060 Natureza ∥ T063 RN66
```

### Entre histórias, depois da US1

```text
Pessoa A: US2 (T041–T045)
Pessoa B: US3 (T046–T055)
Pessoa C: US4 (T056–T073)
```

## Implementation Strategy

### MVP (só a US1)

1. Fases 1 e 2.
2. US1: catálogo e rotas geradas com 501 e 404.
3. **Parar e validar:** quickstart, passo 3 (sem cabeçalhos de marca).

### Entrega incremental

1. MVP (US1).
2. US3 (tabela) e US4 (marcação): o catálogo fica completo e marcado.
3. US2 (troca de versão).
4. US5 (Docker e CI): a partir daqui, todo PR passa pelos portões.

### Pontos de parada obrigatórios

- **T054:** operação fora da tabela exige decisão de Marcelo em `docs/`.
- **Antes de cada commit:** mostrar as alterações a Marcelo e aguardar aprovação (constituição).
