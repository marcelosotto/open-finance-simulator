# Contrato: administração para o back-office

Porta de administração, sem autenticação (RN71). Erros: `{ "erros": [...] }`. As demais rotas usadas pela tela estão nos contratos das specs 002, 003, 004, 007, 008 e 009.

## `GET /admin/ambiente` (Q2)
- 200: `{ "marca": "hml", "portaOpenFinance": 8080 }`. Com a marca vazia: `{ "marca": "", ... }`. A porta é `Simulador:PortaOpenFinance`, para a tela montar o exemplo de token (UI18). O valor é `Simulador:MarcaDoAmbiente`, lido na subida (RN66).

## `GET /admin/cenarios/regras/vencedora?instituicao=&endpoint=&cliente=&recurso=` (Q9)
- `instituicao` e `endpoint` (`operationId`) obrigatórios; `cliente` e `recurso` opcionais.
- 200:
  ```json
  { "instante": "2026-11-05T12:00:00+00:00",
    "comportamento": { "id": "r-...", "...": "regra completa" },
    "estado": null,
    "defeito": null }
  ```
  Cada campo é a regra que venceria entre as ativas (ligada e com pacote ligado ou sem pacote) de cada grupo, pela ordem de RN15, com a regra de comportamento filtrada pelos status declarados da operação. `instante` é a hora do banco (RN73). A frequência não entra: a rota diz qual regra é sorteada, não se dispara. Nada é sorteado nem contado.
- 404: instituição inexistente. 422: `instituicao` ou `endpoint` ausente, ou `endpoint` desconhecido.

## Back-office (Q3)
- `GET /` e qualquer `GET` sem extensão fora de `/admin` e `/saude`, na porta de administração: `index.html` do build do front.
- `GET /<arquivo>` (js, css, imagens): o arquivo do build, ou 404.
- Na porta Open Finance nada disso existe: as respostas não mudam.
- Sem a pasta do build (`Simulador:PastaDoBackOffice`), nada é servido.

## `GET /admin/massa/clientes?banco=&inicio=0&quantidade=100` (UI19)
- Lista os clientes **persistidos** (de lote e avulsos) que têm pelo menos um produto no `banco`, em ordem de documento. Sem `banco`, lista todos os persistidos. Clientes do mundo virtual não entram, porque não são gravados (RN52).
- 200: `{ "clientes": [{ "documento", "tipo", "nome", "natureza", "loteId" }], "inicio": 0, "quantidade": 100, "temMais": false }`.
- `inicio` ≥ 0 (padrão 0); `quantidade` de 1 a 1000 (padrão 100). Fora disso: 422.
- 404: `{ "erros": ["banco não encontrado: {banco}"] }`.

## `GET /admin/massa/resumo` (UI18)
- 200: `{ "clientes": 12, "consentimentos": 30, "recursos": 41, "lotes": 2 }`, contagem do que está persistido. Serve para Primeiros passos marcar o que já foi feito.

## Receptora padrão (RN124)
- Ao subir, o simulador cadastra a receptora de `Simulador:ReceptoraPadrao` (nome padrão `XP`) se o `organisationId` ainda não existe. Ela aparece em `GET /admin/receptoras` como qualquer outra, com usuário e senha de teste.

## Portas (revisão da 010)
- As APIs Open Finance e a administração se separam pela **porta do socket** (`Connection.LocalPort`), nunca pelo cabeçalho Host. Um Host forjado (`x:8081` enviado à 8080, ou o inverso) cai em 404: a administração, com as senhas de teste das receptoras, e o back-office ficam só na 8081, que o Compose publica em `127.0.0.1`.
- Na 8081, método errado responde 404, como rota inexistente: em caminho de página (diferente de GET ou HEAD) e também em `/admin` e `/saude` (por exemplo, `DELETE /admin/ambiente`). As rotas Open Finance têm um fallback que aceita qualquer método; ele pertence à outra porta e sai na escolha da rota, depois da checagem de método, e por isso não sobra o 405.
- `index.html` sai com `Cache-Control: no-cache`; os arquivos do build com hash no nome, com `public, max-age=31536000, immutable`.

## Autor (revisão da 010)
- O back-office manda `x-sim-autor` em percent-encoding UTF-8 (`Jo%C3%A3o`), porque o cabeçalho só aceita ASCII. A administração decodifica; nome em ASCII puro, sem `%`, chega igual.
