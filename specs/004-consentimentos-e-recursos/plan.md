# Implementation Plan: Consentimentos e recursos

**Branch**: `claude/004-consentimentos-e-recursos-8f1b3e` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/004-consentimentos-e-recursos/spec.md`

## Summary

Entrega o modo simples de segurança, o ciclo oficial de consentimentos e a API de Recursos, em um módulo novo. Um portão único no Despachante aplica 400, 401 e 403 a todas as rotas protegidas. A Massa continua dona dos dados e ganha uma porta de escrita.

## Technical Context

**Language/Version**: C# em .NET 10, Minimal API
**Primary Dependencies**: nenhuma nova; driver MongoDB já em uso
**Storage**: MongoDB (`receptoras`, e `consentimentos`, `recursos` e `ajustes` da 003). Token não é gravado
**Testing**: xUnit, Reqnroll, Testcontainers
**Target Platform**: contêiner Linux (Docker Compose)
**Project Type**: monólito modular web
**Performance Goals**: RNF03 (até 10 ms a mais no p95)
**Constraints**: módulos sem referência entre si (ADR-007); relógio e sorteio injetados
**Scale/Scope**: 6 operações oficiais, 1 rota de token, 6 rotas de administração

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. docs como verdade | OK. Q1 a Q9 registradas em `docs/` antes deste plano |
| II. Regra oficial | OK. Diferenças intencionais na RN80 e na RN120 |
| III. Desvio explícito | OK. Nenhuma anomalia nova |
| IV. Marcado | OK. IDs pelo marcador; natureza no cabeçalho, com `misto` |
| V. Só responde | OK. Nenhum registro de chamada oficial |
| VI. Repetível | OK. Relógio e sorteio injetados; a chave do token não afeta dado de negócio |
| VII. Specs como fonte | OK. Rotas, escopos e permissões saem das specs travadas. `POST /token` é a API própria da ADR-006 |
| VIII. Cliente sem mudança | OK. Identidade só no token |
| IX. Testes primeiro | OK. Gherkin e testes antes do código; 100% no domínio |
| X. Simplicidade | OK. Sem dependência nova |

Reavaliado depois do desenho: sem exceções.

## Project Structure

```text
src/Modulos/Simulador.Consentimentos/   # Contratos, Dominio, Aplicacao, Http, Persistencia
src/Modulos/Simulador.Catalogo/         # escopos, permissões e IPortaoDeOperacao
src/Modulos/Simulador.Massa/            # IEscritaDeMassa e campos novos
src/Simulador.Host/AdaptadoresDeConsentimentos.cs
tests/ComumHost/                        # receptoras em memória
tests/Simulador.Testes.Unidade/Consentimentos/
tests/Simulador.Testes.Integracao/Consentimentos/
tests/Simulador.Testes.Aceitacao/Passos/ # passos de consentimento, recursos e segurança
```

**Structure Decision**: módulo novo; ver [research](research.md) R02 e R03. Contratos próprios em [contracts/token-e-admin.md](contracts/token-e-admin.md).

## Complexity Tracking

Sem violações.
