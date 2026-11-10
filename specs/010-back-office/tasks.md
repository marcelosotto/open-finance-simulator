# Tasks: Back-office

**Input**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/admin-back-office.md](contracts/admin-back-office.md)

**Tests**: obrigatórios (constituição, princípio IX). Em cada fase, os testes vêm antes da implementação.

Comandos do front sem Node local: `sh front/scripts/docker.sh <args do npm>` (R4).

## Phase 1: Setup

- [X] T001 Criar o projeto `front/` com `package.json` (scripts `dev`, `build`, `lint`, `typecheck`, `test`, `test:cov`, `e2e`, `ganchos`; sem `prepare`), `tsconfig.json` (`strict`, `experimentalDecorators`, `emitDecoratorMetadata`), `babel.config.js` (preset-env, preset-react, preset-typescript, decorators legacy, `babel-plugin-transform-typescript-metadata`), `.gitignore` e `package-lock.json`
- [X] T002 [P] Configurar o Webpack em `front/webpack/webpack.common.ts`, `webpack.dev.ts` (porta 3000, proxy de `/admin` para `http://localhost:8081`, react-refresh) e `webpack.prod.ts` (mini-css-extract, hash nos nomes), com `html-webpack-plugin`, `dotenv-webpack`, `copy-webpack-plugin` e `fork-ts-checker-webpack-plugin`
- [X] T003 [P] Configurar Tailwind, PostCSS e o CSS base em `front/tailwind.config.js`, `front/postcss.config.js` e `front/src/presentation/estilos/base.css`
- [X] T004 [P] Configurar o ESLint 9 com `typescript-eslint`, react, react-hooks, jest, testing-library e as regras de camada com `no-restricted-imports` (R6) em `front/eslint.config.mjs`; Prettier em `front/.prettierrc`
- [X] T005 [P] Configurar o Jest 30 (`ts-jest`, `jsdom`, `msw`, reporters, threshold 100% em `src/core/**` e 80% global) em `front/jest.config.ts` e `front/tests/setup.ts`
- [X] T006 [P] Husky, lint-staged e commitlint em `front/.husky/pre-commit`, `front/.husky/commit-msg`, `front/.lintstagedrc.json` e `front/commitlint.config.js`; o script `ganchos` ativa, nada ativa sozinho (Q4)
- [X] T007 [P] Scripts Docker em `front/scripts/docker.sh` (npm em `node:22`, `node_modules` em volume) e `front/scripts/playwright.sh` (imagem do Playwright na rede do Compose)

## Phase 2: Foundational

- [X] T008 [P] Testes de `Either` e `Falha` em `front/tests/core/comum/either.test.ts`
- [X] T009 [P] `Either`, `Falha` e erros de domínio em `front/src/core/comum/either.ts` e `front/src/core/comum/falha.ts`
- [X] T010 [P] Teste do cliente HTTP (status → `Falha`, `{ erros }`, rede fora, `x-sim-autor` só quando há autor) em `front/tests/infrastructure/http/cliente-admin.test.ts`
- [X] T011 Cliente axios da administração em `front/src/infrastructure/http/cliente-admin.ts`, com base em `SIM_ADMIN_API_URL` (vazio = mesma origem) validado com zod em `front/src/infrastructure/config.ts`
- [X] T012 Container tsyringe e tokens em `front/src/main/container.ts` e `front/src/main/tokens.ts`; ponto de entrada com `reflect-metadata`, QueryClient, i18next `pt-BR` e roteador em `front/src/main/index.tsx` e `front/src/main/rotas.tsx`
- [X] T013 Mensagens em português dos códigos conhecidos (RN83, RN84, RN96, RN99) em `front/src/presentation/i18n/pt-BR.ts` e componente `ListaDeErros` em `front/src/presentation/comum/ListaDeErros.tsx`
- [X] T014 [P] Componentes base (Botao, Campo, Dialogo de confirmação com Radix, Toast, Carregando, ErroComNovaTentativa) e `cn()` em `front/src/presentation/comum/`

## Phase 3: User Story 5 - Marca do ambiente e faixa (P1)

**Goal**: toda tela mostra a faixa de simulação e a marca quando não é vazia (UI01, UI16, RN72).

**Independent Test**: com `hml`, a marca aparece; com vazio, só a faixa.

