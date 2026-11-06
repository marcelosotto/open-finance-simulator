# Contrato: ferramenta de linha de comando (`tools/Simulador.Ferramentas`)

Origem: [UC24](../../../docs/01-requisitos/casos-de-uso/README.md#uc24), FR-002, FR-009 e FR-027 da [spec](../spec.md). Decisões em [research.md R01 e R10](../research.md).

Uso: `dotnet run --project tools/Simulador.Ferramentas -- <comando> [opções]`.
Código de saída `0` em sucesso e `1` em falha. As mensagens de erro nomeiam o arquivo ou a
operação.

| Comando | O que faz | Opções |
|---|---|---|
| `specs baixar` | Baixa o pacote do commit do `specs.lock.json` e grava os contratos travados e o `catalog.json` em `.specs/{commit}/` | `--token` (opcional; padrão: variável `SPECS_TOKEN`, se existir); `--lock` (padrão: `specs.lock.json`) |
| `specs verificar` | Confere o SHA-256 de cada contrato contra o lock e contra o `catalog.json`. Falha em divergência, arquivo faltando ou contrato fora do catálogo | `--lock`, `--pasta` (padrão: `.specs/{commit}`) |
| `specs diferencas` | Compara as operações de dois locks e lista novas, removidas e alteradas. Com `--resumo`, escreve em Markdown no arquivo indicado (no CI, `$GITHUB_STEP_SUMMARY`) | `--base <lock>`, `--novo <lock>`, `--resumo <arquivo>` |
| `cobertura conferir` | Lê relatórios Cobertura e falha abaixo do mínimo | `--relatorios <glob>`, `--dominio 100`, `--total 80`, `--filtro-dominio "*.Dominio.*"` |

## Saída de `specs diferencas` (exemplo)

```markdown
## Specs: 7bdb9c1 → 1a2b3c4

### Novas (1)
- GET /open-banking/accounts/v2/accounts/{accountId}/statements (accounts-2.6.0)

### Alteradas (1)
- GET /open-banking/consents/v3/consents/{consentId}: schema de resposta mudou

### Removidas (0)
```
