# ADR-010: Back-office em React com Webpack e Clean Architecture

**Status:** Aceito
**Data:** 2026-11-06

## Contexto
O back-office serve para rodar e testar o simulador e ver as telas. A [PA14](../01-requisitos/regras-de-negocio.md#pa14) já definia React com TypeScript, mas sem bundler, estado, testes ou arquitetura. A empresa já tem um padrão de front parecido, e seguir esse padrão facilita para quem vier depois.

## Decisão
- React 18 com TypeScript, empacotado com **Webpack 5**, configurações separadas para `common`, `dev` e `prod`.
- **Clean Architecture** (`core` → `application` → `infrastructure` → `presentation`), **Either** no retorno dos casos de uso e injeção de dependência com **tsyringe**.
- Zustand e TanStack Query para estado, Tailwind e Radix UI para a tela, Jest 30 para testes.
- **Sem login:** o back-office não tem autenticação ([RN71](../01-requisitos/regras-de-negocio.md#rn71)).
- As regras e a stack completa ficam no [guia de front-end](../08-front-end/README.md).

## Alternativas consideradas
- **Vite:** mais simples de configurar, mas foge do padrão da empresa.
- **Arquitetura simples, sem camadas:** menos código, mas foge do padrão e dificulta testar sem a tela.

## Consequências
Mais estrutura do que um back-office pequeno precisaria, aceita para seguir o padrão da empresa. O código fica em `front/`, separado do .NET. Biblioteca fora da stack exige uma nova ADR.
