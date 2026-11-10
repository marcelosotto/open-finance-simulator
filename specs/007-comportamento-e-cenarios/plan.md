# Implementation Plan: Comportamento e cenários

**Branch**: `claude/007-comportamento-e-cenarios-629767` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/007-comportamento-e-cenarios/spec.md`

## Summary

Aplicar perfis, regras de cenário, timeout, indisponibilidade e relógio por instituição. Regras e avaliação ficam em um módulo novo, `Simulador.Cenarios`. O relógio do banco chega a todos os módulos pelo `TimeProvider` já injetado, que devolve um instante único por chamada; TPM, capacidade e esperas usam `HoraReal`. A etapa de comportamento roda no ponto de RN39, antes de qualquer gravação.

## Technical Context

**Language/Version**: C# em .NET 10

**Primary Dependencies**: ASP.NET Minimal API; nenhuma dependência nova

**Storage**: MongoDB, coleção `cenarios` (regras e pacotes) e campo `relogio` em `instituicoes`; Redis para a sequência de regras com semente

**Testing**: xUnit, Reqnroll, Testcontainers

**Target Platform**: Contêiner Linux, Docker Compose

**Project Type**: Monólito modular

**Performance Goals**: RNF03 (10 ms no p95): regras em cache de até 5 s por instância; instituição lida uma vez por chamada

**Constraints**: ordem de RN39; relógio e semente injetados; módulos sem referência entre si (ADR-007)

**Scale/Scope**: 75 operações; 17 cenários Gherkin

## Constitution Check

| Princípio | Situação |
|---|---|
| I. `docs/` como fonte | OK: Q1 a Q18 registradas antes do plano |
| II. Regra oficial por padrão | OK: status fora do contrato não saem de regra (Q4), salvo 503, exceção de RN13 |
| III. Desvio explícito | OK: toda regra e relógio entram no histórico; perfil sem regra só usa o declarado |
| IV. Tudo marcado | OK: as respostas seguem no grupo com marcação |
| V. Só responde | OK: erro genérico; nada registra chamadas (RN18) |
| VI. Repetível | OK: relógio, sorteio e espera injetados; semente por regra |
| VII. Specs oficiais | OK: status declarados vêm do catálogo; enum de 422 em Consentimentos respeitado |
| VIII. Cliente sem mudança | OK: regras e relógio na administração |
| IX. Testes primeiro | OK: domínio e Gherkin escritos antes da implementação |
| X. Simplicidade | OK: sem dependência nova, sem processo agendado (Q2) |

Sem violações. Reavaliado após o desenho: sem mudança.

## Project Structure

### Documentation

```text
specs/007-comportamento-e-cenarios/
├── spec.md, perguntas.md, plan.md, research.md
├── data-model.md, quickstart.md, tasks.md
└── contracts/admin-cenarios.md
```

### Source Code

```text
src/Simulador.Comum/Comportamento/          # HoraReal, IComportamentoDaChamada, etapa
src/Modulos/Simulador.Cenarios/             # novo módulo
├── Contratos/, Aplicacao/, Dominio/, Persistencia/, Http/
src/Modulos/Simulador.Instituicoes/         # relógio, pesos (Q1)
src/Modulos/Simulador.Catalogo/             # operação da chamada, 503 renomeado
src/Modulos/Simulador.Consentimentos/       # etapa antes das gravações, relógio do banco na revogação
src/Modulos/Simulador.Dados/                # etapa depois da cota, RN109
src/Modulos/Simulador.Limites/              # TPM e capacidade em HoraReal
src/Modulos/Simulador.Massa/                # ajustes e estado no relógio do banco
src/Simulador.Host/                         # RelogioDaChamada, filtro, adaptadores
tests/ComumHost/                            # espera registrada, comportamento desligado por padrão
tests/Simulador.Testes.Aceitacao/Passos/ComportamentoPassos.cs
```

**Structure Decision**: módulo próprio, como C3 prevê ("Cenários"), ligado pelo host (ADR-007).

## Complexity Tracking

Sem violações a justificar.
