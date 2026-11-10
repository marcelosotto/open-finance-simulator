// Respostas reais do painel de consentimento (spec 008), reduzidas.
export const opcoesPF = {
  cliente: '95000000080',
  tipo: 'PF',
  banco: 'banco-estavel',
  semProduto: false,
  grupos: [
    { nome: 'cadastro-identificacao', permissoes: ['CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ'] },
    { nome: 'contas-saldos', permissoes: ['ACCOUNTS_READ', 'ACCOUNTS_BALANCES_READ'] },
    { nome: 'cartoes-limites', permissoes: ['CREDIT_CARDS_ACCOUNTS_READ', 'CREDIT_CARDS_ACCOUNTS_LIMITS_READ'] },
    { nome: 'operacoes-de-credito', permissoes: ['LOANS_READ', 'LOANS_WARRANTIES_READ'] },
  ],
  contas: [{ resourceId: 'hml-p-r0x1', marcada: true }],
  cartoes: [{ resourceId: 'hml-p-r0x0', marcada: true }],
  agrupados: ['LOAN'],
  receptoras: [{ organisationId: 'hml-p-org', nome: 'Coletor' }],
};

export const visaoPJ = (pendentes: number, mudar: Record<string, unknown> = {}) => ({
  consentId: 'urn:hml:p-c5x0',
  cliente: 'P0000001000182',
  banco: 'banco-estavel',
  receptora: 'hml-p-org',
  estado: 'AUTHORISED',
  permissoes: ['ACCOUNTS_READ', 'ACCOUNTS_BALANCES_READ', 'RESOURCES_READ'],
  criadoEm: '2026-11-10T10:50:03Z',
  alcada: {
    situacao: pendentes > 0 ? 'pendente' : 'aprovada',
    aprovacoesPendentes: pendentes,
    prazo: '2026-11-25T10:50:03Z',
  },
  decisoes: [],
  recursos: [
    { resourceId: 'hml-p-r1x1', tipo: 'ACCOUNT', estado: pendentes > 0 ? 'PENDING_AUTHORISATION' : 'AVAILABLE' },
  ],
  ...mudar,
});
