# Contrato: administração (porta 8081)

Origem: história 1, cenário 3 da [spec](../spec.md), [RN71](../../../docs/01-requisitos/regras-de-negocio.md#rn71) e [RNF09](../../../docs/01-requisitos/requisitos.md#rnf09). Fica em porta separada das APIs Open Finance (porta 8080).

Nesta spec só há leitura. A administração não tem login nem autenticação ([RN71](../../../docs/01-requisitos/regras-de-negocio.md#rn71)).

## GET /admin/catalogo

Lista as operações do catálogo carregado.

**200**

```json
{
  "commit": "7bdb9c1a6c4c265d7aabcdbc662f94c61718135e",
  "operacoes": [
    {
      "contrato": "accounts",
      "api": "accounts",
      "versao": "v2",
      "metodo": "GET",
      "caminho": "/accounts/{accountId}/balances",
      "caminhoPublicado": "/{banco}/open-banking/accounts/v2/accounts/{accountId}/balances",
      "operationId": "accountsGetAccountsAccountIdBalances",
      "classe": {
        "frequencia": "alta",
        "slaMs": 1500,
        "tpm": "faixa-qca",
        "limiteMensal": 420
      },
      "implementada": false
    }
  ]
}
```

Os valores do exemplo são ilustrativos; o `operationId` real vem da spec travada.

O schema completo de resposta de cada operação não vem nessa lista. Ele é exposto no módulo
por `ICatalogo`, para uso interno das outras specs e da validação de contrato.

## GET /saude/vivo

**200** quando o processo responde.

## GET /saude/pronto

**200** quando as specs foram carregadas e conferidas e a tabela foi lida. **503** caso contrário.
