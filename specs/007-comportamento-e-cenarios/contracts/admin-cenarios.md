# Contrato: administração de cenários e relógio

Porta de administração, sem autenticação (RN71). Autor em `x-sim-autor`. Erros: `{ "erros": [...] }`; 422 inválido, 404 inexistente, 409 conflito.

## Regras
| Método e rota | Resposta |
|---|---|
| `POST /admin/cenarios/regras` | 201 com a regra |
| `GET /admin/cenarios/regras` | 200 lista |
| `GET /admin/cenarios/regras/{id}` | 200 |
| `PUT /admin/cenarios/regras/{id}` | 200 |
| `DELETE /admin/cenarios/regras/{id}` | 204 |
| `POST /admin/cenarios/regras/{id}/ligar` e `/desligar` | 200 |

Corpo: campos de [RegraDeCenario](../data-model.md), exceto os gerados.

## Pacotes
| Método e rota | Resposta |
|---|---|
| `PUT /admin/cenarios/pacotes/{nome}` | 200 (nasce desligado) |
| `GET /admin/cenarios/pacotes` | 200 lista |
| `POST /admin/cenarios/pacotes/{nome}/ligar` e `/desligar` | 200 |
| `DELETE /admin/cenarios/pacotes/{nome}` | 204; 409 se tiver regras |

## Relógio
| Método e rota | Corpo | Resposta |
|---|---|---|
| `PUT /admin/instituicoes/{endereco}/relogio` | `{ "fixadoEm": "..." }` ou `{ "fator": 60 }` | 200 com a instituição |
| `DELETE /admin/instituicoes/{endereco}/relogio` | — | 200 com a instituição |

## Respostas oficiais
Com relógio alterado: `x-sim-clock: 2026-12-31T23:59:50Z`. Erros simulados no envelope oficial, com os códigos de RN13.
