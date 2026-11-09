# language: pt
# Casos de uso: UC05, UC06, UC07, UC08, UC27 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
# Spec 003: os cenários ficam ignorados até a spec ser entregue (ADR-009).
# Na spec 003, os ajustes e o estado calculado são conferidos pela administração.
# Os passos que chamam as APIs oficiais ficam pendentes até as specs 004 e 005.

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
  Cenário: Mundo virtual não grava os itens
    Quando Bruno pede 1.000 itens do mundo virtual
    Então os IDs devem funcionar nas APIs
    E só o total e a semente devem estar gravados

  # RN52
  Cenário: Um pedido se divide entre os bancos
    Dado uma distribuição de 50% no "HML-Itaú" e 50% no "HML-Nubank"
    Quando Bruno pede 1.000 itens do mundo virtual
    Então cerca de metade dos itens deve ser de cada banco
    E cada chamada deve seguir as regras do banco do item

  # RN52
  Cenário: Pedir itens de um banco só
    Quando Bruno pede 1.000 itens do "HML-Itaú"
    Então todos os itens devem ser desse banco

  # RN53
  Cenário: Revogar um consentimento virtual
    Dado um consentimento virtual
    Quando Bruno o revoga
    Então as chamadas com ele devem responder 401

  # RN53
  Cenário: Trocar a semente apaga os ajustes
    Dado um mundo virtual com um consentimento revogado
    Quando Bruno troca a semente e confirma
    Então os ajustes daquele mundo devem sumir

  # RN53, RN58
  Cenário: Limpar os ajustes do mundo virtual
    Dado um mundo virtual com 50 consentimentos revogados
    Quando Diego limpa os ajustes
    Então os consentimentos devem voltar ao estado calculado

  # RN49
  Cenário: Mesmo cliente em vários bancos
    Dado que Mariana tem conta no "Banco Estável" e no "Banco Lento"
    Quando ela consente nos dois bancos
    Então cada consentimento deve mostrar só os dados do seu banco

  # RN49
  Cenário: Dois consentimentos no mesmo banco
    Dado que Mariana tem um consentimento autorizado no "Banco Estável"
    Quando ela autoriza outro consentimento no mesmo banco
    Então os dois devem funcionar

  # RN54
  Cenário: Dois lotes com a mesma semente
    Dado um lote com semente 42
    Quando Diego gera outro lote com semente 42
    Então os documentos e IDs devem ser diferentes
    E o restante dos dados deve ser igual

  # RN56
  Cenário: Bancos precisam somar 100%
    Quando Diego define 60% e 30% entre os bancos
    Então a distribuição deve ser recusada

  # RN56
  Cenário: Produtos não precisam somar 100%
    Quando Diego define 90% com conta, 50% com cartão e 20% com empréstimo
    Então a distribuição deve ser aceita

  # RN57
  Cenário: Documento válido
    Quando Ana cria um cliente simulado
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
    E os dois devem ser simulados

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
