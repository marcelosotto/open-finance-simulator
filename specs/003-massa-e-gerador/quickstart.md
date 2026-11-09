# Quickstart: Massa e gerador

Pré-requisitos: Docker e SDK do .NET 10.

## Testes
```bash
dotnet run --project tools/Simulador.Ferramentas -- specs baixar
dotnet test Simulador.slnx
```
Esperado: os cenários de `massa-e-semente.feature` passam.

## Ambiente
```bash
docker compose up --build
curl http://localhost:8081/admin/massa/perfis
curl -X POST http://localhost:8081/admin/massa/lotes -H "content-type: application/json" -d "{\"quantidade\":100,\"semente\":42}"
curl http://localhost:8081/admin/massa/lotes/lote-0
```
Esperado: 3 perfis prontos, 202 na criação e o lote em `concluido`, com o resumo.

```bash
curl -X PUT http://localhost:8081/admin/massa/mundo-virtual -H "content-type: application/json" -d "{\"total\":10000000,\"semente\":42,\"confirmar\":true}"
curl "http://localhost:8081/admin/massa/mundo-virtual/itens?inicio=9999000&quantidade=1000"
```
Esperado: 1.000 itens, iguais a cada repetição.

```bash
curl -X POST http://localhost:8081/admin/massa/limpezas -H "content-type: application/json" -d "{\"alvo\":\"lote\",\"id\":\"lote-0\",\"confirmar\":true}"
```
Esperado: o lote some, e `GET /admin/massa/historico` mostra a exclusão.
