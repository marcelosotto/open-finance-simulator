# Data Model: Defeitos e anomalias

## EfeitoDaRegra (spec 007, ampliado)
| Campo | Tipo | Regra |
|---|---|---|
| `tipo` | `defeito` | novo valor |
| `defeito` | texto | obrigatório em `defeito`; nome do catálogo (RN110) |
| `campo` | texto | opcional; só em `campo-obrigatorio-ausente`, `formato-invalido` e `enum-invalido` (RN117) |
| `quantidade` | inteiro | opcional; só em `dado-muda-na-paginacao`; diferente de zero; padrão 1 (RN115) |

Os demais campos da regra (alcance, frequência, período, semente, pacote) seguem a 007.

## AnomaliaDaInstituicao (`cenarios`, `_id` = `anomalia:{banco}:{nome}`)
| Campo | Tipo | Regra |
|---|---|---|
| `instituicao` | texto | endereço do banco |
| `nome` | texto | do catálogo (RN113) |
| `ligada` | booleano | ausente = desligada (RN114) |
| `atualizadaEm` | instante | hora real |

## Catálogos
- Defeitos: `campo-obrigatorio-ausente`, `formato-invalido`, `enum-invalido`, `valor-incoerente`, `transaction-id-repetido`, `total-records-incoerente`, `data-fora-da-janela`, `dado-muda-na-paginacao`.
- Anomalias: `revogado-com-403`, `chave-recusada-com-422`, `interacao-diferente`.

## Itens da chamada
- `sim:defeito`: defeito decidido pela etapa.
- `sim:pagina-seguinte`: chave válida (Dados).
