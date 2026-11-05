# Research: Fundação e catálogo

**Feature**: [spec.md](spec.md) · **Plano**: [plan.md](plan.md) · **Data**: 2026-11-05

Cada item segue: **Decisão**, **Por quê** e **Alternativas consideradas**.

## R01 — De onde vêm as specs e como elas são travadas

**Fato levantado:** o repositório `marcelosotto/open-finance-specs` é **privado**. No commit
`7bdb9c1a6c4c265d7aabcdbc662f94c61718135e` ele traz:

- `contracts/expansion/catalog.json`: 35 contratos ativos e 143 operações. Cada contrato tem
  `id`, `domain`, `family`, `specificationVersion`, `versionStatus`, `localPath`, `sha256` e
  `operations` (`method`, `path`, `operationId`, `unitKey`).
- `contracts/expansion/snapshots/`: os 35 OpenAPI oficiais, sem alteração.
- `.gitattributes` com `contracts/** -text`: o hash vale para os bytes exatos.

**Decisão:**
- `specs.lock.json`, na raiz do simulador, guarda o repositório, o commit e a lista dos
  **contratos usados**, cada um com `id`, `localPath` e `sha256` copiados do catálogo
  ([contrato](contracts/specs-lock.schema.json)).
- O build baixa o pacote do commit pela API do GitHub com um token de leitura, confere o
  SHA-256 de cada arquivo travado e copia só os snapshots travados para `.specs/`. Essa pasta
  está no `.gitignore` e vai para a imagem.
- A conferência é dupla: o hash do arquivo contra o `specs.lock.json` e contra o
  `catalog.json` do próprio commit. Contrato travado que falta no catálogo, ou hash
  divergente, quebra o build.

**Por quê:** segue a [ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md). O
`catalog.json` já tem o hash, e repetir o hash no lock torna a troca de versão visível no
diff do PR.

**Alternativas consideradas:**
- Copiar os YAML para dentro do repositório: duplica a fonte, e a ADR-002 manda baixar.
- Submódulo Git: rejeitado na ADR-002.
- Rodar o `verify_catalog.py` do repositório de specs: exigiria Python no CI. A conferência
  própria em .NET é pequena e cobre só o que o simulador usa.

## R02 — Quais contratos entram no catálogo

**Decisão:** entram os 15 contratos que cobrem a [tabela oficial](../../docs/01-requisitos/tabela-de-endpoints.md):

| Contrato | Seção da tabela |
|---|---|
| `consents-3.3.1` | Consentimento |
| `resources-3.1.0` | Recursos |
| `customers-2.3.0` | Dados Cadastrais |
| `accounts-2.5.1` | Contas |
| `credit-cards-accounts-2.4.0` | Cartão de Crédito |
| `loans-2.6.0` | Empréstimo |
| `financings-2.4.0` | Financiamento |
| `unarranged-accounts-overdraft-2.5.0` | Adiantamento a Depositantes |
| `invoice-financings-2.4.0` | Direitos Creditórios Descontados |
| `bank-fixed-incomes-1.1.0` | Renda Fixa Bancária |
| `credit-fixed-incomes-1.1.0` | Renda Fixa Crédito |
| `variable-incomes-1.3.0` | Renda Variável |
| `treasure-titles-1.1.0` | Tesouro Direto |
| `funds-1.1.0` | Fundos |
| `exchanges-1.1.0` | Câmbio |

