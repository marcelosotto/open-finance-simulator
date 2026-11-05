# Tabela oficial de endpoints

Fonte: [referência de endpoints do portal](../00-visao-geral/referencias.md#f02), captura de 30/10/2026. Vale como valor padrão de toda instituição simulada ([PR01](../00-visao-geral/premissas.md#pr01)).

- **Timeout:** 15 s em todos os endpoints.
- **TPS:** 300 em todos os endpoints.
- **SLA:** percentil 95 diário: alta e média-alta 1.500 ms, média 2.000 ms, baixa 4.000 ms.
- **TPM na alta:** depende dos consentimentos ativos ([RN21](regras-de-negocio.md#rn21)).
- **Limite operacional:** mínimo por mês ([RN26](regras-de-negocio.md#rn26)).
- **Coluna POC:** classe usada em `bank-frequency-classes.json`, só quando difere da oficial.

As versões da POC são mais antigas que as do catálogo, então `reserved-balances` nem existia nela. A lista completa de divergências está no [confronto com a POC](confronto-com-a-poc.md#classes-de-frequencia).

## APIs sem limite de tráfego ou operacional

| API | Endpoint | Frequência | SLA (ms) |
|---|---|---|---|
| Consentimento | `POST /consents` | Alta | 1.500 |
| Consentimento | `GET /consents/{consentId}` | Alta | 1.500 |
| Consentimento | `DELETE /consents/{consentId}` | Alta | 1.500 |
| Consentimento | `POST /consents/{consentId}/extends` | Baixa | 4.000 |
| Consentimento | `GET /consents/{consentId}/extensions` | Baixa | 4.000 |
| Recursos | `GET /resources` | Alta | 1.500 |

## APIs de dados

### Dados Cadastrais

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /personal/identifications` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /personal/qualifications` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /personal/financial-relations` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /business/identifications` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /business/qualifications` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /business/financial-relations` | Baixa | 4.000 | 1.000 | 8 |  |

### Contas

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /accounts` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /accounts/{accountId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /accounts/{accountId}/balances` | Alta | 1.500 | Faixa QCA | 420 |  |
| `GET /accounts/{accountId}/reserved-balances` | Alta | 1.500 | Faixa QCA | 420 | ausente |
| `GET /accounts/{accountId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /accounts/{accountId}/transactions-current` | Alta | 1.500 | Faixa QCA | 240 |  |
| `GET /accounts/{accountId}/overdraft-limits` | Alta | 1.500 | Faixa QCA | 420 |  |

### Cartão de Crédito

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /accounts` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /accounts/{creditCardAccountId}` | Média-alta | 1.500 | 2.000 | 120 | Baixa |
| `GET /accounts/{creditCardAccountId}/bills` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /accounts/{creditCardAccountId}/bills/{billId}/transactions` | Média | 2.000 | 1.500 | 30 |  |
| `GET /accounts/{creditCardAccountId}/limits` | Alta | 1.500 | Faixa QCA | 240 |  |
| `GET /accounts/{creditCardAccountId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /accounts/{creditCardAccountId}/transactions-current` | Alta | 1.500 | Faixa QCA | 240 |  |

### Empréstimo

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /contracts` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /contracts/{contractId}` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /contracts/{contractId}/warranties` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/scheduled-instalments` | Média | 2.000 | 1.500 | 30 |  |
| `GET /contracts/{contractId}/payments` | Média-alta | 1.500 | 2.000 | 120 | Média |

### Financiamento

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /contracts` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /contracts/{contractId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/warranties` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/scheduled-instalments` | Média | 2.000 | 1.500 | 30 |  |
| `GET /contracts/{contractId}/payments` | Média | 2.000 | 1.500 | 30 |  |

### Adiantamento a Depositantes

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /contracts` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /contracts/{contractId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/warranties` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/scheduled-instalments` | Média | 2.000 | 1.500 | 30 |  |
| `GET /contracts/{contractId}/payments` | Média | 2.000 | 1.500 | 30 |  |

### Direitos Creditórios Descontados

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /contracts` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /contracts/{contractId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/warranties` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /contracts/{contractId}/scheduled-instalments` | Média | 2.000 | 1.500 | 30 |  |
| `GET /contracts/{contractId}/payments` | Média | 2.000 | 1.500 | 30 |  |

### Renda Fixa Bancária

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /investments` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /investments/{investmentId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /investments/{investmentId}/balances` | Média-alta | 1.500 | 2.000 | 120 |  |
| `GET /investments/{investmentId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /investments/{investmentId}/transactions-current` | Média-alta | 1.500 | 2.000 | 120 |  |

### Renda Fixa Crédito

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /investments` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /investments/{investmentId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /investments/{investmentId}/balances` | Média-alta | 1.500 | 2.000 | 120 |  |
| `GET /investments/{investmentId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /investments/{investmentId}/transactions-current` | Média-alta | 1.500 | 2.000 | 120 |  |

### Renda Variável

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /investments` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /investments/{investmentId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /investments/{investmentId}/balances` | Média | 2.000 | 1.500 | 30 | Média-alta |
| `GET /investments/{investmentId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /investments/{investmentId}/transactions-current` | Média | 2.000 | 1.500 | 30 | Média-alta |
| `GET /broker-notes/{brokerNoteId}` | Média | 2.000 | 1.500 | 30 | Baixa |

### Tesouro Direto

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /investments` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /investments/{investmentId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /investments/{investmentId}/balances` | Média-alta | 1.500 | 2.000 | 120 |  |
| `GET /investments/{investmentId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /investments/{investmentId}/transactions-current` | Média-alta | 1.500 | 2.000 | 120 |  |

### Fundos

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /investments` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /investments/{investmentId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /investments/{investmentId}/balances` | Média-alta | 1.500 | 2.000 | 120 |  |
| `GET /investments/{investmentId}/transactions` | Baixa | 4.000 | 1.000 | 8 | Média |
| `GET /investments/{investmentId}/transactions-current` | Média-alta | 1.500 | 2.000 | 120 |  |

### Câmbio

| Endpoint | Frequência | SLA (ms) | TPM | Limite mensal | POC |
|---|---|---|---|---|---|
| `GET /operations` | Média | 2.000 | 1.500 | 30 | Baixa |
| `GET /operations/{operationId}` | Baixa | 4.000 | 1.000 | 8 |  |
| `GET /operations/{operationId}/events` | Média | 2.000 | 1.500 | 30 |  |
