# C3 — Componentes

Níveis: [C1 — Contexto](C1-contexto.md) · [C2 — Contêineres](C2-conteineres.md)

Mostra os módulos e o caminho de uma chamada.

```mermaid
flowchart LR
  req["Chamada da receptora"]

  subgraph pipeline["Pipeline da chamada"]
    p1["Resolver instituição"]
    p2["Cabeçalhos e segurança do token<br/><small>400, 401, 403</small>"]
    p3["Capacidade<br/><small>529</small>"]
    p4["Tráfego<br/><small>429</small>"]
    p5["Acesso ao recurso e parâmetros<br/><small>403, 422</small>"]
    p6["Limite operacional<br/><small>423</small>"]
    p7["Regras de cenário<br/><small>latência, erro, timeout</small>"]
    p8["Responder<br/><small>dados e paginação</small>"]
    p9["Defeitos e marca"]
    p10["Contar cota<br/><small>só em 2XX</small>"]
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
  mar["Marcação"]

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
  p9-.->mar
  p10-.->lim
```

| Módulo | Responsabilidade |
|---|---|
| Catálogo de specs | Lê as specs fixadas, publica rotas, classes e schemas |
| Instituições | Bancos, perfis, configuração e histórico |
| Massa | Clientes, lotes, perfis de dados e distribuição |
| Gerador de dados | Semente, mundo virtual, ajustes e coerência |
| Consentimentos | Criação no painel, máquina de estados, permissões, alçadas e renovação |
| Recursos | Estados, preparação e seleção |
| Limites | TPM, TPS e limite operacional |
| Cenários | Regras, defeitos e anomalias |
| Marcação | Ambiente e natureza |
| Segurança | Modo simples |

Cada etapa é uma porta testável sozinha. A ordem segue [RN39](../../01-requisitos/regras-de-negocio.md#rn39). A cota é contada depois da resposta, só em 2XX ([ADR-008](../../05-adr/ADR-008-pipeline-de-avaliacao.md)).

Não há módulo de registro de chamadas: o simulador só responde, e quem registra e investiga é a aplicação que consome ([RN18](../../01-requisitos/regras-de-negocio.md#rn18)). A observabilidade cobre só a saúde do simulador ([RNF09](../../01-requisitos/requisitos.md#rnf09)).
