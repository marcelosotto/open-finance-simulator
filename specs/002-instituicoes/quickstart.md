# Quickstart: Instituições simuladas

Pré-requisitos: Docker e SDK do .NET 10.

## Testes
```bash
dotnet run --project tools/Simulador.Ferramentas -- specs baixar
dotnet test Simulador.slnx
```
Esperado: os cenários de `instituicoes.feature` passam; o de RN08 aparece ignorado (`@spec-004`).

## Ambiente
```bash
docker compose up --build
curl http://localhost:8081/admin/instituicoes
curl -X POST http://localhost:8081/admin/instituicoes -H "content-type: application/json" -d "{\"nome\":\"Banco Azul\"}"
curl -i http://localhost:8080/banco-azul/open-banking/accounts/v2/accounts
```
Esperado: 16 bancos na lista, 201 na criação e 501 na chamada, porque a operação chega na spec 005.

```bash
curl -X DELETE http://localhost:8081/admin/instituicoes/banco-estavel
curl -i http://localhost:8080/banco-estavel/open-banking/accounts/v2/accounts
```
Esperado: `inativada` e 503.
