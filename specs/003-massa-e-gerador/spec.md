# Feature Specification: Massa e gerador

**Feature Branch**: `claude/003-massa-e-gerador-9b2f94`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: "Massa e gerador: UC05 a UC08 e UC27, com semente, mundo virtual, ajustes e lotes. Usar docs/ e massa-e-semente.feature. Nome curto: massa-e-gerador"

**Origem em `docs/`**: [UC05](../../docs/01-requisitos/casos-de-uso/README.md#uc05), [UC06](../../docs/01-requisitos/casos-de-uso/README.md#uc06), [UC07](../../docs/01-requisitos/casos-de-uso/README.md#uc07), [UC08](../../docs/01-requisitos/casos-de-uso/README.md#uc08), [UC27](../../docs/01-requisitos/casos-de-uso/README.md#uc27), [RF05](../../docs/01-requisitos/requisitos.md#rf05), [RF06](../../docs/01-requisitos/requisitos.md#rf06), [RF07](../../docs/01-requisitos/requisitos.md#rf07), [RF21](../../docs/01-requisitos/requisitos.md#rf21), [RN46](../../docs/01-requisitos/regras-de-negocio.md#rn46), [RN49](../../docs/01-requisitos/regras-de-negocio.md#rn49) a [RN59](../../docs/01-requisitos/regras-de-negocio.md#rn59), [RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60) a [RN66](../../docs/01-requisitos/regras-de-negocio.md#rn66), [RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71), [RN80](../../docs/01-requisitos/regras-de-negocio.md#rn80), [RN85](../../docs/01-requisitos/regras-de-negocio.md#rn85), [RNF05](../../docs/01-requisitos/requisitos.md#rnf05), [RNF06](../../docs/01-requisitos/requisitos.md#rnf06), [RNF10](../../docs/01-requisitos/requisitos.md#rnf10), [PA17](../../docs/01-requisitos/regras-de-negocio.md#pa17), [PA23](../../docs/01-requisitos/regras-de-negocio.md#pa23), [ADR-004](../../docs/05-adr/ADR-004-semente-e-ajustes.md), [ADR-009](../../docs/05-adr/ADR-009-gherkin-reqnroll.md), [UI07](../../docs/01-requisitos/interface.md#ui07), [campos obrigatórios](../../docs/01-requisitos/campos-obrigatorios.md), [modelo de dados](../../docs/04-dados/modelo-de-dados.md). Cenários Gherkin: [massa-e-semente.feature](../../docs/01-requisitos/gherkin/massa-e-semente.feature).

## Clarifications

### Session 2026-11-09

Decisões aprovadas por Marcelo em 2026-11-09. As respostas de Q1, Q3 a Q8 e a parte de ajustes de Q2 ainda precisam entrar em `docs/` antes do plano (princípio I).

- Q1: A quem pertence um consentimento gerado por lote ou pelo mundo virtual? → A: Nasce sem receptora fixa; a spec 004 faz a ligação pelo token.
- Q2: Como ficam os cenários que dependem das specs 004 e 005, e quem cria ajustes nesta spec? → A: A 003 entrega uma API de administração para ajustes e confere o estado calculado. Os passos que chamam as APIs oficiais ficam pendentes e são ligados na 004 e na 005.
- Q3: Como os consentimentos de um cliente se dividem entre os bancos? → A: Cada consentimento sorteia seu banco pela distribuição, usando a semente. O cliente tem produtos em cada banco sorteado, e o banco pode se repetir.
- Q4: Como lote e mundo virtual definem PF e PJ? → A: A distribuição ganha uma porcentagem de PJ, de 0 a 100%, com padrão 0.
- Q5: Quais são os padrões da distribuição e os valores dos perfis prontos? → A: Sem distribuição informada, 100% no Banco Estável e 100% em `cliente-comum`, com todas as permissões válidas para o tipo de pessoa. O perfil diz quais produtos podem existir, e a porcentagem da distribuição dá a chance de cada um, com 100% se omitida. Valores: `cliente-comum`, R$ 10 a R$ 2.000 por transação e saldo inicial de R$ 5.000; `sem-movimento`, sem transações e saldo inicial de R$ 1.000; `alto-volume`, R$ 1 a R$ 50.000 por transação e saldo inicial de R$ 1.000.000.
- Q6: Mudar o total ou a distribuição do mundo virtual apaga os ajustes? → A: Mudar a distribuição funciona como trocar a semente, com aviso, confirmação e ajustes apagados. Mudar o total preserva os ajustes, e os de itens fora do novo total ficam sem efeito.
- Q7: O que a limpeza por instituição e a limpeza total apagam? → A: Por instituição, apaga consentimentos e recursos daquele banco, e o cliente só se ele não tiver produto em outro banco; a instituição continua existindo. "Tudo" apaga massa, lotes, ajustes e as instituições e perfis criados pelo operador; a definição do mundo virtual fica.
- Q8: O usuário pode informar documento e nome do cliente? → A: O nome pode ser informado. O documento é sempre gerado.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Definir como é a massa (Priority: P1)

Como operador, quero escolher ou criar perfis de dados e definir a distribuição entre bancos, perfis e produtos, para que toda massa gerada depois tenha a forma que o teste precisa ([UC05](../../docs/01-requisitos/casos-de-uso/README.md#uc05), [RN55](../../docs/01-requisitos/regras-de-negocio.md#rn55), [RN56](../../docs/01-requisitos/regras-de-negocio.md#rn56)).

**Why this priority**: lote, cliente avulso e mundo virtual partem de perfis e distribuição.

**Independent Test**: subir o ambiente, listar os perfis prontos, criar um perfil, salvar uma distribuição válida e tentar salvar uma inválida.

**Acceptance Scenarios**:

1. **Given** um ambiente novo, **When** o operador lista os perfis de dados, **Then** vê `cliente-comum`, `sem-movimento` e `alto-volume`.
2. **Given** bancos com 60% e 30%, **When** o operador salva a distribuição, **Then** ela é recusada.
3. **Given** perfis que não somam 100%, **When** o operador salva a distribuição, **Then** ela é recusada.
4. **Given** bancos e perfis somando 100%, com 90% de conta, 50% de cartão e 20% de empréstimo, **When** o operador salva, **Then** ela é aceita.
5. **Given** um perfil em uso, **When** o operador tenta apagá-lo, **Then** a exclusão é recusada.

---

### User Story 2 - Criar um cliente simulado específico (Priority: P1)

Como QA ou desenvolvedor, quero criar um cliente PF ou PJ com produtos nos bancos que escolher, para ter um cliente conhecido em um teste ([UC06](../../docs/01-requisitos/casos-de-uso/README.md#uc06), [RN49](../../docs/01-requisitos/regras-de-negocio.md#rn49), [RN57](../../docs/01-requisitos/regras-de-negocio.md#rn57)).

**Why this priority**: é a menor massa útil, e as specs 004 e 008 precisam dela.

**Independent Test**: criar um cliente em dois bancos e conferir documento, marca, natureza e produtos separados por banco.

**Acceptance Scenarios**:

1. **Given** tipo PF e um perfil de dados, **When** o cliente é criado, **Then** o CPF tem dígitos válidos e começa com 9.
2. **Given** tipo PJ, **When** o cliente é criado, **Then** o CNPJ tem dígitos válidos, inclusive quando alfanumérico.
3. **Given** dois bancos escolhidos, **When** o cliente é criado, **Then** ele tem produtos próprios em cada banco e não pertence a nenhum deles.
4. **Given** um cliente criado, **When** seus dados são consultados, **Then** nome e identificadores trazem a marca do ambiente e a natureza `persistido`.
5. **Given** um produto com histórico padrão, **When** as transações são calculadas, **Then** cobrem os últimos 12 meses, e nenhuma é gravada.

---

### User Story 3 - Gerar um lote em segundo plano (Priority: P1)

Como operador, quero informar quantidade, distribuição, consentimentos por cliente, permissões e semente, acompanhar o progresso e receber um resumo ([UC07](../../docs/01-requisitos/casos-de-uso/README.md#uc07), [RN51](../../docs/01-requisitos/regras-de-negocio.md#rn51), [RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54), [RN56](../../docs/01-requisitos/regras-de-negocio.md#rn56), [RN58](../../docs/01-requisitos/regras-de-negocio.md#rn58)).

**Why this priority**: é o caminho normal para testes e demonstrações.

**Independent Test**: gerar um lote de 100 clientes, acompanhar até o fim, conferir o resumo e confirmar que nenhuma transação foi gravada.

**Acceptance Scenarios**:

1. **Given** um pedido de 100 clientes, **When** o lote termina, **Then** clientes, consentimentos e recursos estão gravados, e nenhuma transação.
2. **Given** um lote concluído, **When** os consentimentos são consultados, **Then** todos estão `AUTHORISED`.
3. **Given** um lote em andamento, **When** o operador consulta o lote, **Then** vê o progresso e a situação.
4. **Given** um lote em andamento, **When** o operador cancela, **Then** a geração para e o que foi criado permanece, marcado como parcial.
5. **Given** um lote com semente 42, **When** o mesmo pedido é gerado em outro ambiente limpo, **Then** os dados são iguais, salvo a marca do ambiente.
6. **Given** um lote com semente 42, **When** outro lote com semente 42 é gerado no mesmo ambiente, **Then** documentos e IDs mudam, e o restante é igual.
7. **Given** um pedido sem semente, **When** o lote é criado, **Then** o sistema gera uma semente e a mostra no lote.

---

### User Story 4 - Usar o mundo virtual em teste de carga (Priority: P2)

Como QA, quero definir total, semente e distribuição do mundo virtual e pedir faixas de itens, para testar com milhões de itens sem gravá-los ([UC08](../../docs/01-requisitos/casos-de-uso/README.md#uc08), [RN52](../../docs/01-requisitos/regras-de-negocio.md#rn52), [RNF06](../../docs/01-requisitos/requisitos.md#rnf06)).

**Why this priority**: atende à carga, mas os testes funcionais funcionam sem ele.

**Independent Test**: definir um mundo de 10 milhões, pedir 1.000 itens, repetir o pedido e conferir itens iguais e nenhum item gravado.

**Acceptance Scenarios**:

1. **Given** um mundo virtual definido, **When** o QA pede 1.000 itens, **Then** recebe IDs com natureza `virtual`, e só total, semente, distribuição e ajustes ficam gravados.
2. **Given** 50% no `HML-Itaú` e 50% no `HML-Nubank`, **When** o QA pede 1.000 itens, **Then** cerca de metade é de cada banco.
3. **Given** o mesmo pedido, **When** o QA filtra `HML-Itaú`, **Then** todos os itens são desse banco.
4. **Given** o mesmo mundo, **When** a mesma faixa é pedida de novo, **Then** os itens são iguais.
5. **Given** um cliente de lote e um item virtual, **When** a natureza é consultada, **Then** o primeiro é `persistido`, o segundo é `virtual`, e ambos são simulados.

---

### User Story 5 - Ajustar dado calculado (Priority: P2)

Como QA, quero gravar uma alteração sobre um dado calculado, como revogar um consentimento virtual ou mudar o valor de uma transação, sem perder a repetibilidade do restante ([UC08](../../docs/01-requisitos/casos-de-uso/README.md#uc08), [RN51](../../docs/01-requisitos/regras-de-negocio.md#rn51), [RN53](../../docs/01-requisitos/regras-de-negocio.md#rn53), [RN46](../../docs/01-requisitos/regras-de-negocio.md#rn46)).

**Why this priority**: dá precisão ao teste sobre massa calculada.

**Independent Test**: ajustar uma transação de R$ 100,00 para R$ 90,00 e conferir o valor e o saldo calculados.

**Acceptance Scenarios**:

1. **Given** uma transação calculada de R$ 100,00, **When** o QA a ajusta para R$ 90,00, **Then** o resultado calculado traz R$ 90,00, e o saldo reflete o ajuste.
2. **Given** um consentimento virtual, **When** o QA o revoga, **Then** o estado calculado passa a ser `REJECTED`.
3. **Given** um mundo com um consentimento revogado, **When** o QA troca a semente sem confirmar, **Then** recebe o aviso e nada muda.
4. **Given** o mesmo mundo, **When** o QA confirma a troca, **Then** os ajustes daquele mundo somem.

---

### User Story 6 - Limpar massa (Priority: P2)

Como operador, quero escolher o que apagar, ver antes o que será removido e confirmar, para devolver o ambiente a um estado conhecido ([UC27](../../docs/01-requisitos/casos-de-uso/README.md#uc27), [RN53](../../docs/01-requisitos/regras-de-negocio.md#rn53), [RN58](../../docs/01-requisitos/regras-de-negocio.md#rn58), [RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71)).

**Why this priority**: sem limpeza automática ([PA17](../../docs/01-requisitos/regras-de-negocio.md#pa17)), o ambiente só volta ao normal assim.

**Independent Test**: criar dois lotes, apagar um e conferir que o outro ficou e que o histórico registrou a exclusão.

**Acceptance Scenarios**:

1. **Given** um lote com 100 clientes, **When** o operador o apaga, **Then** clientes, consentimentos, recursos e ajustes dele somem, e o histórico mostra a exclusão.
2. **Given** um mundo com 50 consentimentos revogados, **When** o operador limpa os ajustes, **Then** eles voltam ao estado calculado.
3. **Given** um alvo escolhido, **When** o operador pede a limpeza sem confirmar, **Then** vê o que será apagado, e nada é removido.
4. **Given** instituições e perfis prontos, **When** o operador limpa tudo, **Then** eles permanecem.

### Edge Cases

- Lote com quantidade zero ou negativa: recusado. Sem limite superior ([RN58](../../docs/01-requisitos/regras-de-negocio.md#rn58)).
- Distribuição com banco inexistente ou perfil inexistente: recusada.
- Apagar um lote em andamento: recusado até que ele termine ou seja cancelado.
- Lote parcial: pode ser apagado inteiro, como qualquer lote.
- Lote interrompido por queda do processo: fica parcial, e não retoma sozinho.
- Faixa pedida além do total do mundo: devolve só os itens que existem; faixa toda fora devolve lista vazia.
- Pedido de itens antes de definir o mundo virtual: recusado, informando que o mundo não foi definido.
- Filtro por banco fora da distribuição do mundo: lista vazia.
- Mesma semente com configuração ou versão de gerador diferente: não há garantia de dado igual ([RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54), [ADR-004](../../docs/05-adr/ADR-004-semente-e-ajustes.md)).
- Ajuste sobre item que não existe no mundo atual: recusado.

## Requirements *(mandatory)*

### Functional Requirements

**Perfis e distribuição**

- **FR-001**: O sistema MUST oferecer prontos os perfis de dados `cliente-comum`, `sem-movimento` e `alto-volume`, com 0 a 4, 0 e 2.000 a 3.000 transações por dia, respectivamente ([RN55](../../docs/01-requisitos/regras-de-negocio.md#rn55)). Faixa por transação e saldo inicial: `cliente-comum`, R$ 10 a R$ 2.000 e R$ 5.000; `sem-movimento`, sem transações e R$ 1.000; `alto-volume`, R$ 1 a R$ 50.000 e R$ 1.000.000.
- **FR-002**: O operador MUST poder criar, alterar e apagar perfis de dados com nome, volume por dia, faixa de valores, saldo inicial, tipos de lançamento e produtos. O nome é obrigatório.
- **FR-003**: O sistema MUST recusar a exclusão de perfil pronto e de perfil em uso, isto é, referenciado por cliente, recurso, lote, distribuição ou pelo mundo virtual.
- **FR-004**: O sistema MUST recusar distribuição cuja soma de bancos ou de perfis difira de 100%, e MUST aceitar porcentagens de produto entre 0 e 100%, sem soma ([RN56](../../docs/01-requisitos/regras-de-negocio.md#rn56)). O perfil de dados define quais produtos podem existir, e a porcentagem da distribuição dá a chance de cada um, com 100% se omitida. A distribuição MUST aceitar uma porcentagem de PJ entre 0 e 100%, com padrão 0.

**Geração**

- **FR-005**: O gerador MUST receber semente e data explícitas, e a mesma semente com a mesma data e configuração MUST produzir o mesmo dado ([RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54), [RNF05](../../docs/01-requisitos/requisitos.md#rnf05)). Sem semente informada, o sistema MUST gerar uma e mostrá-la.
- **FR-006**: Massa persistida e mundo virtual MUST usar o mesmo gerador ([RN52](../../docs/01-requisitos/regras-de-negocio.md#rn52)), com a versão dele gravada no lote ([ADR-004](../../docs/05-adr/ADR-004-semente-e-ajustes.md)).
- **FR-007**: CPFs e CNPJs gerados MUST ter dígitos válidos, incluindo CNPJ alfanumérico, e todo CPF gerado MUST começar com 9 ([RN57](../../docs/01-requisitos/regras-de-negocio.md#rn57)). Documentos MUST NOT se repetir dentro do ambiente, entre massa persistida e mundo virtual.
- **FR-008**: Todo dado gerado MUST carregar marca e natureza nos identificadores e no nome do cliente, reutilizando a marcação da spec 001; a marca é aplicada na criação e não muda ([RN60](../../docs/01-requisitos/regras-de-negocio.md#rn60) a [RN66](../../docs/01-requisitos/regras-de-negocio.md#rn66)).
- **FR-009**: Transações MUST ser calculadas a partir de recurso, dia e semente, nunca gravadas uma a uma ([RN51](../../docs/01-requisitos/regras-de-negocio.md#rn51)), com histórico padrão de 12 meses e opção de histórico maior ([RN59](../../docs/01-requisitos/regras-de-negocio.md#rn59)). Saldo, extrato, limite e faturas MUST ser coerentes ([RN46](../../docs/01-requisitos/regras-de-negocio.md#rn46)).

**Cliente simulado**

- **FR-010**: Usuários MUST poder criar cliente PF ou PJ escolhendo perfil de dados e bancos; o sistema gera documento, nome e produtos em cada banco e mostra o resultado ([UC06](../../docs/01-requisitos/casos-de-uso/README.md#uc06)). O usuário MAY informar o nome; o documento MUST ser sempre gerado, e o sistema MUST recusar um documento informado.
- **FR-011**: O cliente MUST ser identificado pelo documento e não pertencer a um banco; produtos pertencem ao cliente em cada banco ([RN49](../../docs/01-requisitos/regras-de-negocio.md#rn49)).

**Lote**

- **FR-012**: O operador MUST poder gerar lote informando quantidade (obrigatória), distribuição, consentimentos por cliente (padrão 1), permissões e semente ([UC07](../../docs/01-requisitos/casos-de-uso/README.md#uc07), [RN56](../../docs/01-requisitos/regras-de-negocio.md#rn56)). Sem distribuição informada, MUST valer 100% no Banco Estável, 100% em `cliente-comum` e 0% de PJ; sem permissões informadas, todas as válidas para o tipo de pessoa.
- **FR-013**: A geração MUST ocorrer em segundo plano, com progresso, situação (em andamento, concluído, cancelado, parcial) e resumo com clientes, consentimentos e recursos criados.
- **FR-014**: O operador MUST poder cancelar um lote; o que foi criado permanece, marcado como parcial.
- **FR-015**: Consentimentos gerados MUST nascer `AUTHORISED` ([RN80](../../docs/01-requisitos/regras-de-negocio.md#rn80)), e sem validade informada têm prazo indeterminado ([RN85](../../docs/01-requisitos/regras-de-negocio.md#rn85)). Eles MUST nascer sem receptora fixa; a ligação vem do token, na spec 004. Cada consentimento MUST sortear seu banco pela distribuição, usando a semente; o cliente tem produtos em cada banco sorteado, e o banco pode se repetir ([RN49](../../docs/01-requisitos/regras-de-negocio.md#rn49)).
- **FR-016**: Dois lotes com a mesma semente no mesmo ambiente MUST diferir em documentos e IDs e ser iguais no restante; o primeiro lote de uma semente em ambiente limpo MUST reproduzir os mesmos documentos e IDs de outro ambiente limpo ([RN54](../../docs/01-requisitos/regras-de-negocio.md#rn54)).

**Mundo virtual**

- **FR-017**: O sistema MUST manter um único mundo virtual por ambiente, gravando somente total, semente, distribuição e ajustes ([RN52](../../docs/01-requisitos/regras-de-negocio.md#rn52)), e suportar pelo menos 10 milhões de itens ([RNF06](../../docs/01-requisitos/requisitos.md#rnf06)).
- **FR-018**: Uma API de administração MUST devolver uma faixa de itens por índice, dividida entre os bancos pela distribuição, com filtro opcional por banco; a mesma faixa no mesmo mundo MUST devolver os mesmos itens.
- **FR-019**: Os itens virtuais MUST expor clientes, consentimentos `AUTHORISED` e recursos calculados, resolvíveis pelo ID para uso pelas specs seguintes.
- **FR-020**: Trocar a semente ou a distribuição MUST exigir confirmação explícita, depois de avisar sobre os ajustes, e, confirmada, apagar os ajustes do mundo ([RN53](../../docs/01-requisitos/regras-de-negocio.md#rn53)). Mudar o total MUST preservar os ajustes; os de itens fora do novo total ficam sem efeito.

**Ajustes**

- **FR-021**: Usuários MUST poder gravar ajustes sobre dado calculado, e o ajuste MUST vencer o cálculo, inclusive nos saldos derivados ([RN51](../../docs/01-requisitos/regras-de-negocio.md#rn51), [RN53](../../docs/01-requisitos/regras-de-negocio.md#rn53), [RN46](../../docs/01-requisitos/regras-de-negocio.md#rn46)). Os ajustes MUST ser criados por uma API de administração, e esta spec confere o estado calculado. A aplicação nas APIs oficiais é verificada nas specs 004 e 005.

**Limpeza e histórico**

- **FR-022**: O operador MUST poder limpar um lote, uma instituição, os ajustes do mundo virtual ou tudo, vendo antes o que será apagado e confirmando ([UC27](../../docs/01-requisitos/casos-de-uso/README.md#uc27)). Por instituição, MUST apagar consentimentos e recursos daquele banco, e o cliente só se ele não tiver produto em outro banco; a instituição continua existindo. "Tudo" MUST apagar massa, lotes, ajustes e as instituições e perfis criados pelo operador, e manter a definição do mundo virtual.
- **FR-023**: Apagar um lote MUST remover clientes, consentimentos, recursos e ajustes dele ([RN58](../../docs/01-requisitos/regras-de-negocio.md#rn58)). Instituições e perfis prontos MUST NOT ser apagados.
- **FR-024**: Toda alteração de perfil, distribuição, mundo virtual, ajuste, lote ou limpeza MUST gravar data, antes e depois no histórico; operações em massa geram um registro-resumo, não um por item ([RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71)).
- **FR-025**: Todas as operações desta spec MUST ficar na administração, separadas das APIs Open Finance e sem login ([RN71](../../docs/01-requisitos/regras-de-negocio.md#rn71)). Telas ficam na spec 010.

### Key Entities *(include if feature involves data)*

- **Perfil de dados**: modelo de volume, valores, saldo inicial, tipos de lançamento e produtos. Pronto ou criado.
- **Distribuição**: divisão entre bancos, perfis e produtos, com a porcentagem de PJ.
- **Cliente simulado**: documento, nome, tipo, marca e natureza; ligado a recursos e consentimentos em vários bancos.
- **Recurso**: produto do cliente em um banco, com tipo, estado, perfil de dados, saldo inicial, data de criação e marca.
- **Consentimento**: permissões, validade e estado, gerado em `AUTHORISED` e sem receptora fixa nesta spec; a API dele fica na 004.
- **Lote**: parâmetros, semente, versão do gerador, progresso e situação; dono da massa que criou.
- **Mundo virtual**: total, semente e distribuição; um por ambiente.
- **Ajuste**: alvo e alteração sobre dado calculado; pertence a um lote, a um recurso ou ao mundo virtual.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% das repetições, o mesmo pedido com a mesma semente, data e configuração, em ambientes limpos, produz dados iguais, salvo a marca.
- **SC-002**: Um mundo virtual de 10 milhões de itens atende pedidos de faixa sem acrescentar registro de item, medido pela contagem antes e depois.
- **SC-003**: 100% dos CPFs e CNPJs de uma amostra de 100 mil clientes têm dígitos válidos, e nenhum documento se repete.
- **SC-004**: Em um lote de 100 clientes, nenhuma transação é gravada.
- **SC-005**: Depois de apagar um lote, nenhum cliente, consentimento, recurso ou ajuste dele permanece, e os demais lotes ficam intactos.
- **SC-006**: Todos os cenários de `massa-e-semente.feature` passam no CI, exceto os passos que chamam as APIs oficiais, pendentes até as specs 004 e 005, e nenhum teste depende de hora real ou de sorteio sem semente.

## Assumptions

- A spec 002 entrega as instituições (16 prontas, nomes e identificadores), uma forma de consultá-las, e o histórico de configuração com data, antes e depois. A 002 precisa saber se uma instituição tem clientes ([RN06](../../docs/01-requisitos/regras-de-negocio.md#rn06)), dado que nasce nesta spec.
- As APIs Open Finance de consentimento, recursos e dados ficam nas specs 004 e 005, e o painel e as telas nas 008 e 010.
- Instituição inativa pode constar de uma distribuição: a massa é criada e responde como fora do ar quando usada ([RN06](../../docs/01-requisitos/regras-de-negocio.md#rn06)).
- Os clientes das personas (Mariana, Rafael, Indústria Aurora) não nascem com o ambiente: são dados dos cenários de teste.
- A confirmação que [RN53](../../docs/01-requisitos/regras-de-negocio.md#rn53) pede "na tela" é, nesta spec, um passo explícito da administração; a tela vem na 010.
- Dia sem data fornecida usa o relógio injetado do sistema, nunca a hora real em teste.
