# Implementation Plan: Fundação e catálogo

**Branch**: `001-fundacao-e-catalogo` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-fundacao-e-catalogo/spec.md`

## Summary

Criar a base do simulador como monólito modular em .NET 10. O módulo **Catálogo** lê as 15
specs oficiais travadas por commit em `specs.lock.json`, confere o SHA-256 e publica cada
operação em `/{banco}/open-banking/{api}/{versão}/…`. Enquanto a spec responsável não entrega
o comportamento, a operação responde 501. A classe de frequência, o SLA, o TPM e o limite
mensal de cada endpoint ficam em `config/tabela-de-endpoints.json`, conferido por teste contra
a tabela oficial em `docs/`. O módulo **Marcação** gera IDs, `consentId`, texto e cabeçalhos
com a marca do ambiente (valor de configuração, vazio em produção) e a natureza. O CI bloqueia
o merge em formatação, análise estática, verificação das specs, testes, cenários de
`marcacao.feature`, cobertura (100% no domínio e 80% no total) e tabela oficial. Detalhes das
decisões em [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 em .NET 10 (Minimal API), conforme a [ADR-001](../../docs/05-adr/ADR-001-backend-dotnet-10.md)

**Primary Dependencies**: ASP.NET Core Minimal API, `Microsoft.OpenApi` (leitor YAML), `JsonSchema.Net`, OpenTelemetry (R03, R12, R14)

**Storage**: nenhum nesta spec. As specs ficam em arquivo dentro da imagem, e a tabela em `config/tabela-de-endpoints.json`. MongoDB entra na spec 002 e Redis na 006 (R13).

**Testing**: xUnit v3, Reqnroll (`Reqnroll.xunit.v3`), `WebApplicationFactory`, `FakeTimeProvider`, `coverlet.collector` (R11)

**Target Platform**: contêiner Linux, com Docker Compose local (RNF11)

**Project Type**: serviço web (monólito modular) com uma ferramenta de linha de comando para o CI

**Performance Goals**: subida com as 15 specs em menos de 5 s; o despachante e a marcação somam no máximo 10 ms no p95 ([RNF03](../../docs/01-requisitos/requisitos.md#rnf03)); 300 req/s por instância ([RNF02](../../docs/01-requisitos/requisitos.md#rnf02)). A medição com k6 entra quando houver operação com comportamento.

**Constraints**: subida sem rede (specs na imagem); nenhuma rota oficial escrita à mão; nenhum teste com hora real ou sorteio sem semente; nenhum registro de chamadas

**Scale/Scope**: 15 contratos de dados, consentimento e recursos, com todas as operações deles (o número exato sai do `catalog.json` do commit); 2 módulos de domínio nesta spec

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Como o plano atende | Situação |
|---|---|---|
| I. `docs/` é a fonte da verdade | Spec e plano citam UC23, UC24, RN60 a RN66, RN70, RF17, RF18, ADR-002, ADR-005, ADR-007 e ADR-009. O teste da tabela lê o Markdown de `docs/`. Se a tabela e o catálogo divergirem, quem muda é `docs/` (R02). | ✅ |
| II. Regra oficial por padrão | Limites e classes saem da tabela oficial; contratos e caminhos saem das specs oficiais. O 501 é provisório e some quando a spec da operação é entregue. | ✅ |
| III. Desvio sempre explícito | Nenhuma anomalia nesta spec. | ✅ |
| IV. Tudo simulado e marcado | Módulo Marcação com marca configurável, vazia em produção (constituição 2.0.0). A marca é gravada na criação e não depende de perfil. A varredura de marcação roda no CI. | ✅ |
| V. Só responde; não julga nem registra | Sem registro de chamadas; OpenTelemetry só para a saúde do simulador. A resposta 501 não explica comportamento de cenário. | ✅ |
| VI. Repetível | `TimeProvider` e `IGeradorAleatorio` injetados desde a fundação. | ✅ |
| VII. Specs oficiais como fonte das APIs | `specs.lock.json` no commit `7bdb9c1…`, rotas geradas na subida, conferência de SHA-256 no build e validação de contrato. | ✅ |
| VIII. Cliente sem mudança | Mesmos caminhos oficiais sob `/{banco}`. Administração em porta separada. Nenhum cabeçalho próprio exigido da receptora. | ✅ |
| IX. Testes primeiro | `marcacao.feature` ligado ao projeto de aceitação antes do código; cobertura conferida no CI. | ✅ |
| X. Simplicidade | Rotas lidas na subida (sem source generator), teste de arquitetura com reflexão (sem biblioteca), sem MongoDB e Redis até haver uso. As três dependências novas estão justificadas em [research.md](research.md#dependências-novas). | ✅ |
| Stack | .NET 10 Minimal API, monólito modular, Docker Compose, xUnit e Reqnroll. Nenhum item da stack trocado. | ✅ |
| Portões de CI | Formatação, análise estática, testes, cobertura, conformidade com as specs e tabela oficial (R10). | ✅ |

**Administração sem login:** a RN71 define que a administração não tem login nem autenticação.
Nesta spec a porta de administração só expõe leitura do catálogo e saúde, dentro da rede do Compose.

**Re-check após a Fase 1:** o desenho em [data-model.md](data-model.md) e [contracts/](contracts/)
não introduz armazenamento, dependência ou caminho fora do previsto. Todos os itens seguem ✅.

## Project Structure

### Documentation (this feature)

```text
specs/001-fundacao-e-catalogo/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── specs-lock.schema.json
│   ├── tabela-de-endpoints.schema.json
│   ├── cabecalhos-e-identificadores.md
│   ├── respostas-padrao.md
│   ├── admin-api.md
│   └── ferramenta-specs.md
├── checklists/requirements.md
└── tasks.md             # gerado por /speckit-tasks
```

### Source Code (repository root)

```text
specs.lock.json                         # commit e contratos travados (R01)
config/
└── tabela-de-endpoints.json            # classes da tabela oficial (R05)
.specs/                                 # specs baixadas; no .gitignore (R01)

