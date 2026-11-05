# Contrato: cabeçalhos e identificadores marcados

Origem: [RN60](../../../docs/01-requisitos/regras-de-negocio.md#rn60) a [RN66](../../../docs/01-requisitos/regras-de-negocio.md#rn66), [RN70](../../../docs/01-requisitos/regras-de-negocio.md#rn70), [ADR-005](../../../docs/05-adr/ADR-005-marcacao-em-tres-camadas.md). Decisões em [research.md R07](../research.md#r07--marcação).

## Configuração

| Chave | Variável de ambiente | Valor |
|---|---|---|
| `Simulador:MarcaDoAmbiente` | `Simulador__MarcaDoAmbiente` | livre (`dev`, `hml`, `stage`…); vazio em produção |

Lida uma vez na subida. Não existe chave por instituição, perfil ou cenário.

## Cabeçalhos de resposta (APIs Open Finance)

| Cabeçalho | Quando vai | Valor |
|---|---|---|
| `x-sim-environment` | toda resposta, inclusive 404 e 501, se a marca não for vazia | a marca, como configurada |
| `x-sim-data-nature` | só quando a resposta tem dado | `persistido`, `virtual` ou `misto` |

O corpo oficial não muda. Receptora que ignora esses cabeçalhos não sente diferença.

## Identificadores

`{aleatório}`: letras minúsculas e dígitos, do gerador com semente.

| Tipo | Com marca `{m}` | Com marca vazia | Padrão oficial |
|---|---|---|---|
| ID de recurso (`accountId`, `contractId`…) | `{m}-{n}-{aleatório}` | `{n}-{aleatório}` | `^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$` |
| `consentId` | `urn:{m}:{n}-{aleatório}` | `urn:{n}:{aleatório}` | padrão `consentId` da spec de consentimentos |

`{n}` é `p` (persistido) ou `v` (virtual). Se o total passar do tamanho máximo, a parte
aleatória encolhe; a marca e a natureza nunca são cortadas.

**Leitura da marca de um ID existente:** a marca e a natureza são extraídas do próprio ID, não
da configuração atual ([RN64](../../../docs/01-requisitos/regras-de-negocio.md#rn64)).

## Texto marcado

| Campo | Com marca `hml` | Com marca vazia |
|---|---|---|
| `transactionName` | `[HML] PIX recebido` | `PIX recebido` |
| nome do cliente | `[HML] Mariana Alves` | `Mariana Alves` |

O prefixo é `[` + marca em maiúsculas + `] `. O texto original é cortado no fim para caber no
`maxLength` da spec.
