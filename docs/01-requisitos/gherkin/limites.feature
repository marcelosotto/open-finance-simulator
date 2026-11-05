# language: pt
# Casos de uso: UC18, UC19, UC20 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Aplicar os limites oficiais
  Como receptora
  Quero que os limites funcionem como no Open Finance
  Para ajustar meu consumo antes de produção

  # RN20, RN21
  Esquema do Cenário: TPM da alta frequência pelos consentimentos ativos
    Dado que a receptora tem <qca> consentimentos ativos no "Banco Estável"
    Então o TPM de saldos deve ser <tpm>

    Exemplos:
      | qca       | tpm    |
      | 1000000   | 2500   |
      | 2000000   | 5000   |
      | 3000000   | 8000   |
      | 6000000   | 10000  |
      | 8000000   | 12000  |

  # RN20
  Cenário: O TPM é por endpoint
    Dado que o endpoint "/accounts" chegou ao limite no minuto
    Quando Ana chama "/accounts/{accountId}" no mesmo minuto
    Então a chamada não deve receber 429

  # RN22
  Cenário: O contador zera na virada do minuto
    Dado que o limite estourou às 10h25m59s
    Quando Ana chama de novo às 10h26m00s
    Então a chamada deve ser atendida

  # RN23
  Cenário: Passar do TPM responde 429
    Dado que o endpoint tem TPM 1000
    Quando chegam 1001 chamadas no mesmo minuto
    Então a última deve receber 429

  # RN24
  Cenário: Consentimentos e Recursos não têm limite por origem
    Quando chegam 5000 chamadas a "/resources" no mesmo minuto
    Então nenhuma deve receber 429

  # RN26, RN28
  Esquema do Cenário: Limite operacional por classe
    Dado que o limite operacional está ligado
    Quando chega a chamada <n> do mês ao endpoint "<endpoint>" do mesmo recurso e cliente
    Então a resposta deve ter status <status>

    Exemplos:
      | endpoint                       | n   | status |
      | /accounts/{accountId}          | 8   | 200    |
      | /accounts/{accountId}          | 9   | 423    |
      | /accounts/{accountId}/balances | 420 | 200    |
      | /accounts/{accountId}/balances | 421 | 423    |

  # RN27
  Cenário: Erro não consome a cota
    Dado que uma chamada respondeu 500
    Então a cota do mês não deve mudar

  # RN27
  Cenário: 429 não consome a cota
    Dado que uma chamada respondeu 429
    Então a cota do mês não deve mudar

  # RN27
  Cenário: Páginas seguintes não consomem a cota
    Dado uma consulta com 3 páginas
    Quando Ana lê as 3 páginas
    Então a cota deve subir 1

  # RN25
  Cenário: A cota é separada por recurso
    Dado que a conta "A" esgotou a cota de saldos
    Quando Ana consulta o saldo da conta "B"
    Então a resposta deve ter status 200

  # RN29
  Cenário: Passar da capacidade responde 529
    Dado que o "Banco Azul" tem capacidade de 300 TPS
    Quando chegam 301 chamadas no mesmo segundo
    Então a última deve receber 529

  # RN30
  Cenário: Duas instâncias usam o mesmo contador
    Dado duas instâncias do simulador
    Quando 1001 chamadas se dividem entre elas no mesmo minuto
    Então o limite deve ser aplicado sobre o total

  # RN39
  Cenário: Segurança vem antes dos limites
    Dado que o endpoint já passou do TPM
    Quando Ana chama com consentimento revogado
    Então a resposta deve ter status 401
