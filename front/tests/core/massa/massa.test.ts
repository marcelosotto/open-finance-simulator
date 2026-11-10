import {
  alvoPedeId,
  distribuicaoParaSalvar,
  emAndamento,
  paginaValida,
  pedidoDeCliente,
  pedidoDeLimpeza,
  pedidoDeLote,
  perfilParaSalvar,
  perfilPronto,
  podeSalvar,
  porcentagemValida,
  somaDeBancos,
  somaDePerfis,
  type Distribuicao,
  type PerfilDeDados,
} from '@/core/massa/massa';

const distribuicao = (mudancas: Partial<Distribuicao> = {}): Distribuicao => ({
  bancos: { 'banco-estavel': 60, 'banco-lento': 40 },
  perfis: { 'cliente-comum': 100 },
  produtos: { contas: 90, cartoes: 50 },
  pjPct: 10,
  ...mudancas,
});

const perfil = (mudancas: Partial<PerfilDeDados> = {}): PerfilDeDados => ({
  nome: 'meu-perfil',
  transacoesMin: 0,
  transacoesMax: 4,
  valorMin: 10,
  valorMax: 2000,
  saldoInicial: 5000,
  tiposDeLancamento: ['PIX'],
  produtos: ['contas'],
  ...mudancas,
});

describe('Distribuição (UI07, RN56)', () => {
  it('soma bancos e perfis, sem erro de arredondamento', () => {
    expect(somaDeBancos(distribuicao())).toBe(100);
    expect(somaDePerfis(distribuicao({ perfis: { a: 33.3, b: 33.3, c: 33.4 } }))).toBe(100);
    expect(somaDeBancos(distribuicao({ bancos: {} }))).toBe(0);
  });

  it('pode salvar com as duas somas em 100; produtos não somam', () => {
    expect(podeSalvar(distribuicao())).toBe(true);
    expect(podeSalvar(distribuicao({ produtos: {} }))).toBe(true);
  });

  it('bloqueia com bancos ou perfis fora de 100', () => {
    expect(podeSalvar(distribuicao({ bancos: { 'banco-estavel': 90 } }))).toBe(false);
    expect(podeSalvar(distribuicao({ perfis: { 'cliente-comum': 50, 'alto-volume': 60 } }))).toBe(false);
  });

  it('bloqueia porcentagem fora de 0 a 100, mesmo com a soma em 100', () => {
    expect(podeSalvar(distribuicao({ bancos: { a: 120, b: -20 } }))).toBe(false);
    expect(podeSalvar(distribuicao({ produtos: { contas: 101 } }))).toBe(false);
    expect(podeSalvar(distribuicao({ pjPct: -1 }))).toBe(false);
    expect(podeSalvar(distribuicao({ pjPct: NaN }))).toBe(false);
  });

  it('confere a faixa de cada porcentagem', () => {
    expect(porcentagemValida(0)).toBe(true);
    expect(porcentagemValida(100)).toBe(true);
    expect(porcentagemValida(100.01)).toBe(false);
    expect(porcentagemValida(Infinity)).toBe(false);
  });

  it('para salvar, exige nome e as somas em 100', () => {
    expect(distribuicaoParaSalvar('  minha ', distribuicao()).value).toEqual({
      nome: 'minha',
      distribuicao: distribuicao(),
    });
    expect(distribuicaoParaSalvar(' ', distribuicao()).value).toEqual({
      tipo: 'validacao',
      erros: ['NOME_OBRIGATORIO'],
    });
    const fora = distribuicaoParaSalvar('minha', distribuicao({ bancos: { a: 10 } }));
    expect(fora.isLeft()).toBe(true);
    expect(fora.value).toEqual({ tipo: 'validacao', erros: ['DISTRIBUICAO_FORA_DE_100'] });
  });
});

describe('Perfil de dados (RN55)', () => {
  it('reconhece os perfis prontos', () => {
    expect(perfilPronto('cliente-comum')).toBe(true);
    expect(perfilPronto('sem-movimento')).toBe(true);
    expect(perfilPronto('alto-volume')).toBe(true);
    expect(perfilPronto('meu-perfil')).toBe(false);
  });

  it('para salvar, apara o nome e recusa vazio, pronto e número inválido', () => {
    expect(perfilParaSalvar(perfil({ nome: ' meu-perfil ' })).value).toEqual(perfil());
    expect(perfilParaSalvar(perfil({ nome: '' })).value).toEqual({ tipo: 'validacao', erros: ['NOME_OBRIGATORIO'] });
    expect(perfilParaSalvar(perfil({ nome: 'cliente-comum' })).value).toEqual({
      tipo: 'validacao',
      erros: ['PERFIL_PRONTO'],
    });
    expect(perfilParaSalvar(perfil({ valorMax: NaN })).isLeft()).toBe(true);
    expect(perfilParaSalvar(perfil({ saldoInicial: -1 })).value).toEqual({
      tipo: 'validacao',
      erros: ['NUMERO_INVALIDO'],
    });
  });
});