- [X] T015 [P] [US5] Teste de integração de `GET /admin/ambiente` com marca `hml` e vazia em `tests/Simulador.Testes.Integracao/Marcacao/AmbienteTests.cs`
- [X] T016 [US5] Rota `GET /admin/ambiente` → `{ "marca" }` em `src/Modulos/Simulador.Marcacao/Contratos/ExtensoesDaMarcacao.cs` e ligação em `src/Simulador.Host/Program.cs`
- [X] T017 [P] [US5] Testes de `Ambiente` (cor: `dev`, `hml`, `stage` ou `neutra`; vazia = sem marca) e de `ObterAmbiente` em `front/tests/core/ambiente/` e `front/tests/application/ambiente/`
- [X] T018 [US5] `Ambiente`, caso de uso `ObterAmbiente` e repositório HTTP em `front/src/core/ambiente/`, `front/src/application/ambiente/` e `front/src/infrastructure/ambiente/`
- [X] T019 [P] [US5] Teste do layout (faixa sempre, marca com `hml`, nada com vazio, campo "Seu nome" persistido) em `front/tests/presentation/layout/Layout.test.tsx`
- [X] T020 [US5] Layout com topo, marca, faixa "Ambiente simulado, dados fictícios", navegação e campo de autor (store Zustand persistida, Q8) em `front/src/presentation/layout/`

## Phase 4: User Story 1 - Cadastro de bancos (P1) — MVP e modelo das telas

**Goal**: CRUD completo de bancos com SLA, avisos, histórico, anomalias e relógio (UI02 a UI05, RN114).

**Independent Test**: criar "Banco Azul", ajustar limite, ver aviso, ligar anomalia e ver o aviso na lista.

- [X] T021 [P] [US1] Testes das entidades de instituição (`temAnomalia`, `NovaInstituicao` com nome obrigatório aparado, relógio fixado = fator 0) em `front/tests/core/instituicoes/`
- [X] T022 [P] [US1] Testes dos casos de uso (listar, obter, criar, alterar, apagar, reativar, histórico, anomalias, relógio) com repositório falso, `Left` e `Right`, em `front/tests/application/instituicoes/`
- [X] T023 [P] [US1] Teste do repositório HTTP com `msw` em `front/tests/infrastructure/instituicoes/repositorio-http.test.ts`
- [X] T024 [US1] Entidades e contrato do repositório em `front/src/core/instituicoes/`
- [X] T025 [US1] Casos de uso em `front/src/application/instituicoes/`
- [X] T026 [US1] DTOs zod e repositório HTTP (instituições, perfis, histórico, anomalias, relógio) em `front/src/infrastructure/instituicoes/`
- [X] T027 [P] [US1] Testes de páginas: lista com aviso de anomalia (UI02), novo banco só com nome e perfil e abertura da edição (UI03, Q5), SLA dentro/fora (UI04), aviso de limite (UI05), apagar com confirmação e resultado, reativar, histórico, ligar anomalia, relógio, em `front/tests/presentation/instituicoes/`
- [X] T028 [US1] Hooks do TanStack Query e páginas `ListaDeBancos`, `NovoBanco`, `EdicaoDoBanco` (abas: geral, comportamento, limites, anomalias, relógio, histórico) em `front/src/presentation/instituicoes/`

## Phase 5: User Story 2 - Consentimento no painel (P1)

**Goal**: menu em passos, seleção de origem e alçadas (UI10, UI13, UI14), receptoras e busca por `consentId` (Q7).

**Independent Test**: criar PF pelo menu; criar PJ com 2 alçadas e aprovar duas vezes.

- [X] T029 [P] [US2] Testes do `RascunhoDeConsentimento` (`pedido()` com `RESOURCES_READ`; seleção só quando a seção existe; `origemFaltando`; prazo 1 a 12; alçadas 1 a 10 só PJ) e de `VisaoDoConsentimento.podeDecidir` em `front/tests/core/consentimentos/`
- [X] T030 [P] [US2] Testes dos casos de uso (opções, criar, consultar, aprovar, recusar, revogar, receptoras) em `front/tests/application/consentimentos/`
- [X] T031 [P] [US2] Teste do repositório HTTP com `msw` (400, 404, 409, 422 com vários códigos) em `front/tests/infrastructure/consentimentos/`
- [X] T032 [US2] Entidades em `front/src/core/consentimentos/`, casos de uso em `front/src/application/consentimentos/`, DTOs e repositório em `front/src/infrastructure/consentimentos/`
- [X] T033 [P] [US2] Testes de páginas: passos do menu, grupos abríveis com permissões desmarcáveis (Q6), contas e cartões marcados, seção ausente sem opção, agrupados sem seleção, PF só grupos PF, bloqueio sem origem, recusa com código, visão com aprovar e recusar, botões ausentes sem pendência, busca por `consentId`, receptoras, em `front/tests/presentation/consentimentos/`
- [X] T034 [US2] Store do menu (Zustand com Immer) e páginas `NovoConsentimento`, `Consentimento`, `BuscaDeConsentimento`, `Receptoras` em `front/src/presentation/consentimentos/`

