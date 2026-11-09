# Research: Instituições simuladas

## R01 — Módulos sem referência entre si
**Decisão:** Instituições declara `IAmbienteDasInstituicoes` (marca, novo `organisationId`, status declarados). O Host a implementa com `IMarcador` e `ICatalogo`, e implementa `IResolvedorDeInstituicao` com a consulta do cadastro.
**Por quê:** `FronteirasDosModulosTests` e ADR-007.
**Alternativa:** referenciar `Catalogo.Contratos` direto; viola o teste de arquitetura.

## R02 — Resolução sem cache
**Decisão:** cada chamada lê a instituição no MongoDB pela chave primária.
**Por quê:** RN05 pede efeito na próxima chamada, inclusive com várias instâncias (RN30). A leitura por `_id` cabe nos 10 ms (RNF03).
**Alternativa:** cache com invalidação; complexidade sem necessidade demonstrada.

## R03 — Endereço como chave primária
**Decisão:** `_id` da instituição é o endereço. A unicidade do `_id` garante, de forma atômica, a recusa de nome repetido (RN07) e a semeadura idempotente entre instâncias.

## R04 — Edição por JSON Merge Patch
**Decisão:** um `PATCH /admin/instituicoes/{endereco}` sobre `nome`, `marca`, `publico`, `produtos`, `comportamento` e `limites`. `null` remove chave de dicionário, como uma sobrescrita de QCA.
**Por quê:** UC02 e UC03 mudam poucos campos por vez, e uma validação serve para todos.
**Alternativa:** um `PUT` por bloco; mais rotas e o cliente reenviaria tudo.

## R05 — Autor no histórico
**Decisão:** cabeçalho `x-sim-autor` só na administração, com padrão `operador` (RN05).

## R06 — Normalização do endereço
**Decisão:** tabela explícita de letras acentuadas do português.
**Por quê:** o projeto usa `InvariantGlobalization`, e não dependemos do comportamento de `Normalize` nesse modo.

## R07 — Persistência em BSON via JSON
**Decisão:** os registros viram JSON com `System.Text.Json` e depois `BsonDocument`.
**Por quê:** dicionários com chave inteira (pesos por status) não têm serialização direta no BSON, e o JSON é o mesmo da API.

## R08 — Testes
**Decisão:** a aceitação e os testes do repositório usam MongoDB em contêiner (ADR-009). Os demais testes de host usam `RepositorioEmMemoria`, que já contém `banco-teste`.
**Por quê:** os testes da spec 001 chamam `/banco-teste/…` e não dependem de banco de dados.

## R09 — Retry-After
**Decisão:** ligado, com 60 segundos no padrão (RN23), porque o TPM zera na virada do minuto (RN22).

## Dependências novas
| Pacote | Versão | Motivo |
|---|---|---|
| `MongoDB.Driver` | 3.13.0 | ADR-003 |
| `Testcontainers.MongoDb` | 4.16.0 | ADR-009, só nos testes |