describe('Lote', () => {
  it('está em andamento só com a situação em_andamento', () => {
    expect(emAndamento({ situacao: 'em_andamento' })).toBe(true);
    expect(emAndamento({ situacao: 'concluido' })).toBe(false);
    expect(emAndamento({ situacao: 'parcial' })).toBe(false);
  });

  it('aceita quantidade inteira a partir de 1 e opcionais inteiros positivos', () => {
    const pedido = { quantidade: 5, consentimentosPorCliente: 2, historicoMeses: 6, semente: 42 };
    expect(pedidoDeLote(pedido).value).toEqual(pedido);
    expect(pedidoDeLote({ quantidade: 1 }).isRight()).toBe(true);
  });

  it('recusa quantidade inválida e opcionais fora da faixa', () => {
    expect(pedidoDeLote({ quantidade: 0 }).value).toEqual({ tipo: 'validacao', erros: ['QUANTIDADE_INVALIDA'] });
    expect(pedidoDeLote({ quantidade: 1.5 }).isLeft()).toBe(true);
    expect(pedidoDeLote({ quantidade: 1, consentimentosPorCliente: 0 }).value).toEqual({
      tipo: 'validacao',
      erros: ['NUMERO_INVALIDO'],
    });
    expect(pedidoDeLote({ quantidade: 1, historicoMeses: 2.5 }).isLeft()).toBe(true);
    expect(pedidoDeLote({ quantidade: 1, semente: 1.5 }).value).toEqual({
      tipo: 'validacao',
      erros: ['NUMERO_INVALIDO'],
    });
  });

  it('confere a página de clientes', () => {
    expect(paginaValida({ inicio: 0, quantidade: 20 })).toBe(true);
    expect(paginaValida({ inicio: -1, quantidade: 20 })).toBe(false);
    expect(paginaValida({ inicio: 0.5, quantidade: 20 })).toBe(false);
    expect(paginaValida({ inicio: 0, quantidade: 0 })).toBe(false);
    expect(paginaValida({ inicio: 0, quantidade: 1.5 })).toBe(false);
  });
});

describe('Cliente avulso', () => {
  it('apara os bancos e exige pelo menos um', () => {
    expect(pedidoDeCliente({ tipo: 'PF', bancos: [' banco-estavel ', ''] }).value).toEqual({
      tipo: 'PF',
      bancos: ['banco-estavel'],
    });
    expect(pedidoDeCliente({ tipo: 'PJ', bancos: [' '] }).value).toEqual({
      tipo: 'validacao',
      erros: ['BANCO_OBRIGATORIO'],
    });
  });

  it('aceita semente inteira e recusa fracionária', () => {
    expect(pedidoDeCliente({ tipo: 'PF', bancos: ['b'], semente: 7 }).isRight()).toBe(true);
    expect(pedidoDeCliente({ tipo: 'PF', bancos: ['b'], semente: 7.5 }).value).toEqual({
      tipo: 'validacao',
      erros: ['NUMERO_INVALIDO'],
    });
  });
});

describe('Limpeza', () => {
  it('lote e instituição pedem id; os outros alvos não', () => {
    expect(alvoPedeId('lote')).toBe(true);
    expect(alvoPedeId('instituicao')).toBe(true);
    expect(alvoPedeId('ajustes-mundo')).toBe(false);
    expect(alvoPedeId('tudo')).toBe(false);
  });

  it('monta o pedido, aparando o id e ignorando-o quando o alvo não pede', () => {
    expect(pedidoDeLimpeza('lote', ' lote-0 ').value).toEqual({ alvo: 'lote', id: 'lote-0' });
    expect(pedidoDeLimpeza('tudo', 'ignorado').value).toEqual({ alvo: 'tudo' });
    expect(pedidoDeLimpeza('instituicao', ' ').value).toEqual({ tipo: 'validacao', erros: ['ID_OBRIGATORIO'] });
  });
});
