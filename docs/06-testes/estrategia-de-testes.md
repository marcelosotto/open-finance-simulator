# Estratégia de Testes

- **Meta:** 100% no domínio e 80% no total ([RNF08](../01-requisitos/requisitos.md#rnf08)). O CI quebra abaixo disso.
- **Testes primeiro:** o teste nasce falhando antes do código.

| Camada | Ferramenta | O que prova |
|---|---|---|
| Domínio | xUnit | Máquinas de estados, permissões, limites, ordem das etapas |
| Gerador | xUnit com relógio fixo | Determinismo, saldo coerente, janelas |
| Integração | Testcontainers (MongoDB e Redis) | Contadores compartilhados, histórico, expirações |
| Contrato | Validação contra as specs fixadas | Toda operação com `estavel` valida no schema |
| Aceitação | Reqnroll com os `.feature` ([ADR-009](../05-adr/ADR-009-gherkin-reqnroll.md)) | Regras em linguagem de negócio |
| Tabela oficial | Teste que compara classe, TPM, cota e SLA de cada endpoint com a [tabela](../01-requisitos/tabela-de-endpoints.md) | Evita classe ou limite diferente do oficial |
| Jornada | Playwright | As seis etapas no navegador |
| Carga | k6 | Capacidade própria do simulador ([RNF02](../01-requisitos/requisitos.md#rnf02)) e mundo virtual |

## Regras específicas
- **Tempo:** relógio injetado; nenhum teste espera 60 minutos de verdade.
- **Sorteio:** semente fixa em teste.
- **Conformidade:** com perfil `estavel` e sem defeitos, 100% das respostas validam contra a spec. Esse é o teste que protege [PR01](../00-visao-geral/premissas.md#pr01).
- **Marcação:** um teste percorre as APIs e falha se algum ID ou resposta vier sem marca.
