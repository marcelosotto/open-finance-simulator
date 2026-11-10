import {
  alternarGrupo,
  alternarOrigem,
  alternarPermissao,
  estadoDoGrupo,
  montarPedido,
  mostraOrigem,
  origemMarcada,
  origensFaltando,
  podeDecidir,
  podeRevogar,
  rascunhoVazio,
  type OpcoesDoMenu,
  type Rascunho,
  type VisaoDoConsentimento,
} from '@/core/consentimentos/consentimento';

const saldos = { nome: 'contas-saldos', permissoes: ['ACCOUNTS_READ', 'ACCOUNTS_BALANCES_READ'] };
const limites = { nome: 'contas-limites', permissoes: ['ACCOUNTS_READ', 'ACCOUNTS_OVERDRAFT_LIMITS_READ'] };
const cartoes = {
  nome: 'cartoes-limites',
  permissoes: ['CREDIT_CARDS_ACCOUNTS_READ', 'CREDIT_CARDS_ACCOUNTS_LIMITS_READ'],
};

const opcoes = (mudar: Partial<OpcoesDoMenu> = {}): OpcoesDoMenu => ({
  cliente: '95000000080',
  tipo: 'PF',
  banco: 'banco-estavel',
  semProduto: false,
  grupos: [saldos, limites, cartoes],
  contas: [{ resourceId: 'c1', marcada: true }],
  cartoes: [{ resourceId: 'k1', marcada: true }],
  agrupados: ['LOAN'],
  receptoras: [{ organisationId: 'org', nome: 'Coletor' }],
  ...mudar,
});

const rascunho = (mudar: Partial<Rascunho> = {}): Rascunho => ({
  ...rascunhoVazio,
  cliente: '95000000080',
  banco: 'banco-estavel',
  receptora: 'org',
  ...mudar,
});

describe('Grupos de permissões (UI10, Q6)', () => {
  it('marca o grupo inteiro e mostra parcial e desmarcado', () => {
    const r = alternarGrupo(rascunho(), saldos, [saldos, limites]);
    expect(estadoDoGrupo(r, saldos)).toBe('marcado');
    expect(estadoDoGrupo(r, limites)).toBe('parcial');
    expect(estadoDoGrupo(rascunho(), saldos)).toBe('desmarcado');
  });

  it('desmarcar um grupo mantém a permissão usada por outro grupo marcado', () => {
    let r = alternarGrupo(rascunho(), saldos, [saldos, limites]);
    r = alternarGrupo(r, limites, [saldos, limites]);
    r = alternarGrupo(r, saldos, [saldos, limites]);
    expect(r.permissoes).toEqual(['ACCOUNTS_READ', 'ACCOUNTS_OVERDRAFT_LIMITS_READ']);
  });

  it('permissão solta liga e desliga, e pode deixar o grupo incompleto', () => {
    const r = alternarPermissao(rascunho(), 'ACCOUNTS_READ');
    expect(r.permissoes).toEqual(['ACCOUNTS_READ']);
    expect(estadoDoGrupo(r, saldos)).toBe('parcial');
    expect(alternarPermissao(r, 'ACCOUNTS_READ').permissoes).toEqual([]);
  });
});

describe('Seleção de origem (UI13, RN95)', () => {
  it('vem marcada da API e inverte ao clicar', () => {
    const conta = { resourceId: 'c1', marcada: true };
    const r = alternarOrigem(rascunho(), 'c1');
    expect(origemMarcada(rascunho(), conta)).toBe(true);
    expect(origemMarcada(r, conta)).toBe(false);
    expect(alternarOrigem(r, 'c1').invertidas).toEqual([]);
  });

  it('a seção só aparece com opção e com o grupo pedido', () => {
    const comContas = rascunho({ permissoes: saldos.permissoes });
    expect(mostraOrigem(comContas, opcoes(), 'contas')).toBe(true);
    expect(mostraOrigem(comContas, opcoes(), 'cartoes')).toBe(false);
    expect(mostraOrigem(comContas, opcoes({ contas: [] }), 'contas')).toBe(false);
  });

  it('acusa a origem desmarcada quando há opção', () => {
    const r = alternarOrigem(rascunho({ permissoes: [...saldos.permissoes, ...cartoes.permissoes] }), 'c1');
    expect(origensFaltando(r, opcoes())).toEqual(['contas']);
    expect(origensFaltando(r, opcoes({ contas: [] }))).toEqual([]);
  });
});

