# Spec 009: divergências e perguntas

**Aguardando respostas.** Formato: `Q1: A`. A recomendação é sempre a opção A.

Conferi `docs/` e os `.feature` contra os contratos em `.specs/7bdb9c1a6c4c265d7aabcdbc662f94c61718135e/contracts/expansion/snapshots/` (15 arquivos, 75 operações) e contra `src/` no commit `087cc0f`.

## Divergências e lacunas

- **D1. O catálogo de RN110 e o dos cenários não batem.** RN110 (`regras-de-negocio.md:149`) cita "valor incoerente", que não aparece no esquema de `defeitos-e-anomalias.feature:16-28`. Os seis nomes do `.feature` não estão em `docs/`. Ver Q6.
- **D2. RN111 diz "como uma regra de cenário", mas RN15 deixa vencer uma regra só.** Se o defeito for uma regra comum, ele disputa com as regras de status e de latência, e uma latência de 3 s no banco apagaria o defeito. A regra de estado (RN108) já resolveu isso disputando só com regras do mesmo tipo. Ver Q1.
- **D3. O alvo dos defeitos de campo não está definido.** No contrato de transações de contas (`accounts-2.5.1.yaml:909-916`), o primeiro campo obrigatório é `transactionId`, e o primeiro enum é `completedAuthorisedPaymentType`. O `.feature` espera `transactionAmount` e `type`. Ver Q4.
- **D4. RN115 contradiz RN37 sem dizer como.** RN37 congela a lista da chave. RN115 diz só "registros novos ou removidos". Falta dizer quais registros, em que página e como ficam `totalRecords` e `totalPages`. Ver Q7.
- **D5. Anomalias: falta modelo, nomes e API.** RN113 cita três desvios sem nome; os nomes estão só nos `.feature` (`defeitos-e-anomalias.feature:38-41`, `paginacao.feature:102`). Ver Q8 a Q11.
- **D6. RN114 fala da tela, mas a tela é da 010.** O cenário "Anomalias ligadas aparecem na tela" (`defeitos-e-anomalias.feature:43-47`) não tem `@tela`. Ver Q12.
- **D7. Cota e paginação da resposta com defeito.** RN27 e RN17 não dizem nada sobre defeito. Ver Q3.

## Conferido, sem divergência

| Afirmação | Evidência |
|---|---|
| Toda operação com `pagination-key` declara 422 | 35 de 35 |
| Toda operação declara 401 e 403 | 75 de 75 |
| `transactionId` tem `pattern` `^[a-zA-Z0-9][a-zA-Z0-9-]{0,99}$` | `accounts-2.5.1.yaml:926` |
| Sem datas, a consulta de transações cobre só hoje; com datas, até 12 meses para trás | `Regras.cs:174-190` (Dados) |
| O portão de segurança é a única origem do 401 nas rotas protegidas | `Rotas.cs:70-71` e `Acesso.cs:107-131` (Consentimentos); o 401 de `POST /token` (`Rotas.cs:590`) é OAuth |
| O host já envolve o portão (`PortaoPeloConsentimento`) e o resultado (`FiltroDaChamada`) | `AdaptadoresDeConsentimentos.cs:77`, `AdaptadoresDeCenarios.cs:50` |

## Escopo

Entram os 6 cenários de `defeitos-e-anomalias.feature` (14 casos com os exemplos) e "Anomalia de chave com 422" de `paginacao.feature:100`. O aviso visual de RN114 fica para a 010.

## Numeração de RN (para o merge com a 008)

As RNs novas desta spec usam o bloco **RN116 a RN119**, logo depois de RN115. As RNs existentes RN110 a RN115 são reescritas no lugar. Peço que a 008 não use esse bloco. Em `regras-de-negocio.md`, `casos-de-uso/README.md` e `Program.cs`, as mudanças ficam nas linhas da 009.

## Perguntas

### Q1 (D2). O que é um defeito?

- **A (recomendada):** uma regra de cenário com efeito `defeito` e o nome do catálogo. Tem alcance, frequência, período, semente e pacote, como as outras regras (RN111). Disputa só com outras regras de defeito, pela ordem de RN15, e vale junto com a regra de comportamento que vencer.
- **B:** uma regra comum, que disputa com todas (RN15): defeito e latência no mesmo alcance se excluem.
- **C:** uma entidade separada, com API própria, fora das regras.

**Motivo:** A aproveita o alcance, a semente e o cache da 007 e repete a solução de RN108. Com B, quem testa defeito num banco lento perde a latência.

### Q2. Em que chamadas o defeito age?

