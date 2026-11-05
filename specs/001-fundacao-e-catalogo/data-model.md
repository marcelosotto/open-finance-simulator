# Data Model: Fundação e catálogo

**Feature**: [spec.md](spec.md) · **Plano**: [plan.md](plan.md)

Nesta spec nada é persistido em banco. As entidades vivem em memória, carregadas de arquivo na
subida, ou são valores gravados dentro de outros dados (a marca).

## Catálogo

### TravaDasSpecs
Lida de `specs.lock.json` ([schema](contracts/specs-lock.schema.json)).

| Campo | Tipo | Regra |
|---|---|---|
| `repositorio` | texto | `owner/nome` no GitHub; obrigatório |
| `commit` | texto | SHA de 40 caracteres hexadecimais |
| `contratos` | lista de `ContratoTravado` | pelo menos 1; `id` sem repetição |

### ContratoTravado

| Campo | Tipo | Regra |
|---|---|---|
| `id` | texto | igual ao `id` do `catalog.json` (ex.: `accounts-2.5.1`) |
| `caminhoLocal` | texto | `localPath` do catálogo |
| `sha256` | texto | 64 hexadecimais minúsculos |

**Conferência (FR-002):** o SHA-256 dos bytes baixados é igual ao do lock e ao do catálogo do
commit. Falta, sobra ou divergência gera `FalhaDeConferencia` com o arquivo e o motivo.

### OperacaoDoCatalogo
Uma por operação de cada contrato travado.

| Campo | Tipo | Origem |
|---|---|---|
| `contrato` | texto | `ContratoTravado.id` |
| `api` | texto | segmento da API em `servers[0].url` (ex.: `accounts`) |
| `versao` | texto | segmento da versão em `servers[0].url` (ex.: `v2`) e `info.version` |
| `metodo` | texto | `GET`, `POST`, `DELETE`… |
| `caminho` | texto | caminho da spec (ex.: `/accounts/{accountId}/transactions`) |
| `caminhoPublicado` | texto | `/{banco}/open-banking/{api}/{versao}{caminho}` |
| `operationId` | texto | da spec; único no catálogo |
| `parametros` | lista | nome, local (path, query, header), obrigatório, schema |
| `cabecalhosExigidos` | lista | cabeçalhos obrigatórios da operação |
| `respostas` | mapa | código HTTP → schema |

**Chave:** `(contrato, metodo, caminho)`. O mesmo caminho em contratos diferentes gera
operações diferentes, cada uma no seu `caminhoPublicado`.

### ClasseDoEndpoint
Lida de `config/tabela-de-endpoints.json` ([schema](contracts/tabela-de-endpoints.schema.json)).

| Campo | Tipo | Regra |
|---|---|---|
| `contrato` | texto | existe na trava |
| `secao` | texto | título da seção na tabela oficial (ex.: `Contas`) |
| `metodo`, `caminho` | texto | existem no catálogo (FR-015) |
| `frequencia` | enum | `alta`, `media-alta`, `media`, `baixa` |
| `slaMs` | inteiro | 1500, 2000 ou 4000, conforme a frequência |
| `tpm` | inteiro ou `faixa-qca` | `faixa-qca` depende dos consentimentos ativos ([RN21](../../docs/01-requisitos/regras-de-negocio.md#rn21)) |
| `limiteMensal` | inteiro ou nulo | nulo para consentimento e recursos (sem limite) |

Valores comuns no mesmo arquivo: `timeoutSegundos = 15` e `tps = 300`.

### DiferencaEntreVersoes
Saída de `specs diferencas` (UC24).

| Campo | Tipo |
|---|---|
| `commitAnterior`, `commitNovo` | texto |
| `novas` | lista de operações |
| `removidas` | lista de operações |
| `alteradas` | lista de `{ operacao, mudancas[] }`. Mudança de contrato (versão), de parâmetros, de cabeçalhos exigidos ou de schema de resposta |

## Marcação

### MarcaDoAmbiente
Valor da configuração `Simulador:MarcaDoAmbiente`, lido na subida.

| Campo | Tipo | Regra |
|---|---|---|
| `valor` | texto | livre; vazio é válido (produção). Sem validação ([RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60)) |
| `vazia` | lógico | `valor` vazio ou só espaços |
| `textual` | texto | `[{VALOR EM MAIÚSCULAS}] `; vazio quando `vazia` |

### Natureza
Enum: `persistido` (`p`) e `virtual` (`v`).

### NaturezaDaResposta
Enum derivado dos itens da resposta: `persistido`, `virtual` ou `misto`. Nenhum item gera
ausência do cabeçalho ([RN65](../../docs/01-requisitos/regras-de-negocio.md#rn65)).

| Itens da resposta | Resultado |
|---|---|
| todos `persistido` | `persistido` |
| todos `virtual` | `virtual` |
| ambos | `misto` |
| nenhum (erro, lista vazia) | sem `x-sim-data-nature` |

### Marca gravada no dado
Não é entidade própria. Todo dado criado guarda **marca do ambiente** e **natureza** no momento
da criação, e o ID os carrega no prefixo. Ler o dado depois nunca recalcula a marca pela
configuração atual ([RN64](../../docs/01-requisitos/regras-de-negocio.md#rn64)).

**Formatos** (detalhe em [cabecalhos-e-identificadores.md](contracts/cabecalhos-e-identificadores.md)):

| Item | Marca `hml` | Marca vazia |
|---|---|---|
| ID de recurso | `hml-p-1a2b3c` | `p-1a2b3c` |
| `consentId` | `urn:hml:v-1a2b3c` | `urn:v:1a2b3c` |
| `transactionName` | `[HML] PIX recebido` | `PIX recebido` |
| `x-sim-environment` | `hml` | não enviado |

**Regras de tamanho:** ID até 100 caracteres no padrão `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$`;
texto até o `maxLength` do campo na spec. Corta-se a parte aleatória do ID e o fim do texto
original, nunca a marca.
