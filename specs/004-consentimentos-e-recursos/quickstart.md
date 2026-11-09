# Validação rápida

Pré-requisitos: Docker e as specs travadas em `.specs/`.

```bash
dotnet test Simulador.slnx
```

Esperado: passam `consentimento-api.feature`, `recursos.feature` e `seguranca.feature`, exceto os cenários `@spec-005`, `@spec-007` e `@spec-008`, além dos passos ligados de `massa-e-semente.feature`.

Fluxo manual, com `docker compose up`, seguindo [os contratos](contracts/token-e-admin.md):

1. Crie uma receptora em `POST /admin/receptoras` e um cliente em `POST /admin/massa/clientes`.
2. Peça o token com `scope=consents` e chame `POST /consents`: 201 em `AUTHORISED`.
3. Peça o token com `scope=consent:{consentId}` e chame `GET /resources`: 200.
4. Chame `DELETE /consents/{consentId}` e repita `GET /resources`: 401.
