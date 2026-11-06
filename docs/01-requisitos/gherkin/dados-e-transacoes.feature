# language: pt
# Caso de uso: UC16 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
# Spec 005: os cenários ficam ignorados até a spec ser entregue (ADR-009).
@spec-005
Funcionalidade: Consultar dados cadastrais e transacionais
  Como receptora
  Quero respostas iguais às das instituições
  Para desenvolver sobre dados realistas

  Contexto:
    Dado que Mariana tem um consentimento autorizado no "Banco Estável"

  # RN38
  Cenário: Sem identificador de interação
    Quando Ana consulta contas sem x-fapi-interaction-id
    Então a resposta deve ter status 400
    E a resposta deve trazer um x-fapi-interaction-id gerado

  # RN38
  Cenário: Identificador espelhado
    Quando Ana consulta contas com x-fapi-interaction-id válido
    Então a resposta deve trazer o mesmo valor

  # RN40
  Cenário: Resposta segue a versão oficial
    Quando Ana consulta o saldo
    Então a resposta deve validar contra a spec da versão fixada

  # RN41
  Esquema do Cenário: Janela das transações
    Quando Ana consulta "<endpoint>" de <de> até <ate>
    Então a resposta deve ter status <status>

    Exemplos:
      | endpoint              | de    | ate  | status |
      | transactions-current  | D-6   | hoje | 200    |
      | transactions-current  | D-7   | hoje | 422    |
      | transactions          | D-365 | hoje | 200    |
      | transactions          | D-400 | hoje | 422    |

  # RN42
  Cenário: Data inicial sem data final
    Quando Ana consulta transações só com a data inicial
    Então a resposta deve ter status 422

  # RN42
  Cenário: Sem datas vale o dia atual
    Quando Ana consulta transações sem datas
    Então só devem vir transações de hoje

  # RN43
  Cenário: Ordem das transações
    Quando Ana consulta transações
    Então devem vir da mais recente para a mais antiga

  # RN44
  Cenário: Identificador provisório não convive com o definitivo
    Dado uma transação em processamento
    Quando ela é efetivada
    Então só o identificador definitivo deve aparecer

  # RN45
  Cenário: Contraparte obrigatória no PIX
    Quando Ana consulta uma transação PIX
    Então os dados da contraparte devem vir preenchidos

  # RN46
  Cenário: Saldo coerente com o extrato
    Quando Ana soma as transações efetivadas ao saldo inicial
    Então o resultado deve ser igual ao saldo informado

  # RN47
  Cenário: Dados cadastrais não aparecem em Recursos
    Dado um consentimento só com permissões cadastrais
    Quando Ana consulta recursos
    Então a resposta deve ter status 200 com lista vazia