## Phase 6: User Story 3 - Massa (P2)

**Goal**: perfis, distribuições (UI07), lotes, cliente e limpeza.

**Independent Test**: distribuição que soma 100, lote de 3 acompanhado até o fim, cliente do lote aberto.

- [X] T035 [P] [US3] Testes de `Distribuicao` (`somaDeBancos`, `somaDePerfis`, `podeSalvar` só com as duas somas 100 e cada % entre 0 e 100; produtos sem soma) e de `Lote.emAndamento` em `front/tests/core/massa/`
- [X] T036 [P] [US3] Testes dos casos de uso e do repositório HTTP em `front/tests/application/massa/` e `front/tests/infrastructure/massa/`
- [X] T037 [US3] Entidades, casos de uso, DTOs e repositório em `front/src/core/massa/`, `front/src/application/massa/` e `front/src/infrastructure/massa/`
- [X] T038 [P] [US3] Testes de páginas (perfis prontos sem editar, soma e bloqueio, lote com andamento e cancelar, clientes paginados, cliente com consentimentos e link para a visão, limpeza com prévia) em `front/tests/presentation/massa/`
- [X] T039 [US3] Páginas `PerfisDeDados`, `Distribuicoes`, `Lotes`, `Lote`, `NovoCliente`, `Cliente`, `Limpeza` em `front/src/presentation/massa/`

## Phase 7: User Story 4 - Cenários (P2)

**Goal**: regras, pacotes e vencedora (UI06, RN15, Q9).

**Independent Test**: latência no banco e status no endpoint; a vencedora aponta a de endpoint.

- [X] T040 [P] [US4] Testes de unidade de `SelecaoDeRegras.Vencedoras` (as três vencedoras; estado e defeito não disputam com comportamento; status não declarado não vence; fora do período não vence; regra desligada ou de pacote desligado fora) em `tests/Simulador.Testes.Unidade/Cenarios/VencedorasTests.cs`
- [X] T041 [P] [US4] Testes de integração de `GET /admin/cenarios/regras/vencedora` (200 com a de endpoint vencendo a de banco; `instante` pelo relógio fixado do banco; 404 banco; 422 sem `endpoint` e com endpoint desconhecido; nada sorteado: a sequência da regra com semente não avança) em `tests/Simulador.Testes.Integracao/Cenarios/VencedoraTests.cs`
- [X] T042 [US4] `SelecaoDeRegras.Vencedoras` em `src/Modulos/Simulador.Cenarios/Dominio/Avaliacao.cs`; membros `StatusDeclaradosDe` e `HoraDoBanco` em `IAmbienteDosCenarios` (`src/Modulos/Simulador.Cenarios/Contratos/Modelos.cs`); `ServicoDeCenarios.Vencedoras`; rota em `src/Modulos/Simulador.Cenarios/Http/RotasDeCenarios.cs`; implementação em `src/Simulador.Host/AdaptadoresDeCenarios.cs`, com o cálculo de declarados extraído e compartilhado com `ComportamentoDaChamada`
- [X] T043 [P] [US4] Testes de `RegraDeCenario.alcance`, casos de uso e repositório HTTP em `front/tests/core/cenarios/`, `front/tests/application/cenarios/` e `front/tests/infrastructure/cenarios/`
- [X] T044 [US4] Entidades, casos de uso, DTOs e repositório em `front/src/core/cenarios/`, `front/src/application/cenarios/` e `front/src/infrastructure/cenarios/`
- [X] T045 [P] [US4] Testes de páginas (lista por alcance, nova regra por efeito, `defeito` com catálogo e campo, ligar e desligar, pacote com recusa ao apagar, painel "qual regra vale?") em `front/tests/presentation/cenarios/`
- [X] T046 [US4] Páginas `Regras`, `EdicaoDeRegra`, `Pacotes`, `Vencedora` em `front/src/presentation/cenarios/`

