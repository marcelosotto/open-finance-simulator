# Regras de Negócio

> Valem as [premissas](../00-visao-geral/premissas.md), em especial [PR01](../00-visao-geral/premissas.md#pr01): sem configuração, o simulador segue a regra oficial. Todas as regras estão em **rascunho**.

Relacionados: [casos de uso](casos-de-uso/README.md) · [requisitos](requisitos.md) · [tabela de endpoints](tabela-de-endpoints.md) · [confronto com a POC](confronto-com-a-poc.md) · [glossário](glossario.md)

**Coluna Origem:** `Oficial` cita a [fonte](../00-visao-geral/referencias.md) da regra; `Produto` é decisão deste projeto e pode ser mudada pela equipe.

## Instituições simuladas
| ID | Regra | Origem |
|---|---|---|
| <a id="rn01"></a>RN01 | Uma **instituição simulada** representa uma transmissora. Tem identificador, nome, `organisationId` simulado, marca, público atendido (PF, PJ ou ambos), produtos oferecidos e um comportamento. | Produto |
| <a id="rn02"></a>RN02 | O ambiente já nasce com os bancos de perfil **Banco Estável, Banco Lento, Banco Instável, Banco Timeout e Banco Restritivo** e com 11 bancos de nome real, listados nas [personas](../00-visao-geral/personas.md#bancos-com-nome-real). Todos podem ser editados, e novos podem ser criados. | Produto |
| <a id="rn03"></a>RN03 | Criar uma instituição exige só **nome** e **perfil de comportamento**. Sem perfil informado, vale `estavel`. Os demais campos têm padrão ([campos obrigatórios](campos-obrigatorios.md)). | Produto |
| <a id="rn04"></a>RN04 | Um **perfil de comportamento** é um modelo: copiar um perfil para uma instituição permite ajustar os valores depois, sem mudar o modelo nem as outras instituições. | Produto |
| <a id="rn05"></a>RN05 | Mudar a configuração de uma instituição vale para as próximas chamadas, sem reiniciar, e fica no histórico com data, autor, antes e depois. O autor é informado por quem chama a administração, sem autenticação ([RN71](#rn71)); sem valor, fica `operador`. | Produto |
| <a id="rn06"></a>RN06 | Uma instituição com clientes não é apagada, só **inativada**. Inativa, ela responde **503**, como fora do ar ([RN13](#rn13)). Instituição sem clientes é apagada, exceto os bancos prontos, que só são inativados. Uma instituição inativa pode ser reativada, e isso fica no histórico. | Produto |
| <a id="rn07"></a>RN07 | A instituição chamada é identificada pelo **endereço** usado, como numa instituição real, e não por um cabeçalho próprio do simulador ([PR07](../00-visao-geral/premissas.md#pr07)). O endereço sai do nome, sem o prefixo do ambiente, em minúsculas e sem acento (`itau`, `banco-estavel`), e não muda se o nome mudar. Dois nomes com o mesmo endereço contam como nome repetido. | Produto |
| <a id="rn08"></a>RN08 | A instituição define se oferece cada produto. Produto não oferecido segue a regra oficial: permissões por seleção individual são removidas do consentimento, e as de seleção agrupada são mantidas ([RN83](#rn83)). | Oficial [F06](../00-visao-geral/referencias.md#f06) |
| <a id="rn09"></a>RN09 | Um banco inspirado em um banco real usa o **nome real com o prefixo do ambiente**: `HML-Itaú` com a marca `hml` e `DEV-Itaú` com `dev`. Com a marca vazia, o nome fica sem prefixo: `Itaú`. Bancos de perfil têm nome fictício. Bancos de nome real usam o perfil `estavel`, e o que os diferencia são variáveis, como os consentimentos ativos. | Produto |

## Comportamento
| ID | Regra | Origem |
|---|---|---|
| <a id="rn10"></a>RN10 | O comportamento de latência é definido por **classe de frequência**: tempo típico, variação e cauda lenta. Não há configuração por endpoint. O perfil diz se o p95 fica dentro ou fora do SLA oficial ([tabela](tabela-de-endpoints.md)). | Produto |
| <a id="rn11"></a>RN11 | O SLA oficial é p95 diário de 1.500 ms (alta e média-alta), 2.000 ms (média) e 4.000 ms (baixa). O simulador usa esses valores como referência dos perfis; não mede nem cobra SLA. | Oficial [F01](../00-visao-geral/referencias.md#f01) |
| <a id="rn12"></a>RN12 | O **timeout oficial** de cada endpoint é 15 segundos. No perfil `timeout`, a chamada afetada fica pendente até o limite e termina em **504** ([PA20](#pa20)). | Oficial [F02](../00-visao-geral/referencias.md#f02) + Produto |
| <a id="rn13"></a>RN13 | Erros são sorteados pela **taxa de erro** e pelos **pesos por status** da instituição. Ao salvar, só é recusado o status que nenhuma spec declara. Na chamada, só entram os que a spec daquele endpoint declara. **429, 423 e 529** não entram no sorteio: só aparecem quando o limite estoura ou por uma regra de cenário. **503** não é declarado pelas specs oficiais e, por isso, não entra nos pesos: é uma exceção intencional, usada só para "fora do ar" ([RN06](#rn06) e [RN16](#rn16)). | Produto |
| <a id="rn14"></a>RN14 | Uma **regra de cenário** muda o comportamento dentro de um alcance: instituição, endpoint, cliente ou recurso, com período opcional. O alcance pode combinar cliente, recurso e endpoint. Exemplo: "Banco Estável fora do ar das 10h às 10h30" ou "este cliente sempre dá 500 em saldos". | Produto |
| <a id="rn15"></a>RN15 | Com várias regras valendo, a mais específica ganha: recurso, cliente, endpoint e instituição, nessa ordem. Se empatar, ganha a que tem mais filtros e, depois, a mais recente. | Produto |
| <a id="rn16"></a>RN16 | Uma **janela de indisponibilidade** faz a instituição responder 503, 504 ou cortar a conexão, conforme configurado, para todas as chamadas do alcance. | Produto |
| <a id="rn17"></a>RN17 | Falhas de transporte também são simuladas: conexão cortada, corpo truncado e JSON inválido. | Produto |
| <a id="rn18"></a>RN18 | O simulador nunca diz na resposta da API por que ela saiu assim ([PR07](../00-visao-geral/premissas.md#pr07)). Ele só responde: quem registra e investiga as chamadas é a aplicação que consome. | Produto |
| <a id="rn19"></a>RN19 | Um **pacote de cenário** é um grupo de regras com nome, ligado e desligado de uma vez. Dentro dele, cada regra segue [RN15](#rn15). | Produto |

## Limites
| ID | Regra | Origem |
|---|---|---|
| <a id="rn20"></a>RN20 | O **limite de tráfego** é por **endpoint e por origem**. A origem autenticada é a instituição receptora (`organizationId`), e a não autenticada é o IP. | Oficial [F03](../00-visao-geral/referencias.md#f03) |
| <a id="rn21"></a>RN21 | O TPM segue a classe do endpoint: baixa 1.000, média 1.500, média-alta 2.000. A alta depende dos consentimentos ativos entre receptora e transmissora (QCA): até 1 milhão, 2.500; até 2 milhões, 5.000; até 3 milhões, 8.000; até 6 milhões, 10.000; acima disso, soma 2.000 a cada faixa de 2 milhões. No simulador, o QCA é um **valor configurado**, sem contar consentimentos: o valor do banco vale para toda receptora e pode ser sobrescrito por receptora. Banco sem valor fica na primeira faixa. | Oficial [F03](../00-visao-geral/referencias.md#f03) |
| <a id="rn22"></a>RN22 | O TPM conta por **minuto cheio**: do segundo 00,000 ao 59,999, zerando na virada. | Oficial [F03](../00-visao-geral/referencias.md#f03) |
| <a id="rn23"></a>RN23 | Passar do TPM responde **429**, com `code` `TOO_MANY_REQUESTS`. Entre os limites, só o TPM produz 429; uma regra de cenário também pode produzi-lo ([RN13](#rn13)). O limite de concorrência global citado no contrato não é simulado. O `Retry-After` vem ligado e é configurável por instituição. | Oficial [F03](../00-visao-geral/referencias.md#f03) |
| <a id="rn24"></a>RN24 | Não há limite por origem em Token, DCR/DCM, Consentimentos, Recursos e Serviços. | Oficial [F03](../00-visao-geral/referencias.md#f03) |
| <a id="rn25"></a>RN25 | O **limite operacional** é opcional para a instituição. Quando ligado, conta por mês, endpoint, objeto mais granular (consentimento, recurso ou produto), cliente e receptora. O objeto é o último parâmetro do caminho do endpoint; em rota sem parâmetro, o consentimento. O mês é o calendário em UTC, pelo relógio da instituição ([RN73](#rn73)). | Oficial [F04](../00-visao-geral/referencias.md#f04) + Produto |
| <a id="rn26"></a>RN26 | Os mínimos por mês são 8 (baixa), 30 (média), 120 (média-alta), 240 (alta) e 420 para saldos e limites da conta. A instituição pode ampliar. No simulador, todo banco nasce com os valores oficiais de TPM, limite operacional e capacidade, e cada valor pode ser alterado depois, mesmo abaixo do oficial. A tela avisa quando isso acontece. | Oficial [F04](../00-visao-geral/referencias.md#f04) + Produto |
| <a id="rn27"></a>RN27 | Só contam no limite operacional as respostas **2XX**. Páginas seguintes da mesma consulta não contam. | Oficial [F01](../00-visao-geral/referencias.md#f01) |
| <a id="rn28"></a>RN28 | Passar do limite operacional responde **423**, com `code` `LOCKED`. Consentimentos, Recursos, Dados Abertos, Serviços e Segurança não têm esse limite. | Oficial [F04](../00-visao-geral/referencias.md#f04) |
| <a id="rn29"></a>RN29 | A **capacidade** mínima é 300 requisições por segundo, por instituição, somando todos os endpoints. Ao passar da capacidade configurada, a instituição responde **529**, com `code` `SITE_IS_OVERLOADED`. Vale nas rotas cujo contrato declara 529; token e administração não contam. | Oficial [F01](../00-visao-geral/referencias.md#f01) e [F12](../00-visao-geral/referencias.md#f12) + Produto |
| <a id="rn30"></a>RN30 | Os contadores valem para todo o ambiente, mesmo com várias instâncias do simulador. Com os contadores indisponíveis, as chamadas seguem como dentro dos limites, e a falha aparece só na observabilidade ([RNF09](requisitos.md#rnf09)). | Produto |

## Paginação
| ID | Regra | Origem |
|---|---|---|
| <a id="rn31"></a>RN31 | `page` começa em 1 e `page-size` vale 25 por padrão, com máximo de 1.000 registros, salvo outro valor na API. O **mínimo é um dado de cada família**, lido do contrato oficial: nas famílias com `minimum: 25` (Renda Fixa Bancária, Renda Fixa Crédito, Renda Variável, Tesouro Direto, Fundos, Câmbio, além de Consentimentos e Recursos), pedir menos entrega 25, exceto na página única e na última; nas famílias com `minimum: 1` (Dados Cadastrais, Contas, Cartão, Empréstimo, Financiamento, Adiantamento e Direitos Creditórios), vale o `page-size` pedido. Acima do máximo, entrega o máximo ([PA21](#pa21)). O contrato vence o portal pela [ordem das fontes](../00-visao-geral/referencias.md#ordem-das-fontes). | Oficial [F12](../00-visao-geral/referencias.md#f12) e [F05](../00-visao-geral/referencias.md#f05) |
| <a id="rn32"></a>RN32 | A instituição pode ter um máximo de `page-size` menor. Pedir acima disso entrega o máximo dela, e o cálculo da página usa esse tamanho: pedir a página 2 de 1.000, com máximo de 800, devolve os itens 801 a 1.600. O `self` mostra o tamanho aplicado. | Oficial [F05](../00-visao-geral/referencias.md#f05) |
| <a id="rn33"></a>RN33 | `meta.totalRecords` e `meta.totalPages` são obrigatórios quando o contrato do endpoint os declara. Sem registros, `totalPages` é **0**. Nas 14 operações de transações que declaram só `requestDateTime` (Contas, Cartão e investimentos), `meta` não traz totais. Pedir página além do total responde 422 `PAGE_NOT_FOUND`, exceto a primeira, que sempre existe, inclusive onde o total não é exposto. **Exceção intencional:** `GET /consents/{consentId}/extensions` responde esse 422 e o 422 `PARAMETRO_INVALIDO` de [RN42](#rn42), embora `consents-3.3.1` não declare 422 nessa rota. | Oficial [F05](../00-visao-geral/referencias.md#f05) e [F12](../00-visao-geral/referencias.md#f12) + Produto |
| <a id="rn34"></a>RN34 | `first` é obrigatório fora da primeira página, `prev` só existe se houver página anterior, `next` só se houver posterior, `last` fora da última, quando o endpoint prevê. Todos usam o mesmo esquema, host, API e versão da chamada. | Oficial [F05](../00-visao-geral/referencias.md#f05) |
| <a id="rn35"></a>RN35 | A instituição cria a `pagination-key` e a envia nos links. A chave pode ser usada por até **60 minutos**. | Oficial [F04](../00-visao-geral/referencias.md#f04) |
| <a id="rn36"></a>RN36 | Com `pagination-key` inválida ou expirada, a instituição gera **nova chave**, responde com **sucesso** e conta a chamada no limite operacional. Com a cota esgotada, responde 423, como uma consulta nova. | Oficial [F04](../00-visao-geral/referencias.md#f04) + Produto |
| <a id="rn37"></a>RN37 | A lista é consistente durante a paginação de uma mesma chave, salvo se uma regra de cenário simular dado mudando no meio ([RN115](#rn115)). A chave guarda a consulta e o instante de emissão, e não é gravada: ajustes feitos depois da emissão não aparecem nas páginas seguintes, e cada ajuste vale pelo valor que tinha na emissão. Nas listas de recursos, valem o estado do recurso na emissão e só os recursos que já existiam. O acesso direto a um recurso usa o estado atual, mesmo com chave. Chave usada com filtros diferentes dos originais é inválida ([RN36](#rn36)). | Produto |

## Padrões das APIs
| ID | Regra | Origem |
|---|---|---|
| <a id="rn38"></a>RN38 | Sem `x-fapi-interaction-id` válido em endpoint protegido, a instituição gera um valor e responde **400**. Com valor válido, devolve o mesmo valor na resposta. | Oficial [F12](../00-visao-geral/referencias.md#f12) |
| <a id="rn39"></a>RN39 | A ordem de avaliação é: cabeçalhos e segurança do token (400, 401, 403), capacidade (529), tráfego (429), acesso ao recurso (403), parâmetros (422), limite operacional (423), regra de cenário e, por fim, a resposta. Segurança vem antes de negócio; o resto da ordem é decisão de produto. | Oficial [F06](../00-visao-geral/referencias.md#f06) + Produto |

## Dados e transações
| ID | Regra | Origem |
|---|---|---|
| <a id="rn40"></a>RN40 | As respostas seguem o schema da versão oficial do endpoint, salvo [defeito configurado](#rn110). | Oficial [F12](../00-visao-geral/referencias.md#f12) |
| <a id="rn41"></a>RN41 | Em Contas, `/transactions` aceita até 12 meses no passado e 12 meses no futuro, e `/transactions-current` aceita 7 dias no passado (D-6) e 12 meses no futuro. Em Cartão e nos investimentos, `/transactions` aceita até 12 meses no passado e `/transactions-current`, 7 dias (D-6), sempre até hoje. Fora disso, responde 422. | Oficial [F12](../00-visao-geral/referencias.md#f12) |
| <a id="rn42"></a>RN42 | `from*` e `to*` andam juntos: enviar só um responde 422 `PARAMETRO_NAO_INFORMADO`. Sem nenhum dos dois, vale o dia atual. Em `/transactions-current` sem datas, vale também o dia atual ([PA09](#pa09)). **Exceções de faturas, porque o contrato não assume o dia atual:** `/bills` aceita datas isoladas e, sem datas, devolve as faturas dos últimos 12 meses, inclusive a fechada que ainda não venceu; `/bills/{billId}/transactions` exige o par e, sem datas, devolve a fatura inteira. `page` menor que 1, `page-size` menor que 1 ou não inteiro, data malformada, `from*` depois de `to*` e janela excedida respondem 422 `PARAMETRO_INVALIDO`. Os filtros `creditDebitIndicator`, `accountType`, `transactionType` e `payeeMCC` valem onde o contrato os declara e comparam com o campo correspondente da resposta; valor sem correspondência, inclusive fora do enum, devolve 200 com lista vazia. | Oficial [F12](../00-visao-geral/referencias.md#f12) |
| <a id="rn43"></a>RN43 | Transações vêm da mais recente para a mais antiga, em todas as famílias. | Oficial [F12](../00-visao-geral/referencias.md#f12) |
| <a id="rn44"></a>RN44 | Uma transação passa pelos estados `LANCAMENTO_FUTURO`, `TRANSACAO_PROCESSANDO` e `TRANSACAO_EFETIVADA`. O `transactionId` só é imutável depois de efetivada, e identificador provisório e definitivo nunca aparecem juntos. O estado sai do relógio: data futura é `LANCAMENTO_FUTURO`; hoje, nos tipos com imutabilidade em D+1, `TRANSACAO_PROCESSANDO`, com identificador provisório; nos demais casos, `TRANSACAO_EFETIVADA`. Lançamentos futuros existem só em Contas e só nos próximos 30 dias; a consulta segue aceitando 12 meses à frente, e além do dia 30 a lista vem vazia, sem cálculo. A hora da transação é fixa e sai da posição dela no dia, na primeira hora UTC, e nunca passa do instante da consulta, exceto em lançamento futuro: quem consulta nessa primeira hora vê, nas transações ainda não alcançadas, o próprio instante da consulta. | Oficial [F08](../00-visao-geral/referencias.md#f08) |
| <a id="rn45"></a>RN45 | A contraparte (`partieCnpjCpf` e demais) segue a tabela oficial por tipo de transação e sentido. | Oficial [F08](../00-visao-geral/referencias.md#f08) |
| <a id="rn46"></a>RN46 | Saldo e extrato são coerentes: saldo = saldo inicial + transações efetivadas. Lançamento futuro e transação em processamento não entram no saldo atual. Cartão: limite usado e faturas batem com as transações do ciclo. Nas demais famílias, o dado é determinístico, válido no schema e coerente só dentro da resposta. | Produto |
| <a id="rn47"></a>RN47 | Dados Cadastrais não aparecem na API de Recursos. Consentimento só com permissões cadastrais devolve lista vazia, com 200. | Oficial [F09](../00-visao-geral/referencias.md#f09) |

## Massa de dados e semente
| ID | Regra | Origem |
|---|---|---|
| <a id="rn49"></a>RN49 | Um **cliente simulado** é identificado pelo documento e não pertence a um banco. Ele é só massa de dados: não faz login nem usa telas. Pode ter produtos em vários bancos e mais de um consentimento no mesmo banco, cada um independente dos outros: revogar um não afeta os demais. Contas, cartões e demais produtos pertencem ao cliente em cada banco. | Produto |
| <a id="rn50"></a>RN50 | Todo dado gerado aqui é **simulado** e tem uma de duas naturezas: **persistido**, gravado no banco, ou **virtual**, calculado na hora a partir de uma semente. | Produto |
| <a id="rn51"></a>RN51 | Cliente, consentimento, recurso, instituição e configuração são **persistidos** quando criados por tela, API ou lote. Transações nunca são gravadas uma a uma: saem de uma semente, com **ajustes** gravados por cima. | Produto |
| <a id="rn52"></a>RN52 | O **mundo virtual** existe para carga: milhões de clientes, consentimentos e recursos calculados pelo índice, sem gravar os itens, com o mesmo gerador da massa persistida. Existe **um mundo virtual por ambiente**: um pedido de 6 milhões de itens se divide entre os bancos pela [distribuição](#rn56), e cada chamada segue as regras do banco do item. O pedido pode filtrar um banco. Ficam gravados só o **total**, a **semente** e a **distribuição**, para reproduzir o mesmo mundo depois. | Produto |
| <a id="rn53"></a>RN53 | Um dado virtual pode receber um **ajuste** persistido (ex.: revogar um consentimento virtual). Uma transação também pode receber um ajuste, como a mudança do valor. Os ajustes são criados por API de administração, e também por `DELETE` e `/extends` em consentimento virtual nas APIs oficiais. Adicionar produto a cliente virtual é recusado. O ajuste vence o cálculo. Trocar a semente ou a distribuição do mundo virtual apaga os ajustes dele, depois de confirmação na tela. Mudar o total preserva os ajustes, e os de itens fora do novo total ficam sem efeito. | Produto |
| <a id="rn54"></a>RN54 | Cada gerador tem semente explícita. A mesma semente com a mesma data e configuração dá o mesmo dado. O sorteio de erro e latência usa semente aleatória por padrão, e uma semente fixa quando o cenário pedir repetição. Dois lotes podem usar a mesma semente no mesmo ambiente: documento e IDs mudam, para não duplicar, e o resto fica igual. | Produto |
| <a id="rn55"></a>RN55 | Um **perfil de dados** define volume de transações por dia, faixa de valores, saldo inicial, tipos de lançamento e produtos. Vêm prontos: cliente comum (R$ 10 a R$ 2.000 por transação e saldo inicial de R$ 5.000), sem movimento (sem transações e saldo inicial de R$ 1.000) e alto volume (R$ 1 a R$ 50.000 por transação e saldo inicial de R$ 1.000.000). | Produto |
| <a id="rn56"></a>RN56 | A **distribuição** diz como a massa se divide. Os **consentimentos** se dividem entre os bancos, e os clientes, entre os perfis de dados; cada uma dessas somas precisa dar 100%. Cada **produto** tem uma porcentagem própria, de 0 a 100%, sem soma, porque um cliente pode ter vários: o perfil de dados diz quais produtos podem existir, e a porcentagem dá a chance de cada um, com 100% se omitida. A distribuição também traz a porcentagem de clientes **PJ**, de 0 a 100%, com padrão 0. O lote também informa quantos consentimentos cada cliente tem, com padrão 1. Cada consentimento sorteia seu banco pela distribuição, usando a semente; o cliente tem produtos em cada banco sorteado, e o banco pode se repetir. Sem distribuição informada, vale 100% no Banco Estável e 100% em cliente comum; sem permissões informadas, todas as válidas para o tipo de pessoa. | Produto |
| <a id="rn57"></a>RN57 | CPFs e CNPJs gerados têm dígitos válidos, inclusive CNPJ alfanumérico. Todo CPF gerado começa com o **dígito 9**, o que permite até 100 milhões de CPFs. Risco aceito: não há garantia de que um CPF gerado não pertença a uma pessoa real. | Produto |
| <a id="rn58"></a>RN58 | Um lote não tem limite de tamanho e pode ser apagado inteiro. Apagar um lote remove clientes, consentimentos, recursos e ajustes dele, e fica no histórico. | Produto |
| <a id="rn59"></a>RN59 | Dados transacionais e de crédito vão até 12 meses para trás, conforme a regra oficial de compartilhamento. Para testar limites, o histórico gerado pode ser maior. Para a frente, só Contas tem lançamentos, até 30 dias ([RN44](#rn44)), calculados sob demanda, sem gravar transações. | Oficial [F10](../00-visao-geral/referencias.md#f10) + Produto |

## Marcação dos dados
| ID | Regra | Origem |
|---|---|---|
| <a id="rn60"></a>RN60 | Todo dado gerado carrega a marca do **ambiente**, para saber de qual ambiente saiu a informação. A marca é um valor livre da configuração da implantação (`dev`, `hml`, `stage` ou outro), sem lista fechada nem regra: o sistema só concatena o valor. Em produção a configuração fica vazia e nada é concatenado ([RN70](#rn70)). A marca aparece em três lugares, para continuar visível depois de copiado: **cabeçalho** da resposta, **prefixo dos identificadores** e **texto** de campos livres. | Produto |
| <a id="rn61"></a>RN61 | O **cabeçalho** `x-sim-environment` traz a marca do ambiente e não é enviado quando a marca está vazia. O `x-sim-data-nature` traz `persistido`, `virtual` ou `misto`. A resposta oficial não muda de formato: receptora que ignora esses cabeçalhos não sente diferença. | Produto |
| <a id="rn62"></a>RN62 | O **prefixo** dos IDs usa os padrões oficiais: `{marca}-p-…` para persistido e `{marca}-v-…` para virtual. Em consentimentos, a natureza também aparece: `urn:{marca}:p-…` e `urn:{marca}:v-…`. Exemplo com `hml`: `hml-p-1a2b3c` e `urn:hml:p-1a2b3c`. Com a marca vazia, só a natureza fica: `p-1a2b3c` e `urn:p:1a2b3c`. Para o ID seguir o padrão oficial, a marca deve usar só letras, números e hífen; o sistema não confere. | Produto |
| <a id="rn63"></a>RN63 | O **texto** marcado usa dois campos, dentro do tamanho oficial: `transactionName` e nome do cliente, com a marca em maiúsculas entre colchetes na frente. Com `hml`: `[HML] PIX recebido` e `[HML] Mariana Alves`; com `dev`: `[DEV] …`. Com a marca vazia, o texto sai sem marca. | Produto |
| <a id="rn64"></a>RN64 | A marca é aplicada na criação do dado e nunca muda. Dado gerado com a marca `dev` continua `dev` mesmo se for copiado para outro ambiente. | Produto |
| <a id="rn65"></a>RN65 | Uma resposta com dado persistido e dado virtual se declara `misto`. Cada item mostra sua natureza no prefixo do ID. | Produto |
| <a id="rn66"></a>RN66 | Os valores e a forma da marca nunca dependem de configuração da instituição: nenhum perfil desliga a marcação. O valor da marca vem só da configuração do ambiente. | Produto |

## Ambientes e administração
| ID | Regra | Origem |
|---|---|---|
| <a id="rn70"></a>RN70 | O foco são os ambientes de desenvolvimento e homologação. O uso em produção não é bloqueado; se houver, a marca do ambiente fica vazia e nada é concatenado ([RN60](#rn60), [PA05](#pa05)). | Produto |
| <a id="rn71"></a>RN71 | APIs de administração e back-office ficam em endereço separado das APIs Open Finance e **não têm login nem autenticação**: servem só para rodar e testar. Toda alteração grava data, autor informado ([RN05](#rn05)), antes e depois. | Produto |
| <a id="rn72"></a>RN72 | Com a marca do ambiente preenchida, cada tela mostra a marca em destaque. Com a marca vazia, nada é mostrado. | Produto |
| <a id="rn73"></a>RN73 | O relógio pode ser **acelerado ou fixado** para testes, sempre por instituição, nunca global. Ele vale para as datas das respostas, as expirações, a cota mensal e o horário das regras de cenário. TPM e capacidade seguem a hora real, porque medem tráfego real. O operador pode voltar ao relógio real a qualquer momento. Com relógio alterado, o cabeçalho `x-sim-clock` mostra a hora simulada ([PA19](#pa19)). | Produto |

## Consentimento
| ID | Regra | Origem |
|---|---|---|
| <a id="rn80"></a>RN80 | Todo consentimento nasce em `AUTHORISED`, criado por `POST /consents`, pelo painel, por lote ou pelo mundo virtual. **Diferença do processo oficial:** em um banco real, o consentimento nasce em `AWAITING_AUTHORISATION`, e só vai para `AUTHORISED` depois que o cliente autoriza. Aqui, sem jornada, essa etapa é pulada. Consentimento gerado por lote ou pelo mundo virtual nasce sem receptora fixa: a receptora vem do token ([UC25](casos-de-uso/README.md#uc25)). Na API, o consentimento compartilha todos os produtos do cliente naquele banco que as permissões cobrem. Documento que não é de um cliente simulado responde 422 `ERRO_NAO_MAPEADO`. Em pedido PJ, o cliente é o CNPJ de `businessEntity`, e `loggedUser` deve ser o CPF de um cliente simulado PF. `POST /consents` para cliente virtual cria um consentimento persistido sobre os recursos virtuais dele ([RN65](#rn65)). | Produto |
| <a id="rn82"></a>RN82 | De `AUTHORISED`, vai para `REJECTED` ao vencer, por revogação na receptora ou no painel, ou por suspeita de fraude. `REJECTED` é final, e registra quem rejeitou (`USER`, `ASPSP`, `TPP`) e a razão: `DELETE` grava `TPP` e `CUSTOMER_MANUALLY_REVOKED`; painel ou administração, `USER` e `CUSTOMER_MANUALLY_REVOKED`; vencimento, `ASPSP` e `CONSENT_MAX_DATE_REACHED`; suspeita de fraude, `ASPSP` e `INTERNAL_SECURITY_REASON`. | Oficial [F06](../00-visao-geral/referencias.md#f06) + Produto |
| <a id="rn83"></a>RN83 | Permissões vêm em grupos oficiais. Combinação fora do grupo responde 422 `COMBINACAO_PERMISSOES_INCORRETA`. Produto de seleção individual não oferecido sai do pedido; de seleção agrupada, fica. Sem permissão funcional restante: 422 `SEM_PERMISSOES_FUNCIONAIS_RESTANTES`. | Oficial [F06](../00-visao-geral/referencias.md#f06) |
| <a id="rn84"></a>RN84 | Permissões PJ sem dados PJ: 422 `INFORMACOES_PJ_NAO_INFORMADAS`. Dados PJ com permissões PF: 422 `PERMISSOES_PJ_INCORRETAS`. PF e PJ juntas: 422 `PERMISSAO_PF_PJ_EM_CONJUNTO`. | Oficial [F06](../00-visao-geral/referencias.md#f06) |
| <a id="rn85"></a>RN85 | Sem `expirationDateTime`, o prazo é indeterminado. | Oficial [F12](../00-visao-geral/referencias.md#f12) |
| <a id="rn86"></a>RN86 | `DELETE /consents/{consentId}` revoga o consentimento. O painel também revoga. | Oficial [F06](../00-visao-geral/referencias.md#f06) + Produto |
| <a id="rn87"></a>RN87 | A **renovação** só vale para `AUTHORISED`, muda só validade e finalidade, e não tem redirecionamento. `REJECTED`: 422 `ESTADO_CONSENTIMENTO_INVALIDO`. Múltiplas alçadas: 422 `DEPENDE_MULTIPLA_ALCADA`. Data inválida: 422 `DATA_EXPIRACAO_INVALIDA`. Em PF, só o CPF criador renova; em PJ, qualquer CPF de cliente simulado, com o CNPJ do consentimento. Documento diferente: 422 `ERRO_NAO_MAPEADO`. | Oficial [F06](../00-visao-geral/referencias.md#f06) + Produto |
| <a id="rn88"></a>RN88 | Consentimento inexistente, não autorizado, revogado ou expirado responde **401** em Recursos e nas APIs de produto, porque o token associado deixa de valer. | Oficial [F08](../00-visao-geral/referencias.md#f08) |

## Criação no painel
| ID | Regra | Origem |
|---|---|---|
| <a id="rn95"></a>RN95 | Ao criar pelo painel, só contas e cartões têm seleção de origem. Todas as opções vêm marcadas, e pelo menos uma precisa ficar. Crédito, investimento, câmbio e dados cadastrais compartilham tudo. | Oficial [F10](../00-visao-geral/referencias.md#f10) |
| <a id="rn98"></a>RN98 | Na PJ, o consentimento pode exigir **múltiplas alçadas**, informando quantas aprovações faltam. Ele fica `AUTHORISED`, com recursos em `PENDING_AUTHORISATION`, até a última aprovação, com prazo de até 15 dias corridos. Aprovar e recusar são feitos no painel. Recusa ou fim do prazo levam os recursos a `UNAVAILABLE`. | Oficial [F07](../00-visao-geral/referencias.md#f07) e [F13](../00-visao-geral/referencias.md#f13) + Produto |

## Recursos
| ID | Regra | Origem |
|---|---|---|
| <a id="rn100"></a>RN100 | Estados: `AVAILABLE`, `TEMPORARILY_UNAVAILABLE`, `UNAVAILABLE` e `PENDING_AUTHORISATION`. | Oficial [F07](../00-visao-geral/referencias.md#f07) |
| <a id="rn101"></a>RN101 | `PENDING_AUTHORISATION` é só estado inicial. `UNAVAILABLE` é final, exceto em Fundos e Renda Variável. `AVAILABLE` e `TEMPORARILY_UNAVAILABLE` alternam entre si e podem ir para `UNAVAILABLE`. | Oficial [F07](../00-visao-geral/referencias.md#f07) |
| <a id="rn102"></a>RN102 | Todo recurso selecionado aparece em Recursos, em qualquer estado. Sem recursos, responde 200 com lista vazia. | Oficial [F07](../00-visao-geral/referencias.md#f07) |
| <a id="rn103"></a>RN103 | Enquanto a instituição prepara a lista, a primeira consulta responde **202** sem corpo. O limite é 5 minutos depois da criação do consentimento; passou disso, **504**. A duração é configurável por instituição. | Oficial [F07](../00-visao-geral/referencias.md#f07) |
| <a id="rn104"></a>RN104 | Recurso fora de `AVAILABLE` não aparece na lista da API de produto (200) e responde **403** no acesso direto, com `STATUS_RESOURCE_<estado>` ([PA08](#pa08)), em todas as famílias com recurso. | Oficial [F08](../00-visao-geral/referencias.md#f08) |
| <a id="rn105"></a>RN105 | Recurso de outro cliente, inexistente ou fora daquele consentimento responde 403 `RESOURCE_FORBIDDEN`, em todas as famílias com recurso. Vale também para fatura de outro recurso, do mês aberto, sem transações ou vencida há mais de 12 meses. | Oficial [F08](../00-visao-geral/referencias.md#f08) + Produto |
| <a id="rn106"></a>RN106 | Em seleção agrupada, um produto novo do cliente entra sozinho no consentimento vigente. A tela permite adicionar um recurso para simular isso. | Oficial [F07](../00-visao-geral/referencias.md#f07) |
| <a id="rn107"></a>RN107 | Estados por recurso são independentes dentro do mesmo consentimento. | Oficial [F07](../00-visao-geral/referencias.md#f07) |
| <a id="rn108"></a>RN108 | Mudar o estado de um recurso vale na próxima chamada e pode ser agendado por regra de cenário. Em recurso virtual, a mudança é um [ajuste](#rn53). | Produto |

## Defeitos e anomalias
| ID | Regra | Origem |
|---|---|---|
| <a id="rn110"></a>RN110 | Um **defeito de dado** altera o conteúdo da resposta de propósito: campo obrigatório faltando, formato inválido, enum fora da lista, valor incoerente, data fora da janela, `transactionId` repetido, `totalRecords` errado. | Produto |
| <a id="rn111"></a>RN111 | O defeito tem taxa e alcance como uma [regra de cenário](#rn14). Sem configuração, nenhum defeito acontece. | Produto |
| <a id="rn112"></a>RN112 | O simulador não diz na resposta da API que há defeito. | Produto |
| <a id="rn113"></a>RN113 | Uma **anomalia de protocolo** faz a instituição desobedecer uma regra oficial: `pagination-key` inválida com 422, 401 trocado por 403, `x-fapi-interaction-id` diferente. Cada anomalia tem nome, vem desligada e fica registrada ([PR02](../00-visao-geral/premissas.md#pr02)). | Produto |
| <a id="rn114"></a>RN114 | Nenhum perfil nasce com anomalias ligadas. Quando há alguma ligada, isso aparece na tela da instituição. | Produto |
| <a id="rn115"></a>RN115 | Um cenário pode simular dado mudando durante a paginação: registros novos ou removidos entre as páginas. | Produto |

## Segurança
| ID | Regra | Origem |
|---|---|---|
| <a id="rn120"></a>RN120 | O **modo simples** é o modo de segurança desta versão: o operador cadastra a receptora pela administração (nome obrigatório, `organisationId` gerado se omitido, nenhuma pronta ao subir o ambiente), e ela recebe usuário e senha de teste. Há um único `POST /token` no endereço do banco, no formato OAuth (`client_id` e `client_secret`): `scope=consents` dá o **token da receptora**, usado em criar, consultar e revogar consentimentos; `scope=consent:{consentId}` dá o **token do consentimento**, usado em renovar, em Recursos e nas APIs de produto. O token vale 900 segundos pelo relógio injetado, sem refresh, e só no banco que o emitiu; em outro banco responde 401. Só consentimento `AUTHORISED` recebe token do consentimento; o token da receptora não depende de consentimento. Consentimento criado por API só serve à receptora criadora: outra recebe recusa no token e 403 ao consultar ou revogar. Consentimento de lote ou do mundo virtual serve a qualquer receptora cadastrada, sem gravar vínculo. **Diferença do processo oficial:** a renovação oficial exige token `authorization_code`, com redirecionamento; no modo simples, ela usa o token do consentimento, pedido com usuário e senha. Sem nenhuma autenticação não é opção ([ADR-006](../05-adr/ADR-006-modos-de-seguranca.md)). | Oficial [F12](../00-visao-geral/referencias.md#f12) + Produto |
| <a id="rn121"></a>RN121 | No modo FAPI, que está [adiado](#adiados), o servidor de autorização exige PAR e PKCE, `request_uri` de uso único e válido por pelo menos 120 segundos, e access token entre 300 e 900 segundos. | Oficial [F13](../00-visao-geral/referencias.md#f13) |
| <a id="rn122"></a>RN122 | O token do consentimento dá acesso aos escopos concedidos e ao `consent:{consentId}`; o token da receptora dá acesso só ao escopo `consents`. Revogado o consentimento, o token dele deixa de valer em Recursos e nas APIs de produto ([RN88](#rn88)); na renovação, a resposta é a 422 de [RN87](#rn87). | Oficial [F08](../00-visao-geral/referencias.md#f08) |
| <a id="rn123"></a>RN123 | Credenciais e certificados do simulador são de teste e não servem para nada fora dele. | Produto |

## <a id="adiados"></a>Adiados
Fora desta versão. Voltam como novos casos de uso, quando fizerem falta.

- **Intenção pendente:** consentimento em `AWAITING_AUTHORISATION`, com expiração em 60 minutos ([F06](../00-visao-geral/referencias.md#f06)).
- **Jornada com redirecionamento:** login no banco, titularidade, confirmação e volta à receptora ([F10](../00-visao-geral/referencias.md#f10) e [F11](../00-visao-geral/referencias.md#f11)).
- **Consentimento recusado ou não autorizado:** simular esses casos em testes.
- **Modo FAPI:** depende da jornada com redirecionamento.

## Perguntas em aberto
| ID | Pergunta |
|---|---|
| <a id="pa01"></a>PA01 | ~~Qual banco guarda o estado?~~ **Respondida:** MongoDB para o estado, porque a empresa já usa, e Redis para os contadores, por desempenho. Ver [ADR-003](../05-adr/ADR-003-mongodb-e-redis.md). |
| <a id="pa02"></a>PA02 | ~~Quando o modo FAPI completo entra?~~ **Respondida:** adiado, porque depende da jornada com redirecionamento. Até lá, modo simples ([adiados](#adiados)). |
| <a id="pa03"></a>PA03 | ~~Usamos nomes de bancos reais?~~ **Respondida:** sim, com o prefixo do ambiente. Bancos de perfil continuam com nome fictício. Ver [RN09](#rn09). |
| <a id="pa04"></a>PA04 | ~~Como evitar CPF de pessoa real?~~ **Respondida:** CPF com um dígito inicial fixo, até 100 milhões. O risco foi aceito. Ver [RN57](#rn57). |
| <a id="pa05"></a>PA05 | ~~E o ambiente de produção?~~ **Respondida:** o uso em produção não é bloqueado. Em produção a marca do ambiente fica vazia ([RN60](#rn60), [RN70](#rn70)). |
| <a id="pa06"></a>PA06 | ~~Qual o escopo depois dos dados?~~ **Respondida:** a primeira versão cobre só dados e consentimento. Os demais ficam para depois. |
| <a id="pa07"></a>PA07 | ~~O simulador envia reportes à PCM?~~ **Respondida:** não na primeira versão. |
| <a id="pa08"></a>PA08 | ~~Qual o código de bloqueio temporário?~~ **Respondida:** seguir a orientação de cada API. |
| <a id="pa09"></a>PA09 | ~~Qual o padrão de `/transactions-current` sem datas?~~ **Respondida:** dia atual, como diz a spec. |
| <a id="pa10"></a>PA10 | ~~Quantas receptoras?~~ **Respondida:** várias receptoras, com limites e QCA separados por receptora. |
| <a id="pa11"></a>PA11 | ~~Mandamos `Retry-After` no 429?~~ **Respondida:** sim, configurável por instituição e ligado no padrão. |
| <a id="pa12"></a>PA12 | ~~Precisamos de um Diretório simulado?~~ **Respondida:** não. O endereço da instituição fica em configuração. |
| <a id="pa13"></a>PA13 | ~~Várias versões da mesma API ao mesmo tempo?~~ **Respondida:** não. Uma versão por API. |
| <a id="pa14"></a>PA14 | ~~Qual a tecnologia do back-office?~~ **Respondida:** React com TypeScript, com Webpack e Clean Architecture, sem login. Ver [ADR-010](../05-adr/ADR-010-front-end-react.md) e o [guia de front-end](../08-front-end/README.md). |
| <a id="pa15"></a>PA15 | ~~Quem aprova?~~ **Respondida:** Marcelo, por enquanto. |
| <a id="pa16"></a>PA16 | ~~Onde roda em homologação?~~ **Respondida:** Docker agora. A plataforma de homologação é decidida na implantação. |
| <a id="pa17"></a>PA17 | ~~Por quanto tempo guardamos a massa?~~ **Respondida:** massa sem limpeza automática. O simulador não guarda registro das chamadas. |
| <a id="pa18"></a>PA18 | ~~Como é o login simulado?~~ **Respondida:** não há login de cliente. A pessoa usa o painel, já autenticada ([RN49](#rn49)). |
| <a id="pa19"></a>PA19 | ~~O relógio acelerado vale em homologação?~~ **Respondida:** sim, só por instituição, nunca global. |
| <a id="pa21"></a>PA21 | ~~O que fazer com `page-size` acima de 1.000?~~ **Respondida:** reduzir para 1.000 e mostrar no `self`. |
| <a id="pa22"></a>PA22 | ~~Quais bancos de nome real nascem prontos, e com qual comportamento?~~ **Respondida:** os 11 nomes listados nas personas, com o perfil `estavel` e variáveis próprias, sem perfil novo. Ver [RN09](#rn09). |
| <a id="pa23"></a>PA23 | ~~Qual dígito inicial do CPF e quais campos levam a marca?~~ **Respondida:** dígito 9. A marca vai em `transactionName` e no nome do cliente. |
| <a id="pa20"></a>PA20 | ~~Como um timeout real se parece?~~ **Respondida (provisória):** assumimos 504 até alguém confirmar nas instituições. Não há telemetria real, então os perfis são estimativas. Ver [RN12](#rn12). |
