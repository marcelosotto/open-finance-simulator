# language: pt
# Casos de uso: UC04, UC21, UC28 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Simular o comportamento real das instituições
  Como QA
  Quero provocar lentidão, erro e timeout
  Para provar como o sistema reage

  # RN11
  Cenário: Banco estável fica dentro do SLA
    Dado 10.000 chamadas ao endpoint de saldos do "Banco Estável"
    Então o p95 deve ficar abaixo de 1.500 ms

  # RN11
  Cenário: Banco lento fica fora do SLA
    Dado 10.000 chamadas ao endpoint de saldos do "Banco Lento"
    Então o p95 deve ficar acima de 1.500 ms
    E todas as chamadas devem terminar

  # RN12
  Esquema do Cenário: Banco timeout termina do jeito configurado
    Dado que o "Banco Timeout" está configurado para terminar em "<fim>"
    Quando uma chamada é afetada pelo timeout
    Então a resposta só deve terminar depois de 15 segundos
    E o resultado deve ser "<resultado>"

    Exemplos:
      | fim               | resultado         |
      | status 504        | status 504        |
      | conexão cortada   | conexão cortada   |

  # RN14, RN54
  Cenário: Falha repetível para um cliente
    Dado uma regra "Mariana sempre recebe 500 em saldos"
    Quando Mariana é consultada duas vezes
    Então as duas respostas devem ter status 500

  # RN15
  Cenário: A regra mais específica vence
    Dado uma regra "Banco Estável responde 503"
    E uma regra "Mariana responde 200"
    Quando Mariana é consultada
    Então a resposta deve ter status 200
    E o registro de chamada deve mostrar a regra de cliente

  # RN16
  Cenário: Janela de indisponibilidade
    Dado uma janela do "Banco Estável" das 10h às 10h30
    Quando uma chamada chega às 10h10
    Então a resposta deve ter status 503
    Quando uma chamada chega às 10h31
    Então a resposta deve ser normal

  # RN17
  Esquema do Cenário: Falhas de transporte
    Dado uma regra de transporte "<falha>" para o endpoint de contas
    Quando Ana consulta contas
    Então a receptora deve observar "<falha>"

    Exemplos:
      | falha           |
      | conexão cortada |
      | corpo truncado  |
      | JSON inválido   |

  # RN18
  Cenário: A resposta não revela que é simulada
    Dado uma regra que força erro 500
    Quando Ana consulta contas
    Então o corpo da resposta deve seguir a spec
    E a regra aplicada deve aparecer só no registro de chamada

  # RN73
  Cenário: Relógio fixo em uma instituição
    Dado que o relógio do "Banco Azul" está fixado em "2026-12-31T23:59:50Z"
    Quando Ana consulta o saldo
    Então a resposta deve usar a data simulada
    E o cabeçalho "x-sim-clock" deve mostrar a data simulada

  # RN10
  Cenário: Latência por endpoint
    Dado que o endpoint de extrato do "Banco Azul" tem tempo típico de 3 segundos
    Quando Ana consulta o extrato
    Então a resposta deve levar cerca de 3 segundos
    E o endpoint de saldos não deve mudar

  # RN74, RN112
  Cenário: Registro de chamada
    Dado um defeito aplicado em uma chamada
    Quando Bruno busca pelo x-fapi-interaction-id
    Então deve ver instituição, endpoint, status, tempo e defeito
    E os dados pessoais devem estar mascarados