src/
├── Simulador.Host/                     # Program.cs, composição, portas 8080 e 8081, OpenTelemetry
├── Simulador.Comum/                    # TimeProvider, IGeradorAleatorio, envelope de erro oficial
└── Modulos/
    ├── Simulador.Catalogo/
    │   ├── Contratos/                  # ICatalogo, OperacaoDoCatalogo, ClasseDoEndpoint, IManipuladorDeOperacao
    │   ├── Dominio/                    # TravaDasSpecs, ConferenciaDeHash, DiferencaEntreVersoes, TabelaDeEndpoints
    │   ├── Leitura/                    # leitura dos YAML travados
    │   └── Rotas/                      # publicação das rotas, despachante, 404 e 501
    └── Simulador.Marcacao/
        ├── Contratos/                  # IMarcador, MarcaDoAmbiente, Natureza, NaturezaDaResposta
        ├── Dominio/                    # GeradorDeIdentificador, MarcadorDeTexto, AgregadorDeNatureza
        └── Http/                       # filtro de cabeçalhos

tools/
└── Simulador.Ferramentas/              # specs baixar | verificar | diferencas; cobertura conferir

tests/
├── Simulador.Testes.Unidade/           # domínio dos módulos e teste de arquitetura
├── Simulador.Testes.Integracao/        # rotas geradas, 404/501, tabela oficial, varredura de marcação, contrato
└── Simulador.Testes.Aceitacao/         # Reqnroll com docs/01-requisitos/gherkin/marcacao.feature (link)

.github/workflows/ci.yml
Dockerfile
docker-compose.yml
Directory.Build.props                   # .NET 10, TreatWarningsAsErrors, analisadores
.editorconfig
global.json
Simulador.slnx
```

**Structure Decision**: projeto único de serviço, com um projeto .NET por módulo em
`src/Modulos/`. Os módulos expõem só `Contratos` (R08). O host compõe os módulos, e as specs
seguintes acrescentam módulos (Instituições, Massa, Consentimentos…) sem mexer nos existentes.

## Complexity Tracking

Nenhuma violação da constituição a justificar.
