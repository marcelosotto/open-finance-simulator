# language: pt
# Casos de uso: UC05, UC06, UC07, UC08, UC27 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Gerar massa de dados
  Como operador
  Quero massa persistida e virtual
  Para testar de um cliente a milhões

  # RN54
  Cenário: Mesma semente, mesmos dados
    Dado um lote com semente 42
    Quando o mesmo lote é gerado de novo em outro ambiente limpo
    Então os dados devem ser iguais, salvo a marca do ambiente

  # RN51
  Cenário: Transações não são gravadas
    Quando Diego gera um lote de 100 clientes
    Então clientes, consentimentos e recursos devem estar gravados
    E nenhuma transação deve estar gravada

  # RN51, RN53
  Cenário: Ajuste vence o cálculo
    Dado uma transação calculada de R$ 100,00
    Quando Bruno ajusta o valor para R$ 90,00
    Então a API deve devolver R$ 90,00
    E o saldo deve refletir o ajuste

  # RN52
  Cenário: Mundo virtual não grava nada
    Quando Bruno pede 1.000 itens do mundo virtual
    Então os IDs devem funcionar nas APIs
    E nada deve ser gravado no banco

  # RN53
  Cenário: Revogar um consentimento virtual
    Dado um consentimento virtual
    Quando Bruno o revoga
    Então as chamadas com ele devem responder 401

  # RN56
  Cenário: Distribuição precisa somar 100%
    Quando Diego define 60% e 30% entre os bancos
    Então a distribuição deve ser recusada

  # RN57
  Cenário: Documento válido
    Quando Ana cria um cliente sintético
    Então o CPF deve ter dígitos válidos
    E o CPF deve começar com o dígito fixo do simulador

  # RN58
  Cenário: Apagar um lote
    Dado um lote com 100 clientes
    Quando Diego apaga o lote
    Então os clientes, consentimentos, recursos e ajustes dele devem sumir
    E o histórico deve mostrar a exclusão

  # RN50
  Cenário: Natureza do dado
    Dado um cliente criado por lote e um item do mundo virtual
    Então o primeiro deve ser "persistido"
    E o segundo deve ser "virtual"
    E os dois devem ser sintéticos

  # RN55
  Esquema do Cenário: Perfis de dados prontos
    Dado uma conta com o perfil "<perfil>"
    Quando Ana consulta um dia de transações
    Então a quantidade deve ficar em "<volume>"

    Exemplos:
      | perfil        | volume       |
      | cliente-comum | 0 a 4        |
      | sem-movimento | 0            |
      | alto-volume   | 2000 a 3000  |

  # RN59
  Cenário: Histórico de 12 meses
    Quando Diego gera um cliente com histórico padrão
    Então devem existir transações dos últimos 12 meses
