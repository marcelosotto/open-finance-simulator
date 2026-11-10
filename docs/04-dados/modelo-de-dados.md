# Modelo de dados

Relacionados: [modelo de domínio](../03-design/classes/modelo-de-dominio.md) · [ADR-003](../05-adr/ADR-003-mongodb-e-redis.md) · [ADR-004](../05-adr/ADR-004-semente-e-ajustes.md)

MongoDB e Redis foram decididos na revisão de 05/11/2026 ([PA01](../01-requisitos/regras-de-negocio.md#pa01)). As coleções e chaves abaixo ainda são proposta.

## MongoDB
| Coleção | Conteúdo | Observação |
|---|---|---|
| `instituicoes` | Nome, endereço, `organisationId`, público, produtos, comportamento, limites, relógio e QCA sobrescrito por receptora | Nasce com os 5 bancos de perfil e os 11 de nome real |
| `perfis_comportamento` | Modelos de comportamento | Prontos não são apagados |
| `perfis_dados` | Volume, valores, tipos e saldo inicial | Em uso não é apagado |
| `distribuicoes` | Divisão da massa entre bancos, perfis e produtos, e porcentagem de PJ | Bancos e perfis somam 100%; produtos têm porcentagem própria |
| `receptoras` | `organisationId` e credenciais de teste | O QCA sobrescrito fica em `instituicoes`, por `organisationId` da receptora ([PA10](../01-requisitos/regras-de-negocio.md#pa10)) |
| `clientes` | Documento, nome, tipo, marca e natureza | Índice por documento; o cliente não pertence a um banco |
| `consentimentos` | Estado, permissões, validade, rejeição, extensões, aprovações | Mudança de estado atômica; índice para expirar |
| `recursos` | Tipo, estado, perfil, saldo inicial, data de criação e marca | Índices por consentimento e por cliente e banco |
| `mundo_virtual` | Total, semente e distribuição | Um por ambiente; reproduz o mesmo mundo |
| `ajustes` | Mudanças sobre transação ou dado virtual | Chave: alvo; vence o cálculo |
| `lotes` | Parâmetros, semente, progresso e situação | Apagar leva tudo junto |
| `cenarios` | Regras e pacotes de cenário, inclusive as regras de defeito ([RN111](../01-requisitos/regras-de-negocio.md#rn111)), e as anomalias ligadas, um documento por banco e anomalia (`anomalia:{banco}:{nome}`) ([RN113](../01-requisitos/regras-de-negocio.md#rn113)) | Lidas por cache de até 5 s ([RNF12](../01-requisitos/requisitos.md#rnf12)) |
| `historico` | Quem mudou o quê, com antes e depois | Toda escrita de administração |

## Redis
| Chave | Conteúdo | Expira |
|---|---|---|
| `tpm:{banco}:{receptora}:{endpoint}:{minuto}` | Chamadas no minuto | 2 minutos |
| `tps:{banco}:{segundo}` | Chamadas no segundo | 5 segundos |
| `cota:{banco}:{mes}:{endpoint}:{objeto}:{cliente}:{receptora}` | Respostas 2XX no mês | 40 dias |
| `seq:{regra}` | Posição da sequência de uma regra com semente | Sem expiração |

O documento do cliente entra na chave como hash.

## O que não é gravado
- Transações: saem de `recurso + dia + semente`.
- Itens do mundo virtual: saem do índice. Só total, semente, distribuição e ajustes são gravados.
- Chave de paginação: é assinada e carrega o que precisa.
- Chamadas recebidas: o simulador só responde. Quem precisar analisar chamadas usa os logs da aplicação que consome.
