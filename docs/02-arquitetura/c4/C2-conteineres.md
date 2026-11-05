# C2 — Contêineres

Níveis: [C1 — Contexto](C1-contexto.md) · [C3 — Componentes](C3-componentes.md)

```mermaid
flowchart TB
  rec["<b>Receptora</b>"]
  user(["<b>Operador, QA e desenvolvedor</b>"])
  cli(["<b>Cliente sintético</b>"])

  subgraph sim["Simulador Open Finance"]
    api["<b>API Open Finance</b><br/><small>.NET 10 · N instâncias<br/>Rotas das specs, limites e cenários</small>"]
    jornada["<b>Jornada</b><br/><small>Telas do banco e receptora de demonstração</small>"]
    admin["<b>API de administração</b><br/><small>.NET 10 · bancos, massa, cenários</small>"]
    bo["<b>Back-office</b><br/><small>Tela web</small>"]
    worker["<b>Worker</b><br/><small>Lotes e expirações</small>"]
    mongo[("<b>MongoDB</b><br/><small>Bancos, clientes, consentimentos,<br/>recursos, ajustes, cenários, histórico</small>")]
    redis[("<b>Redis</b><br/><small>TPM, TPS e limite operacional</small>")]
    reg[("<b>Registros de chamada</b><br/><small>MongoDB com expiração</small>")]
  end
  specs["<b>open-finance-specs</b>"]

  rec -->|"HTTPS"| api
  cli --> jornada
  user --> bo --> admin
  api --> mongo
  api --> redis
  api --> reg
  jornada --> mongo
  admin --> mongo
  admin --> reg
  worker --> mongo
  specs -.->|"Empacotadas na imagem"| api
```

| Contêiner | Por quê |
|---|---|
| API Open Finance | Separada para escalar sozinha na carga e isolar o caminho quente |
| Jornada | Tem telas e sessão, com ciclo diferente da API |
| API de administração | Fica em endereço separado ([RN71](../../01-requisitos/regras-de-negocio.md#rn71)) |
| Worker | Lotes longos e expirações não podem depender de uma chamada |
| MongoDB | Estado ([ADR-003](../../05-adr/ADR-003-mongodb-e-redis.md)) |
| Redis | Contadores por minuto e por mês, compartilhados entre as instâncias |

Na primeira versão, API, jornada e administração podem ser um único processo com módulos separados ([ADR-007](../../05-adr/ADR-007-arquitetura-e-endereco.md)).
