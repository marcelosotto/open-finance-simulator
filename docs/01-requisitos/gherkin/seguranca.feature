# language: pt
# Caso de uso: UC25 — ../casos-de-uso/README.md
# Regras (RNxx): ../regras-de-negocio.md
Funcionalidade: Autenticar a receptora
  Como receptora
  Quero me autenticar como em produção
  Para testar o caminho real

  # RN120
  Cenário: Chamada sem token
    Quando Ana consulta contas sem token
    Então a resposta deve ter status 401

  # RN121
  @fapi
  Cenário: PAR é obrigatório
    Dado o modo de segurança "FAPI"
    Quando Ana pede autorização sem PAR
    Então o pedido deve ser recusado

  # RN121
  @fapi
  Cenário: request_uri de uso único
    Dado um request_uri já usado
    Quando Ana usa de novo
    Então o pedido deve ser recusado

  # RN121
  @fapi
  Cenário: Validade do token
    Quando Ana obtém um access token
    Então a validade deve ficar entre 300 e 900 segundos

  # RN122
  Cenário: Token sem a permissão
    Dado um consentimento só com contas
    Quando Ana consulta cartões
    Então a resposta deve ter status 403

  # RN71
  Cenário: Administração separada
    Quando Ana chama uma API de administração sem usuário
    Então a resposta deve ter status 401

  # RN123
  Cenário: Credencial do simulador não vale fora dele
    Dado uma credencial emitida pelo simulador
    Então ela deve estar identificada como credencial de teste
