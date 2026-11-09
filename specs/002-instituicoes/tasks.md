# Tasks: Instituições simuladas

**Input**: documentos em `specs/002-instituicoes/`

**Tests**: obrigatórios e antes do código (constituição, princípio IX).

## Phase 1: Setup

- [X] T001 Adicionar `MongoDB.Driver` 3.13.0 e `Testcontainers.MongoDb` 4.16.0 em `Directory.Packages.props`
- [X] T002 Criar o projeto `src/Modulos/Simulador.Instituicoes/Simulador.Instituicoes.csproj`, incluí-lo em `Simulador.slnx` e referenciá-lo no Host e nos testes
- [X] T003 Adicionar o serviço `mongo` e `Simulador__Mongo__Conexao` em `docker-compose.yml`

## Phase 2: Foundational

- [X] T004 Declarar registros públicos e portas (`IRepositorioDeInstituicoes`, `IConsultaDeInstituicoes`, `IConsultaDeClientes`, `IAmbienteDasInstituicoes`) em `src/Modulos/Simulador.Instituicoes/Contratos/`
- [X] T005 Trocar `IResolvedorDeInstituicao.Existe` por `Situacao` assíncrona, tirar o resolvedor provisório e responder 503 para inativa em `src/Modulos/Simulador.Catalogo/Rotas/Despachante.cs`
- [X] T006 Criar `tests/ComumHost/RepositorioEmMemoria.cs` com `banco-teste` e usá-lo como padrão em `tests/ComumHost/FabricaDoSimulador.cs`
- [X] T007 Implementar os adaptadores em `src/Simulador.Host/AdaptadoresDeInstituicoes.cs` e registrar o módulo em `src/Simulador.Host/Program.cs`
- [X] T008 Incluir o módulo em `tests/Simulador.Testes.Unidade/Arquitetura/FronteirasDosModulosTests.cs`

## Phase 3: User Story 1 - Bancos prontos e endereço (P1)

**Goal**: 16 bancos ao subir e chamada pelo endereço. **Independent Test**: listar e chamar um endereço existente, outro inexistente.

- [X] T009 [P] [US1] Testar endereço (`^[a-z0-9-]{1,64}$`, sem acento, sem prefixo), nome com prefixo e perfis em `tests/Simulador.Testes.Unidade/Instituicoes/`
- [X] T010 [P] [US1] Criar `tests/Simulador.Testes.Aceitacao/Passos/InstituicoesPassos.cs` e o contexto com MongoDB em contêiner em `tests/Simulador.Testes.Aceitacao/Infra/ContextoDeInstituicoes.cs`
- [X] T011 [US1] Implementar `Endereco`, `NomeDoBanco`, `CatalogoDePerfis` e `ValoresOficiais` em `src/Modulos/Simulador.Instituicoes/Dominio/`
- [X] T012 [US1] Implementar a semeadura idempotente em `src/Modulos/Simulador.Instituicoes/Aplicacao/Semeadura.cs` e `RepositorioMongo` em `src/Modulos/Simulador.Instituicoes/Persistencia/RepositorioMongo.cs`, com teste em `tests/Simulador.Testes.Integracao/Instituicoes/RepositorioMongoTests.cs`

## Phase 4: User Story 2 - Cadastro (P1)

**Goal**: criar só com o nome. **Independent Test**: criar, abrir e chamar.

- [X] T013 [P] [US2] Testar validação de cadastro (`publico`: `PF`, `PJ` ou `ambos`; produtos da lista fechada) em `tests/Simulador.Testes.Unidade/Instituicoes/ValidacaoTests.cs`
- [X] T014 [P] [US2] Testar a API de criação, 409 e 422 em `tests/Simulador.Testes.Integracao/Instituicoes/AdminInstituicoesTests.cs`
- [X] T015 [US2] Implementar `CadastroDeInstituicoes` em `src/Modulos/Simulador.Instituicoes/Aplicacao/` e as rotas em `src/Modulos/Simulador.Instituicoes/Http/RotasDeInstituicoes.cs`

## Phase 5: User Story 3 - Comportamento (P2)

**Goal**: ajustar sem afetar perfil. **Independent Test**: `PATCH` e leitura.

- [X] T016 [P] [US3] Testar merge patch, faixa de 0 a 100, status recusados e SLA em `tests/Simulador.Testes.Unidade/Instituicoes/`
- [X] T017 [US3] Implementar `AplicadorDeMerge`, `Validacao` e `ComparacaoDeSla` em `src/Modulos/Simulador.Instituicoes/Dominio/`, e o `PATCH`

## Phase 6: User Story 4 - Limites e paginação (P2)

**Goal**: valores oficiais e avisos. **Independent Test**: reduzir um valor e ler o aviso.

- [X] T018 [P] [US4] Testar faixas de QCA, sobrescrita por receptora e avisos em `tests/Simulador.Testes.Unidade/Instituicoes/LimitesTests.cs`
- [X] T019 [US4] Implementar `FaixaDeQca` e `Avisos` em `src/Modulos/Simulador.Instituicoes/Dominio/`

## Phase 7: User Story 5 - Histórico e inativação (P3)

**Goal**: rastreabilidade e fora do ar. **Independent Test**: mudar, ler histórico; inativar e chamar.

- [X] T020 [P] [US5] Testar diferenças em `tests/Simulador.Testes.Unidade/Instituicoes/DiferencasTests.cs` e `DELETE`, reativação e 503 em `tests/Simulador.Testes.Integracao/Instituicoes/InativacaoTests.cs`
- [X] T021 [US5] Implementar `Diferencas` em `src/Modulos/Simulador.Instituicoes/Dominio/`, histórico, `DELETE` e reativação na aplicação e nas rotas

## Phase 8: Polish

- [ ] T022 Rodar `dotnet format`, build com `-warnaserror`, `specs verificar`, testes com cobertura e `cobertura conferir`
- [ ] T023 Conferir os passos de `specs/002-instituicoes/quickstart.md` no Docker Compose

## Dependencies

Setup → Foundational → US1 → US2 → (US3, US4) → US5 → Polish. T010 e T012 usam Docker.

## Parallel

Os testes marcados com [P] em cada história podem ser escritos em paralelo.

## Implementation Strategy

MVP: US1 e US2. As demais entram em seguida, com commits ao longo do caminho.
