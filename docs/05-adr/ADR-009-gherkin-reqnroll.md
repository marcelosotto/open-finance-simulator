# ADR-009: Cenários Gherkin executados com Reqnroll

**Status:** Proposto
**Data:** 2026-11-05

## Contexto
No projeto de transporte, os `.feature` em `docs/` funcionaram como fonte única entre negócio e testes.

## Decisão
Executar os cenários de `docs/01-requisitos/gherkin/` com **Reqnroll** e xUnit, chamando a API real com MongoDB e Redis em contêiner. Etiquetas: `@fapi` para o modo FAPI e `@spec-NNN` para o que ainda não foi implementado.

## Consequências
Cenário novo nasce em `docs/` e vira teste. Sem passo implementado, o teste falha e lembra de implementar.
