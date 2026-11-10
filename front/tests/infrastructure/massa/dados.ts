// Respostas da administração da massa no formato do C# (Contratos/Modelos.cs); os perfis são os capturados do simulador.
export const perfisProntos = [
  {
    nome: 'cliente-comum',
    transacoesMin: 0,
    transacoesMax: 4,
    valorMin: 10,
    valorMax: 2000,
    saldoInicial: 5000,
    tiposDeLancamento: ['PIX', 'TED', 'BOLETO', 'CARTAO'],
    produtos: ['contas', 'cartoes', 'emprestimos'],
  },
  {
    nome: 'sem-movimento',
    transacoesMin: 0,
    transacoesMax: 0,
    valorMin: 0,
    valorMax: 0,
    saldoInicial: 1000,
    tiposDeLancamento: ['PIX', 'TED', 'BOLETO', 'CARTAO'],
    produtos: ['contas'],
  },
  {
    nome: 'alto-volume',
    transacoesMin: 2000,
    transacoesMax: 3000,
    valorMin: 1,
    valorMax: 50000,
    saldoInicial: 1000000,
    tiposDeLancamento: ['PIX', 'TED', 'BOLETO', 'CARTAO'],
    produtos: ['contas', 'cartoes'],
  },
];

export const perfilCriado = {
  nome: 'meu-perfil',
  transacoesMin: 1,
  transacoesMax: 2,
  valorMin: 5,
  valorMax: 50,
  saldoInicial: 100,
  tiposDeLancamento: ['PIX'],
  produtos: ['contas'],
};

export const distribuicaoSalva = {
  nome: 'meia-meia',
  distribuicao: {
    bancos: { 'banco-estavel': 60, 'banco-lento': 40 },
    perfis: { 'cliente-comum': 100 },
    produtos: { contas: 90 },
    pjPct: 10,
  },
};

export const lote = (situacao = 'em_andamento', processados = 3, motivo: string | null = null) => ({
  id: 'lote-0',
  quantidade: 10,
  distribuicao: { bancos: { 'banco-estavel': 100 }, perfis: { 'cliente-comum': 100 }, produtos: null, pjPct: 0 },
  consentimentosPorCliente: 1,
  permissoes: null,
  semente: 42,
  versaoDoGerador: 1,
  historicoMeses: 12,
  posicaoInicial: 0,
  data: '2026-11-10T10:00:00+00:00',
  processados,
  situacao,
  motivo,
  consentimentos: processados,
  recursos: processados * 2,
  atualizadoEm: '2026-11-10T10:00:05+00:00',
});

export const clienteDoLote = (n: number) => ({
  documento: `000000000${String(n).padStart(2, '0')}`,
  tipo: 'PF',
  nome: `Cliente ${n}`,
  marca: 'hml',
  natureza: 'persistido',
  loteId: 'lote-0',
});

export const massaDaMariana = {
  cliente: {
    documento: '12345678909',
    tipo: 'PF',
    nome: 'Mariana Alves',
    marca: 'hml',
    natureza: 'persistido',
    loteId: null,
  },
  consentimentos: [
    {
      consentId: 'urn:hml:p-c0x0',
      cliente: '12345678909',
      banco: 'banco-estavel',
      estado: 'AUTHORISED',
      permissoes: ['ACCOUNTS_READ', 'RESOURCES_READ'],
      recursos: ['hml-p-r0x0'],
      criadoEm: '2026-11-10T10:00:00+00:00',
      marca: 'hml',
      natureza: 'persistido',
      loteId: null,
      expiraEm: null,
    },
  ],
  recursos: [
    {
      resourceId: 'hml-p-r0x0',
      cliente: '12345678909',
      banco: 'banco-estavel',
      tipo: 'ACCOUNT',
      produto: 'contas',
      estado: 'AVAILABLE',
      perfil: 'cliente-comum',
      saldoInicial: 5000,
      sementeDeDados: 7,
      criadoEm: '2026-11-10T10:00:00+00:00',
      inicioDoHistorico: '2025-11-10',
      marca: 'hml',
      natureza: 'persistido',
    },
  ],
};

export const previaDoLote = {
  alvo: 'lote',
  id: 'lote-0',
  removido: false,
  clientes: 10,
  consentimentos: 10,
  recursos: 20,
  ajustes: 0,
  lotes: 1,
  perfis: 0,
  instituicoes: 0,
};
