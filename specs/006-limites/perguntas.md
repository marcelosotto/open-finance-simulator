# Spec 006: divergências e perguntas

**Respondidas em 2026-11-09.** Aprovadas as recomendações, salvo Q4 (mudou para a opção B: limites antes do 403 de recurso, com ressalva em RN39) e Q10 (opção B: códigos em inglês). Decisões na seção Clarifications da [spec](spec.md).

Conferência de `docs/` contra os contratos em `.specs/7bdb9c1a6c4c265d7aabcdbc662f94c61718135e/contracts/expansion/snapshots/` (15 arquivos, conferidos com `specs verificar`). Os números de linha são desses arquivos.

## BLOQUEIO: divergências entre docs e contratos

### D1. A tabela lê a capacidade por endpoint; o contrato diz que é global

`tabela-de-endpoints.md:6` diz "TPS: 300 em todos os endpoints". O 529 é descrito como "limite máximo de TPS global" em todos os 15 contratos (`accounts-2.5.1.yaml:1706`, `consents-3.3.1.yml:1635`, `resources-3.1.0.yml:469`). RN29, UC20 e `glossario.md:29` tratam a capacidade como da instituição. Ver Q1.

### D2. RN23 diz que o 429 é exclusivo do TPM; RN13 e o contrato admitem outras origens

RN23: "429, que é exclusivo desse limite". RN13: 429, 423 e 529 "só aparecem quando o limite estoura ou por uma regra de cenário". O contrato descreve o 429 também por "limite global de requisições concorrentes" (`accounts-2.5.1.yaml:1682`, `resources-3.1.0.yml:457`, `consents-3.3.1.yml:1593`). Ver Q2.

## Conferido, sem divergência

| Afirmação | Evidência |
|---|---|
| Os 69 endpoints de dados declaram 423, 429 e 529 (RN23, RN28, RN29) | contagem por arquivo: 6+7+7+5+5+5+5+5+5+6+5+5+3 = 69 operações, 3 status em cada |
| Consentimentos e Recursos não declaram 423 (RN28) | `consents-3.3.1.yml` (5 operações) e `resources-3.1.0.yml` (1) só têm 429 e 529 |
| Chave existe para não contar a chamada (RN27) | `pagination-key`: "evitar a contagem de chamadas ao endpoint durante a paginação" (`accounts-2.5.1.yaml:1458`) |
| Corpo do erro | `errors[]` com `code`, `title` e `detail` obrigatórios, `code` livre (`accounts-2.5.1.yaml:1342-1373`, `:1358-1362`) |

Sem conflito, mas a registrar:

- Consentimentos e Recursos declaram 429 (`consents-3.3.1.yml:1593`, `resources-3.1.0.yml:457`), e RN24 os isenta. Declarar não obriga a responder: mesmo raciocínio do 503 na spec 002.
- `Retry-After` (RN23, PA11) não aparece em nenhum dos 15 contratos. Cabeçalho a mais não invalida o schema.
- O simulador não expõe `tela` nesta spec: `avisos` já sai na administração (`specs/002-instituicoes/contracts/admin-instituicoes.md:32`).

**Não conferível aqui:** os números e regras de F01, F03 e F04 (TPM, faixas de QCA, mínimos mensais, 300 TPS, 2XX, 60 minutos). Essas fontes não estão no repositório, e os contratos não trazem esses valores.

**A saber para o plano:** Redis ainda não existe no projeto: não está em `docker-compose.yml`, `Directory.Packages.props` nem em `ci.yml`. O ADR-003 já autoriza. Hoje os 403 de recurso e os 422 saem dentro de `ServicoDeDados.Responder` (`src/Modulos/Simulador.Dados/Aplicacao/ServicoDeDados.cs:52-68`, `:317-319`), depois do portão (`Despachante.cs:30`).

## Perguntas

Respostas no formato `Q1: A`. Recomendação sempre na opção A.

### Q1 (BLOQUEIO D1). Qual é o alcance da capacidade?

- **A (recomendada):** um contador por instituição; corrigir `tabela-de-endpoints.md:6` para "300 por instituição".
- **B:** um contador por endpoint, como a tabela lê.

**Motivo:** o contrato (nível 3 da ordem das fontes) diz "TPS global" e vence a tabela do portal (nível 4). RN29 e UC20 já dizem isso; só a tabela destoa.

### Q2 (BLOQUEIO D2). Quem pode produzir 429?

- **A (recomendada):** ajustar RN23: entre os limites, o 429 sai só do TPM; regra de cenário pode produzi-lo (RN13); o motivo "concorrência global" do contrato não é simulado.
- **B:** 429 exclusivo do TPM, retirando a regra de cenário de RN13.
- **C:** simular também o limite de concorrência global.

**Motivo:** A mantém o que a spec 007 precisa, remove a contradição interna e não cria limite novo (princípio X).

