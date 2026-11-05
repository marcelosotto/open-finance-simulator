# ADR-005: Marcação em três camadas

**Status:** Aceito
**Data:** 2026-11-05

## Contexto
Queremos saber sempre que um dado é simulado, de qual ambiente e se é virtual. Só um cabeçalho se perde quando o dado é gravado por outro sistema. Um campo novo no JSON desviaria da spec oficial.

## Decisão
Marcar em três lugares: **cabeçalho** da resposta, **prefixo dos identificadores** e **texto** de campos livres ([RN60](../01-requisitos/regras-de-negocio.md#rn60)).

A marca do ambiente é um valor livre da configuração da implantação (`dev`, `hml`, `stage` ou outro). O sistema só concatena o valor, sem regra. Em produção a configuração fica vazia e nada é concatenado: sem `x-sim-environment`, sem prefixo de ambiente nos IDs e sem marca no texto. A natureza continua aparecendo.

Exemplos com a marca `hml`:
- `x-sim-environment: hml` e `x-sim-data-nature: virtual`
- `accountId`: `hml-v-1a2b3c`, dentro do padrão `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$` da spec
- `consentId`: `urn:hml:v-1a2b3c`
- `transactionName`: `[HML] PIX recebido`

## Consequências
Exemplos com a marca vazia: `accountId` `p-1a2b3c`, `consentId` `urn:p:1a2b3c`, `transactionName` `PIX recebido`.

A marca sobrevive à cópia para outro sistema e o contrato continua válido. Texto marcado pode aparecer em telas de homologação, o que é desejado. Os campos marcados são `transactionName` e nome do cliente ([PA23](../01-requisitos/regras-de-negocio.md#pa23)).
