# C3 — Componentes

Níveis: [C1 — Contexto](C1-contexto.md) · [C2 — Contêineres](C2-conteineres.md)

Mostra os módulos e o caminho de uma chamada.

```mermaid
flowchart LR
  req["Chamada da receptora"]

  subgraph pipeline["Pipeline da chamada"]
    p1["Resolver instituição"]
    p2["Cabeçalhos e segurança<br/><small>400, 401, 403</small>"]
    p3["Capacidade<br/><small>529</small>"]
    p4["Tráfego<br/><small>429</small>"]
    p5["Parâmetros<br/><small>422</small>"]
    p6["Limite operacional<br/><small>423</small>"]
    p7["Regras de cenário<br/><small>latência, erro, timeout</small>"]
    p8["Responder<br/><small>dados e paginação</small>"]
    p9["Defeitos e marca"]
    p10["Contar cota e registrar"]
    p1-->p2-->p3-->p4-->p5-->p6-->p7-->p8-->p9-->p10
  end
  req --> p1

  cat["Catálogo de specs"]
  inst["Instituições"]
  cons["Consentimentos"]
  rec["Recursos"]
  ger["Gerador de dados"]
  lim["Contadores"]
  cen["Cenários"]

  p1-.->inst
  p2-.->cons
  p3-.->lim
  p4-.->lim
  p5-.->cat
  p6-.->lim
  p7-.->cen
  p8-.->ger
  p8-.->rec
  p9-.->cen
```

| Módulo | Responsabilidade |
|---|---|
| Catálogo de specs | Lê as specs fixadas, publica rotas, classes e schemas |
| Instituições | Bancos, perfis, configuração e histórico |
| Massa | Clientes, lotes, perfis de dados e distribuição |
| Gerador de dados | Semente, mundo virtual, ajustes e coerência |
| Consentimentos | Máquina de estados, permissões e renovação |
| Jornada | Sessão, login, confirmação e alçadas |
| Recursos | Estados, preparação e seleção |
| Limites | TPM, TPS e limite operacional |
| Cenários | Regras, defeitos e anomalias |
| Marcação | Ambiente e natureza |
| Segurança | Modos simples e FAPI |
| Registros | Chamadas e investigação |

Cada etapa é uma porta testável sozinha. A ordem segue [RN39](../../01-requisitos/regras-de-negocio.md#rn39).