Ficam fora: pagamentos, Pix automático, enrollments, portabilidade, dados abertos, seguros,
previdência, capitalização, credenciamento, `investments-1.1.0`, `admin`, `common`, `channels`
e `webhook` ([PA06](../../docs/01-requisitos/regras-de-negocio.md#pa06)).

**Por quê:** FR-004 limita o escopo a consentimento, recursos e dados. O FR-015 obriga que
toda operação do catálogo tenha linha na tabela, e vice-versa.

**Ponto de atenção:** se um contrato travado tiver operação que não está na tabela oficial,
o teste do FR-015 quebra. Nesse caso, o ajuste é em `docs/` (incluir a linha na tabela ou
registrar a exclusão), nunca no código.

## R03 — Como as rotas são geradas

**Decisão:**
- Na subida, o módulo Catálogo lê cada YAML travado com `Microsoft.OpenApi` (leitor YAML).
- O caminho base vem do `servers[0].url` de cada spec (por exemplo `/open-banking/accounts/v2`).
- Cada operação é publicada com `MapMethods` em `/{banco}{caminhoBase}{caminho}`. Os
  parâmetros de caminho da spec (`{accountId}`) já têm a sintaxe de rota do ASP.NET.
- Cada rota aponta para um único despachante, que procura o manipulador da operação pelo
  `operationId`. Sem manipulador, responde 501 (R06).

**Por quê:** o requisito "nenhuma rota escrita à mão" ([PR06](../../docs/00-visao-geral/premissas.md#pr06))
fica garantido por construção: não existe chamada `MapGet` com caminho oficial no código. O
leitor oficial da Microsoft entende OpenAPI 3.0 e já é a base do ASP.NET.

**Alternativas consideradas:**
- Source generator em tempo de build: mais rápido na subida, mas muito mais complexo
  (princípio X). Ler 15 YAML na subida custa pouco.
- YamlDotNet direto: exigiria interpretar `$ref` e componentes à mão.

## R04 — Endereço da instituição antes da spec 002

**Decisão:** o primeiro segmento `{banco}` passa por uma porta `IResolvedorDeInstituicao`.
Nesta spec, a implementação aceita qualquer identificador no formato `^[a-z0-9-]{1,64}$`. A
spec 002 troca essa implementação pelo cadastro real, sem mexer nas rotas.

**Por quê:** a [ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md) define o
prefixo, mas o cadastro de bancos é da spec 002.

## R05 — Tabela oficial: arquivo e teste

**Decisão:**
- `config/tabela-de-endpoints.json` guarda, por contrato e operação, a seção da tabela, a
  frequência, o SLA, o TPM (número ou `faixa-qca`) e o limite mensal, além do timeout (15 s)
  e do TPS (300) comuns ([contrato](contracts/tabela-de-endpoints.schema.json)).
- O teste da tabela lê o Markdown de `docs/01-requisitos/tabela-de-endpoints.md` (a fonte da
  verdade, princípio I), associa cada seção ao contrato pelo campo `secao` e compara campo a
  campo. Falha nomeando seção, endpoint, campo, valor esperado e valor encontrado.
- Um segundo teste cruza o arquivo com o catálogo: operação sem linha, ou linha sem operação,
  falha (FR-015).

**Por quê:** a [ADR-002](../../docs/05-adr/ADR-002-specs-fixadas-por-commit.md) pede arquivo
próprio, porque as specs não trazem a classe. Ler o Markdown no teste evita uma terceira cópia.

**Alternativas consideradas:** gerar o JSON a partir do Markdown no build. Rejeitado: o
Markdown é para leitura humana e mudar o formato dele quebraria o build em silêncio. O teste
quebra alto.

## R06 — Resposta das rotas sem comportamento e caminho desconhecido

**Decisão:**
- Rota do catálogo sem manipulador: **501**, com o envelope de erro oficial (`errors[]` com
  `code`, `title` e `detail`, e `meta` com `requestDateTime`), código `NAO_IMPLEMENTADO`.
- Caminho fora do catálogo: **404** no mesmo envelope, código `NAO_ENCONTRADO`.
- As duas respostas levam os cabeçalhos de marcação (R07).

Ver [respostas-padrao.md](contracts/respostas-padrao.md).

**Por quê:** FR-006 e FR-008. O envelope `ResponseError` é comum a todas as specs de dados.

## R07 — Marcação

**Decisão:**
- **Marca do ambiente:** configuração `Simulador:MarcaDoAmbiente` (variável
  `Simulador__MarcaDoAmbiente`), lida uma vez na subida. Vazia é válida e significa produção.
  Sem lista fechada e sem validação ([RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60)).
- **Identificador:** `{marca}-{p|v}-{aleatório}`. Com a marca vazia, `{p|v}-{aleatório}`. A
  parte aleatória vem do gerador com semente e encolhe para o ID caber em 100 caracteres
  (padrão `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$`). A marca nunca é cortada.
- **consentId:** `urn:{marca}:{p|v}-{aleatório}`. Com a marca vazia, `urn:{p|v}:{aleatório}`.
- **Texto:** `[{MARCA}] ` na frente de `transactionName` e do nome do cliente. O texto
  original é cortado para caber no `maxLength` da spec. Com a marca vazia, nada muda.
- **Cabeçalhos:** um filtro aplicado a todas as rotas do catálogo, e também ao 404 e ao 501,
  envia `x-sim-environment` (se a marca não for vazia). `x-sim-data-nature` vai só quando a
  resposta tem dado: `persistido`, `virtual` ou `misto`, conforme a natureza dos itens.
  Erro e lista vazia não levam `x-sim-data-nature`.
- **Imutabilidade:** a marca e a natureza são gravadas no dado na criação (FR-020) e vêm do
  próprio ID ao ser lido. Nunca são recalculadas pela marca atual.
- **Nenhum desligamento:** não existe opção de instituição, perfil ou cenário que toque na
  marca. Um teste confere que o tipo de configuração da instituição não tem campo de marca.

Ver [cabecalhos-e-identificadores.md](contracts/cabecalhos-e-identificadores.md).

**Por quê:** [ADR-005](../../docs/05-adr/ADR-005-marcacao-em-tres-camadas.md) e RN60 a RN66.

## R08 — Monólito modular e fronteiras

**Decisão:**
- Um projeto por módulo (`Simulador.Catalogo`, `Simulador.Marcacao`) mais `Simulador.Comum`
  e o host `Simulador.Host`.
- Cada módulo expõe só o namespace `Contratos` como `public`; o resto é `internal`.
- Um teste de arquitetura, feito com reflexão e sem biblioteca nova, falha se algum módulo
  tiver tipo público fora de `Contratos`, ou se referenciar outro módulo fora de `Contratos`.
- O domínio de cada módulo fica no namespace `*.Dominio`, que é o alvo da meta de 100% de
  cobertura.

**Por quê:** [ADR-007](../../docs/05-adr/ADR-007-arquitetura-e-endereco.md). O compilador já
impede o acesso a tipos internos, e o teste cobre o resto (cenário 4 da história 5).

**Alternativas consideradas:** NetArchTest e ArchUnitNET. Rejeitados: dependência nova sem
ganho real para duas regras (princípio X).

## R09 — Portas Open Finance e administração

**Decisão:** o host abre duas portas Kestrel: **8080** para as APIs Open Finance e **8081**
para a administração. Na 8081 ficam `GET /admin/catalogo`, `GET /saude/vivo` e
`GET /saude/pronto`. Ver [admin-api.md](contracts/admin-api.md).

**Por quê:** [RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71) exige endereço
separado. Um processo só cumpre o "juntos agora, separados depois" da ADR-007.

**Pendência herdada:** a RN71 exige usuário nas APIs de administração. A autenticação da
administração não é desta spec. Nesta fase a porta 8081 só expõe leitura do catálogo e saúde,
e não é publicada fora da rede do Compose.

## R10 — CI e portões

**Decisão:** GitHub Actions (o repositório está no GitHub), em `.github/workflows/ci.yml`:

1. `dotnet format --verify-no-changes`.
2. Build com `TreatWarningsAsErrors` e `AnalysisLevel=latest-recommended` (analisadores do
   próprio SDK).
3. `specs baixar` e `specs verificar` da ferramenta própria, com o segredo `SPECS_TOKEN`.
4. Testes de unidade, integração e aceitação, com `coverlet.collector` (Cobertura XML).
5. `cobertura conferir`: 100% de linhas e ramos em `*.Dominio`, 80% no total.
6. Em PR que muda o `specs.lock.json`: `specs diferencas`, que compara as operações do commit
   da base com as do PR e escreve operações novas, alteradas e removidas no resumo do job.

**Por quê:** os portões da constituição e o fluxo do [UC24](../../docs/01-requisitos/casos-de-uso/README.md#uc24).
Uma ferramenta .NET em `tools/` evita Python e scripts soltos no CI.

**Alternativas consideradas:** limite do coverlet.msbuild. Ele não separa domínio do total na
mesma execução sem rodar duas vezes.

## R11 — Testes

**Decisão:**
- **xUnit v3** e **Reqnroll** com `Reqnroll.xunit.v3` ([ADR-009](../../docs/05-adr/ADR-009-gherkin-reqnroll.md)).
- O projeto de aceitação liga por link o `docs/01-requisitos/gherkin/marcacao.feature`. Cada
  spec seguinte liga os seus. O cenário `@tela` é filtrado e fica para a spec 010.
- Testes de integração usam `WebApplicationFactory`.
- Os cenários que consultam transação usam um **manipulador de teste** registrado só no host de
  teste para a operação de transações de contas. Ele monta dado com os serviços reais de
  marcação, sem código de teste em produção.
- O cenário "A marca nunca muda" ([RN64](../../docs/01-requisitos/regras-de-negocio.md#rn64))
  cria o dado com a marca `dev`, troca a marca do host para `hml` e relê: a marca vem do dado.
- Nesta spec não há Testcontainers: ainda não há MongoDB nem Redis (R13).

## R12 — Validação de contrato

**Decisão:** `JsonSchema.Net` valida a resposta contra o schema da operação, extraído da spec
travada. Um adaptador trata as diferenças do OpenAPI 3.0 para o JSON Schema (`nullable`,
`example`).

**Por quê:** FR-023 e [RNF01](../../docs/01-requisitos/requisitos.md#rnf01). O
`Microsoft.OpenApi` lê a spec, mas não valida instância.

**Risco:** o adaptador de `nullable` precisa de teste próprio. Se algum schema oficial usar
recurso fora do adaptador, o teste de contrato quebra alto, e isso é desejado.

## R13 — O que fica fora do Compose nesta spec

**Decisão:** o `docker-compose.yml` da 001 sobe só o simulador e um coletor OpenTelemetry. O
MongoDB entra na spec 002 e o Redis na 006, quando houver código que os use.

**Por quê:** princípio X: nenhuma dependência sem necessidade demonstrada. O FR-026 ("ambiente
completo com um comando") vale para o que existe em cada spec.

## R14 — Relógio, semente e observabilidade

**Decisão:**
- Relógio: `TimeProvider` do .NET, injetado. Testes usam `FakeTimeProvider`.
- Semente: porta `IGeradorAleatorio`, com implementação sobre `Random(semente)`.
- Observabilidade: OpenTelemetry (logs, métricas e traces) com exportador OTLP, só para a saúde
  do simulador ([RNF09](../../docs/01-requisitos/requisitos.md#rnf09)). Nenhum registro das
  chamadas é guardado ([RN18](../../docs/01-requisitos/regras-de-negocio.md#rn18)).

## Dependências novas

| Pacote | Uso | Necessidade |
|---|---|---|
| `Microsoft.OpenApi` e leitor YAML | Ler as specs travadas | Sem ele, o `$ref` e os componentes teriam que ser lidos à mão |
| `JsonSchema.Net` | Validar resposta contra o schema | FR-023; o SDK não valida JSON Schema |
| `OpenTelemetry.Extensions.Hosting` e exportador OTLP | Saúde do simulador | RNF09 |
| xUnit v3, Reqnroll, `coverlet.collector`, `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Extensions.TimeProvider.Testing` | Testes | ADR-009 e princípio IX |
