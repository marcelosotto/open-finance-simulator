# Data Model: Back-office

Nada novo é persistido. As entidades abaixo são as do `core` do front, montadas a partir das respostas da administração (DTOs validados com zod na `infrastructure`).

## Comum
| Tipo | Campos |
|---|---|
| `Either<L, R>` | `Left` ou `Right`, com `value` |
| `Falha` | `tipo`: `rede`, `recusa`, `inexistente`, `conflito`, `validacao`; `status?`; `erros: string[]` |
| `Ambiente` | `marca` (texto livre, vazio = sem marca); `cor` derivada: `dev`, `hml`, `stage` ou `neutra` |
| `Autor` | nome livre; vazio = não envia `x-sim-autor` |

## Instituições
| Tipo | Campos e regras do `core` |
|---|---|
| `ResumoDeInstituicao` | `endereco`, `nome`, `perfil`, `situacao` (`ativa`/`inativa`), `anomaliasLigadas[]`; `temAnomalia` = lista não vazia (RN114) |
| `Instituicao` | resumo + `organisationId`, `marca`, `publico` (`PF`, `PJ`, `ambos`), `produtos[]`, `pronta`, `comportamento`, `limites`, `relogio?`, `sla[]`, `avisos[]` |
| `SlaDaClasse` | `classe`, `slaMs`, `p95Ms`, `dentroDoSla` (UI04) |
| `AvisoDeLimite` | `campo`, `oficial`, `configurado` (UI05) |
| `NovaInstituicao` | `nome` (obrigatório, aparado), `perfil` (um dos perfis da API) |
| `Relogio` | `baseSimulada`, `baseReal`, `fator`; `fixado` = fator 0 |
| `EntradaDeHistorico` | `data`, `autor`, `acao`, `mudancas[]` (`campo`, `antes`, `depois`) |
| `EstadoDaAnomalia` | `nome`, `ligada`, `descricao` (do catálogo) |

## Consentimento
| Tipo | Campos e regras do `core` |
|---|---|
| `OpcoesDoMenu` | `cliente`, `tipo` (`PF`/`PJ`), `banco`, `semProduto`, `grupos[]` (`nome`, `permissoes[]`), `contas[]`, `cartoes[]` (`resourceId`, `marcada`), `agrupados[]`, `receptoras[]` |
| `RascunhoDeConsentimento` | `cliente`, `banco`, `receptora`, `permissoes` (conjunto), `contas`, `cartoes`, `prazoMeses?` (1 a 12), `aprovacoesPendentes?` (1 a 10, só PJ). `pedido()` junta as permissões e acrescenta `RESOURCES_READ` (RN96). Seleção só vai no pedido quando a seção existe |
| Regra de tela (UI13) | `origemFaltando(grupo)`: com opção e permissão do grupo pedida, nenhuma marcada → bloqueia confirmar |
| `VisaoDoConsentimento` | `consentId`, `cliente`, `banco`, `receptora`, `estado`, `permissoes`, `criadoEm`, `expiraEm?`, `rejeitadoPor?`, `razao?`, `alcada` (`situacao`, `aprovacoesPendentes`, `prazo?`), `decisoes[]`, `recursos[]` (`resourceId`, `tipo`, `estado`); `podeDecidir` = `situacao` `pendente` e pendências > 0 e estado ≠ `REJECTED` (UI14) |
| `Receptora` | `organisationId`, `nome`, `criadaEm?`; ao criar, `usuario`, `senha`, `credencialDeTeste` (mostrados uma vez) |

Passos do menu (UI10): `cliente` → `banco-e-receptora` → `permissoes` (com origem, UI13) → `prazo` → `confirmacao`.

## Massa
| Tipo | Campos e regras do `core` |
|---|---|
| `PerfilDeDados` | `nome`, faixas de transação e valor, `saldoInicial`, `tiposDeLancamento[]`, `produtos[]`; `pronto` = um dos perfis da semeadura (o servidor recusa mudança) |
| `Distribuicao` | `bancos` e `perfis` (nome → %), `produtos` (nome → %), `pjPct`. `somaDeBancos`, `somaDePerfis`; `podeSalvar` = as duas somas 100 e cada % entre 0 e 100 (UI07, RN56) |
| `Lote` | `id`, `quantidade`, `situacao`, `processados`, `consentimentos`, `recursos`, `data`, `motivo?`; `emAndamento` = `situacao` `em_andamento` |
| `ClienteDaMassa` | `documento`, `tipo`, `nome`, `loteId?`; detalhe com `recursos[]` e `consentimentos[]` |
| `PreviaDeLimpeza` | `alvo`, `id?`, `removido`, contagens |

## Cenários
| Tipo | Campos e regras do `core` |
|---|---|
| `RegraDeCenario` | como no [modelo da 007](../007-comportamento-e-cenarios/data-model.md) e efeito `defeito` da 009; `alcance` = `recurso`, `cliente`, `endpoint` ou `instituicao` (o filtro mais específico informado), só para agrupar a lista |
| `PacoteDeCenario` | `nome`, `ligado`, `criadoEm` |
| `Vencedoras` | `instante`, `comportamento?`, `estado?`, `defeito?` ([contrato](contracts/admin-back-office.md)) |
