# Data model: APIs de dados

Nada novo é gravado.

| Entidade | Campos | Regras |
|---|---|---|
| Transação (massa) | ganha `Estado` | RN44, derivado do relógio; ID provisório só em `TRANSACAO_PROCESSANDO` |
| Família | contrato, tipo de recurso, nome do filtro de data, aceita futuro | Tabela no domínio de Dados |
| Pedido de página | página, tamanho aplicado, total | Mínimo do contrato, máximo da instituição (RN31, RN32) |
| Chave | instante, resumo da consulta, assinatura | Válida por `ValidadeDaChaveMin` (RN35 a RN37) |
| Fatura | ID, mês, total | Soma das transações do mês (RN46) |

Estados de transação: `LANCAMENTO_FUTURO` → `TRANSACAO_PROCESSANDO` (só D+1) → `TRANSACAO_EFETIVADA`, conforme a data avança.
