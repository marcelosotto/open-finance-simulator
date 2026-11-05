# Quickstart: validar a Fundação e catálogo

Roteiro para provar, de ponta a ponta, que a spec 001 está entregue. Formatos e respostas estão
em [contracts/](contracts/); as entidades, em [data-model.md](data-model.md).

## Pré-requisitos

- SDK do .NET 10 e Docker com Compose.
- Token do GitHub com leitura em `marcelosotto/open-finance-specs` (repositório privado), na
  variável `SPECS_TOKEN`.

## 1. Baixar e conferir as specs (FR-001, FR-002)

```bash
dotnet run --project tools/Simulador.Ferramentas -- specs baixar
dotnet run --project tools/Simulador.Ferramentas -- specs verificar
```

**Esperado:** os 15 contratos de `specs.lock.json` em `.specs/7bdb9c1…/` e saída `0`.

**Prova da falha:** troque um caractere de um `sha256` no `specs.lock.json` e rode
`specs verificar` de novo. **Esperado:** saída `1`, com o nome do contrato divergente.
Desfaça a troca.

## 2. Rodar os testes e os portões (FR-014, FR-015, FR-022, FR-027, FR-028)

```bash
dotnet format --verify-no-changes
dotnet build -warnaserror
dotnet test --collect:"XPlat Code Coverage"
dotnet run --project tools/Simulador.Ferramentas -- cobertura conferir --relatorios "tests/**/coverage.cobertura.xml"
```

**Esperado:** tudo verde, incluindo:
- o teste da tabela oficial (arquivo × `docs/01-requisitos/tabela-de-endpoints.md`);
- o teste de catálogo × tabela (nenhuma operação sem linha, nenhuma linha sem operação);
- a varredura de marcação;
- os cenários de `marcacao.feature`, exceto `@tela`;
- 100% de cobertura em `*.Dominio` e pelo menos 80% no total.

**Prova da falha:** mude a frequência de um endpoint em `config/tabela-de-endpoints.json`.
**Esperado:** o teste da tabela falha nomeando seção, endpoint e campo. Desfaça a troca.

## 3. Subir o ambiente (FR-003, FR-026)

```bash
docker compose up --build
```

Com a marca `hml` (padrão do Compose):

```bash
curl -i http://localhost:8081/saude/pronto
curl -s http://localhost:8081/admin/catalogo | head -c 600
curl -i http://localhost:8080/banco-teste/open-banking/accounts/v2/accounts
curl -i http://localhost:8080/banco-teste/open-banking/nao-existe/v1/xpto
```

**Esperado:**
- `/saude/pronto` → 200.
- `/admin/catalogo` → as operações dos 15 contratos, com a classe de cada uma.
- `accounts` → **501** com `NAO_IMPLEMENTADO` e `x-sim-environment: hml`, sem
  `x-sim-data-nature` ([respostas-padrao.md](contracts/respostas-padrao.md)).
- caminho inexistente → **404** com `NAO_ENCONTRADO` e `x-sim-environment: hml`.

**Sem rede:** desligue a rede do contêiner e reinicie. **Esperado:** sobe igual, porque as
specs estão na imagem.

## 4. Marca vazia (produção)

```bash
Simulador__MarcaDoAmbiente= docker compose up --build
curl -i http://localhost:8080/banco-teste/open-banking/accounts/v2/accounts
```

**Esperado:** 501 **sem** `x-sim-environment`. O comportamento de IDs e texto com marca vazia é
provado pelos cenários "Marca vazia em produção" e "Consentimento com a marca vazia".

## 5. Troca de versão das specs (UC24, FR-009)

Num PR que muda o `commit` e os contratos do `specs.lock.json`:

```bash
dotnet run --project tools/Simulador.Ferramentas -- specs diferencas --base <lock da main> --novo specs.lock.json
```

**Esperado:** lista de operações novas, alteradas e removidas. No CI, a mesma lista aparece no
resumo do job ([ferramenta-specs.md](contracts/ferramenta-specs.md)).
