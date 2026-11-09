# Tasks: Consentimentos e recursos

**Input**: `specs/004-consentimentos-e-recursos/` (plan, spec, research, data-model, contracts)
**Tests**: obrigatórios (princípio IX): testes antes da implementação. Cobertura de 100% em `*.Dominio.*`.

## Phase 1: Setup

- [x] T001 Criar o projeto em `src/Modulos/Simulador.Consentimentos/Simulador.Consentimentos.csproj`, incluir em `Simulador.slnx` e em `src/Simulador.Host/Simulador.Host.csproj`
- [x] T002 Incluir o módulo em `tests/Simulador.Testes.Unidade/Arquitetura/FronteirasDosModulosTests.cs`

## Phase 2: Foundational

- [x] T003 Teste de leitura de escopos e permissões em `tests/Simulador.Testes.Unidade/Catalogo/LeitorDeSpecsTests.cs`
- [x] T004 Ler escopos e permissões em `src/Modulos/Simulador.Catalogo/Leitura/LeitorDeSpecs.cs` e `Contratos/OperacaoDoCatalogo.cs`
- [x] T005 Criar `IPortaoDeOperacao` e chamá-lo em `src/Modulos/Simulador.Catalogo/Rotas/Despachante.cs`
- [x] T006 Campos novos em `Consentimento` e `Ajuste` e porta `IEscritaDeMassa` em `src/Modulos/Simulador.Massa/Contratos/Modelos.cs`
- [x] T007 Implementar a escrita e os ajustes em `src/Modulos/Simulador.Massa/Aplicacao/Servicos.cs` e `Persistencia/RepositorioMongo.cs`, e em `tests/ComumHost/RepositorioDeMassaEmMemoria.cs`
- [x] T008 Contratos e portas em `src/Modulos/Simulador.Consentimentos/Contratos/Modelos.cs`
- [x] T009 Adaptadores em `src/Simulador.Host/AdaptadoresDeConsentimentos.cs` e registro em `src/Simulador.Host/Program.cs`
- [x] T010 Ajustar testes de rota sem manipulador em `tests/Simulador.Testes.Integracao/Catalogo/RotasGeradasTests.cs` e `Marcacao/VarreduraDeMarcacaoTests.cs`

## Phase 3: US1 - Autenticar a receptora (P1)

**Independent Test**: cadastrar receptora, pedir token e chamar rota protegida; repetir sem token e com senha errada.

- [x] T011 [P] [US1] Testes de domínio do token e do portão em `tests/Simulador.Testes.Unidade/Consentimentos/DominioTests.cs`
- [x] T012 [P] [US1] Passos de `seguranca.feature` em `tests/Simulador.Testes.Aceitacao/Passos/ConsentimentosPassos.cs`
- [x] T013 [US1] Token assinado (900 s) e regras do portão em `src/Modulos/Simulador.Consentimentos/Dominio/Acesso.cs`
- [x] T014 [US1] Cadastro de receptoras (nome obrigatório; `usuario` e `senha` com prefixo `teste-`) em `src/Modulos/Simulador.Consentimentos/Aplicacao/Servicos.cs` e `Persistencia/RepositorioMongo.cs`
- [x] T015 [US1] `POST /{banco}/token`, portão e administração em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`

## Phase 4: US2 - Criar consentimento (P1)

**Independent Test**: criar consentimentos válidos e inválidos e conferir status, código e recursos.

- [x] T016 [P] [US2] Testes de grupos, PF/PJ e remoção em `tests/Simulador.Testes.Unidade/Consentimentos/DominioTests.cs`
- [x] T017 [P] [US2] Passos de criação em `tests/Simulador.Testes.Aceitacao/Passos/ConsentimentosPassos.cs`
- [x] T018 [US2] Grupos oficiais e validações RN83 e RN84 em `src/Modulos/Simulador.Consentimentos/Dominio/Permissoes.cs`
- [x] T019 [US2] `consentsPostConsents` em `src/Modulos/Simulador.Consentimentos/Aplicacao/Servicos.cs` e `Http/Rotas.cs`

## Phase 5: US3 - Consultar e revogar (P1)

**Independent Test**: revogar um de dois consentimentos e conferir 401 no revogado.

- [x] T020 [P] [US3] Testes de vencimento e rejeição em `tests/Simulador.Testes.Unidade/Consentimentos/DominioTests.cs`
- [x] T021 [P] [US3] Passos de revogação e de `massa-e-semente.feature` em `tests/Simulador.Testes.Aceitacao/Passos/`
- [x] T022 [US3] Rejeição e vencimento em `src/Modulos/Simulador.Consentimentos/Dominio/Ciclo.cs`
- [x] T023 [US3] `GET` e `DELETE` e `POST /admin/consentimentos/{consentId}/revogar` em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`

## Phase 6: US4 - Consultar recursos (P1)

**Independent Test**: consultar recursos durante e depois da preparação.

- [x] T024 [P] [US4] Testes de preparação em `tests/Simulador.Testes.Unidade/Consentimentos/DominioTests.cs`
- [x] T025 [P] [US4] Passos de `recursos.feature` em `tests/Simulador.Testes.Aceitacao/Passos/ConsentimentosPassos.cs`
- [x] T026 [US4] Preparação em `src/Modulos/Simulador.Consentimentos/Dominio/Ciclo.cs`
- [x] T027 [US4] `resourcesGetResources`, com paginação básica, em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`

## Phase 7: US5 - Renovar (P2)

**Independent Test**: renovar, conferir `/extensions` e provocar os erros de RN87.

- [x] T028 [P] [US5] Testes de RN87 em `tests/Simulador.Testes.Unidade/Consentimentos/DominioTests.cs`
- [x] T029 [US5] Regra de renovação em `src/Modulos/Simulador.Consentimentos/Dominio/Ciclo.cs`
- [x] T030 [US5] `consentsPostConsentsConsentIdExtends` e `consentsGetConsentsConsentIdExtensions` em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`

## Phase 8: US6 - Alterar recursos (P2)

**Independent Test**: percorrer transições e conferir em `GET /resources`.

- [x] T031 [P] [US6] Testes de RN101 em `tests/Simulador.Testes.Unidade/Consentimentos/DominioTests.cs`
- [x] T032 [US6] Transições em `src/Modulos/Simulador.Consentimentos/Dominio/Ciclo.cs`
- [x] T033 [US6] Administração de recursos e produtos em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`

## Phase 9: Polish

- [x] T034 Validar respostas contra os contratos em `tests/Simulador.Testes.Integracao/Consentimentos/ContratoTests.cs`
- [x] T035 Tirar `@spec-004` dos `.feature` em `docs/01-requisitos/gherkin/` e atualizar `tests/Simulador.Testes.Unidade/Arquitetura/EtiquetasDeSpecTests.cs`
- [x] T036 Rodar formatação, análise estática, testes e cobertura conforme `.github/workflows/ci.yml`

## Dependencies

Setup → Foundational → US1 → demais histórias. US2 e US4 alimentam US3, US5 e US6.

## Implementation Strategy

MVP: US1 a US4. Depois, US5 e US6. Em cada história, testes primeiro.
