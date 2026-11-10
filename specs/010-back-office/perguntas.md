# Spec 010: divergências e perguntas

**Respondidas em 2026-11-10.** Marcelo aprovou a opção A de Q1 a Q10 ("Siga as recomendações"), repassada pela sessão controladora. Decisões na seção Clarifications da [spec](spec.md).

Conferi `docs/` (em especial [interface.md](../../docs/01-requisitos/interface.md), o [guia de front-end](../../docs/08-front-end/README.md) e a [ADR-010](../../docs/05-adr/ADR-010-front-end-react.md)) contra os contratos de administração das specs 002 a 009 e contra `src/` no commit `0292043`.

## Divergências e lacunas

- **D1. A marca do ambiente tem duas fontes.** O guia de front-end pede `SIM_AMBIENTE` com `dev` ou `hml`, validado com zod (`docs/08-front-end/README.md`, "Variáveis de ambiente"). A constituição (princípio IV) diz que a marca é um valor livre, sem lista fechada e vazio em produção. O código diz que não existe outra fonte além de `Simulador:MarcaDoAmbiente` (`ExtensoesDaMarcacao.cs:15`, RN66). Hoje a API de administração não expõe a marca. Ver Q2.
- **D2. A UI06 pede "qual vence em caso de conflito", mas a API não diz.** `GET /admin/cenarios/regras` devolve só a lista. A ordem de RN15 está em `SelecaoDeRegras.Vencedora` (`Simulador.Cenarios/Dominio/Avaliacao.cs:26`). Se o front repetir essa ordem, a regra passa a existir em dois lugares. Ver Q9.
- **D3. Não há lista de consentimentos na administração.** Só existe `GET /admin/consentimentos/{consentId}`. Para chegar à UI14 por outro caminho que não seja logo depois de criar, sobram a busca por `consentId` e o cliente da massa (`GET /admin/massa/clientes/{documento}`, que traz os consentimentos). Ver Q7.
- **D4. Não há Node nesta máquina.** `node` e `npm` não existem nem no Git Bash nem no PowerShell. O Docker existe. O CI (`.github/workflows/ci.yml`) só tem .NET. O guia diz que "o CI roda lint, checagem de tipos, testes com threshold de cobertura e build de produção". Ver Q4.
- **D5. O front precisa chegar à porta 8081.** A administração só escuta em `127.0.0.1:8081` (`docker-compose.yml`, FR-025 da 002). Não há CORS no host. Se o front for servido de outra origem, o navegador bloqueia as chamadas. Ver Q3.
- **D6. Husky mexe na configuração do Git do repositório inteiro.** `husky` grava `core.hooksPath` no `.git/config`, que é compartilhado por todas as worktrees e vale também para commits só de .NET. Ver Q4.
- **D7. O escopo da linha 010 é amplo.** "Bancos, massa e cenários" mais UI10, UI13 e UI14 cobre umas 40 rotas de administração. Ver Q1.

## Conferido, sem divergência

| Afirmação | Evidência |
|---|---|
| Lista e detalhe de instituições trazem `anomaliasLigadas` | contrato da 009, "Instituições"; `RotasDeInstituicoes.cs` (lista recebe `IConsultaDeAnomalias`) |
| O detalhe do banco já traz `sla` (UI04) e `avisos` (UI05) | contrato da 002, "GET de uma instituição" |
| O menu de consentimento já vem pronto da API: grupos do tipo do cliente, contas e cartões marcados, agrupados e receptoras | contrato da 008, `GET /admin/consentimentos/opcoes` |
| A visão admin do consentimento já aplica RN97 e RN99 (pendências zeradas em `REJECTED`, sem `PENDING_AUTHORISATION`) | contrato da 008, `GET /admin/consentimentos/{consentId}` |
| Nenhuma rota oficial precisa ser chamada pela tela | todas as telas têm rota em `/admin/...` |
| O último número de RN é RN123 | `regras-de-negocio.md:167` |

## Numeração de RN

Reservo **RN124 a RN127** para esta spec. Até aqui não vejo diferença do comportamento oficial nas telas, então provavelmente nenhuma será usada. Se Q2 ou Q9 criarem rota nova, ela entra no contrato desta spec, não em RN, porque não é desvio do oficial.

## Perguntas

### Q1 (D7). Quais telas entram nesta spec?

