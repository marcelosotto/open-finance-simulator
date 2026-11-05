# 04 — Múltiplas alçadas

```mermaid
sequenceDiagram
  actor H as Helena
  actor P as Paula
  participant J as Jornada do banco
  participant S as Consentimentos
  participant R as Receptora

  H->>J: Confirma pela Indústria Aurora
  J->>S: Autoriza com pendência
  S-->>J: AUTHORISED, recursos PENDING_AUTHORISATION
  R->>S: GET /resources
  S-->>R: 200 com PENDING_AUTHORISATION
  R->>S: GET /accounts/{id}
  S-->>R: 403 STATUS_RESOURCE_PENDING_AUTHORISATION
  P->>J: Aprova na gestão do banco
  J->>S: Última aprovação
  S-->>J: Recursos AVAILABLE
  R->>S: GET /accounts/{id}
  S-->>R: 200
```
