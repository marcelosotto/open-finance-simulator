# Data Model: Instituições simuladas

Fonte: [modelo de dados](../../docs/04-dados/modelo-de-dados.md) e [perfis](../../docs/03-design/perfis-de-comportamento.md).

## Instituicao (`instituicoes`, `_id` = endereco)
| Campo | Regra |
|---|---|
| endereco | Do nome, sem prefixo, `^[a-z0-9-]{1,64}$`; imutável (RN07) |
| nome | Obrigatório; com prefixo de ambiente se inspirado em banco real (RN09) |
| organisationId | `IMarcador`, natureza persistido (RN62); imutável |
| perfil | Um dos 5; imutável depois de copiado (RN04) |
| marca | Padrão: nome |
| publico | `PF`, `PJ` ou `ambos`; padrão `ambos` |
| produtos | Subconjunto não repetido de `contas`, `cartoes`, `emprestimos`, `financiamentos`, `adiantamentos`, `direitos-creditorios`, `investimentos`, `cambio`; padrão todos |
| situacao | `ativa` ⇄ `inativa` (RN06) |
| pronta | Verdadeiro nos 16 bancos da semeadura |
| comportamento, limites | Cópias ajustáveis |

## Comportamento
- `latenciaPorClasse`: `alta`, `media-alta`, `media`, `baixa` → `tipicoMs`, `variacaoMs`, `caudaLentaPct`
- `taxaDeErroPct`: 0 a 100
- `pesosPorStatus`: status → peso ≥ 0; recusa status não declarado e 429, 423, 529
- `janela`: `duracaoMin`, `intervaloMin`, opcional
- `recursoIndisponivelPct`: 0 a 100

Com taxa acima de zero, ao menos um peso precisa ser positivo.

## Limites
`qca`, `qcaPorReceptora`, `tpm` (`alta` opcional — nulo segue a faixa de QCA —, `mediaAlta`, `media`, `baixa`), `limiteOperacionalLigado`, `limiteMensal` (`alta`, `saldosELimites`, `mediaAlta`, `media`, `baixa`), `capacidadeTps`, `pageSizeMaximo`, `validadeDaChaveMin`, `retryAfterLigado`, `retryAfterSeg`, `preparacaoDeRecursosSeg`.

Valores devem ser positivos; `preparacaoDeRecursosSeg` e QCA aceitam zero, e `pageSizeMaximo` vai até 1.000 (RN31). TPM da alta por QCA conforme RN21.

## EntradaDeHistorico (`historico`)
`instituicao`, `data`, `autor`, `acao` (`criada`, `alterada`, `inativada`, `reativada`, `apagada`), `mudancas[]` com `campo`, `antes`, `depois`. O histórico fica mesmo com a instituição apagada.

## Transições de `DELETE`
| Estado | Resultado |
|---|---|
| Com clientes ou pronta, ativa | Inativa, com histórico |
| Com clientes ou pronta, já inativa | Sem mudança |
| Sem clientes e não pronta | Apagada, com histórico |
