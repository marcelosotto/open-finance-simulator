# Constituição do Simulador Open Finance

## Princípios Fundamentais

### I. A documentação em docs/ é a fonte da verdade

- Toda especificação, plano e tarefa MUST partir de `docs/` e citar os IDs de origem:
  RN, UC, RF, RNF, UI, ADR e PR.
- O código MUST NOT criar regra. Comportamento novo entra primeiro em `docs/`.
- Divergência entre código e `docs/` é defeito: corrige-se o código ou, com aprovação,
  a documentação.

### II. Regra oficial por padrão

- Sem configuração, toda instituição simulada MUST seguir as regras oficiais do Open Finance
  Brasil: limites, códigos HTTP, estados, paginação e formatos.
- Conflitos entre fontes MUST ser resolvidos pela ordem de `docs/00-visao-geral/referencias.md`.
- Todo banco nasce com os limites oficiais, e cada valor pode ser alterado por configuração.
- Diferença intencional do processo oficial MUST estar escrita em uma RN. Exemplo: o
  consentimento nasce `AUTHORISED`, sem a etapa `AWAITING_AUTHORISATION` (RN80).

### III. Desvio sempre explícito

- Toda anomalia de protocolo MUST ter nome, nascer desligada e aparecer no histórico de
  configuração.
- Um desvio MUST NOT acontecer por padrão ou por acaso.

### IV. Tudo simulado e marcado

- Os componentes MUST NOT aceitar nem guardar dado real como massa.
- Todo dado gerado MUST carregar ambiente e natureza (persistido ou virtual) no cabeçalho, no
  prefixo dos IDs e no texto marcado (RN60 a RN66).
- A marca de ambiente serve para saber de qual ambiente saiu a informação. Ela é um valor
  livre definido na configuração da implantação (`dev`, `hml`, `stage` ou outro), sem lista
  fechada nem regra: o sistema só concatena o valor. Em produção a configuração fica vazia e
  nada é concatenado.
- Nenhum perfil pode desligar a marcação. O valor da marca vem só da configuração do ambiente.

### V. Só responde; não julga nem registra

- O simulador produz lentidão, erro e dado ruim. Ele MUST NOT validar a qualidade dos dados
  nem avaliar a reação de quem consome.
- Ele MUST NOT explicar na resposta da API por que ela saiu assim, e MUST NOT guardar registro
  das chamadas. Quem registra e investiga é a aplicação que consome (RN18).
- A observabilidade própria cobre apenas a saúde do simulador (RNF09).

### VI. Repetível

- Semente e relógio MUST ser injetados.
- O mesmo cenário, com a mesma semente, a mesma massa e o mesmo relógio, MUST dar o mesmo
  resultado.
- Dado virtual é calculado na hora e não é gravado. Só total, semente, distribuição e ajustes
  são persistidos (RN52 e RN53).

### VII. Specs oficiais como fonte das APIs

- Rotas, contratos e exemplos MUST sair de `open-finance-specs`, fixado por commit em
  `specs.lock.json`. O commit inicial é `7bdb9c1a6c4c265d7aabcdbc662f94c61718135e`.
- Nenhuma rota oficial pode ser escrita à mão.
- Com perfil `estavel` e sem defeitos, toda resposta MUST validar contra o schema oficial.

### VIII. Cliente sem mudança

- A receptora MUST usar os mesmos caminhos e cabeçalhos de produção, trocando só endereço e
  credenciais.
- Atalhos de teste MUST ficar em APIs de administração, separadas das APIs Open Finance.
- A identidade da receptora, o cliente e o consentimento MUST vir do token, nunca de cabeçalho
  próprio.

### IX. Testes primeiro (INEGOCIÁVEL)

- Os cenários Gherkin aprovados em `docs/` MUST existir antes da implementação e rodar no CI.
- A cobertura MUST ser de 100% no domínio e de 80% no total.
- Nenhum teste pode depender de hora real ou de sorteio sem semente.

### X. Simplicidade

- Isto é um simulador: a solução mais simples que atende à regra MUST ser a escolhida.
- Nenhuma dependência nova entra sem necessidade demonstrada.
- Trocar um item da stack exige ADR.

## Restrições Técnicas e Stack

- **Back-end:** .NET 10 com Minimal API, em monólito modular (ADR-001 e ADR-007).
- **Dados:** MongoDB para estado e Redis para contadores compartilhados (ADR-003).
- **Back-office:** React com TypeScript (PA14).
- **Endereço:** cada instituição tem um prefixo na URL (ADR-007).
- **Segurança:** modo simples nesta versão. Modo FAPI adiado (ADR-006).
- **Avaliação da chamada:** pipeline único, com ordem fixa de etapas (ADR-008 e RN39).
- **Execução:** o ambiente completo MUST subir com Docker Compose (RNF11).
- **Testes:** xUnit, Reqnroll, Testcontainers, Playwright e k6 (ADR-009).
- **Metas:** RNF01 a RNF12 valem para toda spec. O simulador não pode ser o gargalo de um teste.

## Fluxo de Desenvolvimento e Portões de Qualidade

- As specs seguem `docs/07-gerenciamento/sequencia-de-specs.md`, em ordem.
- Só caso de uso **Aprovado** vira spec. Itens **Adiados** e **Removidos** ficam fora.
- Cada spec segue: `/speckit-specify`, `/speckit-clarify`, `/speckit-plan`, `/speckit-tasks`,
  `/speckit-analyze` e `/speckit-implement`.
- Todo plano MUST passar pelo Constitution Check.
- **Portões de CI:** formatação, análise estática, testes, cobertura, conformidade com as specs
  e teste da tabela oficial de endpoints.
- As alterações MUST ser mostradas a Marcelo, com aprovação dele, antes de cada commit.

## Governance

- Esta constituição prevalece sobre outras práticas do projeto.
- Emendas MUST ser aprovadas por Marcelo (PA15) e registradas com a versão nova.
- Versionamento semântico: MAJOR para remover ou redefinir princípio, MINOR para princípio ou
  seção nova, PATCH para ajuste de texto.
- Toda revisão de plano e de código MUST verificar a conformidade com esta constituição.
  Qualquer exceção MUST ser justificada no plano.

**Version**: 2.0.0 | **Ratified**: 2026-11-05 | **Last Amended**: 2026-11-05
