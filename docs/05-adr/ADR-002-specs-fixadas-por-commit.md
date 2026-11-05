# ADR-002: Specs oficiais fixadas por commit

**Status:** Proposto
**Data:** 2026-11-05

## Contexto
A POC copiou as specs e ficou com versões antigas: Contas 2.4.2 contra 2.5.1 no catálogo. O repositório `open-finance-specs` guarda as specs com commit e SHA-256.

## Decisão
O simulador lê as specs de `open-finance-specs`, por um commit gravado em `specs.lock.json`. O build baixa esse commit, roda a verificação de hashes e empacota as specs na imagem. As classes de frequência saem de um arquivo próprio, com a [tabela oficial](../01-requisitos/tabela-de-endpoints.md), porque as specs não trazem essa informação.

## Alternativas consideradas
- **Submódulo Git:** funciona, mas complica o CI e o dia a dia.
- **Ler do repositório ao subir:** o simulador passaria a depender da rede para iniciar.

## Consequências
Atualizar a versão é uma mudança pequena e revisável. A tabela de classes continua manual: mudou a tabela oficial, muda o arquivo.
