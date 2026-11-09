# Modelo de dados: Consentimentos e recursos

## Receptora (coleção `receptoras`)
| Campo | Regra |
|---|---|
| `organisationId` | Chave; gerado se omitido; único |
| `nome` | Obrigatório |
| `usuario`, `senha` | Gerados, com prefixo `teste-` (RN123); `usuario` é único |

## Token (não gravado)
Tipo (`receptora` ou `consentimento`), `organisationId`, banco, `consentId` opcional, expiração (criação + 900 s).

## Consentimento (coleção `consentimentos`, spec 003, ganha campos)
| Campo novo | Regra |
|---|---|
| `razao` | Código oficial de `rejection.reason` |
| `atualizadoEm` | `statusUpdateDateTime`; se ausente, `criadoEm` |
| `usuarioLogado` | CPF do criador, em consentimento por API |
| `aprovacoesPendentes` | Padrão 0; nasce na spec 008 |
| `extensoes` | Lista de extensões |

`receptora` já existe: `organisationId` da criadora, ou nulo em lote e mundo virtual.

**Estado**: `AUTHORISED` → `REJECTED`, final (RN82).

## Extensão
`novaValidade?`, `validadeAnterior?`, documento e `rel` do usuário, `solicitadaEm`, IP e user-agent.

## Recurso (spec 003)
Sem campos novos. Transições (RN101): `PENDING_AUTHORISATION` só como inicial; `AVAILABLE` ⇄ `TEMPORARILY_UNAVAILABLE`; qualquer um desses → `UNAVAILABLE`; `UNAVAILABLE` → `AVAILABLE` só em `FUND` e `VARIABLE_INCOME`.

## Ajuste (spec 003, ganha campos)
Novos: `razao`, `expiraEm`, `atualizadoEm`, `extensoes`. Em recurso virtual: alvo `recurso:{id}`, tipo `recurso`, `estado`.
