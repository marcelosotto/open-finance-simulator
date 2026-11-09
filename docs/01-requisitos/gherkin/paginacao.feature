# language: pt
# Caso de uso: UC17 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Paginar como o Open Finance
  Como receptora
  Quero a paginação oficial
  Para ler listas grandes sem gastar a cota

  # RN31
  Cenário: Valores padrão
    Quando Ana pede transações sem page e page-size
    Então a resposta deve ter até 25 registros

  # RN31
  Cenário: Mínimo de 25 registros nas famílias que o declaram
    Dado uma lista de fundos com 47 registros
    Quando Ana pede page-size 5
    Então a primeira página deve ter 25 registros
    E a segunda deve ter 22 registros

  # RN31
  Cenário: Tamanho pedido nas famílias sem mínimo de 25
    Dado uma lista de contas com 47 registros
    Quando Ana pede page-size 5
    Então a primeira página deve ter 5 registros
    E o link self deve mostrar page-size 5

  # RN33
  Cenário: Página além do total
    Dado uma lista com 3 páginas
    Quando Ana pede a página 9
    Então a resposta deve ter status 422 com "PAGE_NOT_FOUND"

  # RN31, PA21
  Cenário: Pedido acima do máximo
    Quando Ana pede page-size 5000
    Então a resposta deve ter até 1000 registros
    E o link self deve mostrar page-size 1000

  # RN32
  Cenário: Instituição com máximo menor
    Dado que o "Banco Restritivo" tem máximo de 800 registros
    Quando Ana pede a página 2 com page-size 1000
    Então a resposta deve trazer os itens 801 a 1600

  # RN33
  Cenário: Lista vazia com totais
    Dado que a única conta do consentimento está indisponível
    Quando Ana lista contas
    Então a resposta deve ter status 200
    E meta.totalRecords deve ser 0
    E meta.totalPages deve ser 0

  # RN33
  Cenário: Transações vazias não trazem totais
    Dado que a conta não tem transações no período
    Quando Ana consulta transações
    Então a resposta deve ter status 200
    E meta não deve ter totais

  # RN34
  Cenário: Links da primeira página
    Dado uma lista com 3 páginas
    Quando Ana pede a primeira página
    Então links deve ter self, next e last
    E links não deve ter first nem prev

  # RN34
  Cenário: Links da última página
    Dado uma lista com 3 páginas
    Quando Ana pede a última página
    Então links deve ter self, first e prev
    E links não deve ter next nem last

  # RN35
  @spec-006
  Cenário: Chave dentro da validade
    Dado uma chave emitida há 59 minutos
    Quando Ana pede a página seguinte
    Então a cota não deve mudar

  # RN36
  Esquema do Cenário: Chave recusada gera nova chave com sucesso
    Dado uma chave "<estado>"
    Quando Ana pede a página seguinte
    Então a resposta deve ter status 200
    E os links devem trazer uma nova chave

    Exemplos:
      | estado   |
      | expirada |
      | inválida |

  # RN36
  @spec-006
  Cenário: Chave recusada conta na cota
    Dado uma chave "expirada"
    Quando Ana pede a página seguinte
    Então a cota deve subir 1

  # RN113
  @spec-009
  Cenário: Anomalia de chave com 422
    Dado que o "Banco Restritivo" tem a anomalia "chave-recusada-com-422"
    E uma chave expirada
    Quando Ana pede a página seguinte
    Então a resposta deve ter status 422

  # RN37
  Cenário: Lista estável durante a paginação
    Dado uma consulta com 3 páginas
    Quando Ana lê as 3 páginas com a mesma chave
    Então nenhum registro deve se repetir ou faltar