## Phase 8: Servir, empacotar e provar no navegador

- [X] T047 [P] Testes de integração do back-office: `GET /` e rota da SPA na 8081 devolvem `index.html`; arquivo do build servido; `/admin/inexistente` continua 404 JSON; porta 8080 não serve tela; sem pasta, nada muda; em `tests/Simulador.Testes.Integracao/Host/BackOfficeTests.cs`
- [X] T048 `BackOffice.cs` (R1) em `src/Simulador.Host/BackOffice.cs` e ligação em `src/Simulador.Host/Program.cs`
- [X] T049 Estágio `node:22` na `Dockerfile` (npm ci, build) e cópia de `front/dist` para `/app/backoffice`; `.dockerignore` sem `front/node_modules`
- [X] T050 Playwright em `front/playwright.config.ts`, `front/e2e/apoio.ts`, `front/e2e/painel.spec.ts` (os 9 cenários de `consentimento-no-painel.feature`) e `front/e2e/ambiente.spec.ts` (`@tela` de `marcacao.feature`)
- [X] T051 Jobs `front` (setup-node 22, npm ci, lint, typecheck, test:cov, build) e `painel` (Compose, Playwright) em `.github/workflows/ci.yml`

## Phase 9: Polish

- [X] T052 Atualizar `docs/06-testes/estrategia-de-testes.md` (onde rodam os testes do front e do painel) e o quickstart com o que mudou na implementação
- [ ] T053 Rodar os portões: .NET (restore, format, build `-warnaserror`, specs baixar e verificar, test com cobertura, cobertura conferir, docker build) e front (lint, typecheck, test:cov, build, Playwright)

## Dependencies

- Setup → Foundational → US5 (layout usado por todas) → US1 (modelo das telas) → US2, US3, US4 em qualquer ordem → Phase 8 → Polish.
- T042 depende de T040 e T041; T048 de T047; T050 de T048 e T049.

## Parallel Opportunities

- Setup: T002 a T007 em paralelo depois de T001.
- Em cada história, os testes `[P]` de `core`, `application` e `infrastructure` rodam juntos.
- US3 e US4 (front) podem andar em paralelo depois de US1; T040 a T042 (back-end) em paralelo com qualquer fase do front.

## Implementation Strategy

MVP = Setup + Foundational + US5 + US1: o cadastro de bancos completo é o padrão das outras telas (guia, regra 10). Depois US2 (P1), US3 e US4 (P2), e por fim empacotamento e Playwright.

## Phase 10: Usabilidade (Q11 a Q14)

- [ ] T054 [P] Testes de integração de `GET /admin/massa/clientes?banco=` (lista por banco em ordem, paginação com `temMais`, sem banco lista todos, 422 fora da faixa, 404 banco inexistente) e de `GET /admin/massa/resumo` em `tests/Simulador.Testes.Integracao/Massa/ClientesDoBancoTests.cs`, também com MongoDB
- [ ] T055 Rotas, serviço e repositório (MongoDB e memória) em `src/Modulos/Simulador.Massa/` e `tests/ComumHost/RepositorioDeMassaEmMemoria.cs`
- [ ] T056 [P] Testes da receptora padrão (sobe com `XP`; nome, id e credenciais configuráveis; idempotente ao subir de novo com o mesmo MongoDB; nome vazio desliga; histórico com autor `sistema`; token emitido com as credenciais) em `tests/Simulador.Testes.Integracao/Consentimentos/ReceptoraPadraoTests.cs`
- [ ] T057 Receptora padrão no início do módulo Consentimentos (RN124), desligada por padrão só na fábrica de testes
- [ ] T058 Front: caso de uso e repositório de clientes do banco e resumo; componente `SeletorDeCliente` (UI19) usado no menu de consentimento, na massa e nos cenários; mensagens claras para 404
- [ ] T059 Front: menu de consentimento em banco, cliente e receptora (receptora só com mais de uma) (UI10)
- [ ] T060 Front: ajuda no topo de cada tela (UI17) e página Primeiros passos como página inicial (UI18)
- [ ] T061 `docs/08-front-end/guia-de-uso.md` em português simples
- [ ] T062 Testes do front (core, casos de uso, repositório, telas) e Playwright atualizado (seletor de cliente, receptora padrão, primeiros passos)
- [ ] T063 Rodar todos os portões de novo