describe('Pedido (RN96)', () => {
  it('acrescenta RESOURCES_READ e manda só a seleção das seções que aparecem', () => {
    const r = rascunho({ permissoes: saldos.permissoes, prazoMeses: '12' });
    expect(montarPedido(r, opcoes()).value).toEqual({
      cliente: '95000000080',
      banco: 'banco-estavel',
      receptora: 'org',
      permissoes: ['ACCOUNTS_READ', 'ACCOUNTS_BALANCES_READ', 'RESOURCES_READ'],
      contas: ['c1'],
      prazoMeses: 12,
    });
  });

  it('sem prazo é indeterminado; cartões entram quando pedidos', () => {
    const pedido = montarPedido(rascunho({ permissoes: cartoes.permissoes }), opcoes());
    expect(pedido.value).toEqual(expect.objectContaining({ cartoes: ['k1'] }));
    expect(pedido.value).not.toHaveProperty('prazoMeses');
    expect(pedido.value).not.toHaveProperty('contas');
  });

  it('alçadas só em PJ, de 1 a 10 (RN99)', () => {
    const pj = opcoes({ tipo: 'PJ' });
    const r = rascunho({ permissoes: saldos.permissoes, aprovacoesPendentes: '2' });
    expect(montarPedido(r, pj).value).toEqual(expect.objectContaining({ aprovacoesPendentes: 2 }));
    expect(montarPedido(r, opcoes()).value).not.toHaveProperty('aprovacoesPendentes');
    expect(montarPedido({ ...r, aprovacoesPendentes: '11' }, pj).value).toEqual({
      tipo: 'validacao',
      erros: ['ALCADA_INVALIDA'],
    });
  });

  it('junta todos os erros de formato e faixa', () => {
    const r = alternarOrigem(rascunho({ receptora: '', prazoMeses: '13', permissoes: saldos.permissoes }), 'c1');
    expect(montarPedido(r, opcoes()).value).toEqual({
      tipo: 'validacao',
      erros: ['RECEPTORA_OBRIGATORIA', 'SEM_ORIGEM_SELECIONADA', 'PRAZO_INVALIDO'],
    });
    expect(montarPedido(rascunho({ prazoMeses: '1.5' }), opcoes()).value).toEqual({
      tipo: 'validacao',
      erros: ['PERMISSOES_OBRIGATORIAS', 'PRAZO_INVALIDO'],
    });
  });
});

describe('Visão do consentimento (UI14, RN99)', () => {
  const visao = (mudar: Partial<VisaoDoConsentimento> = {}): VisaoDoConsentimento => ({
    consentId: 'urn:hml:p-c0x0',
    cliente: 'P0000001000182',
    banco: 'banco-estavel',
    receptora: 'org',
    estado: 'AUTHORISED',
    permissoes: [],
    criadoEm: '2026-11-10T10:50:03Z',
    expiraEm: null,
    rejeitadoPor: null,
    razao: null,
    alcada: { situacao: 'pendente', aprovacoesPendentes: 2, prazo: '2026-11-25T10:50:03Z' },
    decisoes: [],
    recursos: [],
    ...mudar,
  });

  it('decide só com pendência válida', () => {
    expect(podeDecidir(visao())).toBe(true);
    expect(podeDecidir(visao({ estado: 'REJECTED' }))).toBe(false);
    expect(podeDecidir(visao({ alcada: null }))).toBe(false);
    expect(podeDecidir(visao({ alcada: { situacao: 'recusada', aprovacoesPendentes: 1, prazo: null } }))).toBe(false);
    expect(podeDecidir(visao({ alcada: { situacao: 'pendente', aprovacoesPendentes: 0, prazo: null } }))).toBe(false);
  });

  it('revoga enquanto não está REJECTED', () => {
    expect(podeRevogar(visao())).toBe(true);
    expect(podeRevogar(visao({ estado: 'REJECTED' }))).toBe(false);
  });
});
