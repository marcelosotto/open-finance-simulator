# Data Model: Comportamento e cenários

## RegraDeCenario (`cenarios`, `_id` = `regra:{id}`)
| Campo | Regra |
|---|---|
| `id`, `nome` | Gerado; nome opcional |
| `instituicao`, `endpoint`, `cliente`, `recurso` | Ao menos um (Q12); `endpoint` é `operationId` conhecido |
| `efeito` | Ver abaixo |
| `frequenciaPct` | 0 a 100, padrão 100; `estado` exige 100 |
| `inicio`, `fim` | UTC, início antes do fim |
| `semente` | Opcional |
| `ligada`, `pacote` | Valem juntos: regra de pacote desligado não vale |
| `criadaEm`, `atualizadaEm` | Hora real |

### Efeito
| `tipo` | Campos |
|---|---|
| `latencia` | `latenciaMs` ≥ 0 |
| `status` | `status`: 200, erro declarado ou 503; `esperaMs` opcional para 504 |
| `timeout` | `esperaMs` opcional, padrão 15.000 |
| `indisponibilidade` | `resultado`: `503`, `504` ou `corte`; `esperaMs` opcional para 504 |
| `transporte` | `falha`: `conexao-cortada`, `corpo-truncado` ou `json-invalido` |
| `estado` | `estado`: um dos quatro de RN100 |

## PacoteDeCenario (`cenarios`, `_id` = `pacote:{nome}`)
`nome`, `ligado` (nasce desligado), `criadoEm`.

## RelogioSimulado (campo `relogio` em `instituicoes`, nulo = real)
`baseSimulada`, `baseReal`, `fator` (0 = fixado). Hora = base simulada + (agora real − base real) × fator.

## Redis
`seq:{regra}:{versao}`: posição da sequência de regra com semente.
