# Implementation Plan: Consentimento no painel

**Branch**: `claude/008-consentimento-no-painel-997cad` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/008-consentimento-no-painel/spec.md`

## Summary

Entregar a API de administração do painel de consentimentos (Q1): opções do menu, criação com seleção de origem e prazo, consulta, e aprovação e recusa de múltiplas alçadas. Tudo fica no módulo `Simulador.Consentimentos`, com as regras puras em `Dominio/Painel.cs` e `Dominio/Alcadas.cs`. O estado do recurso no consentimento (RN97) é calculado na leitura por uma função pública do módulo, que o host aplica antes da regra de cenário, em Recursos e nas APIs de produto, sem tocar no módulo Dados.

## Technical Context

**Language/Version**: C# em .NET 10

**Primary Dependencies**: ASP.NET Minimal API; nenhuma dependência nova

**Storage**: MongoDB, coleção `consentimentos` (dois campos opcionais novos: alçada recusada e decisões); nada em Redis

**Testing**: xUnit, Reqnroll, Testcontainers

**Target Platform**: Contêiner Linux, Docker Compose

**Project Type**: Monólito modular

**Performance Goals**: RNF03: o cálculo de RN97 é uma função pura sobre o consentimento já lido pelo portão

**Constraints**: módulos sem referência entre si (ADR-007); relógio do banco (RN73); sem mexer nos módulos Cenários e Dados, que a 009 altera em paralelo

**Scale/Scope**: 5 rotas de administração; 11 cenários Gherkin

## Constitution Check

| Princípio | Situação |
|---|---|
| I. `docs/` como fonte | OK: Q1 a Q9 registradas em `docs/` (RN95 a RN99, UC10, UC11) antes do plano |
| II. Regra oficial por padrão | OK: permissões validadas pelas mesmas regras de `POST /consents` (RN08, RN83, RN84) |
| III. Desvio explícito | OK: criação e decisões vão para o histórico; o painel só existe na administração |
| IV. Tudo marcado | OK: consentimento com marca e prefixo, como o da API |
| V. Só responde | OK: nada registra chamadas |
| VI. Repetível | OK: prazo da alçada pelo relógio do banco, sem processo agendado (Q2) |
| VII. Specs oficiais | OK: Recursos e APIs de produto seguem os schemas; estados do enum oficial |
| VIII. Cliente sem mudança | OK: a receptora usa as mesmas APIs e o mesmo token |
| IX. Testes primeiro | OK: domínio e Gherkin escritos antes da implementação |
| X. Simplicidade | OK: sem dependência nova, sem job; estado calculado |

Sem violações. Reavaliado após o desenho: sem mudança.

## Project Structure

### Documentation

```text
specs/008-consentimento-no-painel/
├── spec.md, perguntas.md, plan.md, research.md
├── data-model.md, quickstart.md, tasks.md
└── contracts/admin-painel.md
```

### Source Code

```text
src/Modulos/Simulador.Consentimentos/
├── Dominio/Painel.cs          # grupos nomeados, opções, validação do pedido (RN95, RN96)
├── Dominio/Alcadas.cs         # situação, estado no consentimento, decisões (RN97 a RN99)
├── Http/Painel.cs             # rotas /admin/consentimentos (contracts/admin-painel.md)
├── Http/Rotas.cs              # Recursos usa o estado no consentimento
└── Contratos/                 # campos novos, RecursoNoConsentimento, função pública de RN97
src/Modulos/Simulador.Massa/Contratos/Modelos.cs   # campos opcionais no consentimento
src/Simulador.Host/AdaptadoresDeConsentimentos.cs  # mapeia campos e aplica RN97 antes da regra
src/Simulador.Host/AdaptadoresDeDados.cs           # aplica RN97 com o consentimento da chamada
tests/Simulador.Testes.Unidade/Consentimentos/PainelTests.cs
tests/Simulador.Testes.Integracao/Consentimentos/PainelTests.cs
tests/Simulador.Testes.Aceitacao/Passos/PainelPassos.cs
```

**Structure Decision**: o painel é parte do módulo Consentimentos (C3: "criação no painel, máquina de estados, permissões, alçadas"). `Program.cs` não muda: as rotas entram por `MapearConsentimentos`.

## Complexity Tracking

Sem violações a justificar.
