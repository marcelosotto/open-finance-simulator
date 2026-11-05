# Perfis de comportamento

**Estimativas.** Não há telemetria real, então os valores são pontos de partida, e não medições de instituições ([PA20](../01-requisitos/regras-de-negocio.md#pa20)).

| Parâmetro | `estavel` | `lento` | `instavel` | `timeout` | `restritivo` |
|---|---|---|---|---|---|
| p50 em endpoint de alta | 200 ms | 1.200 ms | 300 ms | 300 ms | 200 ms |
| p95 em endpoint de alta | 800 ms | 2.500 ms (fora do SLA) | 1.400 ms | 1.400 ms | 800 ms |
| Taxa de erro | 0,2% | 0,5% | 8% | 1% | 0,5% |
| Erros mais comuns | 500 | 504 | 500, 503 e 504 | 504 | 500 |
| Chamadas que passam de 15 s | Nenhuma | 0,1% | 0,5% | 5% | Nenhuma |
| Janelas de indisponibilidade | Nenhuma | Nenhuma | 2 min a cada hora | Nenhuma | Nenhuma |
| Recurso indisponível | 0,2% | 0,2% | 2% | 0,2% | 1% |
| Limite operacional | Ligado no mínimo oficial | Ligado | Ligado | Ligado | Ligado |
| `page-size` máximo | 1.000 | 500 | 1.000 | 1.000 | 200 |
| Preparação de Recursos (202) | Nenhuma | 30 s | 1 min | 3 min | Nenhuma |
| Anomalias ligadas | Nenhuma | Nenhuma | Nenhuma | Nenhuma | Cota e TPM abaixo do oficial |

Em endpoints de média e baixa frequência, os tempos crescem na proporção do SLA da classe.

Os `page-size` menores são permitidos pela regra oficial ([RN32](../01-requisitos/regras-de-negocio.md#rn32)). Limite abaixo do oficial não é, e por isso aparece como anomalia ([RN113](../01-requisitos/regras-de-negocio.md#rn113)).
