# Spec 007: divergências e perguntas

**Aguardando decisões.** Nenhum plano, tarefa ou código foi feito. A [spec](spec.md) usa, de forma provisória, a opção A de cada pergunta.

Conferência de `docs/` e dos `.feature` contra os contratos em `.specs/7bdb9c1a6c4c265d7aabcdbc662f94c61718135e/contracts/expansion/snapshots/` (15 arquivos, 75 operações). Os números de linha são desses arquivos, de `docs/` e de `src/` no commit `2b17b54`.

## BLOQUEIO: divergências

### D1. RN13 diz que só se recusa status não declarado; o cenário exige recusar 429

`regras-de-negocio.md:28`: "Ao salvar, só é recusado o status que nenhuma spec declara". Os 15 contratos declaram 429 e 529, e 13 declaram 423. `comportamento.feature:62-65` exige recusar peso 429, e o código já recusa (`Regras.cs:84`, `ValoresOficiais.cs:24`). Além disso, `Regras.cs:84` hoje aceita peso em 200, 201, 202 e 204, que não são erro. Ver Q1.

### D2. RN39 põe a regra de cenário depois do acesso ao recurso; "Bloqueio programado" exige 403

`regras-de-negocio.md:66` coloca o 403 de recurso antes da regra de cenário. `recursos.feature:88-92` e RN108 (`regras-de-negocio.md:143`) querem que uma regra bloqueie a conta e que a resposta seja 403. Se a regra só agir na sua etapa, ela chega tarde. Ver Q2.

### D3. RN16 fala em "todas as chamadas"; RN39 deixa a janela para o fim

`regras-de-negocio.md:31`: a janela vale "para todas as chamadas do alcance". Por RN39, uma chamada com token inválido recebe 401, e uma acima do TPM recebe 429, mesmo com o banco "fora do ar". Já o banco inativo responde 503 antes de tudo (RN06, `Despachante.cs:25-28`). Ver Q3.

### D4. Regra de cenário pode forçar status que o endpoint não declara

RN13 limita ao declarado só o sorteio (`regras-de-negocio.md:28`). Para regra, não diz nada. Pelos contratos: 503 não existe em nenhuma das 75 operações; 423 falta em Consentimentos e Recursos (6 operações); 422 falta em 6 operações (`GET /consents/{consentId}` e `GET .../extensions` em `consents-3.3.1.yml`, a segunda já tratada como exceção em RN33, e 4 em `customers-2.3.0.yaml`).

Há também restrição de enum que o teste de schema só pega se o caso rodar: o `code` do 422 é enum nas 3 operações de Consentimentos que o declaram. Em `DELETE` o único valor é `CONSENTIMENTO_EM_STATUS_REJEITADO` (`consents-3.3.1.yml:1164-1165`), sem código genérico. Em `POST` e em `extends` existe `ERRO_NAO_MAPEADO` (`:1206`, `:1252`). Ver Q4 e Q14.

### D5. Só o perfil `timeout` termina em 504, mas todo perfil tem chamadas acima de 15 s

RN12 e UC21 só definem o fim em 504 "no perfil `timeout`" (`regras-de-negocio.md:27`, `casos-de-uso/README.md:292`). `perfis-de-comportamento.md:11` dá 0,1% e 0,5% acima de 15 s para `lento` e `instavel`, sem dizer como terminam. O mesmo vale para o 504 sorteado (`:10`): `confronto-com-a-poc.md:108` critica exatamente o 504 que não espera. O contrato define 504 como "não atendida dentro do tempo limite" (`accounts-2.5.1.yaml:1646`). Ver Q5.

### D6. `recursoIndisponivelPct` está no perfil, mas nenhuma regra diz o que faz

Linha `perfis-de-comportamento.md:13`, campo gravado em `Modelos.cs:22`, nenhum leitor em `src/` e nenhuma RN. Ver Q6.

### D7. Relógio por banco e datas gravadas pela administração se misturam

RN37 compara o instante de emissão da chave com a data dos ajustes (`regras-de-negocio.md:60`). A emissão passa a usar o relógio do banco (RN73), mas ajustes e mudanças de estado são gravados com a hora global (`Servicos.cs:725`). Com o relógio fixado no passado, todo ajuste pareceria posterior à chave; no futuro, anterior. Ver Q7.

## Divergência menor, a corrigir em `docs/`

