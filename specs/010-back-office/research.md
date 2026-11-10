# Research: Back-office

## R1. Servir o front na porta de administração (Q3)
- **Decision**: `BackOffice.cs` no host registra, dentro de `UseWhen(porta == administração)`, `UseDefaultFiles` e `UseStaticFiles` sobre `Simulador:PastaDoBackOffice` (padrão: `backoffice` ao lado do executável). Um `MapFallback` no grupo de administração devolve `index.html` para `GET` sem extensão que não começa com `/admin` nem `/saude`. Sem a pasta, nada é servido e nada muda.
- **Rationale**: mesma origem, sem CORS; a porta 8081 já fica em `127.0.0.1`. A condição pela porta do `Host` é a mesma que o `RequireHost` usa, então a porta 8080 nunca serve tela, e o 404 oficial continua igual.
- **Alternatives**: nginx (contêiner novo); CORS (abre a administração a outra origem).

## R2. Marca do ambiente (Q2)
- **Decision**: `GET /admin/ambiente` → `{ "marca": "hml" }`, mapeada em `Simulador.Marcacao` com a `MarcaDoAmbiente` já registrada. Cor: `dev` azul, `hml` âmbar, `stage` roxo, outra cinza; vazia, sem marca.
- **Rationale**: uma só fonte (RN66).

## R3. Vencedora de cenário (Q9)
- **Decision**: `SelecaoDeRegras.Vencedoras(regras, chamada, instante, declarados)` devolve as três vencedoras (comportamento, estado, defeito), reaproveitando `Vencedora`, `VencedoraDeEstado` e `VencedoraDeDefeito`. A rota lê as regras ativas pelo mesmo retrato com cache de ≤5 s do pipeline, a hora pelo relógio do banco (`RelogioDosBancos`) e os status declarados da operação (mesmo cálculo de `ComportamentoDaChamada`, extraído para um método estático). `instituicao` e `endpoint` obrigatórios (422); `cliente` e `recurso` opcionais; banco inexistente: 404; operação desconhecida: 422.
- **Rationale**: a ordem de RN15 fica num só lugar; nada é sorteado (sem efeito sobre `seq:` em Redis).
- **Alternatives**: repetir a ordem no front (contra o princípio I).

## R4. Node sem instalação (Q4)
- **Decision**: `front/scripts/docker.sh` roda `npm` em `node:22` com a pasta montada e `node_modules` num volume nomeado (`sim-front-node-modules`), para não escrever binários Linux na árvore do Windows. O CI usa `actions/setup-node` com Node 22. O Playwright roda em `mcr.microsoft.com/playwright:v<versão do pacote>-noble`, na rede do Compose.
- **Rationale**: nada instalado na máquina; mesmas versões no CI.

## R5. Versões
- **Decision**: ranges do guia, como `^`: React 18.3, TypeScript 5.x (o 7 está fora do `^5.5`), Webpack 5, React Router 6, Tailwind 3, Radix 1.x, Jest 30, ESLint 9, axios 1. O `package-lock.json` é versionado.

## R6. Camadas e lint
- **Decision**: `no-restricted-imports` do ESLint por pasta (`files` de cada camada), sem plugin novo: `core` não importa React, axios, zod, tsyringe nem outra camada; `application` só `core` (e `tsyringe` para os decorators, como no exemplo do guia); `infrastructure` não importa `presentation`; `presentation` não importa `infrastructure`.
- **Rationale**: a tabela de camadas do guia, com o que o ESLint já tem.

## R7. Either e erros
- **Decision**: `core/comum/either.ts` (`left`, `right`, `isLeft`, `isRight`). `Falha` é união discriminada: `rede`, `recusa` (status e `erros[]`), `inexistente`, `conflito`, `validacao`. O repositório HTTP traduz status e corpo `{ erros }` em `Falha`; a tela traduz códigos conhecidos (RN83, RN84, RN96) com o i18next e mostra os desconhecidos como vieram.

## R8. Estado e dados
- **Decision**: TanStack Query para toda leitura (chaves por área), mutações invalidam a lista e o detalhe. Zustand com Immer: passo e rascunho do menu de consentimento, autor (persistido em `localStorage`, Q8) e filtro da lista de regras.

## R9. Testes do front
- **Decision**: Jest com `jsdom`, `ts-jest` para `core` e `application` e `babel-jest` não é usado: `ts-jest` com `isolatedModules` cobre TSX também. `msw` 2 no Node para repositórios e páginas. `jest-html-reporter` e `jest-sonar-reporter` em `front/relatorios/`. Threshold: `core` 100%; global 80%.

## R10. Playwright (Q10)
- **Decision**: `front/e2e/` com `@playwright/test`, base `SIM_BACKOFFICE_URL` (padrão `http://localhost:8081`). Cada teste cria seus dados pela administração (receptora, cliente PF e PJ) com nomes únicos, para não depender de ordem. Cenários: os 9 de `consentimento-no-painel.feature` e "Ambiente visível nas telas". "Fim do prazo da alçada" usa o relógio do banco fixado 16 dias à frente (UC28), num banco criado pelo teste.
