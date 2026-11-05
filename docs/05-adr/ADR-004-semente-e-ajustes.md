# ADR-004: Transações por semente, com ajustes persistidos

**Status:** Proposto
**Data:** 2026-11-05

## Contexto
Gravar transações de milhões de clientes sairia caro. A POC gera transações por `recurso + dia` e provou a ideia, inclusive com saldo coerente.

## Decisão
Manter o gerador por semente. Entidades (cliente, consentimento e recurso) podem ser persistidas ou virtuais. Transações nunca são gravadas. Alterações ficam em **ajustes**, aplicados sobre o resultado calculado.

## Consequências
Volume sem custo de armazenamento e testes repetíveis. Mudar o gerador muda os dados de todas as sementes, por isso o gerador tem versão, gravada no lote.
