# C1 — Contexto

Níveis: [C2 — Contêineres](C2-conteineres.md) · [C3 — Componentes](C3-componentes.md)

Mostra quem usa o simulador e com o que ele fala.

```mermaid
flowchart TB
  dev(["<b>Desenvolvedor e QA</b><br/><small>Configuram e investigam</small>"])
  op(["<b>Operador</b><br/><small>Mantém bancos e massa</small>"])

  sim["<b>Simulador Open Finance</b><br/><small>Instituições simuladas para dev e homologação</small>"]

  rec["<b>Receptora</b><br/><small>Coletor da XP ou outro cliente</small>"]
  specs["<b>open-finance-specs</b><br/><small>Specs oficiais fixadas por commit</small>"]
  obs["<b>Observabilidade</b><br/><small>OTLP</small>"]
  pcm["<b>Central PCM simulada</b><br/><small>Depois da primeira versão</small>"]

  rec -->|"APIs Open Finance"| sim
  dev --> sim
  op --> sim
  specs -->|"Contratos"| sim
  sim --> obs
  sim -.->|"Reporte SERVER"| pcm
```

A receptora chama o simulador como chamaria uma instituição real ([PR07](../../00-visao-geral/premissas.md#pr07)).
