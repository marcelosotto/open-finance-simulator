# Quickstart: Limites

Pré-requisito: Docker em execução e `.specs` baixado (`specs baixar`).

```bash
dotnet test Simulador.slnx --filter "FullyQualifiedName~Limites"
```

Esperado: cenários de `limites.feature`, os dois de cota de `paginacao.feature` e o teste com Redis real passam.

Validação manual: `docker compose up --build`, e baixar a capacidade de um banco em `PATCH /admin/instituicoes/{endereco}` (porta 8081). Chamadas acima do valor no mesmo segundo respondem 529. Com `docker compose stop redis`, as chamadas seguem normais.
