# Research: Defeitos e anomalias

## R01. Onde aplicar o defeito

**Decisão:** na saída, no `FiltroDaChamada` do host. O resultado normal roda num buffer, o JSON é alterado pelo domínio de Cenários e depois é escrito. O defeito é decidido na etapa de comportamento (`ComportamentoDaChamada.Decidir`), que guarda o defeito no item da chamada.

**Por quê:** é o mesmo ponto da falha de transporte (RN17), cobre Dados, Recursos e Consentimentos sem mexer na montagem, e a cota já foi contada quando o 2XX foi decidido (RN27, ADR-008, RN116).

**Alternativa rejeitada:** alterar a montagem em `ServicoDeDados`, porque não cobriria Consentimentos e misturaria defeito com dado.

## R02. Disputa e sorteio

**Decisão:** `SelecaoDeRegras.Vencedora` ignora as regras de defeito, como já ignora as de estado. `VencedoraDeDefeito` escolhe entre as de defeito pela ordem de RN15. A frequência da vencedora de defeito é sorteada sempre que a chamada chega à etapa, antes da decisão do perfil, e a sequência fica em `seq:{regra}:{atualizadaEm}`. O defeito só entra na decisão quando ela segue normal ou termina com corpo truncado ou JSON inválido.

## R03. Cache

**Decisão:** o cache de 5 s passa a guardar um retrato com as regras ativas e as anomalias ligadas por banco. Ele continua com o mesmo mecanismo: leitura fora da trava, descarte da leitura que falhou e geração invalidada por escrita. Toda escrita de anomalia invalida o cache.

## R04. Anomalias sem corrida de escrita

**Decisão:** um documento por banco e anomalia (`anomalia:{banco}:{nome}`). Ligar duas anomalias do mesmo banco ao mesmo tempo não perde nenhuma, porque não há ler-alterar-gravar no mesmo documento. Isso detalha a Q8, que falava em `anomalias:{banco}`.

## R05. 401 e `x-fapi-interaction-id`

**Decisão:** `PortaoPeloConsentimento`, no host, consulta as anomalias do banco da rota depois do portão:

- resultado com status 401 vira 403 `FORBIDDEN`;
- cabeçalho de resposta igual ao recebido vira um UUID v4 novo, do gerador injetado.

Consentimentos não muda.

## R06. Chave de paginação

**Decisão:** `ServicoDeDados.Responder` faz duas coisas novas:

- com `pagination-key` informada que `ChaveDePaginacao.Abrir` recusa, consulta a anomalia e responde 422 `PARAMETRO_INVALIDO` antes da cota;
- com chave válida, marca o item `sim:pagina-seguinte`, usado por `dado-muda-na-paginacao`.

A chave `v2`, o corte por hora real e o conjunto de recursos não mudam.

## R07. Schema do alvo

**Decisão:** o filtro usa o schema da resposta do status que saiu (`OperacaoDoCatalogo.Respostas`, com `$ref` resolvidos). Sem `properties.data`, o defeito não age. `totalPages` de RN115 é recalculado pelo `page-size` do link `self`.

## R07a. Campo de data sem `format`

**Achado na implementação:** `transactionDateTime` de Contas (`accounts-2.5.1.yaml:956`) não declara `format`, só `pattern`. Pelo `format`, `formato-invalido` e `data-fora-da-janela` não achariam nenhum campo nas transações de Contas.

**Decisão:** campo de data é o que tem `format` `date` ou `date-time`, ou, sem `format`, o que tem nome terminado em `Date` (data) ou `DateTime` (data e hora). Registrado em RN117.

Também por isso, as transações de Contas não têm `totalRecords` (`MetaOnlyRequestDateTime`). Os passos de `total-records-incoerente` e de RN115 usam as transações da fatura do cartão, cujo `meta` tem os totais.

## R08. Instituições

**Decisão:** a porta `IConsultaDeAnomalias` fica em `Simulador.Instituicoes.Contratos`, com a implementação vazia `SemAnomalias`, como `IConsultaDeClientes`. O host a implementa lendo o cache de Cenários.

## R09. Revisão independente (2026-11-10)

Correções aprovadas pelo Marcelo depois da revisão:

1. Apagar o banco (RN06) apaga as anomalias dele, pela porta `ILimpezaDeAnomalias` de Instituições, que o host implementa com Cenários. O banco recriado com o mesmo nome nasce sem anomalias (RN113, RN114).
2. A `quantidade` de `dado-muda-na-paginacao` vai de -1000 a 1000, sem o zero. Fora disso, a gravação recusa com erro de validação, e a aplicação ainda limita o valor, de modo que `int.MinValue` não estoura (RN115).
3. Sem `campo`, o alvo é o primeiro candidato presente no item; o defeito não falha em silêncio quando falta o campo preferido (RN117).
4. Um teste prova que a chamada com erro do perfil também avança a sequência do defeito (Q2, RN111).
5. O sorteio sem semente do defeito usa um gerador próprio (`GeradorDosDefeitos`), com a semente do ambiente deslocada. Assim, as outras sequências não se deslocam (RN111, ADR-004).
6. Se a leitura das anomalias falhar (MongoDB fora), a anomalia conta como desligada: o 401 do portão segue 401, sem virar 500 (RN113, ADR-008).
7. Docs: RN15 ganha a exceção das regras de estado e de defeito, RN38 a nota de `interacao-diferente`, e a ADR-008 diz onde agem anomalias e defeitos. Perfis deixa claro que anomalias e defeitos não fazem parte do perfil.
8. Schema em cache: o `JsonObject` de `JsonNode.Parse` monta os filhos na primeira leitura, sem trava. Como chamadas simultâneas compartilham o schema, a árvore é montada inteira antes de entrar no cache.
9. `formato-invalido` só reescreve texto: com `campo` objeto, lista ou número, a resposta sai normal (RN117).

### Segunda rodada (2026-11-10)

- O MongoDB fora leva cerca de 30 s para acusar, e o portão lê as anomalias duas vezes. Agora a consulta na chamada espera no máximo 1 s e guarda a falha por 5 s: o portão e a chave degradam depressa. Os testes simulam leitura que nunca termina.
- `Remover` apaga as anomalias antes do banco. Se a limpeza falha, o banco fica e o histórico não registra a remoção.
- `SementeDoAmbiente` lê `Simulador:Semente` do mesmo jeito no host e em Cenários. Valor ausente ou malformado vale uma semente sorteada uma vez por processo. O gerador dos defeitos usa sempre a semente derivada por XOR, então as duas sementes nunca coincidem.
- RN111 deixa de citar a ADR-004, RN115 justifica o teto como decisão de produto, e RN113 registra que a administração responde 500 com o MongoDB fora.
