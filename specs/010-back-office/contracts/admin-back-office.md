# Contrato: administração para o back-office

Porta de administração, sem autenticação (RN71). Erros: `{ "erros": [...] }`. As demais rotas usadas pela tela estão nos contratos das specs 002, 003, 004, 007, 008 e 009.

## `GET /admin/ambiente` (Q2)
- 200: `{ "marca": "hml" }`. Com a marca vazia: `{ "marca": "" }`. O valor é `Simulador:MarcaDoAmbiente`, lido na subida (RN66).

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
