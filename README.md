# Simulador Open Finance

Simula instituições financeiras do Open Finance Brasil para que os times testem situações reais sem depender de um banco de verdade: consentimentos, recursos, dados cadastrais e transacionais, limites, lentidão, timeout, instabilidade e dados ruins.

> **Situação:** documentação em rascunho, para discussão. Ainda não há código. Depois de aprovados os casos de uso, as specs do Spec Kit geram o projeto.

O projeto nasce da POC `src/OpenFinance.FakeProxy` do repositório [OpenToken](https://github.com/marcelosotto/OpenToken). A POC provou a ideia; este repositório especifica a versão para desenvolvimento e homologação.

## Stack
- Front-end: React com Webpack, sem login ([ADR-010](docs/05-adr/ADR-010-front-end-react.md) e [guia](docs/08-front-end/README.md))
- Back-end: .NET 10 ([ADR-001](docs/05-adr/ADR-001-backend-dotnet-10.md))
- Dados: MongoDB para o que tem estado e Redis para os contadores, decididos ([ADR-003](docs/05-adr/ADR-003-mongodb-e-redis.md))
- Specs oficiais: repositório [open-finance-specs](https://github.com/marcelosotto/open-finance-specs), fixado por commit ([ADR-002](docs/05-adr/ADR-002-specs-fixadas-por-commit.md))

## Fluxo de trabalho
1. Discutir e aprovar a documentação em `docs/`, caso de uso por caso de uso.
2. Rodar o Spec Kit na ordem da [sequência de specs](docs/07-gerenciamento/sequencia-de-specs.md).
3. Implementar em `src/`, com testes em `tests/`.

## Por onde começar
| Preciso… | Onde encontrar |
|---|---|
| Entender o objetivo e o escopo | [Termo de abertura](docs/00-visao-geral/termo-de-abertura.md) |
| Ver o que a POC faz certo e errado | [Confronto da POC com as regras oficiais](docs/01-requisitos/confronto-com-a-poc.md) |
| Decidir os pontos em aberto | [Perguntas em aberto](docs/01-requisitos/regras-de-negocio.md#perguntas-em-aberto) |
| Ler as regras | [Regras de negócio](docs/01-requisitos/regras-de-negocio.md) · [Tabela oficial de endpoints](docs/01-requisitos/tabela-de-endpoints.md) |
| Discutir os casos de uso | [Casos de uso](docs/01-requisitos/casos-de-uso/README.md) · [Cenários Gherkin](docs/01-requisitos/gherkin/) |
| Entender a arquitetura | [C1](docs/02-arquitetura/c4/C1-contexto.md) · [C2](docs/02-arquitetura/c4/C2-conteineres.md) · [C3](docs/02-arquitetura/c4/C3-componentes.md) · [Modelo de domínio](docs/03-design/classes/modelo-de-dominio.md) · [Sequências](docs/03-design/sequencia/README.md) · [Dados](docs/04-dados/modelo-de-dados.md) |
| Ver as decisões técnicas | [ADRs](docs/05-adr/README.md) |
| Rodar o Spec Kit depois | [Sequência de specs](docs/07-gerenciamento/sequencia-de-specs.md) · [Insumo da constituição](docs/07-gerenciamento/insumo-da-constituicao.md) |

## Estrutura
```
docs/
  00-visao-geral/     Termo de abertura, premissas, personas, referências
  01-requisitos/      Regras, requisitos, casos de uso, Gherkin, glossário, confronto com a POC
  02-arquitetura/c4/  C1 Contexto, C2 Contêineres, C3 Componentes
  03-design/          Modelo de domínio e diagramas de sequência
  04-dados/           Modelo de dados
  05-adr/             Architecture Decision Records
  06-testes/          Estratégia de testes
  07-gerenciamento/   Sequência de specs, insumo da constituição, riscos
```

Para ler como site: `pip install mkdocs-material` e `mkdocs serve`.
