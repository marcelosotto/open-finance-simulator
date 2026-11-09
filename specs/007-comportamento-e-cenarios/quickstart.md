# Quickstart: Comportamento e cenários

Com o ambiente de `docker compose up`:

1. Criar uma regra: `POST /admin/cenarios/regras` com `{ "instituicao": "banco-estavel", "efeito": { "tipo": "status", "status": 500 } }`. Toda chamada que chegar à etapa responde 500.
2. Fixar o relógio: `PUT /admin/instituicoes/banco-estavel/relogio` com `{ "fixadoEm": "2026-12-31T23:59:50Z" }`. As respostas trazem `x-sim-clock`.
3. Voltar: `DELETE /admin/instituicoes/banco-estavel/relogio`.

Validação automática: `dotnet test Simulador.slnx`. Os cenários de `comportamento.feature` rodam sem etiqueta.
