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
