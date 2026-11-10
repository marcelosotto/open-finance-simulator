# Perfis de comportamento

**Estimativas.** Não há telemetria real, então os valores são pontos de partida, e não medições de instituições ([PA20](../01-requisitos/regras-de-negocio.md#pa20)).

| Parâmetro | `estavel` | `lento` | `instavel` | `timeout` | `restritivo` |
|---|---|---|---|---|---|
| p50 em endpoint de alta | 200 ms | 1.200 ms | 300 ms | 300 ms | 200 ms |
| p95 em endpoint de alta | 800 ms | 2.500 ms (fora do SLA) | 1.400 ms | 1.400 ms | 800 ms |
| Taxa de erro | 0,2% | 0,5% | 8% | 1% | 0,5% |
| Erros mais comuns | 500 | 504 | 500 e 504 | 504 | 500 |
| Chamadas que passam de 15 s | Nenhuma | 0,1% | 0,5% | 5% | Nenhuma |
| Janelas de indisponibilidade | Nenhuma | Nenhuma | 2 min a cada hora | Nenhuma | Nenhuma |
| Recurso indisponível ([RN109](../01-requisitos/regras-de-negocio.md#rn109)) | 0,2% | 0,2% | 2% | 0,2% | 1% |
| Limite operacional | Ligado, no oficial | Ligado, no oficial | Ligado, no oficial | Ligado, no oficial | Ligado, na metade do oficial |
| TPM | Oficial | Oficial | Oficial | Oficial | Metade do oficial |
| Validade da `pagination-key` | 60 min | 60 min | 60 min | 60 min | 10 min |
| `page-size` máximo | 1.000 | 500 | 1.000 | 1.000 | 200 |
| Preparação de Recursos (202) | Nenhuma | 30 s | 1 min | 3 min | Nenhuma |

Em endpoints de média e baixa frequência, os tempos crescem na proporção do SLA da classe, a partir da alta: 1 na média-alta, 1,33 na média e 2,67 na baixa.

Na configuração por classe ([RN10](../01-requisitos/regras-de-negocio.md#rn10)), o **tempo típico** é o p50, a **variação** é o p95 menos o p50, e a **cauda lenta** é a porcentagem de chamadas que esperam 15 s e terminam em 504 ([RN12](../01-requisitos/regras-de-negocio.md#rn12)).

O Banco Instável mostra 503 nas janelas de indisponibilidade, e não no sorteio de erros ([RN13](../01-requisitos/regras-de-negocio.md#rn13)).

Todo banco nasce com os limites oficiais, e o Banco Restritivo já vem com os dele alterados. Qualquer banco pode ter os limites alterados depois ([RN26](../01-requisitos/regras-de-negocio.md#rn26)).

Anomalias e defeitos não fazem parte do perfil, nem do Banco Restritivo. Todo banco nasce com as anomalias desligadas ([RN114](../01-requisitos/regras-de-negocio.md#rn114)); elas se ligam por banco, na administração ([RN113](../01-requisitos/regras-de-negocio.md#rn113)), e trocar o perfil não as liga nem desliga. Defeitos são regras de cenário ([RN111](../01-requisitos/regras-de-negocio.md#rn111)). Quando um cenário diz que o Banco Restritivo tem uma anomalia, ela foi ligada no passo do teste.
