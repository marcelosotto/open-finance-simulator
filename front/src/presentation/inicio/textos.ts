// Primeiros passos (UI18), em português simples.
export const textosDoInicio = {
  titulo: 'Primeiros passos',
  feito: 'Feito',
  aFazer: 'A fazer',
  opcional: 'Quando quiser',
  abrir: 'Abrir a tela',
  passos: {
    banco: {
      titulo: 'Criar um banco',
      texto:
        'Os bancos são as instituições de onde o seu app lê os dados. Já vêm {{n}} bancos prontos; crie outro se quiser um comportamento próprio.',
    },
    clientes: {
      titulo: 'Gerar clientes (massa)',
      texto:
        'Clientes de mentira, com contas, cartões e transações. Gere um lote em Massa → Lotes. Agora há {{n}} clientes.',
    },
    consentimento: {
      titulo: 'Criar um consentimento',
      texto:
        'O cliente autoriza o seu app a ler os dados dele. Escolha banco e cliente na lista. Agora há {{n}} consentimentos.',
    },
    acompanhar: {
      titulo: 'Ver, aprovar ou revogar',
      texto: 'Abra o consentimento para ver o estado, aprovar ou recusar as alçadas de empresa e revogar.',
    },
    api: {
      titulo: 'Testar o app chamando a API',
      texto:
        'O app pede um token no banco com o usuário e a senha da receptora e chama as APIs Open Finance na porta 8080.',
      receptora: 'Receptora {{nome}}: usuário {{usuario}}, senha {{senha}}.',
      semReceptora: 'Ainda não há receptora. Crie uma em Receptoras.',
      exemplo: 'Exemplo de pedido de token',
    },
  },
  telas: {
    titulo: 'Tela → para que serve',
    tela: 'Tela',
    serve: 'Para que serve',
  },
} as const;
