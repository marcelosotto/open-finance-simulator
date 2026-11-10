# Tasks: Comportamento e cenários

**Input**: [plan.md](plan.md), [spec.md](spec.md), [data-model.md](data-model.md), [contracts/](contracts/admin-cenarios.md)

**Tests**: obrigatórios e antes da implementação (princípio IX).

## Phase 1: Setup

- [X] T001 Criar o projeto `src/Modulos/Simulador.Cenarios/Simulador.Cenarios.csproj`, incluir em `Simulador.slnx`, no host e nos testes
- [X] T002 Criar `HoraReal` e `IComportamentoDaChamada` em `src/Simulador.Comum/Comportamento/`

## Phase 2: Foundational

- [X] T003 Criar os contratos em `src/Modulos/Simulador.Cenarios/Contratos/Modelos.cs` e os esqueletos do domínio em `Dominio/`
- [X] T004 [P] Escrever os testes do domínio em `tests/Simulador.Testes.Unidade/Cenarios/DominioDosCenariosTests.cs` e confirmar a falha
- [X] T005 Escrever `tests/Simulador.Testes.Aceitacao/Passos/ComportamentoPassos.cs`, retirar `@spec-007` de `comportamento.feature` e `recursos.feature`, e confirmar a falha
- [X] T006 Trocar `TimeProvider` por `HoraReal` na fábrica `tests/ComumHost/FabricaDoSimulador.cs`, com etapa nula por padrão, `ComComportamento()` e espera registrada

## Phase 3: User Story 1 - Perfis (P1)

**Goal**: cada banco responde conforme o perfil. **Independent Test**: distribuição de tempo e status por perfil.

- [X] T007 [US1] Implementar amostra de latência, cauda, janela do perfil e sorteio em `src/Modulos/Simulador.Cenarios/Dominio/Avaliacao.cs`
- [X] T008 [US1] Recusar pesos abaixo de 400 em `src/Modulos/Simulador.Instituicoes/Dominio/Regras.cs` (Q1)
- [X] T009 [US1] Implementar `ComportamentoDaChamada` e `EsperaReal` em `src/Simulador.Host/AdaptadoresDeCenarios.cs`
- [X] T010 [US1] Guardar a operação da chamada em `src/Modulos/Simulador.Catalogo/Rotas/Despachante.cs` e renomear o 503 (Q14)
- [X] T011 [US1] Chamar a etapa e RN109 em `src/Modulos/Simulador.Dados/Aplicacao/ServicoDeDados.cs`
- [X] T012 [US1] Chamar a etapa antes das gravações em `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs` e renomear 503 e 504 (Q14)

## Phase 4: User Story 2 - Timeout e indisponibilidade (P1)

**Goal**: 504 após 15 s, janela e transporte. **Independent Test**: espera registrada e status por período.

- [X] T013 [US2] Implementar timeout, indisponibilidade e transporte em `src/Modulos/Simulador.Cenarios/Dominio/Avaliacao.cs`
- [X] T014 [US2] Corromper a saída no filtro de `src/Simulador.Host/FiltroDaChamada.cs` (Q13)

## Phase 5: User Story 3 - Regras e pacotes (P1)

**Goal**: regras e pacotes pela administração. **Independent Test**: conflitos e pacote.

- [X] T015 [US3] Implementar seleção por RN15 e disparo com semente em `src/Modulos/Simulador.Cenarios/Dominio/Selecao.cs`
- [X] T016 [US3] Validar regras (Q4, Q12) em `src/Modulos/Simulador.Cenarios/Dominio/Validacao.cs`
- [X] T017 [US3] Implementar serviço, cache de 5 s e rotas em `src/Modulos/Simulador.Cenarios/Aplicacao/` e `Http/` conforme `contracts/admin-cenarios.md`
- [X] T018 [P] [US3] Implementar `src/Modulos/Simulador.Cenarios/Persistencia/RepositorioMongo.cs` e teste em `tests/Simulador.Testes.Integracao/Cenarios/`
- [X] T019 [US3] Sobrepor o estado nas leituras em `src/Simulador.Host/AdaptadoresDeDados.cs` e `AdaptadoresDeConsentimentos.cs` (Q2)

## Phase 6: User Story 4 - Relógio (P2)

**Goal**: relógio por instituição. **Independent Test**: fixar um banco e conferir os demais.

- [X] T020 [US4] Criar `RelogioSimulado` e as rotas em `src/Modulos/Simulador.Instituicoes/`
- [X] T021 [US4] Implementar `RelogioDaChamada` e `x-sim-clock` em `src/Simulador.Host/FiltroDaChamada.cs`
- [X] T022 [US4] Usar `HoraReal` em TPM e capacidade em `src/Modulos/Simulador.Limites/Aplicacao/ServicoDeLimites.cs`
- [X] T023 [US4] Usar o relógio do banco nas escritas de administração em `src/Modulos/Simulador.Massa/Aplicacao/Servicos.cs` e `src/Modulos/Simulador.Consentimentos/Http/Rotas.cs` (Q7)

## Phase 7: Polish

- [X] T024 Conferir `docs/02-arquitetura/c4/C3-componentes.md` e `docs/03-design/sequencia/02-chamada-de-dados.md`: já mostram Cenários e a etapa de regras, sem mudança
- [X] T025 Conferir o `Dockerfile`: copia `src/` inteiro, sem mudança
- [X] T026 Rodar os portões de `.github/workflows/ci.yml`

## Dependencies

Setup → Foundational → US1 → US2 e US3 → US4. Cada história só depende da etapa criada em US1.
