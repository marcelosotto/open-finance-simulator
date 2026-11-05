# language: pt
# Casos de uso: UC10, UC11 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Criar consentimento no painel
  Como usuário do sistema
  Quero criar um consentimento por um menu
  Para ter dados prontos para os testes

  # RN80, RN95
  Cenário: Criar consentimento
    Quando Bruno escolhe Mariana, o "Banco Estável" e a receptora "Coletor"
    E escolhe contas e cartões, com prazo de 12 meses
    E confirma
    Então o consentimento deve nascer em "AUTHORISED"
    E os recursos selecionados devem estar disponíveis

  # RN85
  Cenário: Prazo indeterminado
    Quando Bruno cria um consentimento sem validade
    Então o consentimento não deve ter data final

  # RN95
  Cenário: Pelo menos uma origem
    Dado que Bruno desmarcou todas as contas
    Quando tenta confirmar
    Então o consentimento não deve ser criado

  # RN95
  Cenário: Crédito não tem seleção de origem
    Quando Bruno escolhe operações de crédito
    Então todas as operações do cliente devem ser compartilhadas

  # RN83
  Cenário: Grupo de permissões incompleto
    Quando Bruno escolhe um grupo incompleto de contas
    Então o consentimento não deve ser criado

  # RN84
  Cenário: Permissões de pessoa física e jurídica
    Dado o cliente PF Mariana
    Quando Bruno abre as permissões
    Então só devem aparecer permissões PF

  # RN08
  Cenário: Produto não oferecido não aparece
    Dado um banco que não oferece contas
    Quando Bruno abre as permissões nesse banco
    Então contas não devem aparecer

  # RN98
  Cenário: Duas aprovações pendentes
    Dado um consentimento da Indústria Aurora com duas aprovações pendentes
    Quando Bruno aprova duas vezes no painel
    Então os recursos devem ir para "AVAILABLE"

  # RN98
  Cenário: Recusa na alçada
    Dado um consentimento da Indústria Aurora com aprovação pendente
    Quando Bruno recusa no painel
    Então os recursos devem ir para "UNAVAILABLE"

  # RN98
  Cenário: Fim do prazo da alçada
    Dado um consentimento com aprovação pendente há 16 dias
    Então os recursos devem estar em "UNAVAILABLE"

  # RN98
  Cenário: Empresa sem múltiplas alçadas
    Quando Bruno cria um consentimento do Comércio Sol sem alçadas
    Então os recursos devem nascer em "AVAILABLE"
