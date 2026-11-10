# Implementation Plan: Back-office

**Branch**: `claude/010-back-office-d94942` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/010-back-office/spec.md`

## Summary

Entregar o back-office em `front/` (React 18, TypeScript, Webpack 5, Clean Architecture, guia de front-end) com as telas de bancos, consentimento no painel, receptoras, massa e cenários (Q1), servido pelo próprio simulador na porta de administração (Q3). No back-end entram duas rotas de leitura: `GET /admin/ambiente` (marca, Q2) e `GET /admin/cenarios/regras/vencedora` (UI06, Q9), e o host passa a servir os arquivos estáticos do front só na porta 8081. O CI ganha os jobs `front` (lint, tipos, testes com cobertura, build) e `painel` (Playwright contra o Compose) (Q4, Q10).

## Technical Context

**Language/Version**: C# em .NET 10 (back-end); TypeScript ^5.5 e React ^18.3 (front)

**Primary Dependencies**: back-end sem dependência nova (`Microsoft.AspNetCore.StaticFiles` já vem no framework). Front: só a stack do [guia](../../docs/08-front-end/README.md#stack); Playwright vem da constituição (testes) e da [estratégia](../../docs/06-testes/estrategia-de-testes.md)

**Storage**: nenhum novo. O front guarda só o nome do autor no `localStorage` (Q8)

**Testing**: xUnit e Testcontainers (rotas novas e arquivos estáticos); Jest 30, ts-jest 30, Testing Library e `msw` (front); Playwright (painel, Q10)

**Target Platform**: contêiner Linux (Docker Compose); navegador atual

**Project Type**: monólito modular + front web servido pelo host

**Performance Goals**: a tela não cria carga: listas com o tamanho que a API já pagina; acompanhamento de lote a cada 2 s, só enquanto ele está em andamento

**Constraints**: sem Node na máquina: comandos do front pela imagem `node:22` (Q4); não ativar `core.hooksPath`; invariantes das specs 007 a 009 intactos (cache de regras ≤5 s, chave `v2.`, corte da RN37, cota da RN27, visão admin da RN97, anomalias da RN113 e RN114); a rota de vencedora só lê

**Scale/Scope**: ~45 rotas de administração consumidas; 5 áreas de tela; 2 rotas novas; 10 cenários de navegador

## Constitution Check

| Princípio | Situação |
|---|---|
| I. `docs/` como fonte | OK: Q1 a Q10 registradas em `interface.md` (UI01, UI06) e no guia de front-end antes do plano. O front valida só formato e faixas dos contratos; regra fica no servidor (FR-007) |
| II. Regra oficial por padrão | OK: a tela não muda nenhum padrão; mostra os oficiais ao lado (UI04, UI05) |
| III. Desvio explícito | OK: anomalias e regras são ligadas pela tela e vão para o histórico, como pela API |
| IV. Tudo marcado | OK: a marca vem de `Simulador:MarcaDoAmbiente` por `GET /admin/ambiente`; sem segunda fonte (Q2, RN66) |
| V. Só responde | OK: nada é registrado; a vencedora não sorteia nem avança sequência |
| VI. Repetível | OK: a vencedora usa o relógio do banco (RN73) e não consome sorteio |
| VII. Specs oficiais | OK: nenhuma rota oficial muda; as novas ficam em `/admin` |
| VIII. Cliente sem mudança | OK: front só na porta de administração, nunca nas APIs Open Finance |
| IX. Testes primeiro | OK: testes de rota, de `core`, casos de uso, componentes e Playwright escritos antes da implementação de cada parte |
| X. Simplicidade | OK: sem contêiner novo e sem CORS (Q3); Playwright é da stack de testes da constituição, não lib nova do front |

Sem violações. Reavaliado após o desenho: sem mudança.

## Project Structure

### Documentation

```text
specs/010-back-office/
├── spec.md, perguntas.md, plan.md, research.md
├── data-model.md, quickstart.md, tasks.md
└── contracts/admin-back-office.md
```

### Source Code

```text
src/Modulos/Simulador.Marcacao/Contratos/ExtensoesDaMarcacao.cs   # MapearAmbiente: GET /admin/ambiente
src/Modulos/Simulador.Cenarios/Dominio/Avaliacao.cs               # SelecaoDeRegras.Vencedoras (as três)
src/Modulos/Simulador.Cenarios/Http/RotasDeCenarios.cs            # GET /admin/cenarios/regras/vencedora
src/Modulos/Simulador.Cenarios/Contratos/Modelos.cs               # IAmbienteDosCenarios: declarados por operação e hora do banco
src/Simulador.Host/AdaptadoresDeCenarios.cs                       # implementa os dois membros novos
src/Simulador.Host/BackOffice.cs                                  # arquivos estáticos e fallback só na porta 8081
src/Simulador.Host/Program.cs                                     # liga MapearAmbiente e o back-office
Dockerfile                                                        # estágio node:22 que gera front/dist
.github/workflows/ci.yml                                          # jobs front e painel
tests/Simulador.Testes.Unidade/Cenarios/VencedorasTests.cs
tests/Simulador.Testes.Integracao/Cenarios/VencedoraTests.cs
tests/Simulador.Testes.Integracao/Host/BackOfficeTests.cs
tests/Simulador.Testes.Integracao/Marcacao/AmbienteTests.cs

front/
├── package.json, package-lock.json, tsconfig.json, babel.config.js
├── jest.config.ts, eslint.config.mjs, .prettierrc, commitlint.config.js, .lintstagedrc.json
├── tailwind.config.js, postcss.config.js, .husky/ (sem ativação automática)
├── webpack/ webpack.common.ts, webpack.dev.ts, webpack.prod.ts
├── public/index.html
├── src/
│   ├── core/            comum (Either, Falha), instituicoes, consentimentos, massa, cenarios, ambiente
│   ├── application/     casos de uso por área
│   ├── infrastructure/  http (axios, cliente admin, mapa de erros), DTOs zod e repositórios por área
│   ├── presentation/    layout (topo, faixa, autor), páginas e componentes por área, hooks do TanStack Query, stores
│   └── main/            container tsyringe, tokens, rotas, index.tsx
├── tests/               espelha src/ (msw)
├── e2e/                 Playwright: painel.spec.ts, ambiente.spec.ts
└── scripts/docker.sh    roda npm pela imagem node:22
```

**Structure Decision**: back-end muda só onde a rota mora (Marcação e Cenários) e no host (arquivos estáticos). O front fica em `front/`, separado do .NET (ADR-010), e entra na imagem pelo estágio Node da `Dockerfile`.

## Complexity Tracking

Sem violações a justificar.