- **D8.** UC28 cita "60 minutos do consentimento" (`casos-de-uso/README.md:134`), que é a intenção pendente, [adiada](../../docs/01-requisitos/regras-de-negocio.md#adiados). Proposta: trocar por "validade do token, da chave de paginação e virada do mês".

## Conferido, sem divergência

| Afirmação | Evidência |
|---|---|
| 500 e 504 existem em todas as operações | contagem por arquivo: 75 de 75 |
| 503 não é declarado (RN13) | 0 de 75 |
| Corpo de erro livre fora de Consentimentos 422 | `ResponseErrorMetaSingle`, `code` com `pattern` livre (`accounts-2.5.1.yaml:1342-1373`); nenhum enum de `code` nos outros 14 arquivos |
| `x-fapi-interaction-id` espelhado em toda resposta | `accounts-2.5.1.yaml:1381`; o portão já grava o cabeçalho antes das etapas seguintes (`Rotas.cs:56`) |
| Fatores de latência por classe | `perfis-de-comportamento.md:20` igual a `ValoresOficiais.cs:14-17` |
| Linhas de erro, janela e limites dos cinco perfis | `perfis-de-comportamento.md:5-18` igual a `ValoresOficiais.cs:55-65` |
| Cabeçalho `x-sim-clock` | não aparece nos contratos; cabeçalho a mais não invalida o schema, como `Retry-After` na 006 |

**Não conferível aqui:** o timeout de 15 s e o SLA vêm de F01 e F02, que não estão no repositório. Os valores dos perfis são estimativas próprias (PA20).

## Cenários no escopo

Entram os 16 cenários de `comportamento.feature` (etiqueta na linha 5), inclusive os quatro citados: "Banco timeout termina em 504" (:23), "Erro sorteado não simula limite" (:62), "Janela de indisponibilidade" (:68) e "Relógio alterado não muda o TPM" (:102). Entra também "Bloqueio programado" (`recursos.feature:88`). `limites.feature` não tem cenário etiquetado. Os `@spec-008`, `@spec-009` e `@tela` ficam de fora.

## A saber para o plano

- **Testes existentes:** com perfis ativos, o Banco Estável passa a ter 200 ms típicos e 0,2% de erro. A fábrica de testes usa relógio falso (`FabricaDoSimulador.cs:113`), e uma espera nele nunca termina sem avançar. O plano precisa de um modo sem latência e sem sorteio para as suítes anteriores.
- **"10.000 chamadas" (`comportamento.feature:13`, `:18`):** esbarram em TPM, capacidade e cota. O passo eleva os limites do banco de teste (RN26) e usa relógio falso (princípio IX).
- **Etapa de cenário:** hoje acesso, parâmetros e cota ficam dentro de `ServicoDeDados.Responder` (`ServicoDeDados.cs:60-101`), e em Consentimentos a validação fica junto do efeito. A etapa precisa entrar entre a cota e a resposta, antes de qualquer gravação.
- **Relógio em `src/`:** `TimeProvider` é único e global; há leituras em Consentimentos, Dados, Limites, Massa e `EnvelopeDeErro`. `ServicoDeLimites.cs:16` deve continuar na hora real, e `:50` (cota) e `:53` (envelope) passam à hora do banco. `ServicoDeDados.cs:428` relê o relógio: com relógio acelerado, cada chamada precisa de um único instante.
- **Latência própria (RNF03):** relógio entra no documento da instituição, já lido uma vez por `InstituicoesDaChamada`. As regras precisam de cache curto por banco, dentro dos 5 s de RNF12.
- **`Preparacao.Situacao`** (`Ciclo.cs:72`) precisa lidar com banco cujo relógio está antes da criação do consentimento (Q17).

## Perguntas

Respostas no formato `Q1: A`. Recomendação sempre na opção A.

### Q1 (BLOQUEIO D1). O que é recusado nos pesos do sorteio?

- **A (recomendada):** corrigir RN13: ao salvar, recusa status que nenhuma spec declara, 429, 423, 529 e 503, e também qualquer status abaixo de 400.
- **B:** só corrigir RN13 para 429, 423 e 529, mantendo peso em 2XX.

**Motivo:** o cenário e o FR-018 da 006 já decidem 429, 423 e 529. Peso em 2XX não é erro: seria sucesso sem corpo definido.

### Q2 (BLOQUEIO D2). Como a regra muda o estado de um recurso (RN108)?

- **A (recomendada):** a regra sobrepõe o estado na leitura: durante o período, o recurso vale como o estado configurado em toda rota, inclusive na etapa de acesso (403) e nas listas (RN104). Nada é gravado, e o estado volta ao fim do período. RN39 ganha essa ressalva.
- **B:** a regra grava a mudança ao chegar a hora, por um processo em segundo plano; em recurso virtual, vira ajuste.

**Motivo:** A não precisa de processo agendado, é repetível com relógio simulado e não deixa resíduo (princípios VI e X). **Custo de A:** RN108 hoje sugere ajuste para recurso virtual, e a regra de transição de RN101 não se aplica à sobreposição.

### Q3 (BLOQUEIO D3). Em que ponto a indisponibilidade age?

- **A (recomendada):** literal em RN39: na etapa de regra de cenário, depois de token e limites. RN16 passa a dizer "todas as chamadas do alcance que chegam a essa etapa". A janela do perfil `instavel` segue igual.
- **B:** janela de alcance só instituição age logo na entrada, como banco inativo; RN39 ganha ressalva. Janela com cliente, recurso ou endpoint fica na etapa normal.

**Motivo:** ordem única (ADR-008), e alcance por cliente ou recurso depende do token. **Custo de A:** quem manda token inválido durante a janela recebe 401, e as chamadas contam no TPM.

### Q4 (BLOQUEIO D4). Quais status uma regra pode forçar?

- **A (recomendada):** ao salvar, aceita 200 (segue o fluxo normal), qualquer status de erro declarado por alguma spec e 503. Na chamada, se o endpoint não declara o status, a regra não age nele, salvo o 503, exceção de RN13. O 429 usa o `Retry-After` do banco.
- **B:** a regra responde o status mesmo sem declaração no endpoint.
- **C:** recusa ao salvar se algum endpoint do alcance não declarar o status.

**Motivo:** resposta fora do contrato é anomalia, e anomalia é da spec 009, com nome (princípio III, RN113). C impediria "Banco fora do ar com 423", que vale para os 69 endpoints de dados. 200 cobre `comportamento.feature:37`.

### Q5 (BLOQUEIO D5). Como termina uma chamada lenta ou com 504?

- **A (recomendada):** toda chamada da cauda lenta, em qualquer perfil, segura 15 s e termina em 504. Todo 504 da etapa de comportamento (sorteio, janela, regra) também segura 15 s. Regra pode informar outra duração. O 504 de RN103 segue imediato.
- **B:** só o perfil `timeout` espera; em `lento` e `instavel`, a cauda responde normalmente depois de 15 s, e o 504 sorteado é imediato.

**Motivo:** o timeout oficial vale para todo endpoint (RN12), e 504 significa tempo esgotado. "Tempo configurado" em UC21 fica na regra.

### Q6 (BLOQUEIO D6). O que faz `recursoIndisponivelPct`?

- **A (recomendada):** nova RN: sorteio por chamada, no acesso direto, respondendo 403 `STATUS_RESOURCE_TEMPORARILY_UNAVAILABLE` (RN104). Listas não mudam, e o estado gravado não muda.
- **B:** fica fora desta versão: sai da tabela de perfis e do cadastro.
- **C:** vale na geração: a porcentagem dos recursos nasce `TEMPORARILY_UNAVAILABLE`.

**Motivo:** o perfil promete, e a 002 deixou o sorteio para a 007. C muda massa já entregue na 003.

### Q7 (BLOQUEIO D7). Qual relógio usam as escritas da administração?

- **A (recomendada):** escrita ligada a um banco e que entra em resposta ou em comparação (ajuste de transação, estado de recurso, revogação) usa o relógio do banco. Histórico (RN05), lotes e criação de massa usam a hora real.
- **B:** toda administração usa a hora real, e RN37 fica sem garantia com relógio alterado.

**Motivo:** mantém RN37 coerente. O histórico registra quando o operador agiu.

### Q8. O que a regra vencedora substitui?

- **A (recomendada):** uma só regra vence (RN15) e substitui só o que define: efeito de latência troca a latência; status, timeout, indisponibilidade e falha de transporte trocam o sorteio de erro e a janela do perfil. O que ela não define segue o perfil. Regras nunca se somam.
- **B:** a vencedora desliga todo o perfil naquela chamada, inclusive a latência.

**Motivo:** `comportamento.feature:35-39` só exige o status; A mantém o resto realista.

### Q9. Regra com frequência abaixo de 100% que não dispara

- **A (recomendada):** a chamada segue o perfil, sem procurar a próxima regra.
- **B:** tenta a próxima regra, por ordem de RN15.

**Motivo:** a vencedora é escolhida uma vez; B torna a frequência final difícil de calcular.

### Q10. Como a semente repete o sorteio (RN54)?

- **A (recomendada):** regra com semente usa um contador próprio em Redis: a N-ésima avaliação sempre dá o mesmo resultado, em qualquer instância. Sem Redis, sorteia sem repetição (RN30). Frequência 0 ou 100% não sorteia. O sorteio do perfil não tem semente própria; em teste, usa o gerador injetado.
- **B:** resultado calculado de semente + `x-fapi-interaction-id`, sem contador.
- **C:** semente também por instituição, para o sorteio do perfil.

**Motivo:** em B, repetir exige a receptora reenviar os mesmos IDs, o que não acontece. Sob concorrência, A preserva a sequência, mas não diz qual chamada ficou com qual posição.

### Q11. Como funciona o período?

- **A (recomendada):** instantes absolutos em UTC, início incluído e fim excluído, pelo relógio do banco. Sem recorrência diária. A janela do perfil começa em múltiplos do intervalo a partir de 00:00 UTC (`instavel`: minutos 00 e 01 de cada hora), também pelo relógio do banco.
- **B:** aceitar também faixa diária recorrente ("das 10h às 10h30, todo dia").

**Motivo:** o exemplo de UC04 ("todo dia") é rodar o teste, com regra sem período. RN73 cita só regras, e usar o mesmo relógio na janela do perfil evita esperar por ela.

### Q12. A regra precisa de instituição?

- **A (recomendada):** não, como `modelo-de-dominio.md:98` (0..1): basta um filtro. Sem instituição, vale em todo banco (ex.: "Mariana sempre recebe 500 em saldos"); o período usa o relógio do banco chamado.
- **B:** instituição obrigatória.

**Motivo:** o cliente não pertence a um banco (RN49). **Custo de A:** o histórico dessas regras não fica em nenhum banco; fica sob o identificador `cenarios`.

### Q13. Falha de transporte conta na cota?

- **A (recomendada):** conexão cortada não conta. Corpo truncado e JSON inválido contam, porque a resposta saiu com 200 (RN27).
- **B:** nenhuma falha de transporte conta.

**Motivo:** leitura direta de "só 2XX".

### Q14. Quais `code`, `title` e `detail` nos erros de comportamento?

- **A (recomendada):** nome HTTP em inglês, como a Q10 da 006: `INTERNAL_SERVER_ERROR`, `GATEWAY_TIMEOUT`, `SERVICE_UNAVAILABLE` e os demais; 429, 423 e 529 usam os de RN23, RN28 e RN29; os 503 e 504 existentes (`Despachante.cs:69`, `Rotas.cs:319`) mudam para esses nomes. Em 422 de Consentimentos: `ERRO_NAO_MAPEADO` em `POST` e `extends`, `CONSENTIMENTO_EM_STATUS_REJEITADO` em `DELETE`. 403 de regra usa `FORBIDDEN`.
- **B:** A, sem renomear os 503 e 504 que já existem.

**Motivo:** um único padrão. Os enums de D4 impedem código livre nesses três casos. Os textos são genéricos (RN18).

### Q15. Quem sofre a latência?

- **A (recomendada):** só chamadas que chegam à etapa de comportamento, nas 75 operações. Recusas anteriores (400, 401, 403, 422, 423, 429, 529), `POST /token` e administração respondem sem latência simulada.
- **B:** toda resposta das rotas oficiais sofre latência.

**Motivo:** segue RN39, e RN10 usa classe de frequência, que `POST /token` não tem.

### Q16. Como funciona o relógio acelerado?

- **A (recomendada):** continua da hora simulada atual, com fator maior que 0 e até 100.000. Mudar o fator parte da hora simulada daquele momento. Fixar e acelerar se excluem. Toda mudança entra no histórico (RN05). Latência e os 15 s seguem na hora real.
- **B:** acelerar exige informar a data de partida.

**Motivo:** UC28 diz "fixa uma data ou define um fator". RN73 não lista latência entre o que muda.

### Q17. Relógio que volta para trás ou fica antes de um dado

Token, chave ou consentimento criados em uma hora e lidos em outra; consentimento criado depois da hora simulada.

- **A (recomendada):** nada é corrigido: cada verificação compara com a hora simulada atual. Tempo decorrido negativo conta como zero. Um token pode vencer na hora ou durar mais; é o que o QA pediu.
- **B:** ao mudar o relógio, invalidar tokens e chaves já emitidos.

**Motivo:** `POST /token` é barato de repetir. B exige gravar o que hoje é só assinado.

### Q18. Efeitos fora desta spec

UC04 lista "defeito" entre os efeitos (`casos-de-uso/README.md:117`).

- **A (recomendada):** a 007 entrega latência, status, timeout, indisponibilidade, falha de transporte e mudança de estado. Defeito de dado e RN115 ficam na 009; tela de regras (UI06) na 010; a 007 entrega a API de administração.
- **B:** incluir defeito de dado agora.

**Motivo:** segue `sequencia-de-specs.md:17-18`.
