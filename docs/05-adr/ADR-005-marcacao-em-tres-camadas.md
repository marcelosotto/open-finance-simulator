# ADR-005: Marcação em três camadas

**Status:** Proposto
**Data:** 2026-11-05

## Contexto
Queremos saber sempre que um dado é sintético, de qual ambiente e se é virtual. Só um cabeçalho se perde quando o dado é gravado por outro sistema. Um campo novo no JSON desviaria da spec oficial.

## Decisão
Marcar em três lugares: **cabeçalho** da resposta, **prefixo dos identificadores** e **texto** de campos livres ([RN60](../01-requisitos/regras-de-negocio.md#rn60)).

Exemplos em `hml`:
- `x-sim-environment: hml` e `x-sim-data-nature: virtual`
- `accountId`: `hml-v-1a2b3c`, dentro do padrão `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$` da spec
- `consentId`: `urn:sim-hml:…`
- `transactionName`: `[SIM-HML] PIX recebido`

## Consequências
A marca sobrevive à cópia para outro sistema e o contrato continua válido. Texto marcado pode aparecer em telas de homologação, o que é desejado. Falta decidir quais campos entram ([PA04](../01-requisitos/regras-de-negocio.md#pa04)).
