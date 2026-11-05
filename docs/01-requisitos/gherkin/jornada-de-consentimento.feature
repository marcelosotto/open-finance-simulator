# language: pt
# Casos de uso: UC10, UC11 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Jornada de consentimento
  Como cliente sintético
  Quero autorizar o compartilhamento no meu banco
  Para testar o fluxo igual ao real

  Contexto:
    Dado que Mariana tem duas contas e um cartão no "Banco Estável"
    E que Ana criou um consentimento de contas e cartões para Mariana

  # RN90, RN94, RN95
  Cenário: Jornada completa
    Quando Mariana faz login no banco
    E confirma o compartilhamento das duas contas e do cartão
    Então o consentimento deve estar em "AUTHORISED"
    E Mariana deve voltar para a receptora
    E os recursos escolhidos devem aparecer em Recursos

  # RN93
  Cenário: Titular diferente
    Quando Rafael faz login no banco no lugar de Mariana
    Então a jornada deve ser interrompida com uma mensagem clara
    E o resumo do consentimento não deve aparecer

  # RN94
  Cenário: Prazo indeterminado na confirmação
    Dado um consentimento sem data final
    Quando Mariana chega à confirmação
    Então o prazo deve aparecer como "Indeterminado"

  # RN95
  Cenário: Pelo menos uma origem
    Quando Mariana desmarca todas as contas
    Então a confirmação deve ser bloqueada

  # RN95
  Cenário: Crédito não tem seleção de origem
    Dado um consentimento com operações de crédito
    Quando Mariana chega à confirmação
    Então não deve haver seleção de contratos

  # RN96
  Cenário: Cancelar antes de confirmar
    Quando Mariana cancela a confirmação
    Então o consentimento deve estar em "REJECTED"
    E Mariana deve voltar para a receptora

  # RN81
  Cenário: Retomar depois de uma falha
    Dado que o banco caiu durante o login
    Quando Mariana tenta de novo em 30 minutos
    Então a mesma intenção deve seguir válida

  # RN81
  Cenário: Desistir e voltar tarde demais
    Dado que Mariana abandonou a jornada há 61 minutos
    Quando ela tenta continuar
    Então a jornada deve informar que o pedido expirou

  # RN91
  Cenário: Jornada pela receptora de demonstração
    Quando Carla inicia uma jornada na receptora de demonstração
    Então ela deve conseguir escolher banco, dados e prazo
    E ver o resultado no final

  # RN99
  Cenário: Falha programada no redirecionamento
    Dado uma regra de timeout no redirecionamento do "Banco Estável"
    Quando Mariana confirma
    Então a receptora não deve receber o retorno no prazo

  # RN98
  Cenário: Duas aprovadoras
    Dado um consentimento da Indústria Aurora
    Quando Helena confirma
    Então o consentimento deve estar em "AUTHORISED"
    E os recursos devem estar em "PENDING_AUTHORISATION"
    Quando Paula aprova
    Então os recursos devem estar em "AVAILABLE"

  # RN98
  Cenário: Aprovadora recusa
    Dado um consentimento da Indústria Aurora confirmado por Helena
    Quando Paula recusa
    Então os recursos devem estar em "UNAVAILABLE"

  # RN98
  Cenário: Sócio único não tem etapa extra
    Dado um consentimento do Comércio Sol
    Quando o sócio confirma
    Então os recursos devem estar em "AVAILABLE"

  # RN92
  Cenário: Sem etapas extras no login
    Quando Mariana faz login no banco
    Então a próxima tela deve ser a confirmação

  # RN97
  Cenário: Redirecionamento automático
    Quando Mariana confirma
    Então deve ver uma tela informativa
    E deve voltar à receptora sem clicar em nada
