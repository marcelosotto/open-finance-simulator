# language: pt
# Casos de uso: UC12, UC13 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: API de Consentimentos
  Como receptora
  Quero consultar, revogar e renovar consentimentos
  Para seguir o ciclo oficial

  # RN82
  Cenário: REJECTED é final
    Dado um consentimento em "REJECTED"
    Quando Ana tenta renovar o consentimento
    Então a resposta deve ter status 422 com "ESTADO_CONSENTIMENTO_INVALIDO"

  # RN86, RN88
  Cenário: Revogar pela API
    Dado um consentimento autorizado
    Quando Ana chama DELETE no consentimento
    Então a resposta deve ter status 204
    E o consentimento deve estar em "REJECTED"
    E as APIs de dados devem responder 401

  # RN82
  Cenário: Revogar no painel
    Dado um consentimento autorizado de Mariana
    Quando Bruno revoga no painel
    Então a rejeição deve registrar quem revogou

  # RN49
  Cenário: Revogar um não afeta o outro
    Dado que Mariana tem dois consentimentos no "Banco Estável"
    Quando Ana revoga um deles
    Então o outro deve continuar válido

  # RN88
  Esquema do Cenário: Consentimento sem validade responde 401
    Dado um consentimento "<estado>"
    Quando Ana consulta "<api>"
    Então a resposta deve ter status 401

    Exemplos:
      | estado         | api      |
      | inexistente    | recursos |
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
