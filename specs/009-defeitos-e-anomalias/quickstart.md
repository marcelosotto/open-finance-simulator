# Quickstart: Defeitos e anomalias

Com o ambiente de `docker compose up`:

1. Defeito: `POST /admin/cenarios/regras` com `{ "instituicao": "banco-estavel", "efeito": { "tipo": "defeito", "defeito": "enum-invalido" } }`. As transações passam a trazer `type` fora da lista no primeiro item.
2. Anomalia: `POST /admin/cenarios/anomalias/banco-estavel/revogado-com-403/ligar`. Consentimento revogado passa a responder 403, e `GET /admin/instituicoes` mostra `anomaliasLigadas`.
3. Desligar: `POST /admin/cenarios/anomalias/banco-estavel/revogado-com-403/desligar` e `DELETE /admin/cenarios/regras/{id}`.

Validação automática: `dotnet test Simulador.slnx`. Os cenários de `defeitos-e-anomalias.feature` e "Anomalia de chave com 422" rodam sem etiqueta.
