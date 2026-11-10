# ADR-008: Pipeline único de avaliação da chamada

**Status:** Aceito
**Data:** 2026-11-05

## Contexto
A ordem dos limites e erros muda o resultado da chamada, e anomalias e defeitos multiplicam os desvios possíveis.

## Decisão
Cada verificação é uma etapa com contrato único e ordem fixa ([RN39](../01-requisitos/regras-de-negocio.md#rn39)). Anomalias trocam o comportamento de uma etapa, sem criar caminho paralelo: `revogado-com-403` e `interacao-diferente` mudam o resultado do portão de segurança, e `chave-recusada-com-422` muda a etapa de parâmetros, antes da cota ([RN113](../01-requisitos/regras-de-negocio.md#rn113)). Se a leitura das anomalias falha, a etapa segue o comportamento oficial. Defeitos de dado não são etapa: a etapa de regra de cenário decide o defeito junto com o resto, e ele é aplicado na saída, sobre a resposta 2XX já decidida e contada, antes da falha de transporte ([RN111](../01-requisitos/regras-de-negocio.md#rn111), [RN116](../01-requisitos/regras-de-negocio.md#rn116)). A cota é contada só em 2XX, quando a resposta já está decidida e antes da espera simulada; assim a espera não alarga a janela entre conferir e contar. Conexão cortada não conta. Quem desiste durante a espera simulada já consumiu a unidade. Marcelo manteve assim para evitar a corrida que deixava passar do limite mensal: contar só depois da espera deixava várias chamadas conferirem a mesma folga durante a espera e passarem todas.

## Consequências
Cada etapa é testada sozinha. Etapas no caminho quente precisam ser leves ([RNF03](../01-requisitos/requisitos.md#rnf03)). Chamadas simultâneas sobre o mesmo objeto podem ultrapassar a cota em poucas unidades; risco aceito.
