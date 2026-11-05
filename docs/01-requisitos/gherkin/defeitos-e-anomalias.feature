# language: pt
# Caso de uso: UC22 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Injetar defeitos e anomalias
  Como QA
  Quero respostas fora do padrão
  Para ver como a receptora reage

  # RN111
  Cenário: Sem configuração não há defeito
    Quando Ana faz 1.000 consultas no "Banco Estável"
    Então todas devem validar contra a spec

  # RN110
  Esquema do Cenário: Catálogo de defeitos
    Dado o defeito "<defeito>" com taxa de 100%
    Quando Ana consulta transações
    Então a resposta deve trazer "<efeito>"
    E o registro de chamada deve mostrar "<defeito>"

    Exemplos:
      | defeito                   | efeito                         |
      | campo-obrigatorio-ausente | sem transactionAmount          |
      | formato-invalido          | data fora do formato           |
      | enum-invalido             | type fora da lista             |
      | transaction-id-repetido   | transactionId igual entre itens|
      | total-records-incoerente  | totalRecords diferente da soma |
      | data-fora-da-janela       | transação fora do período      |

  # RN111
  Cenário: Defeito com taxa
    Dado um defeito com taxa de 10% e semente fixa
    Quando Bruno roda 1.000 consultas duas vezes
    Então as duas rodadas devem afetar as mesmas chamadas

  # RN113
  Esquema do Cenário: Anomalias de protocolo
    Dado a anomalia "<anomalia>" ligada no "Banco Azul"
    Quando acontece "<situacao>"
    Então a resposta deve ser "<resultado>"

    Exemplos:
      | anomalia               | situacao                 | resultado         |
      | revogado-com-403       | consentimento revogado   | status 403        |
      | chave-recusada-com-422 | chave expirada           | status 422        |
      | interacao-diferente    | chamada válida           | outro id na volta |

  # RN114
  Cenário: Anomalias ligadas aparecem na tela
    Dado o "Banco Azul" com a anomalia "revogado-com-403" ligada
    Quando Diego abre a lista de bancos
    Então o banco deve mostrar um aviso de anomalias ligadas

  # RN115
  Cenário: Dado muda durante a paginação
    Dado uma regra que adiciona registros entre páginas
    Quando Ana lê todas as páginas
    Então totalRecords deve mudar entre as páginas
