# Contrato: respostas padrão das APIs Open Finance

Origem: FR-006 e FR-008 da [spec](../spec.md). Decisões em [research.md R06](../research.md#r06--resposta-das-rotas-sem-comportamento-e-caminho-desconhecido).

Ambas usam o envelope de erro oficial (`ResponseError`) das specs de dados, com
`Content-Type: application/json; charset=utf-8` e os cabeçalhos de
[marcação](cabecalhos-e-identificadores.md) (sem `x-sim-data-nature`, porque não há dado).

## 501 — operação do catálogo ainda sem comportamento

Vale para `/{banco}/open-banking/{api}/{versão}/…` quando a operação existe na spec travada e
nenhuma spec do simulador entregou o manipulador dela.

```json
{
  "errors": [
    {
      "code": "NAO_IMPLEMENTADO",
      "title": "Operação não implementada",
      "detail": "A operação existe na especificação oficial, mas ainda não está disponível neste simulador."
    }
  ],
  "meta": { "requestDateTime": "2026-11-05T12:00:00Z" }
}
```

## 404 — caminho fora do catálogo

Vale para qualquer caminho que não corresponda a uma operação travada, incluindo método errado
em caminho existente.

```json
{
  "errors": [
    {
      "code": "NAO_ENCONTRADO",
      "title": "Recurso não encontrado",
      "detail": "O caminho solicitado não existe nesta instituição."
    }
  ],
  "meta": { "requestDateTime": "2026-11-05T12:00:00Z" }
}
```

`requestDateTime` vem do `TimeProvider` injetado.
