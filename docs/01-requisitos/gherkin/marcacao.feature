# language: pt
# Caso de uso: UC23 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Marcar os dados com ambiente e natureza
  Como equipe
  Quero reconhecer um dado simulado em qualquer lugar
  Para nunca confundi-lo com dado real

  # RN61
  Esquema do Cenário: Cabeçalhos da resposta
    Dado a marca de ambiente "<marca>"
    E um recurso "<natureza>"
    Quando Ana consulta o recurso
    Então o cabeçalho "x-sim-environment" deve ser "<marca>"
    E o cabeçalho "x-sim-data-nature" deve ser "<natureza>"

    Exemplos:
      | marca | natureza   |
      | dev   | persistido |
      | hml   | virtual    |
      | stage | virtual    |

  # RN62
  Esquema do Cenário: Prefixo dos identificadores
    Dado a marca de ambiente "hml"
    Quando é criado um recurso "<natureza>"
    Então o ID deve começar com "<prefixo>"
    E o ID deve seguir o padrão oficial

    Exemplos:
      | natureza   | prefixo |
      | persistido | hml-p-  |
      | virtual    | hml-v-  |

  # RN62
  Esquema do Cenário: Prefixo dos consentimentos
    Dado a marca de ambiente "hml"
    Quando é criado um consentimento "<natureza>"
    Então o consentId deve começar com "<prefixo>"
    E o consentId deve seguir o padrão oficial

    Exemplos:
      | natureza   | prefixo        |
      | persistido | urn:hml:p- |
      | virtual    | urn:hml:v- |

  # RN63
  Cenário: Marca nos campos livres
    Dado a marca de ambiente "hml"
    Quando Ana consulta uma transação
    Então transactionName deve começar com "[HML]"
    E o campo deve respeitar o tamanho oficial

  # RN61
  Cenário: A marca não muda o contrato
    Quando Ana valida a resposta contra a spec oficial
    Então a resposta deve ser válida

  # RN64
  Cenário: A marca nunca muda
    Dado um cliente criado com a marca "dev"
    Quando a massa é copiada para um ambiente com a marca "hml"
    Então o cliente deve continuar marcado como "dev"

  # RN65
  Cenário: Lista com dados misturados
    Dado uma lista com dado persistido e dado virtual
    Quando Ana consulta a lista
    Então o cabeçalho "x-sim-data-nature" deve ser "misto"

  # RN66
  Cenário: Nenhum perfil desliga a marca
    Quando Diego tenta desligar a marcação de um banco
    Então a opção não deve existir

  # RN60
  Cenário: A marca aparece nos três lugares
    Dado a marca de ambiente "hml"
    Quando Ana consulta uma transação
    Então a marca deve aparecer no cabeçalho, no identificador e no texto

  # RN60, RN62, RN63, RN70
  Cenário: Marca vazia em produção
    Dado a marca de ambiente vazia
    Quando Ana consulta uma transação persistida
    Então o cabeçalho "x-sim-environment" não deve ser enviado
    E o cabeçalho "x-sim-data-nature" deve ser "persistido"
    E o ID deve começar com "p-"
    E transactionName não deve ter marca de ambiente

  # RN62, RN70
  Cenário: Consentimento com a marca vazia
    Dado a marca de ambiente vazia
    Quando é criado um consentimento "virtual"
    Então o consentId deve começar com "urn:v:"
    E o consentId deve seguir o padrão oficial

  @tela
  # RN72
  Cenário: Ambiente visível nas telas
    Dado a marca de ambiente "hml"
    Quando Diego abre o back-office
    Então a marca "hml" deve aparecer em destaque
