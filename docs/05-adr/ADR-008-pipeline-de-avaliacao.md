# ADR-008: Pipeline único de avaliação da chamada

**Status:** Proposto
**Data:** 2026-11-05

## Contexto
Na POC, a ordem dos limites e erros está numa cadeia de `if` dentro de um método. Anomalias e defeitos multiplicariam esses desvios.

## Decisão
Cada verificação é uma etapa com contrato único e ordem fixa ([RN39](../01-requisitos/regras-de-negocio.md#rn39)). Anomalias trocam o comportamento de uma etapa, sem criar caminho paralelo. A cota é contada depois da resposta, só em 2XX.

## Consequências
Cada etapa é testada sozinha, e o registro de chamada mostra o que cada uma decidiu. Etapas no caminho quente precisam ser leves ([RNF03](../01-requisitos/requisitos.md#rnf03)).