- **A (recomendada):** tudo o que tem regra de tela em `interface.md` e o que é preciso para usar essas telas:
  - **Bancos (P1):** UI01 a UI05, com histórico, anomalias e relógio.
  - **Consentimento (P1):** UI10, UI13 e UI14, com busca por `consentId` e revogação.
  - **Receptoras (P1):** lista e criação, porque o menu exige uma.
  - **Massa (P2):** perfis de dados, distribuições (UI07), lotes (gerar, acompanhar, cancelar, clientes), cliente avulso e limpeza com prévia.
  - **Cenários (P2):** regras com todos os efeitos, inclusive `defeito`, pacotes e a vencedora (UI06).
  - **Ficam para depois:** mundo virtual, ajustes, transações de um recurso, mudar estado de recurso, adicionar produto a cliente e histórico da massa.
- **B:** tudo o que a administração oferece, inclusive mundo virtual e ajustes.
- **C:** só bancos e consentimento (P1); massa e cenários em outra spec.

**Motivo:** A cobre o comando da linha 010 ("bancos, massa e cenários") e todas as UI do `interface.md`. Os itens deixados de fora não têm regra de tela e são usados por quem já escreve JSON. B dobra o tamanho sem nenhuma UI nova. C deixa de fora a massa e os cenários, que estão no nome da spec.

### Q2 (D1). De onde a tela tira a marca do ambiente?

- **A (recomendada):** de uma rota nova, `GET /admin/ambiente`, que devolve `{ "marca": "hml" }` ou `{ "marca": "" }` lido de `Simulador:MarcaDoAmbiente`. O front não tem variável de marca: `SIM_AMBIENTE` sai do guia de front-end, e a cor vem de uma lista fixa para `dev`, `hml` e `stage`, com uma cor neutra para qualquer outro valor.
- **B:** variável `SIM_AMBIENTE` no build do front, com valor livre (corrigir o `dev | hml` do guia), vazia em produção.
- **C:** como está no guia: `SIM_AMBIENTE` só com `dev` ou `hml`.

**Motivo:** com A, a marca tem uma fonte só, como pedem a constituição e RN66, e a imagem do front serve a qualquer ambiente sem novo build. Com B, as duas configurações podem divergir: a tela diria `hml` sobre dados `stage`. C contradiz o princípio IV ("sem lista fechada").

### Q3 (D5). Como o front chega à API e sobe no Compose?

- **A (recomendada):** o próprio simulador serve o build do front na porta de administração (8081), em `/`, e o front chama `/admin/...` na mesma origem. A `Dockerfile` ganha um estágio Node que gera o build. Sem CORS, sem contêiner novo, e o back-office herda o `127.0.0.1` da administração. No desenvolvimento, o `webpack-dev-server` faz proxy de `/admin` para `localhost:8081`. `SIM_ADMIN_API_URL` fica vazio por padrão (mesma origem).
- **B:** um contêiner `nginx` próprio para o front, com proxy de `/admin` para `simulador:8081`, em `127.0.0.1:8082`.
- **C:** front em outra origem, com CORS liberado na administração.

**Motivo:** A é a solução mais simples (princípio X) e não abre a administração para outra origem. B adiciona uma imagem e uma configuração a manter. C abre uma porta sem login para qualquer origem do navegador.

### Q4 (D4, D6). Como rodar os portões do front, aqui e no CI?

- **A (recomendada):**
  - **No CI:** um job novo, `front`, em paralelo ao de .NET, com `actions/setup-node` (Node 22 LTS) e `npm ci`, `lint`, `typecheck`, `test` com cobertura e `build`. Com Q3 A, o `docker build` também gera o front.
  - **Nesta máquina:** rodo os mesmos comandos dentro da imagem `node:22` pelo Docker, sem instalar nada.
  - **Husky, lint-staged e commitlint:** configurados em `front/`, mas o `prepare` não ativa os ganchos sozinho. Quem quiser roda `npm run ganchos` uma vez. Eu não ativo aqui, porque isso mudaria o `.git/config` de todas as worktrees.
- **B:** igual a A, mas Marcelo instala o Node 22 nesta máquina, e os ganchos do Husky ficam ativos pelo `prepare`.
- **C:** os portões do front ficam fora do CI por enquanto.

