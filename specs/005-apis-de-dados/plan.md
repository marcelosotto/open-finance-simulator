# Implementation Plan: APIs de dados

**Branch**: `claude/005-apis-de-dados-874b2a` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), com as decisões Q1 a Q10.

## Summary

Os 69 endpoints de dados deixam de responder 501. Um módulo novo, `Simulador.Dados`, atende as 13 famílias com um único manipulador: lê a forma da resposta do contrato travado, preenche com a massa (identificadores, transações, saldo, faturas) e aplica a paginação oficial. Contas e cartão têm dados coerentes; nas demais famílias o dado sai do schema, sem regra por campo (Q5). A massa ganha o estado da transação (RN44), lançamentos futuros em Contas (até 30 dias) e congelamento de ajustes pelo instante da chave (RN37).

## Technical Context

**Language/Version**: C# / .NET 10 (Minimal API)

**Primary Dependencies**: as já existentes; nenhuma nova

**Storage**: MongoDB, via massa; nada novo é gravado

**Testing**: xUnit, Reqnroll, Testcontainers

**Target Platform**: Docker Compose

**Project Type**: serviço web, monólito modular

**Performance Goals**: RNF01 a RNF12; consultas sem data futura mantêm o custo atual (Q7)

**Constraints**: chave de paginação não gravada; dado virtual não gravado; relógio e semente injetados

**Scale/Scope**: 69 operações, 13 contratos

## Constitution Check

| Princípio | Situação |
|---|---|
| I. `docs/` é a fonte | Decisões registradas antes do plano (`61e0205`) |
| II. Regra oficial | Mínimo, janelas e `meta` seguem o contrato |
| III. Desvio explícito | Anomalia RN113 fica em `@spec-009` |
| IV. Marcação | IDs com o prefixo do recurso; `transactionName` e nome marcados |
| V. Só responde | Nenhuma chamada é registrada |
| VI. Repetível | Sem sorteio próprio; tudo deriva de recurso, dia e semente |
| VII. Specs como fonte | Formato e mínimo lidos do contrato; rota nenhuma à mão |
| VIII. Cliente sem mudança | Mesmos caminhos; identidade do token |
| IX. Testes primeiro | Cenários aprovados; validação dos 69 endpoints contra o schema |
| X. Simplicidade | Um manipulador e um gerador, em vez de 69 rotas |

Sem exceções a justificar.

## Project Structure

```text
src/Modulos/Simulador.Dados/
├── Contratos/    # IAmbienteDosDados, registro do módulo
├── Dominio/      # Paginação, janelas de data, chave, famílias
└── Aplicacao/    # Manipulador e gerador por schema
src/Simulador.Host/AdaptadoresDeDados.cs
src/Modulos/Simulador.Catalogo/  # manipulador por família
src/Modulos/Simulador.Massa/     # estado, futuros, saldo, ajustes até o instante
src/Modulos/Simulador.Consentimentos/  # escrita condicional do estado do recurso
tests/  # unidade, integração e passos Gherkin
```

**Structure Decision**: módulo próprio, ligado aos demais pelo host (ADR-007).

## Artefatos

[research.md](research.md), [data-model.md](data-model.md), [contracts/comportamento-dos-dados.md](contracts/comportamento-dos-dados.md), [quickstart.md](quickstart.md).
