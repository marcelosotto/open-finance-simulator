# Tasks: Consentimento no painel

**Input**: [plan.md](plan.md), [spec.md](spec.md), [data-model.md](data-model.md), [contracts/](contracts/admin-painel.md)

**Tests**: obrigatórios e antes da implementação (princípio IX).

## Phase 1: Foundational

- [ ] T001 Acrescentar `AlcadaRecusada` e `Decisoes` opcionais ao consentimento em `src/Modulos/Simulador.Massa/Contratos/Modelos.cs` e em `src/Modulos/Simulador.Consentimentos/Contratos/Modelos.cs`, e mapear em `src/Simulador.Host/AdaptadoresDeConsentimentos.cs`
- [ ] T002 Criar os esqueletos `src/Modulos/Simulador.Consentimentos/Dominio/Painel.cs` e `Dominio/Alcadas.cs`
- [ ] T003 [P] Escrever os testes do domínio em `tests/Simulador.Testes.Unidade/Consentimentos/PainelTests.cs` e confirmar a falha
- [ ] T004 Escrever `tests/Simulador.Testes.Aceitacao/Passos/PainelPassos.cs` e o passo "duas aprovadoras" em `ConsentimentosPassos.cs`, retirar `@spec-008` de `consentimento-no-painel.feature` e `consentimento-api.feature`, e confirmar a falha

## Phase 2: User Story 1 - Criar consentimento pelo menu (P1)

**Goal**: criação com seleção e prazo. **Independent Test**: criar para "Coletor" e consultar Recursos.

- [ ] T005 [US1] Implementar a validação do pedido e a escolha dos recursos em `Dominio/Painel.cs` (RN95, RN96)
- [ ] T006 [US1] Implementar `POST /admin/consentimentos` e `GET /admin/consentimentos/{consentId}` em `src/Modulos/Simulador.Consentimentos/Http/Painel.cs`, ligados em `MapearAdministracao`

## Phase 3: User Story 2 - Opções do menu (P1)

**Goal**: só o que vale para o cliente. **Independent Test**: opções PF e PJ.

- [ ] T007 [US2] Implementar grupos nomeados e opções em `Dominio/Painel.cs` e `GET /admin/consentimentos/opcoes` em `Http/Painel.cs` (RN84, RN08)

## Phase 4: User Story 3 - Múltiplas alçadas (P1)

**Goal**: pendente, aprovada, recusada e vencida. **Independent Test**: Recursos e acesso direto nos quatro casos.

- [ ] T008 [US3] Implementar situação, estado no consentimento e decisões em `Dominio/Alcadas.cs` (RN97 a RN99)
- [ ] T009 [US3] Implementar `/aprovar` e `/recusar` em `Http/Painel.cs`, com versão e histórico
- [ ] T010 [US3] Expor `EstadoNoConsentimento` e `RecursoNoConsentimento`; usar em Recursos (`Http/Rotas.cs`) e nos adaptadores do host (`AdaptadoresDeConsentimentos.cs`, `AdaptadoresDeDados.cs`), antes da regra de cenário

## Phase 5: Polish

- [ ] T011 [P] Testes de integração da administração em `tests/Simulador.Testes.Integracao/Consentimentos/PainelTests.cs`: códigos de 422, 404, 409, concorrência, schema oficial de Recursos
- [ ] T012 Rodar os portões: formatação, build com `-warnaserror`, specs, suíte com cobertura, `cobertura conferir` e `docker build`

## Dependencies

T001 → T002 → (T003, T004) → T005 → T006 → T007 → T008 → T009 → T010 → T011 → T012.
