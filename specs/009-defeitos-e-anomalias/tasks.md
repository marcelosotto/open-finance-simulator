# Tasks: Defeitos e anomalias

**Input**: [plan.md](plan.md), [spec.md](spec.md), [data-model.md](data-model.md), [contracts/](contracts/admin-defeitos-e-anomalias.md)

**Tests**: obrigatórios e antes da implementação (princípio IX).

## Phase 1: Setup

- [X] T001 Registrar Q1 a Q13 em `docs/` (RN110 a RN117, RN36, RN37, RN88, UC22, modelo de dados, perfis, sequência de specs)

## Phase 2: Foundational

- [X] T002 Ampliar `EfeitoDaRegra`, `DecisaoDeComportamento`, `ICenarios` e `IRepositorioDeCenarios` em `src/Modulos/Simulador.Cenarios/Contratos/Modelos.cs`, e criar os nomes das anomalias, o item de página seguinte e `IAnomaliasDaChamada` em `src/Simulador.Comum/Comportamento/`
- [X] T003 [P] Escrever os testes do domínio em `tests/Simulador.Testes.Unidade/Cenarios/DefeitosEAnomaliasTests.cs` e confirmar a falha
- [X] T004 Escrever `tests/Simulador.Testes.Aceitacao/Passos/DefeitosPassos.cs` e os passos novos de `DadosPassos.cs`, retirar `@spec-009` de `defeitos-e-anomalias.feature` e `paginacao.feature`, atualizar `EtiquetasDeSpecTests` e confirmar a falha
- [X] T005 Guardar anomalias no repositório em memória `tests/ComumHost/ComportamentoDeTeste.cs` e em `src/Modulos/Simulador.Cenarios/Persistencia/RepositorioMongo.cs`

## Phase 3: User Story 1 - Defeitos de dado (P1)

**Goal**: dado ruim de propósito, com taxa e alcance. **Independent Test**: regra de defeito com 100% em transações.

- [X] T006 [US1] Implementar o catálogo, a validação e a seleção dos defeitos em `src/Modulos/Simulador.Cenarios/Dominio/Avaliacao.cs`
- [X] T007 [US1] Implementar a aplicação no corpo (RN117) em `src/Modulos/Simulador.Cenarios/Dominio/Defeitos.cs`
- [X] T008 [US1] Sortear o defeito em toda chamada que chega à etapa e incluí-lo na decisão em `src/Modulos/Simulador.Cenarios/Aplicacao/ServicoDeCenarios.cs`
- [X] T009 [US1] Aplicar o defeito na saída, antes da falha de transporte, em `src/Simulador.Host/AdaptadoresDeCenarios.cs`
- [X] T010 [US1] Publicar `GET /admin/cenarios/defeitos` em `src/Modulos/Simulador.Cenarios/Http/RotasDeCenarios.cs`

## Phase 4: User Story 2 - Anomalias de protocolo (P1)

**Goal**: desvio nomeado ligado por banco. **Independent Test**: ligar e provocar cada anomalia.

- [X] T011 [US2] Implementar o catálogo, o cache e ligar e desligar as anomalias, com histórico, em `src/Modulos/Simulador.Cenarios/Aplicacao/ServicoDeCenarios.cs` e `Http/RotasDeCenarios.cs`
- [X] T012 [US2] Trocar 401 por 403 e o `x-fapi-interaction-id` em `PortaoPeloConsentimento`, em `src/Simulador.Host/AdaptadoresDeConsentimentos.cs`
- [X] T013 [US2] Responder 422 com chave ruim e marcar a página seguinte em `src/Modulos/Simulador.Dados/Aplicacao/ServicoDeDados.cs`
- [X] T014 [US2] Mostrar `anomaliasLigadas` em `src/Modulos/Simulador.Instituicoes/Http/RotasDeInstituicoes.cs`, pela porta `IConsultaDeAnomalias` implementada no host

## Phase 5: User Story 3 - Dado mudando na paginação (P2)

**Goal**: `totalRecords` muda entre as páginas. **Independent Test**: ler as páginas com a mesma chave.

- [X] T015 [US3] Implementar `dado-muda-na-paginacao` (RN115) em `src/Modulos/Simulador.Cenarios/Dominio/Defeitos.cs`

## Phase 6: Polish

- [X] T016 Testes de integração da administração e do cache em `tests/Simulador.Testes.Integracao/Cenarios/DefeitosEAnomaliasTests.cs`, inclusive anomalias simultâneas no mesmo banco
- [ ] T017 Rodar os portões: formatação, build, specs, testes, cobertura e `docker build`

## Dependencies

T001 → T002 → T003, T004, T005 → US1 → US2 → US3 → Polish. Dentro de cada história, domínio antes de aplicação e host.
