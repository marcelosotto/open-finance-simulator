# Tasks: Massa e gerador

**Input**: documentos em `specs/003-massa-e-gerador/`

**Tests**: obrigatórios e antes do código (constituição, princípio IX).

## Phase 1: Setup

- [ ] T001 Criar `src/Modulos/Simulador.Massa/Simulador.Massa.csproj`, incluí-lo em `Simulador.slnx` e referenciá-lo no Host e em `tests/Simulador.Testes.Unidade/Simulador.Testes.Unidade.csproj`
- [ ] T002 Incluir o módulo em `tests/Simulador.Testes.Unidade/Arquitetura/FronteirasDosModulosTests.cs`

## Phase 2: Foundational

- [ ] T003 [P] Testar identificador com corpo determinístico em `tests/Simulador.Testes.Unidade/Marcacao/GeradorDeIdentificadorTests.cs`
- [ ] T004 Acrescentar `Identificador` e `ConsentId` com corpo a `IMarcador` em `src/Modulos/Simulador.Marcacao/`
- [ ] T005 [P] Testar sorteio, documentos e ciclo de bancos em `tests/Simulador.Testes.Unidade/Massa/`
- [ ] T006 Implementar `Sorteio`, `Documentos` (CPF com 9; virtual de 0 a 49.999.999; persistido de 50.000.000 a 99.999.998; CNPJ alfanumérico) e `CicloDeBancos` em `src/Modulos/Simulador.Massa/Dominio/`
- [ ] T007 Declarar registros e portas (`IRepositorioDeMassa`, `IConsultaDeMassa`, `IAmbienteDaMassa`) em `src/Modulos/Simulador.Massa/Contratos/`
- [ ] T008 Criar `tests/ComumHost/RepositorioDeMassaEmMemoria.cs` e usá-lo como padrão em `tests/ComumHost/FabricaDoSimulador.cs`
- [ ] T009 Implementar `RepositorioMongo` em `src/Modulos/Simulador.Massa/Persistencia/RepositorioMongo.cs`, com teste em `tests/Simulador.Testes.Integracao/Massa/RepositorioMongoTests.cs`
- [ ] T010 Implementar os adaptadores em `src/Simulador.Host/AdaptadoresDaMassa.cs` e registrar o módulo em `src/Simulador.Host/Program.cs`

## Phase 3: User Story 1 - Definir como é a massa (P1)

**Goal**: perfis e distribuição. **Independent Test**: listar prontos, salvar distribuição válida e inválida.

- [ ] T011 [P] [US1] Testar perfis prontos e validação ("Bancos e perfis somam 100%, com até 2 casas. Produtos e PJ entre 0 e 100.") em `tests/Simulador.Testes.Unidade/Massa/ValidacaoTests.cs`
- [ ] T012 [US1] Implementar `PerfisProntos`, `Validacao` e `Produtos` em `src/Modulos/Simulador.Massa/Dominio/`
- [ ] T013 [US1] Implementar `CadastroDeMassa` e rotas de perfis e distribuições em `src/Modulos/Simulador.Massa/Aplicacao/` e `src/Modulos/Simulador.Massa/Http/RotasDaMassa.cs`, com teste em `tests/Simulador.Testes.Integracao/Massa/AdminMassaTests.cs`

## Phase 4: User Story 2 - Criar um cliente simulado (P1)

**Goal**: cliente avulso com produtos por banco. **Independent Test**: criar em dois bancos e conferir.

- [ ] T014 [P] [US2] Testar o gerador e o cálculo de transações em `tests/Simulador.Testes.Unidade/Massa/GeradorTests.cs`
- [ ] T015 [US2] Implementar `GeradorDeMassa` e `CalculoDeTransacoes` em `src/Modulos/Simulador.Massa/Dominio/`
- [ ] T016 [US2] Implementar a criação de cliente, a reserva de posições e as rotas em `src/Modulos/Simulador.Massa/Aplicacao/` e `src/Modulos/Simulador.Massa/Http/RotasDaMassa.cs`

## Phase 5: User Story 3 - Gerar um lote (P1)

**Goal**: lote em segundo plano. **Independent Test**: lote de 100, resumo e nenhuma transação gravada.

- [ ] T017 [P] [US3] Testar lote, cancelamento, semente repetida e interrupção em `tests/Simulador.Testes.Integracao/Massa/LotesTests.cs`
- [ ] T018 [US3] Implementar `ExecutorDeLotes` e `InicioDaMassa` em `src/Modulos/Simulador.Massa/Aplicacao/` e as rotas de lote

## Phase 6: User Story 4 - Mundo virtual (P2)

**Goal**: faixas sem gravar. **Independent Test**: 10 milhões, 1.000 itens, repetição igual.

- [ ] T019 [P] [US4] Testar `MundoCalculado`, filtro por banco e leitura de ID em `tests/Simulador.Testes.Unidade/Massa/MundoCalculadoTests.cs`
- [ ] T020 [US4] Implementar `MundoCalculado` em `src/Modulos/Simulador.Massa/Dominio/`, a definição do mundo e `IConsultaDeMassa` em `src/Modulos/Simulador.Massa/Aplicacao/`

## Phase 7: User Story 5 - Ajustes (P2)

**Goal**: ajuste vence o cálculo. **Independent Test**: R$ 100 vira R$ 90 e o saldo acompanha.

- [ ] T021 [P] [US5] Testar ajustes em `tests/Simulador.Testes.Integracao/Massa/AjustesTests.cs`
- [ ] T022 [US5] Implementar a criação e a aplicação de ajustes em `src/Modulos/Simulador.Massa/Aplicacao/` e as rotas

## Phase 8: User Story 6 - Limpar massa (P2)

**Goal**: prévia e exclusão. **Independent Test**: apagar um lote e manter o outro.

- [ ] T023 [P] [US6] Testar os quatro alvos de limpeza em `tests/Simulador.Testes.Integracao/Massa/LimpezaTests.cs`
- [ ] T024 [US6] Implementar a limpeza, `IConsultaDeClientes` e o histórico em `src/Modulos/Simulador.Massa/Aplicacao/` e `src/Simulador.Host/AdaptadoresDaMassa.cs`

## Phase 9: Polish

- [ ] T025 Tirar `@spec-003` de `docs/01-requisitos/gherkin/massa-e-semente.feature`, criar `tests/Simulador.Testes.Aceitacao/Passos/MassaPassos.cs` e ajustar `tests/Simulador.Testes.Unidade/Arquitetura/EtiquetasDeSpecTests.cs`
- [X] T026 Registrar em `docs/` a exceção do mundo virtual na limpeza total (UC27)
- [ ] T027 Rodar os portões do CI: formatação, build, testes e cobertura de `tools/Simulador.Ferramentas`

## Dependencies

Setup e Foundational vêm antes de tudo. US2 depende de US1; US3 e US4 dependem de US2; US5 depende de US3 e US4; US6 depende de US3 e US5.

## Parallel Example

T003 e T005 podem andar juntas, assim como os testes de cada história.

## Implementation Strategy

MVP: US1 a US3. As histórias P2 entram na sequência, e os portões rodam ao final de cada fase.
