# Quickstart: APIs de dados

Pré-requisitos: .NET 10, Docker e as specs baixadas em `.specs/`.

```bash
dotnet run --project tools/Simulador.Ferramentas -- specs baixar
```

```bash
dotnet test Simulador.slnx
```

Resultado esperado: os cenários de `dados-e-transacoes.feature`, `paginacao.feature` e `recursos.feature` passam, e `TodasAsFamiliasTests` valida os 69 endpoints contra o schema oficial. Ficam ignorados só os cenários `@spec-006` e `@spec-009`.