- **A (recomendada):** só na resposta que a etapa decidiu como normal (2XX) e que tem corpo JSON, em qualquer rota oficial que passa pela etapa (dados, Recursos e Consentimentos). O sorteio da frequência acontece em toda chamada que chega à etapa, mesmo que ela termine em erro, para a sequência da semente não depender dos sorteios do perfil. Sem corpo (202, 204) ou sem o campo-alvo, a resposta sai normal.
- **B:** só nas APIs de dados.
- **C:** sorteio só nas chamadas que terminariam em 2XX.

**Motivo:** com C, um 500 sorteado pelo perfil desloca a sequência, e o cenário "Defeito com taxa" deixa de repetir as mesmas chamadas.

### Q3 (D7). A resposta com defeito conta na cota? E na paginação?

- **A (recomendada):** conta, porque saiu com 2XX, como corpo truncado e JSON inválido (RN17, RN27). O defeito não muda a chave nem os links: a próxima página é calculada como se não houvesse defeito, e cada página sorteia de novo. Página seguinte continua sem contar (RN27). Com falha de transporte também valendo, o defeito é aplicado antes e a falha corrompe o corpo já alterado.
- **B:** não conta, por ser resposta "errada".

**Motivo:** a receptora recebeu um 2XX, e o banco real contaria. B deixaria a cota diferente do que a receptora vê.

### Q4 (D3). Qual campo os defeitos de campo atingem?

- **A (recomendada):** a regra pode informar `campo` (nome de uma propriedade do item de `data`). Sem `campo`, vale um critério fixo:
  - `campo-obrigatorio-ausente` tira o primeiro obrigatório cujo nome termina em `Amount`; se não houver, o último obrigatório;
  - `formato-invalido` reescreve o primeiro campo `date` ou `date-time` em `dd/MM/aaaa`;
  - `enum-invalido` troca `type`, se for enum, ou o primeiro enum, por `VALOR_FORA_DA_LISTA`.

  Atinge só o primeiro item da página (ou o objeto `data`, em detalhe). `campo` informado que não existe no endpoint: a resposta sai normal.
- **B:** o mesmo critério, mas em todos os itens da página.
- **C:** `campo` obrigatório na regra.

**Motivo:** com A, o `.feature` passa sem configurar campo, e o resultado é previsível. Um item ruim no meio de itens bons é o caso mais difícil para a receptora.

### Q5. Como funcionam os outros três defeitos do `.feature`?

- **A (recomendada):**
  - `transaction-id-repetido`: o segundo item recebe o `transactionId` do primeiro. Com menos de dois itens, ou sem `transactionId`, sai normal.
  - `total-records-incoerente`: `meta.totalRecords` vira o valor real mais 1, e `totalPages` não muda. Só onde o `meta` do contrato tem `totalRecords`.
  - `data-fora-da-janela`: os campos `date` e `date-time` do primeiro item com `transactionId` vão para dois anos antes do instante da chamada, pelo relógio do banco (RN73). Ficam fora de qualquer janela aceita (RN59).
- **B:** `data-fora-da-janela` usa o dia anterior ao `from` informado, e sai normal sem `from`.

**Motivo:** dois anos antes está fora de qualquer período, com ou sem datas na consulta, inclusive fatura antiga. B não age na consulta sem datas, que é a padrão.

### Q6 (D1). "Valor incoerente" de RN110

- **A (recomendada):** entra no catálogo como `valor-incoerente`: o primeiro objeto de valor (`amount` e `currency`) do primeiro item recebe `currency` `USD`. O valor continua válido no schema, mas não bate com a conta em reais.
- **B:** sai de RN110; o catálogo fica com os seis do `.feature`.
- **C:** o `amount` é multiplicado por 100.

**Motivo:** RN110 promete o defeito. A é o único dos três que não se confunde com `formato-invalido` e que a receptora só pega com uma regra de negócio.

### Q7 (D4). Como funciona o dado que muda na paginação (RN115)?

- **A (recomendada):** um defeito do catálogo, `dado-muda-na-paginacao`, com `quantidade` (padrão +1; negativa remove). Só age na página seguinte de uma lista paginada, ou seja, com chave válida.
  - Com quantidade positiva, acrescenta no início da página cópias do primeiro item, com o identificador acrescido de `-N1`, `-N2` e assim por diante, e soma a quantidade em `totalRecords`.
  - Com quantidade negativa, tira os primeiros itens da página e subtrai em `totalRecords`.
  - Nos dois casos, `totalPages` é recalculado, e o resultado continua válido no schema.

  RN37 cita essa exceção. Para saber se a página é seguinte, Dados marca a chamada com chave válida.
- **B:** um efeito novo de regra de comportamento, fora do catálogo de defeitos.
- **C:** gravar registros de verdade entre as páginas.

