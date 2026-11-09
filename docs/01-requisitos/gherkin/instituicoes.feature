# language: pt
# Casos de uso: UC01, UC02, UC03 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Cadastrar instituições simuladas
  Como operador
  Quero criar bancos com comportamentos diferentes
  Para que os times testem cada situação

  # RN02
  Cenário: O ambiente nasce com os bancos prontos
    Dado um ambiente recém-criado
    Então devem existir os bancos "Banco Estável", "Banco Lento", "Banco Instável", "Banco Timeout" e "Banco Restritivo"
    E devem existir os bancos de nome real do ambiente

  # RN09
  Esquema do Cenário: Nome real com prefixo do ambiente
    Dado um ambiente "<ambiente>"
    Quando Diego cria um banco inspirado no "Itaú"
    Então o nome do banco deve ser "<nome>"

    Exemplos:
      | ambiente | nome     |
      | hml      | HML-Itaú |
      | dev      | DEV-Itaú |
      |          | Itaú     |

  # RN03
  Cenário: Criar um banco só com o nome
    Quando Diego cria o banco "Banco Azul"
    Então o banco deve existir com o perfil "estavel"
    E deve ter um organisationId e um endereço próprios

  # RN04
  Cenário: Ajustar um banco não muda o perfil
    Dado que o "Banco Azul" usa o perfil "lento"
    Quando Diego muda a latência do "Banco Azul"
    Então o perfil "lento" deve continuar igual
    E os outros bancos com esse perfil não devem mudar

  # RN05
  Cenário: A mudança vale sem reiniciar e fica no histórico
    Quando Diego muda a taxa de erro do "Banco Azul" para 5%
    Então a próxima chamada deve usar 5%
    E o histórico deve mostrar Diego, o valor antigo e o novo

  # RN06
  Cenário: Banco com clientes só é inativado
    Dado que o "Banco Azul" tem clientes
    Quando Diego tenta apagar o "Banco Azul"
    Então o banco deve ser inativado
    E as chamadas a ele devem responder como fora do ar

  # RN07
  Cenário: A receptora escolhe o banco pelo endereço
    Dado que Ana chama o endereço do "Banco Lento"
    Então a resposta deve vir com o comportamento do "Banco Lento"
    E nenhum cabeçalho próprio do simulador deve ser exigido

  # RN13
  Cenário: Status que a spec não declara é recusado
    Quando Diego configura o status 418 para o endpoint de saldos
    Então a configuração deve ser recusada

  # RN01
  Cenário: Dados de uma instituição
    Quando Diego abre o "Banco Azul"
    Então deve ver público, produtos, marca e organisationId

  # RN08
  Cenário: Instituição sem conta de depósito
    Dado que o "Banco Azul" não oferece contas
    Quando Ana cria um consentimento de contas e cartões nesse banco
    Então as permissões de contas devem sair do consentimento
