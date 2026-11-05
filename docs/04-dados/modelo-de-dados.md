# Modelo de dados

Relacionados: [modelo de domínio](../03-design/classes/modelo-de-dominio.md) · [ADR-003](../05-adr/ADR-003-mongodb-e-redis.md) · [ADR-004](../05-adr/ADR-004-semente-e-ajustes.md)

MongoDB e Redis foram decididos na revisão de 05/11/2026 ([PA01](../01-requisitos/regras-de-negocio.md#pa01)). As coleções e chaves abaixo ainda são proposta.

## MongoDB
| Coleção | Conteúdo | Observação |
|---|---|---|
| `instituicoes` | Nome, `organisationId`, público, produtos, comportamento e limites | Nasce com os bancos de perfil e os de nome real |
| `perfis_comportamento` | Modelos de comportamento | Prontos não são apagados |
| `perfis_dados` | Volume, valores, tipos e saldo inicial | Em uso não é apagado |
| `distribuicoes` | Divisão da massa entre bancos, produtos e perfis | Somas em 100% |
| `receptoras` | `organisationId` e QCA por instituição | [PA10](../01-requisitos/regras-de-negocio.md#pa10) |
| `clientes` | Documento, nome, tipo, credenciais de teste, marca e natureza | Índice por instituição e documento |
| `consentimentos` | Estado, permissões, validade, rejeição, extensões, aprovações | Mudança de estado atômica; índice para expirar |
| `recursos` | Tipo, estado, perfil, saldo inicial, data de criação e marca | Índices por consentimento e cliente |
| `ajustes` | Mudanças sobre transação ou dado virtual | Chave: alvo; vence o cálculo |
| `lotes` | Parâmetros, semente, progresso e situação | Apagar leva tudo junto |
| `cenarios` | Regras, defeitos e anomalias | Índice por alcance |
| `historico` | Quem mudou o quê, com antes e depois | Toda escrita de administração |
| `registros_chamada` | Chamada, status, tempo, regra aplicada | Expira por TTL ([PA17](../01-requisitos/regras-de-negocio.md#pa17)) |
| `sessoes_jornada` | Etapa, cliente e consentimento | Expira em 60 minutos |

## Redis
| Chave | Conteúdo | Expira |
|---|---|---|
| `tpm:{banco}:{receptora}:{endpoint}:{minuto}` | Chamadas no minuto | 2 minutos |
| `tps:{banco}:{segundo}` | Chamadas no segundo | 5 segundos |
| `cota:{banco}:{mes}:{endpoint}:{objeto}:{cliente}:{receptora}` | Respostas 2XX no mês | 40 dias |

O documento do cliente entra na chave como hash.

## O que não é gravado
- Transações: saem de `recurso + dia + semente`.
- Mundo virtual: sai do índice. Só ajustes são gravados.
- Chave de paginação: é assinada e carrega o que precisa.
- ClickHouse: sai do simulador. Quem precisar analisar chamadas usa os registros ou OpenTelemetry.