**Motivo:** A não grava nada (princípio X) e entra no mesmo sorteio e alcance dos defeitos. C quebraria RN37 para todos.

### Q8 (D5). O que é uma anomalia?

- **A (recomendada):** uma chave por instituição, ligada ou desligada, sem taxa, período ou alcance. Nasce desligada (RN114), e toda mudança vai para o histórico (PR02). Fica em `cenarios` como `anomalias:{banco}` e é lida pelo mesmo cache de 5 s das regras (RNF12). API:
  - `GET /admin/cenarios/anomalias`: catálogo, com nome e descrição;
  - `GET /admin/cenarios/anomalias/{banco}`: o estado de cada anomalia no banco;
  - `POST /admin/cenarios/anomalias/{banco}/{nome}/ligar` e `/desligar`.

  Banco inexistente: 404. Nome fora do catálogo: 404.
- **B:** uma regra de cenário com efeito `anomalia`, com alcance e taxa.

**Motivo:** RN113 e RN114 falam de anomalia ligada no banco, e o `.feature` só liga e desliga. ADR-008 já prevê que a anomalia troca o comportamento de uma etapa.

### Q9 (D5). Quais 401 o `revogado-com-403` troca?

- **A (recomendada):** todo 401 do portão de segurança nas rotas protegidas do banco: token ausente, expirado ou de outro banco, e consentimento inexistente, não autorizado, revogado ou expirado (RN88). A resposta é 403 com `code` `FORBIDDEN` (RN13). O 401 OAuth de `POST /token` não muda. O host troca o resultado em volta do portão, sem mexer em Consentimentos.
- **B:** só o 401 por estado do consentimento (RN88). O 401 de token continua. Exige uma mudança pequena no portão, dentro de Consentimentos, onde a 008 está trabalhando.

**Motivo:** RN113 diz "401 trocado por 403", e A não toca no módulo da 008. **Custo de A:** o nome cita "revogado", mas a troca vale para todo 401 do portão. A descrição no catálogo deixa isso claro.

### Q10 (D5). Como age o `chave-recusada-com-422`?

- **A (recomendada):** chave enviada, mas inválida ou expirada (RN36): responde 422 `PARAMETRO_INVALIDO`, na etapa de parâmetros (RN39), antes da cota. Por isso não conta (RN27). Chave válida ou sem chave: normal. As 35 operações com `pagination-key` declaram 422.
- **B:** 422 também para chave válida.

**Motivo:** o desvio oficial é só na chave ruim (RN36). B quebraria toda paginação do banco.

### Q11 (D5). Como age o `interacao-diferente`?

- **A (recomendada):** toda resposta de rota protegida em que o portão devolveria o `x-fapi-interaction-id` recebido sai com outro UUID v4 válido, do gerador injetado, inclusive as respostas de erro depois do portão. O 400 por falta do cabeçalho (RN38) já gera um valor e não muda.
- **B:** só nas respostas 2XX.

**Motivo:** a regra oficial vale para toda resposta (RN38), e o desvio também.

### Q12 (D6). Como o aviso de RN114 fica visível nesta spec?

- **A (recomendada):** `GET /admin/instituicoes` e `GET /admin/instituicoes/{endereco}` ganham `anomaliasLigadas`, a lista de nomes ligados (vazia por padrão). O dado vem de uma porta de Instituicoes implementada no host, sem referência entre módulos (ADR-007). O cenário roda nesta spec, pela API, e o aviso visual fica para a 010 (UI).
- **B:** o cenário ganha `@tela` e vai para a 010; nesta spec só existe a API de Cenários.

**Motivo:** com A, a 010 só desenha o que a API já entrega, e o cenário não fica ignorado.

### Q13. O que mais muda em `docs/`?

- **A (recomendada):**
  - RN110 lista o catálogo com os nomes;
  - RN111 diz que o defeito é efeito de regra e disputa só com defeitos (Q1, Q2);
  - RN113 lista as anomalias com os nomes;
  - RN115 descreve Q7, e RN37 cita a exceção;
  - RN116 cobre cota e paginação do defeito (Q3);
  - RN117 cobre o alvo dos defeitos de campo (Q4, Q5);
  - RN36 e RN88 citam a anomalia que os desobedece;
  - UC22 ganha fluxos alternativos;
  - o modelo de dados ganha `anomalias:{banco}`;
  - entram o contrato de administração e a lista de defeitos em `perfis-de-comportamento.md`.
- **B:** só as RNs de defeito e anomalia, sem tocar em RN36, RN37 e RN88.

**Motivo:** quem lê RN36, RN37 ou RN88 precisa saber que existe uma exceção nomeada (princípio III).
