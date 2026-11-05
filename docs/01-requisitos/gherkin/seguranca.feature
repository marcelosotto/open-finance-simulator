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

  # RN122
  Cenário: Token sem a permissão
    Dado um consentimento só com contas
    Quando Ana consulta cartões
    Então a resposta deve ter status 403

  # RN120
  Cenário: Token só para consentimento autorizado
    Dado um consentimento revogado
    Quando a receptora pede um token para ele
    Então o pedido deve ser recusado

  # RN120
  Cenário: Credencial inválida
    Quando a receptora pede um token com senha errada
    Então o pedido deve ser recusado

  # RN71
  Cenário: Administração separada
    Quando Ana chama uma API de administração sem usuário
    Então a resposta deve ter status 401

  # RN123
  Cenário: Credencial do simulador não vale fora dele
    Dado uma credencial emitida pelo simulador
    Então ela deve estar identificada como credencial de teste
