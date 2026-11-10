# Quickstart: Consentimento no painel

Com o ambiente de `docker compose up`, uma receptora cadastrada e um cliente com produtos no Banco Estável:

1. Opções: `GET /admin/consentimentos/opcoes?cliente={documento}&banco=banco-estavel`.
2. Criar: `POST /admin/consentimentos` com `{ "cliente": "...", "banco": "banco-estavel", "receptora": "...", "permissoes": ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ"], "prazoMeses": 12 }`. Nasce `AUTHORISED`.
3. PJ com alçadas: acrescentar `"aprovacoesPendentes": 2`; depois `POST /admin/consentimentos/{id}/aprovar` duas vezes, ou `/recusar`.
4. Consultar: `GET /admin/consentimentos/{id}`.

Validação automática: `dotnet test Simulador.slnx`. Os cenários de `consentimento-no-painel.feature` rodam sem etiqueta.
