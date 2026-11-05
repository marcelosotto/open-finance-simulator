# Premissas do Projeto

Premissas valem para **toda** a documentação e para todo o desenvolvimento. Qualquer exceção precisa estar escrita e justificada.

**Situação:** aprovadas na revisão de 05/11/2026, com Marcelo ([decisões](../07-gerenciamento/decisoes-revisao-2026-11-05.md)).

## <a id="pr01"></a>PR01 — O padrão é fazer igual ao Open Finance
Sem nenhuma configuração, uma instituição simulada segue as regras oficiais: limites, códigos HTTP, estados, paginação e formatos. Se a regra oficial existe, ela vale. A [ordem das fontes](referencias.md#ordem-das-fontes) resolve conflitos.

## <a id="pr02"></a>PR02 — Desviar da regra é sempre uma escolha explícita
O simulador precisa se comportar mal, porque as instituições reais às vezes se comportam mal. Mas todo desvio da regra oficial é uma **anomalia configurada**: tem nome, é ligada de propósito e aparece no histórico. Um desvio nunca acontece por padrão ou por acaso.

## <a id="pr03"></a>PR03 — Tudo é sintético e se reconhece como sintético
Nenhum dado real de cliente entra no simulador. Todo dado gerado carrega a marca do ambiente e da natureza, de um jeito que continua visível depois de ser copiado para outro sistema ([RN60](../01-requisitos/regras-de-negocio.md#rn60)).

## <a id="pr04"></a>PR04 — O simulador produz o problema; não julga o resultado
O simulador gera lentidão, erro e dado ruim. Ele não valida a qualidade de dados nem diz se o cliente reagiu bem. Quem avalia é o sistema sob teste e suas ferramentas, como o MQD.

## <a id="pr05"></a>PR05 — O mesmo cenário dá o mesmo resultado
Um teste precisa ser repetível. Com a mesma semente, a mesma massa e o mesmo relógio, as mesmas respostas voltam. O sorteio de falhas também parte de uma semente quando o cenário pedir repetição ([RN54](../01-requisitos/regras-de-negocio.md#rn54)).

## <a id="pr06"></a>PR06 — A spec oficial é a fonte das APIs
Rotas, contratos e exemplos saem das specs do repositório `open-finance-specs`, fixadas por commit. Nenhuma API é escrita à mão: uma versão nova entra atualizando o commit, sem reescrever o simulador ([ADR-002](../05-adr/ADR-002-specs-fixadas-por-commit.md)).

## <a id="pr07"></a>PR07 — O cliente sob teste não muda de código para falar com o simulador
Para a receptora, o simulador é uma instituição. Ela usa os mesmos caminhos, cabeçalhos e fluxos que usaria em produção, trocando só o endereço e as credenciais. Atalhos de teste existem, mas ficam em APIs de administração separadas.

## <a id="pr08"></a>PR08 — Foco em desenvolvimento e homologação
O simulador existe para desenvolvimento e homologação, e não recebe dado real. O uso em produção não é bloqueado nem decidido agora. Se um dia houver, será preciso decidir como fica a marcação dos dados ([RN70](../01-requisitos/regras-de-negocio.md#rn70)).
