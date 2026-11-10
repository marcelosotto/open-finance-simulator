# ADR-004: Transações por semente, com ajustes persistidos

**Status:** Aceito
**Data:** 2026-11-05

## Contexto
Gravar transações de milhões de clientes sairia caro. Gerar por `recurso + dia` evita esse custo e mantém o saldo coerente.

## Decisão
Usar o gerador por semente. Entidades (cliente, consentimento e recurso) podem ser persistidas ou virtuais. Transações nunca são gravadas. Alterações ficam em **ajustes**, aplicados sobre o resultado calculado.

## Consequências
Volume sem custo de armazenamento e testes repetíveis. Mudar o gerador muda os dados de todas as sementes, por isso o gerador tem versão, gravada no lote.

## Implantação da spec 007
O ajuste de valor passou a gravar também a hora real (`atualizadoEmReal`, e `deReal` e `ateReal` em cada valor anterior), para o corte da paginação ([RN37](../01-requisitos/regras-de-negocio.md#rn37)). A massa lê documentos recusando campos desconhecidos, o que tem duas consequências:
- **Voltar à versão anterior** quebra a leitura das transações já ajustadas, porque a versão anterior recusa esses campos. Risco aceito.
- **Bancos de desenvolvimento** com ajustes gravados por versões intermediárias da branch da 007 devem ser recriados. Produção não é afetada.

O campo `estadosAnteriores` do recurso e do ajuste não é mais gravado nem lido. Ele continua no modelo só para que documentos gravados antes da 007 sigam legíveis.
