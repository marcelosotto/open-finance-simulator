# language: pt
# Casos de uso: UC09, UC12, UC13 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: API de Consentimentos
  Como receptora
  Quero criar, consultar, revogar e renovar consentimentos
  Para seguir o ciclo oficial

  # RN80
  Cenário: Criar consentimento
    Quando Ana cria um consentimento com permissões válidas de contas
    Então a resposta deve ter status 201
    E o consentimento deve estar em "AWAITING_AUTHORISATION"
    E não deve ter recursos

  # RN81
  Cenário: Intenção expira em 60 minutos
    Dado um consentimento em "AWAITING_AUTHORISATION" há 61 minutos
    Quando Ana consulta o consentimento
    Então ele deve estar em "REJECTED"

  # RN82
  Cenário: REJECTED é final
    Dado um consentimento em "REJECTED"
    Quando Ana tenta renovar o consentimento
    Então a resposta deve ter status 422 com "ESTADO_CONSENTIMENTO_INVALIDO"

  # RN83
  Esquema do Cenário: Regras de permissões
    Dado um banco que "<oferta>"
    Quando Ana cria um consentimento com "<pedido>"
    Então o resultado deve ser "<resultado>"

    Exemplos:
      | oferta                      | pedido                        | resultado                                |
      | oferece contas              | grupo incompleto de contas    | 422 COMBINACAO_PERMISSOES_INCORRETA      |
      | não oferece contas          | só contas                     | 422 SEM_PERMISSOES_FUNCIONAIS_RESTANTES  |
      | não oferece contas          | contas e cartões              | 201 sem as permissões de contas          |
      | não oferece crédito         | operações de crédito          | 201 com as permissões de crédito         |

  # RN84
  Esquema do Cenário: Regras de pessoa física e jurídica
    Quando Ana cria um consentimento "<pedido>"
    Então a resposta deve ter status 422 com "<codigo>"

    Exemplos:
      | pedido                           | codigo                        |
      | com permissões PJ sem dados PJ   | INFORMACOES_PJ_NAO_INFORMADAS |
      | com dados PJ e permissões PF     | PERMISSOES_PJ_INCORRETAS      |
      | com permissões PF e PJ           | PERMISSAO_PF_PJ_EM_CONJUNTO   |

  # RN85
  Cenário: Prazo indeterminado
    Quando Ana cria um consentimento sem expirationDateTime
    Então o consentimento não deve ter data final

  # RN86, RN88
  Cenário: Revogar pela API
    Dado um consentimento autorizado
    Quando Ana chama DELETE no consentimento
    Então a resposta deve ter status 204
    E o consentimento deve estar em "REJECTED"
    E as APIs de dados devem responder 401

  # RN82
  Cenário: Revogar no banco
    Dado um consentimento autorizado de Mariana
    Quando Mariana revoga na tela do banco
    Então a rejeição deve registrar quem revogou

  # RN88
  Esquema do Cenário: Consentimento sem validade responde 401
    Dado um consentimento "<estado>"
    Quando Ana consulta "<api>"
    Então a resposta deve ter status 401

    Exemplos:
      | estado         | api      |
      | não autorizado | recursos |
      | revogado       | contas   |
      | expirado       | saldos   |

  # RN87
  Cenário: Renovar
    Dado um consentimento autorizado que vence em 30 dias
    Quando Ana renova por mais 12 meses
    Então a validade deve mudar
    E a extensão deve aparecer em "/extensions"

  # RN87
  Cenário: Renovar para data anterior
    Dado um consentimento autorizado
    Quando Ana renova para uma data menor que a validade atual
    Então a resposta deve ter status 422 com "DATA_EXPIRACAO_INVALIDA"

  # RN87
  Cenário: Renovação com múltiplas alçadas
    Dado um consentimento da Indústria Aurora com duas aprovadoras
    Quando Ana tenta renovar
    Então a resposta deve ter status 422 com "DEPENDE_MULTIPLA_ALCADA"