**Motivo:** A cumpre o guia ("o CI roda lint, checagem de tipos, testes e build") sem tocar a máquina nem os commits só de .NET. B mexe na máquina e na configuração do Git de todas as sessões. C deixa a cobertura de 80% sem trava.

### Q5. Como funciona "o resto já vem preenchido" do novo banco (UI03)?

- **A (recomendada):** a tela de novo banco pede só nome e perfil e cria o banco. Em seguida, abre a edição do banco criado, com todos os campos preenchidos pelo que a API devolveu (padrões de RN03 e do perfil) e prontos para ajustar.
- **B:** um formulário único com todos os campos, preenchidos no front pelos padrões do perfil (`GET /admin/perfis-de-comportamento`), e um só `POST`.

**Motivo:** com A, os padrões vêm só do servidor, e o front não repete RN03 nem a tabela de perfis. B repete no front os padrões de campos que hoje não estão todos na lista de perfis (público, produtos e limites).

### Q6. Como Bruno escolhe as permissões no menu (UI10)?

- **A (recomendada):** cada grupo da API aparece com uma caixa e, aberto, com as permissões dele, todas marcadas. Bruno pode desmarcar uma permissão e montar um grupo incompleto; a API recusa com o código oficial (RN83), e a tela mostra. `RESOURCES_READ` aparece fixa, sempre marcada (RN96).
- **B:** só grupos inteiros: não dá para montar grupo incompleto na tela.

**Motivo:** o cenário "Grupo de permissões incompleto" de `consentimento-no-painel.feature` pede que Bruno escolha um grupo incompleto e veja a recusa. Com B, o cenário não pode ser feito pela tela.

### Q7 (D3). Como se chega a um consentimento já criado (UI14)?

- **A (recomendada):** logo depois de criar, por uma busca por `consentId` no menu, e pela tela do cliente da massa, que lista os consentimentos dele com link. Sem rota nova.
- **B:** uma rota nova de lista, `GET /admin/consentimentos?cliente=&banco=`, e uma tela de lista.

**Motivo:** A usa só rotas que já existem e cobre os cenários da 008. A lista de B pode ser feita depois, se fizer falta.

### Q8. Quem é o autor das alterações feitas pela tela?

- **A (recomendada):** um campo "Seu nome" no topo, guardado no navegador e enviado em `x-sim-autor` em toda escrita. Vazio, o cabeçalho não vai, e a API grava `operador`.
- **B:** a tela nunca envia autor: tudo fica como `operador`.

**Motivo:** RN71 pede "autor informado", e o histórico só serve para Diego se disser quem mudou. Não há login, então o nome é livre.

### Q9 (D2). Como a UI06 mostra a regra que vence?

- **A (recomendada):** uma rota nova, `GET /admin/cenarios/regras/vencedora?instituicao=&endpoint=&cliente=&recurso=`, que usa a mesma `SelecaoDeRegras` do pipeline, pelo relógio do banco, e devolve a vencedora de comportamento, a de estado e a de defeito, ou nenhuma. A tela de cenários tem um painel "qual regra vale para esta chamada?". A lista agrupa as regras por alcance (recurso, cliente, endpoint, instituição). A frequência não entra: a resposta diz qual regra é sorteada, não se ela dispara.
- **B:** o front reimplementa a ordem de RN15 e marca a vencedora na lista.
- **C:** só listar por alcance, na ordem de RN15, sem apontar a vencedora.

**Motivo:** com A, a ordem de RN15 fica em um lugar só, e a tela não pode discordar do pipeline. B duplica a regra no front, contra o princípio I. C não cumpre a UI06.

### Q10. Os cenários do painel rodam no navegador nesta spec?

- **A (recomendada):** sim. Uma suíte Playwright pequena em `front/e2e/`, contra o simulador real subido pelo Compose. Cobre os 9 cenários de `consentimento-no-painel.feature` e o `@tela` de `marcacao.feature`. Roda num job próprio do CI, depois do build. Aqui, roda pela imagem oficial do Playwright no Docker.
- **B:** sem Playwright agora. Os cenários são provados com Testing Library e `msw`, e o Playwright fica para uma spec de testes.

**Motivo:** a [estratégia de testes](../../docs/06-testes/estrategia-de-testes.md) pede Playwright para o painel, e a constituição (princípio IX) pede que os cenários aprovados rodem no CI. Com B, os cenários do painel ficam provados só contra uma API falsa.
