# Tasks: Limites

**Input**: documentos em `specs/006-limites/`. Testes obrigatórios (princípio IX).

## Phase 1: Setup

- [x] T001 Criar o módulo em `src/Modulos/Simulador.Limites/Simulador.Limites.csproj`, incluí-lo em `Simulador.slnx`, no host e em `Directory.Packages.props` (`StackExchange.Redis`, `Testcontainers.Redis`)
- [x] T002 [P] Adicionar o serviço `redis` e `Simulador__Redis__Conexao` em `docker-compose.yml`, e o projeto novo em `Dockerfile`, se ele listar projetos

## Phase 2: Foundational

- [x] T003 Escrever testes de domínio de janelas, chaves e objeto da cota em `tests/Simulador.Testes.Unidade/Limites/DominioDosLimitesTests.cs`
- [x] T004 Implementar janelas, chaves ("`lim:cap:{banco}:{segundo}`", "`lim:tpm:{banco}:{operationId}:{receptora}:{minuto}`", "`lim:cota:{banco}:{yyyyMM}:{operationId}:{objeto}:{cliente}:{receptora}`") e objeto em `src/Modulos/Simulador.Limites/Dominio/Regras.cs`
- [x] T005 Definir `IContadores`, `ILimites` e registro em `src/Modulos/Simulador.Limites/Contratos/Modelos.cs`
- [x] T006 Implementar `ContadoresRedis`, com soma atômica e falha rápida (Q8), em `src/Modulos/Simulador.Limites/Persistencia/ContadoresRedis.cs`
- [x] T007 Adicionar contadores nulos e em memória e `ComContadoresEmMemoria()` em `tests/ComumHost/`
- [x] T008 Expor a receptora da chamada em `src/Modulos/Simulador.Consentimentos/Contratos/ExtensoesDosConsentimentos.cs`
- [x] T009 Adicionar `IEtapaDeEntrada` e chamá-la após o portão em `src/Modulos/Simulador.Catalogo/Rotas/Despachante.cs`

## Phase 3: User Story 1 - 429 (P1)

**Goal**: TPM por endpoint e receptora. **Independent Test**: TPM + 1 chamadas no minuto.

- [x] T010 [US1] Escrever os passos de tráfego em `tests/Simulador.Testes.Aceitacao/Passos/LimitesPassos.cs`
- [x] T011 [US1] Implementar o tráfego e o 429 com `Retry-After` em `src/Modulos/Simulador.Limites/Aplicacao/ServicoDeLimites.cs`
- [x] T012 [US1] Ligar instituição e catálogo à etapa, com faixa de QCA, em `src/Simulador.Host/AdaptadoresDeLimites.cs`

## Phase 4: User Story 2 - 423 (P1)

**Goal**: cota mensal, com chave. **Independent Test**: 9 chamadas a `/accounts/{accountId}`.

- [x] T013 [US2] Escrever os passos de cota em `tests/Simulador.Testes.Aceitacao/Passos/LimitesPassos.cs` e os dois de `paginacao.feature` em `tests/Simulador.Testes.Aceitacao/Passos/DadosPassos.cs`
- [x] T014 [US2] Implementar conferência e contagem em `src/Modulos/Simulador.Limites/Aplicacao/ServicoDeLimites.cs`
- [x] T015 [US2] Inserir a etapa da cota após `PAGE_NOT_FOUND`, com o resultado da chave e o 423 `LOCKED`, em `src/Modulos/Simulador.Dados/Aplicacao/ServicoDeDados.cs`
- [x] T016 [US2] Adaptar a cota por classe (420 para `SaldosELimites`) em `src/Simulador.Host/AdaptadoresDeDados.cs`

## Phase 5: User Story 3 - 529 (P2)

**Goal**: capacidade da instituição. **Independent Test**: capacidade + 1 no segundo.

- [x] T017 [US3] Escrever os passos de capacidade em `tests/Simulador.Testes.Aceitacao/Passos/LimitesPassos.cs`
- [x] T018 [US3] Implementar a capacidade nas operações que declaram 529 em `src/Modulos/Simulador.Limites/Aplicacao/ServicoDeLimites.cs`

## Phase 6: User Story 4 - Ambiente e ordem (P1)

**Goal**: contadores compartilhados e RN39. **Independent Test**: duas instâncias sobre o mesmo contador.

- [x] T019 [US4] Escrever os passos de instâncias e de ordem em `tests/Simulador.Testes.Aceitacao/Passos/LimitesPassos.cs`
- [x] T020 [US4] Testar `ContadoresRedis`, inclusive indisponível, em `tests/Simulador.Testes.Integracao/Limites/ContadoresRedisTests.cs`
- [x] T021 [US4] Validar 429, 423 e 529 contra o contrato em `tests/Simulador.Testes.Integracao/Limites/RespostasDeLimiteTests.cs`

## Phase 7: Polish

- [x] T022 Remover as etiquetas `@spec-006` de `docs/01-requisitos/gherkin/limites.feature` e `docs/01-requisitos/gherkin/paginacao.feature`, e atualizar `tests/Simulador.Testes.Unidade/Arquitetura/EtiquetasDeSpecTests.cs`
- [x] T023 Rodar os portões de `.github/workflows/ci.yml`

## Dependencies

Setup → Foundational → US1 → US2, US3 e US4. T010, T013, T017 e T019 antes das implementações correspondentes.

**Parallel**: T002 com T001. As histórias compartilham `LimitesPassos.cs` e `ServicoDeLimites.cs`, então seguem em sequência.

**Strategy**: MVP é a US1; depois US2, que fecha a herança da 005; depois US3 e US4.
