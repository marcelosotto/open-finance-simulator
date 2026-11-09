# Modelo de dados: Massa e gerador

| Entidade | Campos | Regras |
|---|---|---|
| PerfilDeDados | nome, transacoesMin, transacoesMax, valorMin, valorMax, saldoInicial, tiposDeLancamento, produtos | Nome obrigatório e único. Min ≤ max. Prontos não mudam nem saem. Em uso não sai. |
| Distribuicao | bancos (endereço → %), perfis (nome → %), produtos (nome → %), pjPct | Bancos e perfis somam 100%, com até 2 casas. Produtos e PJ entre 0 e 100. |
| ClienteSimulado | documento, tipo (`PF`/`PJ`), nome, marca, natureza, loteId? | Documento é a chave. Não pertence a um banco. |
| Recurso | resourceId, cliente, banco, tipo, produto, estado, perfil, saldoInicial, sementeDeDados, criadoEm, inicioDoHistorico, marca, natureza, loteId? | Estado inicial `AVAILABLE`. |
| Consentimento | consentId, cliente, banco, estado, permissoes, expiraEm?, recursos, receptora?, rejeitadoPor?, criadoEm, marca, natureza, loteId? | Nasce `AUTHORISED`, sem receptora. |
| Lote | id, quantidade, distribuicao, consentimentosPorCliente, permissoes?, semente, versaoDoGerador, historicoMeses, posicaoInicial, data, processados, situacao, motivo?, resumo, atualizadoEm | Situações: `em_andamento`, `concluido`, `parcial`. Motivos: `cancelado`, `interrompido`, `falha`. |
| MundoVirtual | total, semente, distribuicao | Um por ambiente. Total de 1 a 50.000.000. |
| Ajuste | alvo, tipo, valor?, estado?, rejeitadoPor?, recursoId?, banco?, loteId?, doMundo | A chave é `alvo`. Tipos: `transacao`, `consentimento`. |

## Perfis prontos
| Perfil | Transações/dia | Valor por transação | Saldo inicial |
|---|---|---|---|
| `cliente-comum` | 0 a 4 | R$ 10 a R$ 2.000 | R$ 5.000 |
| `sem-movimento` | 0 | — | R$ 1.000 |
| `alto-volume` | 2.000 a 3.000 | R$ 1 a R$ 50.000 | R$ 1.000.000 |

## Produtos
| Produto | Tipo oficial | Permissões |
|---|---|---|
| `contas` | `ACCOUNT` | `ACCOUNTS_*` |
| `cartoes` | `CREDIT_CARD_ACCOUNT` | `CREDIT_CARDS_ACCOUNTS_*` |
| `emprestimos` | `LOAN` | `LOANS_*` |
| `financiamentos` | `FINANCING` | `FINANCINGS_*` |
| `adiantamentos` | `UNARRANGED_ACCOUNT_OVERDRAFT` | `UNARRANGED_ACCOUNTS_OVERDRAFT_*` |
| `direitos-creditorios` | `INVOICE_FINANCING` | `INVOICE_FINANCINGS_*` |
| `investimentos` | `BANK_FIXED_INCOME` | `BANK_FIXED_INCOMES_READ` |
| `cambio` | `EXCHANGE` | `EXCHANGES_READ` |

Um consentimento recebe os recursos do cliente naquele banco cobertos pelas permissões (RN80). O produto só é gerado se o banco o oferece (RN08).
