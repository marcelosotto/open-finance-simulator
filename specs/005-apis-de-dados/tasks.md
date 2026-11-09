# Tasks: APIs de dados

**Input**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md)

**Testes**: obrigatórios, antes da implementação (princípio IX).

## Phase 1: Setup

- [x] T001 Criar o projeto `src/Modulos/Simulador.Dados/Simulador.Dados.csproj` e incluí-lo em `Simulador.slnx` e `src/Simulador.Host/Simulador.Host.csproj`

## Phase 2: Foundational

- [x] T002 Adicionar `IManipuladorDeFamilia` em `src/Modulos/Simulador.Catalogo/Contratos/OperacaoDoCatalogo.cs` e usá-lo em `src/Modulos/Simulador.Catalogo/Rotas/Despachante.cs` e em `/admin/catalogo`
- [x] T003 Expor o consentimento da chamada em `src/Modulos/Simulador.Consentimentos/Contratos/ExtensoesDosConsentimentos.cs`
- [x] T004 [P] Testes de unidade de paginação, janelas, chave e famílias em `tests/Simulador.Testes.Unidade/Dados/DominioDosDadosTests.cs`
- [x] T005 Implementar paginação, janelas, chave e famílias em `src/Modulos/Simulador.Dados/Dominio/`
- [x] T006 Implementar o gerador por schema em `src/Modulos/Simulador.Dados/Aplicacao/GeradorPorSchema.cs`, com testes em `tests/Simulador.Testes.Unidade/Dados/GeradorPorSchemaTests.cs`
- [x] T007 Ligar o módulo no host em `src/Simulador.Host/AdaptadoresDeDados.cs` e `src/Simulador.Host/Program.cs`

## Phase 3: User Story 1 - Consultar contas e transações (P1)

**Teste independente**: consultar saldo e transações de uma conta e conferir a soma e o schema.

- [x] T008 [P] [US1] Testes de estado, futuros até 30 dias e saldo em `tests/Simulador.Testes.Unidade/Massa/DominioDaMassaTests.cs`
- [x] T009 [P] [US1] Passos de `dados-e-transacoes.feature` em `tests/Simulador.Testes.Aceitacao/Passos/DadosPassos.cs`
- [x] T010 [US1] Estado, ID provisório, futuros e saldo em `src/Modulos/Simulador.Massa/Dominio/GeradorDeMassa.cs` e `src/Modulos/Simulador.Massa/Contratos/Modelos.cs`
- [x] T011 [US1] Manipulador de contas e transações em `src/Modulos/Simulador.Dados/Aplicacao/ManipuladorDeDados.cs`

## Phase 4: User Story 2 - Paginar listas (P1)

**Teste independente**: percorrer 47 registros com chave válida, expirada e inválida.

- [x] T012 [P] [US2] Passos de `paginacao.feature` em `tests/Simulador.Testes.Aceitacao/Passos/DadosPassos.cs`
- [x] T013 [US2] Links, meta, chave e ajustes congelados em `src/Modulos/Simulador.Dados/Aplicacao/ManipuladorDeDados.cs` e `src/Modulos/Simulador.Massa/Aplicacao/Servicos.cs`

## Phase 5: User Story 4 - Respeitar o estado do recurso (P1)

**Teste independente**: variar o estado de um recurso e consultar lista e acesso direto.

- [x] T014 [P] [US4] Passos `@spec-005` retirados de `recursos.feature` em `tests/Simulador.Testes.Aceitacao/Passos/ConsentimentosPassos.cs`
- [x] T015 [US4] 403 `RESOURCE_FORBIDDEN` e `STATUS_RESOURCE_<estado>` em `src/Modulos/Simulador.Dados/Aplicacao/ManipuladorDeDados.cs`

## Phase 6: User Story 3 - Consultar todas as famílias (P2)

**Teste independente**: chamar os 69 endpoints e validar contra o schema.

- [x] T016 [P] [US3] Teste dos 69 endpoints em `tests/Simulador.Testes.Integracao/Dados/TodasAsFamiliasTests.cs`
- [x] T017 [US3] Produtos `renda-fixa-credito` e `tesouro-direto` em `src/Modulos/Simulador.Massa/Dominio/Catalogos.cs`, `src/Modulos/Simulador.Instituicoes/Dominio/ValoresOficiais.cs` e `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`
- [x] T018 [US3] Faturas, limites e dados cadastrais em `src/Modulos/Simulador.Dados/Aplicacao/ManipuladorDeDados.cs`

## Phase 7: User Story 5 - Pendências da spec 004 (P3)

**Teste independente**: duas mudanças concorrentes de estado.

- [x] T019 [P] [US5] Teste de corrida em `tests/Simulador.Testes.Integracao/Consentimentos/RevisaoTests.cs`
- [x] T020 [US5] Escrita condicional em `MudarEstado`, em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs`, `src/Modulos/Simulador.Massa/Aplicacao/Servicos.cs` e `src/Simulador.Host/AdaptadoresDeConsentimentos.cs`

## Phase 8: Polish

- [x] T021 Ajustar os testes de 501 e de etiquetas em `tests/Simulador.Testes.Integracao/Catalogo/RotasGeradasTests.cs` e `tests/Simulador.Testes.Unidade/Arquitetura/`
- [x] T022 Rodar todos os portões de `.github/workflows/ci.yml`

## Dependencies

Setup → Foundational → US1 → US2 → US4 → US3 → US5 → Polish. Os testes [P] de cada história podem ser escritos juntos.

## Implementation Strategy

MVP: US1 e US2. As demais reaproveitam o mesmo manipulador.
