# 01 — Consentimento no painel

Criação pelo menu ([UC10](../../01-requisitos/casos-de-uso/README.md#uc10)), seguida da primeira chamada da receptora ([UC25](../../01-requisitos/casos-de-uso/README.md#uc25)).

```mermaid
sequenceDiagram
  actor U as Usuário do painel
  participant B as Back-office
  participant S as Consentimentos
  participant R as Receptora
  participant A as Autorização
  participant D as APIs de dados

  U->>B: Escolhe cliente, instituição e receptora
  U->>B: Escolhe permissões, contas e prazo
  B->>S: Cria o consentimento
  S-->>B: AUTHORISED, com consentId
  R->>A: Usuário, senha e consentId
  A-->>R: Token
  R->>D: GET /accounts com o token
  D-->>R: 200
```
