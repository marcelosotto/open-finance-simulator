# ADR-007: Monólito modular e instituição por endereço

**Status:** Proposto
**Data:** 2026-11-05

## Contexto
A receptora não deve mudar para falar com o simulador ([PR07](../00-visao-geral/premissas.md#pr07)), e a POC escolhe o banco por cabeçalho.

## Decisão
- **Monólito modular:** um código, com módulos separados. API, jornada, administração e worker podem rodar juntos no começo e separados depois.
- **Endereço:** cada instituição tem um prefixo: `https://{host}/{banco}/open-banking/accounts/v2/…`. A receptora só troca a URL base. Subdomínio por banco fica como opção se a infraestrutura permitir ([PA16](../01-requisitos/regras-de-negocio.md#pa16)).

## Consequências
Criar um banco não exige DNS nem deploy. Os links de paginação carregam o prefixo. Com subdomínios, seria mais realista e exigiria certificado e DNS curinga.
