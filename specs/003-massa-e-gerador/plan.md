# Implementation Plan: Massa e gerador

**Branch**: `claude/003-massa-e-gerador-9b2f94` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-massa-e-gerador/spec.md`

## Summary

Criar o módulo **Massa**, com perfis de dados, distribuição, gerador determinístico, cliente avulso, lote em segundo plano, mundo virtual, ajustes e limpeza, tudo pela administração e gravado no MongoDB. As APIs Open Finance ficam nas specs 004 e 005, que usarão a porta `IConsultaDeMassa`. Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 em .NET 10 (Minimal API), [ADR-001](../../docs/05-adr/ADR-001-backend-dotnet-10.md)

**Primary Dependencies**: `MongoDB.Driver`, já no projeto. Nenhuma dependência nova.

**Storage**: MongoDB, coleções `perfis_dados`, `distribuicoes`, `clientes`, `consentimentos`, `recursos`, `mundo_virtual`, `ajustes`, `lotes` e `contadores`; o histórico usa a coleção `historico` da spec 002.

**Testing**: xUnit v3, Reqnroll, `WebApplicationFactory`, `FakeTimeProvider`, Testcontainers

**Target Platform**: contêiner Linux com Docker Compose (RNF11)

**Project Type**: serviço web, monólito modular

**Performance Goals**: item virtual em O(1), sem varrer o mundo; mundo de pelo menos 10 milhões de itens (RNF06)

**Constraints**: nenhum sorteio fora da semente (princípio VI); módulo não referencia outro módulo (ADR-007); 100 milhões de CPFs no total (RN57)

**Scale/Scope**: 1 módulo novo; lotes em blocos de 500 clientes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Como o plano atende | Situação |
|---|---|---|
| I. `docs/` é a fonte da verdade | Spec e plano citam UC05 a UC08, UC27, RN46, RN49 a RN66, RN71, RN80, RN85. As decisões do clarify entraram em `docs/` antes do código. | ✅ |
| II. Regra oficial por padrão | Consentimento nasce `AUTHORISED`, diferença escrita em RN80. Tipos e permissões saem das specs travadas. | ✅ |
| III. Desvio sempre explícito | Nenhuma anomalia nesta spec. | ✅ |
| IV. Tudo simulado e marcado | IDs e nome passam por `IMarcador`; o documento nunca é informado (RNF10). | ✅ |
| V. Só responde; não julga nem registra | O histórico é de configuração, não de chamadas. | ✅ |
| VI. Repetível | Semente explícita, relógio injetado; só total, semente, distribuição e ajustes são gravados no mundo. | ✅ |
| VII. Specs oficiais como fonte das APIs | Nenhuma rota oficial nesta spec. | ✅ |
| VIII. Cliente sem mudança | Tudo na porta de administração. | ✅ |
| IX. Testes primeiro | Sai a etiqueta `@spec-003`; testes antes do código e domínio com 100%. | ✅ |
| X. Simplicidade | Nenhuma dependência nova; identidade sequencial em vez de sorteio com colisão. | ✅ |
| Stack | MongoDB (ADR-003). Nada trocado. | ✅ |
| Portões de CI | Inalterados. | ✅ |

**Re-check após a Fase 1:** `data-model.md` e `contracts/` não introduzem nada fora do previsto. Todos seguem ✅.

## Project Structure

### Documentation (this feature)

```text
specs/003-massa-e-gerador/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/admin-massa.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/Modulos/Simulador.Massa/
├── Contratos/      # registros públicos, portas e registro no host
├── Dominio/        # sorteio, documentos, perfis, validação, gerador, transações, virtual
├── Aplicacao/      # casos de uso, execução do lote, limpeza
├── Persistencia/   # RepositorioMongo
└── Http/           # rotas de administração
src/Modulos/Simulador.Marcacao/                # identificador com corpo determinístico
src/Simulador.Host/AdaptadoresDaMassa.cs       # liga Massa a Instituições e Marcação
tests/ComumHost/RepositorioDeMassaEmMemoria.cs
tests/Simulador.Testes.Unidade/Massa/
tests/Simulador.Testes.Integracao/Massa/
tests/Simulador.Testes.Aceitacao/Passos/MassaPassos.cs
```

**Structure Decision**: um projeto novo em `src/Modulos/`. O teste de fronteiras proíbe referência entre módulos, então Massa declara `IAmbienteDaMassa` e o Host a implementa com `IMarcador`, `IConsultaDeInstituicoes` e `IRepositorioDeInstituicoes`. A Massa também implementa `IConsultaDeClientes`, pelo Host.

## Complexity Tracking

Nenhuma violação da constituição a justificar.
