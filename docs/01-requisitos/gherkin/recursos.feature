# language: pt
# Casos de uso: UC14, UC15 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
# Spec 004: os cenários ficam ignorados até a spec ser entregue (ADR-009).
@spec-004
Funcionalidade: API de Recursos
  Como receptora
  Quero saber o que posso consumir
  Para chamar só o que está disponível

  Contexto:
    Dado que Rafael tem um consentimento autorizado com duas contas

  # RN102
  Cenário: Todos os recursos aparecem em qualquer estado
    Dado que uma conta está em "TEMPORARILY_UNAVAILABLE"
    Quando Ana consulta recursos
    Então as duas contas devem aparecer

  # RN103
  Cenário: Instituição preparando a lista
    Dado que o banco leva 2 minutos para preparar os recursos
    Quando Ana consulta recursos depois de 1 minuto
    Então a resposta deve ter status 202 sem corpo

  # RN103
  Cenário: Preparação passa de 5 minutos
    Dado que o banco ficou preparando por mais de 5 minutos
    Quando Ana consulta recursos
    Então a resposta deve ter status 504

  # RN104
  @spec-005
  Esquema do Cenário: Comportamento por estado
    Dado que a conta está em "<estado>"
    Quando Ana lista contas
    Então a conta "<lista>"
    Quando Ana consulta a conta
    Então a resposta deve ter status <status>

    Exemplos:
      | estado                  | lista       | status |
      | AVAILABLE               | aparece     | 200    |
      | TEMPORARILY_UNAVAILABLE | não aparece | 403    |
      | UNAVAILABLE             | não aparece | 403    |
      | PENDING_AUTHORISATION   | não aparece | 403    |

  # RN105
  @spec-005
  Cenário: Conta de outro cliente
    Quando Ana consulta uma conta que não é de Rafael
    Então a resposta deve ter status 403 com "RESOURCE_FORBIDDEN"

  # RN105
  @spec-005
  Cenário: Conta fora do consentimento
    Dado que Mariana tem a conta A em um consentimento e a conta B em outro
    Quando Ana consulta a conta B com o primeiro consentimento
    Então a resposta deve ter status 403 com "RESOURCE_FORBIDDEN"

  # RN101
  Esquema do Cenário: Transições de estado
    Dado um recurso em "<de>"
    Quando Diego muda o estado para "<para>"
    Então a mudança deve ser "<resultado>"

    Exemplos:
      | de                      | para                  | resultado |
      | PENDING_AUTHORISATION   | AVAILABLE             | aceita    |
      | AVAILABLE               | PENDING_AUTHORISATION | recusada  |
      | UNAVAILABLE             | AVAILABLE             | recusada  |
      | AVAILABLE               | UNAVAILABLE           | aceita    |

  # RN101
  Cenário: Fundos podem voltar de UNAVAILABLE
    Dado um fundo em "UNAVAILABLE"
    Quando Diego muda o estado para "AVAILABLE"
    Então a mudança deve ser aceita

  # RN106
  Cenário: Produto novo em seleção agrupada
    Dado um consentimento com operações de crédito
    Quando Diego adiciona um empréstimo ao cliente
    Então o empréstimo deve aparecer em Recursos

  # RN100, RN107
  Cenário: Estados independentes
    Dado um consentimento com uma conta "AVAILABLE" e outra "PENDING_AUTHORISATION"
    Quando Ana consulta recursos
    Então cada conta deve aparecer com o próprio estado

  # RN108
  @spec-007
  Cenário: Bloqueio programado
    Dado uma regra que bloqueia a conta às 10h
    Quando Ana consulta a conta às 10h01
    Então a resposta deve ter status 403