### Q3. A capacidade vale em quais rotas?

- **A (recomendada):** nas 75 operações dos 15 contratos, que declaram 529; `POST /token` e administração ficam de fora e não contam.
- **B:** só nos 69 endpoints de dados.
- **C:** também em `POST /token`.

**Motivo:** RN24 isenta Consentimentos e Recursos só do limite por origem, e ambos os contratos declaram 529 (`consents-3.3.1.yml:1634`, `resources-3.1.0.yml:468`). O token do modo simples não está em nenhum contrato.

### Q4. Onde entram o 403 de recurso e o `PAGE_NOT_FOUND` na ordem de RN39?

Com o endpoint acima do TPM, uma conta de outro cliente responde 403 ou 429? Com a cota esgotada, página além do total responde 422 ou 423?

- **A (recomendada):** literal: o 403 de RN104 e RN105 é segurança (antes de capacidade e TPM), e `PAGE_NOT_FOUND` é parâmetro (antes da cota). Sem mudar `docs/`.
- **B:** capacidade e TPM logo após token e consentimento; o 403 de recurso e o 422 vêm depois; RN39 ganha essa ressalva.

**Motivo:** RN39 lista 403 e 422 sem distinguir, e `docs/` é a fonte da verdade. **Custo de A:** ler o recurso antes de contar; B poupa essa leitura, mas muda a regra.

### Q5. Cota esgotada durante a paginação

- **A (recomendada):** chave válida não confere a cota e responde 200; chave recusada confere e responde 423, sem emitir chave. UC19 passo 1 ganha a ressalva.
- **B:** toda chamada confere a cota: esgotada, a página seguinte de chave válida também responde 423.

**Motivo:** o contrato diz que a chave evita a contagem (`accounts-2.5.1.yaml:1458`). Com B, a chamada que usa a última unidade da cota não conseguiria terminar sua própria coleta. Chave recusada conta como consulta nova (RN36), logo responde como consulta nova. UC19 passo 1 e a sequência 02 hoje conferem sempre.

### Q6. O que é o "objeto mais granular" de RN25?

- **A (recomendada):** o último parâmetro do caminho (`accountId`, `billId`, `operationId`, `brokerNoteId`); rota sem parâmetro usa o consentimento.
- **B:** sempre o recurso do consentimento (a fatura conta na conta do cartão), e rota sem parâmetro usa o consentimento.

**Motivo:** A é a leitura direta de "mais granular" e sai do próprio contrato. F04 não está no repositório.

### Q7. Em que fuso vira o mês da cota?

- **A (recomendada):** UTC, como o resto do simulador (`ServicoDeDados.cs:114`).
- **B:** horário de Brasília.

**Motivo:** `docs/` não define. A é coerente e simples; B muda só as 3 horas da virada. O minuto e o segundo não são afetados.

### Q8. Se os contadores ficarem indisponíveis?

- **A (recomendada):** a chamada segue como se estivesse dentro dos limites, e a falha vai só para a observabilidade (RNF09).
- **B:** responde 500.

**Motivo:** o simulador não pode produzir erro que ninguém configurou (princípio III), o 503 é só "fora do ar" (RN13), e o ADR-003 já aceita perder contadores.

### Q9. Como contar a cota sob concorrência?

- **A (recomendada):** como manda o ADR-008: confere antes, conta depois do 2XX. Chamadas simultâneas sobre o mesmo objeto podem ultrapassar a cota em poucas unidades. Risco aceito e registrado.
- **B:** reserva atômica antes e devolve se não for 2XX; nunca ultrapassa, mas exige emendar o ADR-008.

**Motivo:** o contador é por objeto, cliente e mês, então a corrida é rara e não afeta o TPM, que é atômico. Princípio X.

### Q10. Quais `code`, `title` e `detail` nas recusas?

- **A (recomendada):** `MUITAS_REQUISICOES` (429), `LIMITE_OPERACIONAL_ATINGIDO` (423), `SITE_SOBRECARREGADO` (529), com título e detalhe fixos e genéricos.
- **B:** nomes do HTTP em inglês (`TOO_MANY_REQUESTS`, `LOCKED`, `SITE_IS_OVERLOADED`).

**Motivo:** `code` é livre no contrato (`accounts-2.5.1.yaml:1358-1362`), e a spec 005 usou português (`PARAMETRO_INVALIDO`). O status já diz o que aconteceu, então não fere RN18.

### Q11. O cenário "Erro não consome a cota" (`limites.feature:61`) entra nesta spec?

O sorteio de 500 só existe na spec 007: hoje `TaxaDeErro` é só configuração.

- **A (recomendada):** fica na 006, com o 500 provocado pelo teste (dependência de dados com falha).
- **B:** etiquetar `@spec-007` e entregar lá.

**Motivo:** a regra "só 2XX conta" é desta spec, e A a prova sem esperar o sorteio.
