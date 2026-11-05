# Insumo da constituição

Texto para usar no `/speckit-constitution`, depois de aprovar as premissas e as ADRs.

1. **A documentação em `docs/` é a fonte da verdade.** Toda spec, plano e tarefa cita os IDs de origem (RN, UC, RF, RNF, UI, ADR, PR). Código não cria regra. Comportamento novo entra primeiro em `docs/`.
2. **Regra oficial por padrão.** Sem configuração, o simulador segue o Open Finance ([PR01](../00-visao-geral/premissas.md#pr01)). A ordem de conflito é a das [fontes](../00-visao-geral/referencias.md#ordem-das-fontes).
3. **Desvio sempre explícito.** Toda anomalia tem nome, vem desligada e fica registrada ([PR02](../00-visao-geral/premissas.md#pr02)).
4. **Tudo sintético e marcado.** Nenhum dado real entra, e nenhum dado sai sem marca ([PR03](../00-visao-geral/premissas.md#pr03)).
5. **Produz o problema, não julga.** O simulador não valida qualidade ([PR04](../00-visao-geral/premissas.md#pr04)).
6. **Repetível.** Semente e relógio injetados; mesmo cenário, mesmo resultado ([PR05](../00-visao-geral/premissas.md#pr05)).
7. **Specs como fonte das APIs.** Nenhuma rota oficial escrita à mão ([PR06](../00-visao-geral/premissas.md#pr06)).
8. **Cliente sem mudança.** A receptora só troca endereço e credenciais ([PR07](../00-visao-geral/premissas.md#pr07)).
9. **Testes primeiro (inegociável).** Gherkin aprovado antes da implementação, 100% no domínio e 80% no total.
10. **Simplicidade.** Nenhuma dependência nova sem necessidade demonstrada; trocar item da stack exige ADR.

**Portões de CI:** formatação, análise estática, testes, cobertura, conformidade com as specs e teste da tabela oficial.
