// Ajuda no topo de cada tela (UI17): para que a tela serve e o que precisa existir antes, em português simples.
export const textosDaAjuda = {
  primeirosPassos: 'Primeiros passos',
  precisa: 'Precisa existir antes: {{coisas}}',
  nadaAntes: 'Não precisa de nada antes.',
  telas: {
    primeirosPassos: {
      para: 'O caminho para usar o simulador pela primeira vez, na ordem. Cada passo leva à tela certa e fica marcado quando já foi feito.',
      precisa: '',
    },
    bancos: {
      para: 'Os bancos simulados. É deles que o seu app lê os dados, como leria de um banco de verdade. O aviso amarelo mostra quando um banco tem um desvio de protocolo ligado de propósito.',
      precisa: '',
    },
    novoBanco: {
      para: 'Cria um banco simulado só com nome e perfil de comportamento. O resto vem preenchido e pode ser ajustado em seguida.',
      precisa: '',
    },
    banco: {
      para: 'Ajusta um banco: dados gerais, rapidez e erros, limites, desvios de protocolo, relógio e o histórico de mudanças.',
      precisa: 'o banco',
    },
    novoConsentimento: {
      para: 'Simula um cliente autorizando outra instituição a ler os dados dele. O consentimento já nasce autorizado.',
      precisa: 'um banco e clientes nesse banco (gere em Massa → Lotes)',
    },
    consentimento: {
      para: 'Mostra um consentimento: estado, permissões, recursos e as aprovações que faltam. Aqui também se aprova, recusa ou revoga.',
      precisa: 'um consentimento criado',
    },
    consentimentos: {
      para: 'Abre um consentimento pelo identificador (consentId). Os de um cliente aparecem na tela do cliente, em Massa → Clientes.',
      precisa: 'um consentimento criado',
    },
    receptoras: {
      para: 'As instituições que leem os dados (o seu app). A receptora padrão já vem cadastrada; usuário e senha servem para pedir o token na API.',
      precisa: '',
    },
    massa: {
      para: 'Os clientes de mentira do simulador: perfis de dados, distribuição, lotes, clientes avulsos e limpeza.',
      precisa: 'um banco',
    },
    perfis: {
      para: 'Diz quantas transações, valores e produtos cada tipo de cliente tem. Os perfis prontos não podem ser mudados.',
      precisa: '',
    },
    distribuicoes: {
      para: 'Diz como a massa se divide entre bancos e perfis. Bancos e perfis precisam somar 100%.',
      precisa: 'bancos e perfis de dados',
    },
    lotes: {
      para: 'Gera muitos clientes de uma vez, com produtos e consentimentos. É o jeito mais rápido de ter massa para testar.',
      precisa: 'um banco (e, se quiser, uma distribuição)',
    },
    lote: {
      para: 'Acompanha um lote: andamento, o que foi criado e os clientes gerados.',
      precisa: 'um lote gerado',
    },
    clientes: {
      para: 'Abre um cliente da massa escolhendo o banco e o cliente na lista, ou cria um cliente avulso.',
      precisa: 'um banco',
    },
    cliente: {
      para: 'Mostra um cliente: os produtos dele em cada banco e os consentimentos.',
      precisa: 'o cliente',
    },
    limpeza: {
      para: 'Apaga massa: um lote, um banco ou tudo. Primeiro mostra o que será apagado; só então confirma.',
      precisa: '',
    },
    cenarios: {
      para: 'Regras que mudam o comportamento de propósito (lentidão, erro, queda, dado ruim), para testar como o seu app reage.',
      precisa: 'um banco',
    },
    regra: {
      para: 'Cria ou ajusta uma regra: onde ela vale (banco, endpoint, cliente, recurso), o efeito e com que frequência.',
      precisa: 'um banco',
    },
    pacotes: {
      para: 'Agrupa regras para ligar e desligar juntas, como um roteiro de teste.',
      precisa: '',
    },
  },
} as const;

export type TelaComAjuda = keyof typeof textosDaAjuda.telas;
