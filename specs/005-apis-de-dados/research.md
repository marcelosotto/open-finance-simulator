# Research: APIs de dados

## R01. Um manipulador por família, em vez de um por operação

- **Decisão:** o catálogo aceita um `IManipuladorDeFamilia`, que responde se atende a operação. O manipulador por `operationId` continua valendo e tem precedência.
- **Motivo:** 69 registros manuais repetiriam o que o contrato já diz.
- **Alternativa rejeitada:** listar os `operationId` no código; quebra a cada versão de contrato.

## R02. Resposta montada a partir do schema travado

- **Decisão:** um gerador percorre o schema da resposta 200, emite os campos obrigatórios e os campos com valor da massa. Valores vêm de: valor da massa que caiba no schema, `enum`, `example`, formato. `links` e `meta` são montados à parte e só levam o que o schema declara.
- **Motivo:** princípio VII e Q5. Nenhum dos 13 contratos usa `allOf`, `oneOf`, `anyOf`, `not` ou `if`.
- **Alternativa rejeitada:** um modelo por família; muito código sem regra que o exija.

## R03. Mínimo de `page-size`

- **Decisão:** lido do `minimum` do parâmetro no catálogo. Consentimentos e Recursos já aplicam 25, que é o que seus contratos declaram (`resources-3.1.0.yml`, `consents-3.3.1.yml:1460`); nada muda neles.

## R04. Chave de paginação

- **Decisão:** texto assinado com HMAC, com o instante de emissão e o resumo da consulta (operação, consentimento e datas pedidas). Nada é gravado. Validade: `ValidadeDaChaveMin` da instituição.
- **Efeito:** o instante da chave é o "agora" dos cálculos das páginas seguintes, então estado e ajustes ficam congelados (Q8).
- **Limite:** o congelamento vale para transações. Listas de recursos refletem o estado atual do recurso (RN104 e RN108).
- **Spec 006:** decide a contagem de cota lendo o resultado da chave; aqui só existe a nova chave.

## R05. Estado da transação

- **Decisão:** calculado na massa, só para o tipo `ACCOUNT`: data futura, `LANCAMENTO_FUTURO`; hoje com tipo em D+1 (`accounts-2.5.1.yaml:47-88`), `TRANSACAO_PROCESSANDO`, com ID provisório; demais, `TRANSACAO_EFETIVADA`.
- O saldo soma só efetivadas (RN46). Dias depois de D+30 nem entram no laço (Q7).

## R06. Famílias sem produto na massa

- **Achado:** Renda Fixa Crédito e Tesouro Direto não tinham produto; `investimentos` gera só `BANK_FIXED_INCOME`.
- **Decisão:** novos produtos `renda-fixa-credito` e `tesouro-direto`, necessários para "todas as famílias". Os perfis prontos não mudam, então a massa atual continua igual.

## R07. Faturas

- **Decisão:** uma fatura por mês com transações. O ID é `{recurso}-b{yyyyMM}`. Nota de negociação: `{recurso}-n1`. O recurso dono sai do prefixo do ID (RN105).
