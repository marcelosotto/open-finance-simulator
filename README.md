# Simulador Open Finance

Simula instituições financeiras do Open Finance Brasil para que os times testem situações reais sem depender de um banco de verdade: consentimentos, recursos, dados cadastrais e transacionais, limites, lentidão, timeout, instabilidade e dados ruins. Tudo é simulado e marcado como tal.

## Como usar

1. Suba o ambiente completo. Precisa só do Docker:

   ```bash
   docker compose up --build
   ```

2. Abra o **back-office** no navegador: <http://localhost:8081>. A primeira página é **Primeiros passos**: siga os passos na ordem. Toda tela tem, no topo, duas ou três linhas dizendo para que serve e o que precisa existir antes. O passo a passo em português simples está no [guia de uso](docs/08-front-end/guia-de-uso.md).
3. O seu app chama as **APIs Open Finance** em <http://localhost:8080>, no endereço de cada banco, como `http://localhost:8080/banco-estavel/open-banking/...`.
4. O app se identifica como a receptora padrão **XP**, que já vem cadastrada. Usuário e senha de teste ficam na tela **Receptoras**, e Primeiros passos mostra um exemplo pronto. Com eles, o app pede o token ao banco:

   ```bash
   curl -X POST http://localhost:8080/banco-estavel/token \
     -d grant_type=client_credentials -d client_id=<usuario> -d client_secret=<senha> -d scope=consents
   ```

   Depois, cria o consentimento por `POST /consents` ou usa um criado na tela, e lê os dados com o token do consentimento (`scope=consent:{consentId}`), como em produção ([RN120](docs/01-requisitos/regras-de-negocio.md#rn120)).

A administração (porta 8081) não tem login e só aceita conexões da própria máquina ([RN71](docs/01-requisitos/regras-de-negocio.md#rn71)).

## Situação

As specs de 001 a 010 estão entregues, na ordem da [sequência de specs](docs/07-gerenciamento/sequencia-de-specs.md):

| Spec | O que o simulador faz |
|---|---|
| [001 — Fundação e catálogo](specs/001-fundacao-e-catalogo/spec.md) | Lê as specs oficiais fixadas por commit, gera as rotas, confere a tabela oficial de endpoints e marca todo dado com o ambiente |
| [002 — Instituições](specs/002-instituicoes/spec.md) | Bancos simulados prontos e criados, com perfil de comportamento, endereço próprio e histórico |
| [003 — Massa e gerador](specs/003-massa-e-gerador/spec.md) | Clientes de mentira por semente: lotes, clientes avulsos, mundo virtual, ajustes e limpeza |
| [004 — Consentimentos e recursos](specs/004-consentimentos-e-recursos/spec.md) | Receptoras, token no modo simples, consentimentos e a API de Recursos |
| [005 — APIs de dados](specs/005-apis-de-dados/spec.md) | Contas, cartões, crédito, investimentos, câmbio e cadastro, com transações e a paginação oficial |
| [006 — Limites](specs/006-limites/spec.md) | TPM, limite operacional mensal e capacidade, com as respostas 429 oficiais |
| [007 — Comportamento e cenários](specs/007-comportamento-e-cenarios/spec.md) | Lentidão, erros, timeout, indisponibilidade, relógio do banco e regras de cenário por alcance |
| [008 — Consentimento no painel](specs/008-consentimento-no-painel/spec.md) | Criação de consentimento pela administração, com seleção de origem e múltiplas alçadas |
| [009 — Defeitos e anomalias](specs/009-defeitos-e-anomalias/spec.md) | Dado ruim de propósito na resposta e desvios nomeados do protocolo, por banco |
| [010 — Back-office](specs/010-back-office/spec.md) | As telas de bancos, massa, consentimento, receptoras e cenários, com Primeiros passos e a receptora padrão XP |

O projeto nasce da POC `src/OpenFinance.FakeProxy` do repositório [OpenToken](https://github.com/marcelosotto/OpenToken). A POC provou a ideia; este repositório especifica a versão para desenvolvimento e homologação.

## Stack
- Front-end: React com Webpack, sem login ([ADR-010](docs/05-adr/ADR-010-front-end-react.md) e [guia](docs/08-front-end/README.md)), servido pelo próprio simulador na porta de administração
- Back-end: .NET 10 ([ADR-001](docs/05-adr/ADR-001-backend-dotnet-10.md)), monólito modular ([ADR-007](docs/05-adr/ADR-007-arquitetura-e-endereco.md))
- Dados: MongoDB para o que tem estado e Redis para os contadores ([ADR-003](docs/05-adr/ADR-003-mongodb-e-redis.md))
- Specs oficiais: repositório [open-finance-specs](https://github.com/marcelosotto/open-finance-specs), fixado por commit ([ADR-002](docs/05-adr/ADR-002-specs-fixadas-por-commit.md))

## Configurações

Variáveis de ambiente do processo do simulador. No Compose e em variável de ambiente, `:` vira `__` (por exemplo, `Simulador__MarcaDoAmbiente`). Os padrões abaixo valem quando a chave não é informada.

| Configuração | Padrão | Efeito |
|---|---|---|
| `Simulador__MarcaDoAmbiente` | vazia (`hml` no Compose) | Marca concatenada no prefixo dos IDs, nos textos e no cabeçalho `x-sim-environment` (`hml`, `dev`, `stage`…). Vazia em produção: nada é concatenado ([RN60](docs/01-requisitos/regras-de-negocio.md#rn60), [RN72](docs/01-requisitos/regras-de-negocio.md#rn72)) |
| `Simulador__PortaOpenFinance` | `8080` | Porta das APIs Open Finance |
| `Simulador__PortaAdministracao` | `8081` | Porta da administração e do back-office. As duas se separam pela porta da conexão, nunca pelo cabeçalho Host |
| `Simulador__Mongo__Conexao` | `mongodb://localhost:27017` | MongoDB das instituições, massa, consentimentos, receptoras e cenários |
| `Simulador__Mongo__Base` | `simulador` | Nome da base no MongoDB |
| `Simulador__Redis__Conexao` | `localhost:6379` | Redis dos contadores de limites e das sequências de cenário |
| `Simulador__Semente` | sorteada por processo | Semente dos geradores. Com o mesmo valor, a mesma massa e o mesmo relógio, as respostas se repetem ([RN54](docs/01-requisitos/regras-de-negocio.md#rn54)) |
| `Simulador__Token__Chave` | sorteada por processo | Chave que assina os tokens. Sem ela, tokens emitidos antes de reiniciar deixam de valer; fixe-a para várias instâncias |
| `Simulador__ReceptoraPadrao__Nome` | `XP` | Receptora já cadastrada ao subir. Vazio: sem receptora padrão ([RN124](docs/01-requisitos/regras-de-negocio.md#rn124)) |
| `Simulador__ReceptoraPadrao__OrganisationId` | `{marca}-p-receptora-padrao` | Identificador fictício da receptora padrão, nunca o de uma instituição real |
| `Simulador__ReceptoraPadrao__Usuario` | gerado na primeira subida | Usuário de teste fixo da receptora padrão |
| `Simulador__ReceptoraPadrao__Senha` | gerada na primeira subida | Senha de teste fixa da receptora padrão |
| `Simulador__PastaDasSpecs` | `specs` ao lado do executável (`/app/specs` na imagem) | Pasta com as specs oficiais baixadas |
| `Simulador__ArquivoDaTrava` | `specs.lock.json` ao lado do executável | Trava com o commit e os SHA-256 das specs |
| `Simulador__ArquivoDaTabela` | `config/tabela-de-endpoints.json` ao lado do executável | Tabela oficial de endpoints (classe, TPM, cota e SLA) |
| `Simulador__PastaDoBackOffice` | `backoffice` ao lado do executável | Build do back-office. Sem a pasta, a 8081 serve só a administração |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | vazia | Coletor OpenTelemetry, só para a saúde do simulador. Vazia: sem telemetria ([RNF09](docs/01-requisitos/requisitos.md#rnf09)) |

O front-end não tem configuração de marca: a tela lê a marca e a porta das APIs em `GET /admin/ambiente`. A variável `SIM_ADMIN_API_URL` do build do front fica vazia para usar a mesma origem ([guia de front-end](docs/08-front-end/README.md)).

## Como rodar

Pré-requisitos: SDK do .NET 10 e Docker. As specs vêm do repositório público `open-finance-specs`, no commit de `specs.lock.json`. O Node não é obrigatório: os comandos do front rodam pela imagem `node:22`.

```bash
# Ambiente completo (APIs Open Finance em :8080, administração e back-office em 127.0.0.1:8081)
docker compose up --build

# Sem Docker: baixar e conferir as specs, testar e subir
dotnet run --project tools/Simulador.Ferramentas -- specs baixar
dotnet run --project tools/Simulador.Ferramentas -- specs verificar
dotnet test Simulador.slnx
dotnet run --project src/Simulador.Host -- --Simulador:PastaDasSpecs .specs/<commit>
```

Portões do front-end, pelo Docker (os mesmos do job `front` do CI):

```bash
sh front/scripts/docker.sh ci
sh front/scripts/docker.sh run lint
sh front/scripts/docker.sh run typecheck
sh front/scripts/docker.sh run test:cov
sh front/scripts/docker.sh run build
```

Com o Node 22 ou mais novo instalado, os mesmos comandos rodam com `npm` dentro de `front/`. `npm run dev` sobe o front na porta 3000, repassando `/admin` para a 8081.

Fluxos no navegador (Playwright), com o Compose no ar (job `painel` do CI):

```bash
sh front/scripts/playwright.sh
```

Os ganchos de commit do front (Husky, lint-staged e commitlint) são opcionais e não se ativam sozinhos, porque mudam a configuração do Git de todas as worktrees. Para ativar: `npm run ganchos`, dentro de `front/`.

Comandos da ferramenta: `specs baixar`, `specs verificar`, `specs diferencas --base <lock> --novo <lock>` e `cobertura conferir`. Detalhes em [ferramenta-specs.md](specs/001-fundacao-e-catalogo/contracts/ferramenta-specs.md).

## Fluxo de trabalho
1. Discutir e aprovar a documentação em `docs/`, caso de uso por caso de uso.
2. Rodar o Spec Kit na ordem da [sequência de specs](docs/07-gerenciamento/sequencia-de-specs.md).
3. Implementar em `src/` e `front/`, com testes em `tests/` e `front/tests/`.

## Por onde começar
| Preciso… | Onde encontrar |
|---|---|
| Usar o simulador pela primeira vez | [Guia de uso](docs/08-front-end/guia-de-uso.md) |
| Entender o objetivo e o escopo | [Termo de abertura](docs/00-visao-geral/termo-de-abertura.md) |
| Ver o que a POC faz certo e errado | [Confronto da POC com as regras oficiais](docs/01-requisitos/confronto-com-a-poc.md) |
| Decidir os pontos em aberto | [Perguntas em aberto](docs/01-requisitos/regras-de-negocio.md#perguntas-em-aberto) |
| Ler as regras | [Regras de negócio](docs/01-requisitos/regras-de-negocio.md) · [Comportamento de tela](docs/01-requisitos/interface.md) · [Tabela oficial de endpoints](docs/01-requisitos/tabela-de-endpoints.md) |
| Discutir os casos de uso | [Casos de uso](docs/01-requisitos/casos-de-uso/README.md) · [Cenários Gherkin](docs/01-requisitos/gherkin/) |
| Entender a arquitetura | [C1](docs/02-arquitetura/c4/C1-contexto.md) · [C2](docs/02-arquitetura/c4/C2-conteineres.md) · [C3](docs/02-arquitetura/c4/C3-componentes.md) · [Modelo de domínio](docs/03-design/classes/modelo-de-dominio.md) · [Sequência do painel](docs/03-design/sequencia/01-consentimento-no-painel.md) · [Dados](docs/04-dados/modelo-de-dados.md) |
| Ver as decisões técnicas | [ADRs](docs/05-adr/README.md) |
| Construir telas | [Guia de front-end](docs/08-front-end/README.md) |
| Testar | [Estratégia de testes](docs/06-testes/estrategia-de-testes.md) |
| Rodar o Spec Kit | [Sequência de specs](docs/07-gerenciamento/sequencia-de-specs.md) · [Insumo da constituição](docs/07-gerenciamento/insumo-da-constituicao.md) |

## Estrutura
```
src/
  Simulador.Host/          Composição, portas 8080 e 8081, adaptadores entre módulos, back-office estático
  Simulador.Comum/         Semente, comportamento, envelope de erro e leitura do autor
  Modulos/                 Monólito modular (ADR-007); os módulos não se referenciam
    Simulador.Catalogo/        Specs oficiais, rotas geradas e tabela de endpoints
    Simulador.Marcacao/        Marca do ambiente e natureza nos IDs, textos e cabeçalhos
    Simulador.Instituicoes/    Bancos simulados, perfis, limites configurados e histórico
    Simulador.Massa/           Gerador por semente, lotes, clientes, mundo virtual, ajustes e limpeza
    Simulador.Consentimentos/  Receptoras, token, consentimentos, Recursos e o painel
    Simulador.Dados/           APIs de dados e paginação
    Simulador.Limites/         TPM, limite operacional e capacidade (Redis)
    Simulador.Cenarios/        Regras, pacotes, relógio, defeitos e anomalias
front/                     Back-office em React (Clean Architecture), testes Jest e Playwright (e2e/)
tools/                     Ferramenta de linha de comando: specs e cobertura
tests/                     Unidade, integração e aceitação (Reqnroll com os .feature de docs/)
config/                    Tabela oficial de endpoints em JSON
deploy/                    Configuração do coletor OpenTelemetry
specs/                     Specs do Spec Kit (spec, plano, tarefas)
docs/
  00-visao-geral/          Termo de abertura, premissas, personas, referências
  01-requisitos/           Regras, requisitos, casos de uso, Gherkin, telas, glossário, confronto com a POC
  02-arquitetura/c4/       C1 Contexto, C2 Contêineres, C3 Componentes
  03-design/               Modelo de domínio e diagramas de sequência
  04-dados/                Modelo de dados
  05-adr/                  Architecture Decision Records
  06-testes/               Estratégia de testes
  07-gerenciamento/        Sequência de specs, insumo da constituição, riscos
  08-front-end/            Guia de front-end e guia de uso do back-office
```

Para ler como site: `pip install mkdocs-material` e `mkdocs serve`.
