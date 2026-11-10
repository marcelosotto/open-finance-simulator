# Comportamento de tela

Detalhes de interface que **não são regra de negócio**. Ao mudar um botão, atualize só este documento. Como construir as telas está no [guia de front-end](../08-front-end/README.md).

## Back-office
| ID | Tela | Comportamento | Regra |
|---|---|---|---|
| <a id="ui01"></a>UI01 | Topo | Mostra a marca do ambiente em destaque, com cor diferente por marca. Com a marca vazia, não mostra nada. | [RN72](regras-de-negocio.md#rn72) |
| <a id="ui02"></a>UI02 | Bancos | Lista os bancos com perfil, situação e um aviso quando há anomalias ligadas. | [RN114](regras-de-negocio.md#rn114) |
| <a id="ui03"></a>UI03 | Novo banco | Pede nome e perfil; o resto já vem preenchido e pode ser ajustado. | [RN03](regras-de-negocio.md#rn03) |
| <a id="ui04"></a>UI04 | Comportamento | Mostra ao lado de cada classe o SLA oficial e se o valor configurado fica dentro ou fora. | [RN11](regras-de-negocio.md#rn11) |
| <a id="ui05"></a>UI05 | Limites | Mostra o valor oficial ao lado do configurado. Aceita qualquer valor e avisa quando fica abaixo do oficial. | [RN26](regras-de-negocio.md#rn26) |
| <a id="ui06"></a>UI06 | Cenários | Lista as regras ativas por alcance e mostra qual vence em caso de conflito. | [RN15](regras-de-negocio.md#rn15) |
| <a id="ui07"></a>UI07 | Distribuição | Soma as porcentagens de bancos e perfis e bloqueia salvar fora de 100%. Produtos têm porcentagem própria, sem soma. | [RN56](regras-de-negocio.md#rn56) |

## Consentimento no painel
| ID | Tela | Comportamento | Regra |
|---|---|---|---|
| <a id="ui10"></a>UI10 | Novo consentimento | Um menu em passos: cliente, instituição e receptora, permissões e prazo. | [RN80](regras-de-negocio.md#rn80) |
| <a id="ui13"></a>UI13 | Seleção de origem | Contas e cartões do cliente naquele banco, já marcados, só os de produto que o banco ainda oferece. Com opção, pelo menos um fica marcado; sem opção, a seção não aparece. Crédito, investimento, câmbio e cadastro aparecem só como grupos, sem seleção. | [RN95](regras-de-negocio.md#rn95), [RN96](regras-de-negocio.md#rn96) |
| <a id="ui14"></a>UI14 | Alçadas | Mostra quantas aprovações faltam, com botões de aprovar e recusar. | [RN98](regras-de-negocio.md#rn98) |
| <a id="ui16"></a>UI16 | Todas | Faixa fixa: "Ambiente simulado, dados fictícios". | [PR03](../00-visao-geral/premissas.md#pr03) |
