# Implementation Plan: Limites

**Branch**: `claude/cool-hawking-9037b3` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/006-limites/spec.md`

## Summary

Entregar capacidade (529), tráfego (429) e limite operacional (423) em Redis. Capacidade e tráfego formam uma etapa única, logo após o portão do token, para as 75 operações. A cota fica no serviço de dados, depois de acesso ao recurso e parâmetros, porque depende de ambos e do resultado da chave.

## Technical Context

**Language/Version**: C# em .NET 10

**Primary Dependencies**: ASP.NET Minimal API; novo `StackExchange.Redis`, autorizado pelo ADR-003

**Storage**: Redis para contadores; nenhuma coleção nova no MongoDB

**Testing**: xUnit, Reqnroll, Testcontainers (novo `Testcontainers.Redis`)

**Target Platform**: Contêiner Linux, Docker Compose

**Project Type**: Monólito modular

**Performance Goals**: RNF02 (300 req/s por instância) e RNF03 (10 ms no p95)

**Constraints**: tempo máximo curto no Redis e falha rápida (Q8); relógio injetado; módulos sem referência entre si (ADR-007)

**Scale/Scope**: 75 operações; 69 com TPM e cota

## Constitution Check

| Princípio | Situação |
|---|---|
| I. `docs/` como fonte | OK: Q1 a Q11 registradas antes do plano |
| II. Regra oficial por padrão | OK: limites oficiais já são o padrão da instituição (spec 002) |
| III. Desvio explícito | OK: falha do Redis não gera erro (Q8) |
| IV. Tudo marcado | OK: recusas passam pelo mesmo grupo de rotas com marcação |
| V. Só responde | OK: contadores guardam totais, sem registro de chamada |
| VI. Repetível | OK: `TimeProvider` injetado em todas as janelas |
| VII. Specs oficiais | OK: 529 aplicado por declaração do contrato; classes vêm da tabela |
| VIII. Cliente sem mudança | OK: receptora vem do token |
| IX. Testes primeiro | OK: Gherkin aprovado antes; cobertura mantida |
| X. Simplicidade | OK: uma dependência já decidida; sem reserva de cota (Q9) |

Sem violações. Reavaliado após o desenho: sem mudança.

## Project Structure

### Documentation

```text
specs/006-limites/
├── spec.md, perguntas.md, plan.md, research.md
├── data-model.md, quickstart.md, tasks.md
└── contracts/respostas-de-limite.md
```

### Source Code

```text
src/Modulos/Simulador.Limites/      # novo módulo
├── Contratos/                      # IContadores, ILimites, registro
├── Aplicacao/ServicoDeLimites.cs
├── Dominio/                        # janelas, chaves, objeto da cota
└── Persistencia/ContadoresRedis.cs
src/Modulos/Simulador.Catalogo/     # IEtapaDeEntrada no Despachante
src/Modulos/Simulador.Consentimentos/ # expõe a receptora da chamada
src/Modulos/Simulador.Dados/        # etapa da cota e resultado da chave
src/Simulador.Host/AdaptadoresDeLimites.cs
tests/ComumHost/                    # contadores em memória
tests/Simulador.Testes.Aceitacao/Passos/LimitesPassos.cs
docker-compose.yml                  # serviço redis
```

**Structure Decision**: módulo próprio, como C3 prevê ("Limites"), ligado pelo host (ADR-007).

## Complexity Tracking

Sem violações a justificar.
