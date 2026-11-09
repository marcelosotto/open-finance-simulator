# ADR-008: Pipeline único de avaliação da chamada

**Status:** Aceito
**Data:** 2026-11-05

## Contexto
A ordem dos limites e erros muda o resultado da chamada, e anomalias e defeitos multiplicam os desvios possíveis.

## Decisão
Cada verificação é uma etapa com contrato único e ordem fixa ([RN39](../01-requisitos/regras-de-negocio.md#rn39)). Anomalias trocam o comportamento de uma etapa, sem criar caminho paralelo. A cota é contada só em 2XX, quando a resposta já está decidida e antes da espera simulada; assim a espera não alarga a janela entre conferir e contar. Conexão cortada não conta.

## Consequências
Cada etapa é testada sozinha. Etapas no caminho quente precisam ser leves ([RNF03](../01-requisitos/requisitos.md#rnf03)). Chamadas simultâneas sobre o mesmo objeto podem ultrapassar a cota em poucas unidades; risco aceito.
