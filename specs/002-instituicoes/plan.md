# Implementation Plan: Instituições simuladas

**Branch**: `claude/002-instituicoes-b8feb2` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-instituicoes/spec.md`

## Summary

Criar o módulo **Instituições**, com os 16 bancos prontos, os 5 perfis de comportamento, o
cadastro pela administração e o histórico, tudo no MongoDB. O endereço `{banco}` passa a ser
resolvido pelo cadastro, com 404 para desconhecido e 503 para inativo. A edição usa um único
`PATCH` em JSON Merge Patch. O sorteio de latência e erro fica para a 007, e os contadores de
limite, para a 006. Decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 em .NET 10 (Minimal API), [ADR-001](../../docs/05-adr/ADR-001-backend-dotnet-10.md)

**Primary Dependencies**: `MongoDB.Driver` (ADR-003) e, só nos testes, `Testcontainers.MongoDb` (ADR-009)

**Storage**: MongoDB, coleções `instituicoes` e `historico`

**Testing**: xUnit v3, Reqnroll, `WebApplicationFactory`, `FakeTimeProvider`, Testcontainers

**Target Platform**: contêiner Linux com Docker Compose (RNF11)

**Project Type**: serviço web, monólito modular

**Performance Goals**: resolver a instituição dentro dos 10 ms do caminho da chamada (RNF03)

**Constraints**: sem cache de configuração, para valer na próxima chamada em todas as instâncias (RN05, RN30); módulo não referencia outro módulo (ADR-007)

**Scale/Scope**: dezenas de instituições; 1 módulo novo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Como o plano atende | Situação |
|---|---|---|
| I. `docs/` é a fonte da verdade | Spec e plano citam UC01 a UC03, RN01 a RN13, RN20 a RN37, RN71, RF01 a RF03, UI02 a UI05 e as ADRs. As decisões novas entraram em `docs/` antes do código, com aprovação de Marcelo. | ✅ |
| II. Regra oficial por padrão | Todo banco nasce com os limites oficiais, exceto os do `restritivo`, escritos em `perfis-de-comportamento.md`. | ✅ |
| III. Desvio sempre explícito | Nenhuma anomalia nesta spec. O 503 de "fora do ar" está escrito em RN06, RN13 e RN16. | ✅ |
| IV. Tudo simulado e marcado | `organisationId` vem de `IMarcador`; o prefixo do nome vem só da marca do ambiente (RN09, RN66). | ✅ |
| V. Só responde; não julga nem registra | O histórico é de configuração, não de chamadas. O 503 não explica o motivo (RN18). | ✅ |
| VI. Repetível | `TimeProvider` e `IGeradorAleatorio` injetados. | ✅ |
| VII. Specs oficiais como fonte das APIs | Nenhuma rota oficial escrita à mão. Os status válidos saem do catálogo travado. | ✅ |
| VIII. Cliente sem mudança | Banco só pelo endereço. `x-sim-autor` existe apenas na administração. | ✅ |
| IX. Testes primeiro | `instituicoes.feature` sem a etiqueta da 002; testes escritos antes do código. | ✅ |
| X. Simplicidade | Só duas dependências novas, as duas previstas nas ADRs. Um `PATCH` no lugar de vários endpoints. Sem cache. | ✅ |
| Stack | MongoDB (ADR-003), Testcontainers (ADR-009). Nada trocado. | ✅ |
| Portões de CI | Inalterados. O CI usa Docker para os testes com contêiner. | ✅ |

**Re-check após a Fase 1:** `data-model.md` e `contracts/` não introduzem nada fora do previsto. Todos seguem ✅.

## Project Structure

### Documentation (this feature)

```text
specs/002-instituicoes/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/admin-instituicoes.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/Modulos/Simulador.Instituicoes/
├── Contratos/      # registros públicos, portas e registro no host
├── Dominio/        # endereço, nome, QCA, perfis, validação, avisos, diferenças e merge
├── Aplicacao/      # CadastroDeInstituicoes e semeadura
├── Persistencia/   # RepositorioMongo
└── Http/           # rotas de administração
src/Simulador.Host/AdaptadoresDeInstituicoes.cs   # liga Instituições a Catálogo e Marcação
src/Modulos/Simulador.Catalogo/                   # resolvedor passa a devolver a situação; 503
tests/ComumHost/RepositorioEmMemoria.cs           # banco-teste e testes sem contêiner
tests/Simulador.Testes.Unidade/Instituicoes/
tests/Simulador.Testes.Integracao/Instituicoes/
tests/Simulador.Testes.Aceitacao/Passos/InstituicoesPassos.cs
```

**Structure Decision**: um projeto novo em `src/Modulos/`. O teste de fronteiras proíbe referência entre módulos, então Instituições declara a porta `IAmbienteDasInstituicoes` e o Host a implementa com `ICatalogo` e `IMarcador`.

## Complexity Tracking

Nenhuma violação da constituição a justificar.
