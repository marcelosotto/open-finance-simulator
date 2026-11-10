# Implementation Plan: Defeitos e anomalias

**Branch**: `claude/009-defeitos-e-anomalias-44a40c` | **Date**: 2026-11-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/009-defeitos-e-anomalias/spec.md`

## Summary

Os defeitos de dado viram um efeito novo das regras de cenário da 007 (`defeito`). Eles disputam só entre si e são decididos na mesma etapa e com o mesmo cache. O efeito é aplicado ao corpo já montado, na saída da chamada, pelo filtro do host que já corrompe o corpo nas falhas de transporte (RN17). Assim, Dados e Consentimentos não mudam a montagem.

As anomalias são chaves por banco, guardadas em `cenarios` e lidas pelo mesmo cache de 5 s. Cada uma troca o comportamento de uma etapa existente (ADR-008):

- 401 e `x-fapi-interaction-id`: em volta do portão, no host, sem tocar em Consentimentos;
- chave de paginação: na etapa de parâmetros de Dados.

A administração das instituições passa a mostrar `anomaliasLigadas` por uma porta implementada no host.

## Technical Context

**Language/Version**: C# em .NET 10

**Primary Dependencies**: ASP.NET Minimal API; nenhuma dependência nova

**Storage**: MongoDB, coleção `cenarios` (regras de defeito como regras; `anomalia:{banco}:{nome}`); Redis só para a sequência de regras com semente, que já existe

**Testing**: xUnit, Reqnroll, Testcontainers

**Target Platform**: Contêiner Linux, Docker Compose

**Project Type**: Monólito modular

**Performance Goals**: RNF03. O defeito só reprocessa o corpo quando a chamada foi sorteada. As anomalias saem do cache em memória.

**Constraints**: ordem de RN39; semente injetada; módulos sem referência entre si (ADR-007); sem regressão das correções da 007 (cache de regras, chave `v2`, corte por hora real, cota no 2XX decidido)

**Scale/Scope**: 8 defeitos, 3 anomalias, 7 cenários Gherkin (14 casos)

## Constitution Check

| Princípio | Situação |
|---|---|
| I. `docs/` como fonte | OK: Q1 a Q13 registradas em RN110 a RN117, RN36, RN37, RN88, UC22 e no modelo de dados antes do código |
| II. Regra oficial por padrão | OK: sem regra e sem anomalia, nada muda (RN111, RN114) |
| III. Desvio explícito | OK: cada defeito e anomalia tem nome, nasce desligado e vai para o histórico |
| IV. Tudo marcado | OK: o corpo alterado continua no grupo com marcação |
| V. Só responde | OK: nada indica o defeito (RN112) |
| VI. Repetível | OK: frequência pela sequência da semente; datas pelo instante da chamada |
| VII. Specs oficiais | OK: alvo dos defeitos lido do schema travado; 403 e 422 declarados em todas as operações afetadas |
| VIII. Cliente sem mudança | OK: tudo na administração |
| IX. Testes primeiro | OK: domínio e Gherkin antes da implementação |
| X. Simplicidade | OK: sem dependência nova; reaproveita regra, cache e filtro da 007 |

Sem violações. Reavaliado após o desenho: sem mudança.

## Project Structure

### Documentation

```text
specs/009-defeitos-e-anomalias/
├── spec.md, perguntas.md, plan.md, research.md
├── data-model.md, quickstart.md, tasks.md
└── contracts/admin-defeitos-e-anomalias.md
```

### Source Code

```text
src/Simulador.Comum/Comportamento/        # nomes das anomalias, item da página seguinte, IAnomaliasDaChamada
src/Modulos/Simulador.Cenarios/           # efeito defeito, catálogos, aplicação no corpo, anomalias e API
src/Modulos/Simulador.Instituicoes/       # anomaliasLigadas na lista e no detalhe (porta IConsultaDeAnomalias)
src/Modulos/Simulador.Dados/              # 422 da anomalia de chave; marca de página seguinte
src/Simulador.Host/                       # filtro do defeito, portão com anomalias, adaptadores
tests/ComumHost/                          # repositório de cenários em memória com anomalias
tests/Simulador.Testes.Unidade/Cenarios/  # domínio dos defeitos e anomalias
tests/Simulador.Testes.Aceitacao/Passos/DefeitosPassos.cs
```

**Structure Decision**: tudo no módulo Cenários, ligado pelo host (ADR-007). Consentimentos não muda, porque a spec 008 trabalha nele em paralelo.

## Complexity Tracking

Sem violações a justificar.
