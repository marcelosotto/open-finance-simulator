import {
  ApagarDistribuicao,
  ApagarPerfilDeDados,
  CancelarLote,
  ClientesDoLote,
  ConfirmarLimpeza,
  CriarClienteAvulso,
  GerarLote,
  ListarDistribuicoes,
  ListarLotes,
  ListarPerfisDeDados,
  ObterClienteDaMassa,
  ObterLote,
  PreverLimpeza,
  SalvarDistribuicao,
  SalvarPerfilDeDados,
  TOKENS_DA_MASSA,
  casosDaMassa,
} from '@/application/massa/casos';
import { left, right } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import type {
  Distribuicao,
  Lote,
  MassaDoCliente,
  PerfilDeDados,
  RepositorioDaMassa,
  ResultadoDaLimpeza,
} from '@/core/massa/massa';

const perfil: PerfilDeDados = {
  nome: 'meu-perfil',
  transacoesMin: 0,
  transacoesMax: 1,
  valorMin: 1,
  valorMax: 2,
  saldoInicial: 3,
  tiposDeLancamento: ['PIX'],
  produtos: ['contas'],
};
const distribuicao: Distribuicao = { bancos: { b: 100 }, perfis: { 'cliente-comum': 100 }, produtos: {}, pjPct: 0 };
const lote: Lote = {
  id: 'lote-0',
  quantidade: 1,
  situacao: 'em_andamento',
  processados: 0,
  consentimentos: 0,
  recursos: 0,
  data: '2026-11-10T10:00:00Z',
  motivo: null,
};
const massa: MassaDoCliente = {
  cliente: { documento: '1', tipo: 'PF', nome: 'M', marca: 'hml', natureza: 'persistido', loteId: null },
  consentimentos: [],
  recursos: [],
};
const limpeza: ResultadoDaLimpeza = {
  alvo: 'tudo',
  id: null,
  removido: false,
  clientes: 1,
  consentimentos: 0,
  recursos: 0,
  ajustes: 0,
  lotes: 0,
  perfis: 0,
  instituicoes: 0,
};
const recusa: Falha = { tipo: 'recusa', status: 422, erros: ['perfil desconhecido: x'] };

const repositorioFalso = () => ({
  perfis: jest.fn().mockResolvedValue(right([perfil])),
  salvarPerfil: jest.fn().mockResolvedValue(right(perfil)),
  apagarPerfil: jest.fn().mockResolvedValue(right(undefined)),
  distribuicoes: jest.fn().mockResolvedValue(right([{ nome: 'd', distribuicao }])),
  salvarDistribuicao: jest.fn().mockResolvedValue(right({ nome: 'd', distribuicao })),
  apagarDistribuicao: jest.fn().mockResolvedValue(right(undefined)),
  gerarLote: jest.fn().mockResolvedValue(right(lote)),
  lotes: jest.fn().mockResolvedValue(right([lote])),
  obterLote: jest.fn().mockResolvedValue(right(lote)),
  cancelarLote: jest.fn().mockResolvedValue(left({ tipo: 'conflito', status: 409, erros: ['lote já terminou'] })),
  clientesDoLote: jest.fn().mockResolvedValue(right([massa.cliente])),
  criarCliente: jest.fn().mockResolvedValue(left(recusa)),
  obterCliente: jest.fn().mockResolvedValue(right(massa)),
  limpar: jest.fn().mockResolvedValue(right(limpeza)),
  clientesDoBanco: jest
    .fn()
    .mockResolvedValue(right({ clientes: [massa.cliente], inicio: 0, quantidade: 10, temMais: false })),
  resumo: jest.fn().mockResolvedValue(right({ clientes: 1, consentimentos: 0, recursos: 2, lotes: 0 })),
});

const montar = () => {
  const falso = repositorioFalso();
  const repositorio: RepositorioDaMassa = falso;
  return { falso, repositorio };
};

describe('Casos de uso da massa', () => {
  it('listagens e leituras repassam o repositório (Right)', async () => {
    const { falso, repositorio } = montar();
    expect((await new ListarPerfisDeDados(repositorio).executar()).value).toEqual([perfil]);
    expect((await new ListarDistribuicoes(repositorio).executar()).isRight()).toBe(true);
    expect((await new ListarLotes(repositorio).executar()).value).toEqual([lote]);
    expect((await new ObterLote(repositorio).executar('lote-0')).value).toEqual(lote);
    expect(falso.obterLote).toHaveBeenCalledWith('lote-0');
  });

  it('salva perfil válido e recusa pronto sem chamar a API', async () => {
    const { falso, repositorio } = montar();
    const caso = new SalvarPerfilDeDados(repositorio);
    expect((await caso.executar({ ...perfil, nome: ' meu-perfil ' })).value).toEqual(perfil);
    expect(falso.salvarPerfil).toHaveBeenCalledWith(perfil);

    const pronto = await caso.executar({ ...perfil, nome: 'cliente-comum' });
    expect(pronto.value).toEqual({ tipo: 'validacao', erros: ['PERFIL_PRONTO'] });
    expect(falso.salvarPerfil).toHaveBeenCalledTimes(1);
  });

  it('apaga perfil criado e recusa pronto', async () => {
    const { falso, repositorio } = montar();
    const caso = new ApagarPerfilDeDados(repositorio);
    expect((await caso.executar('meu-perfil')).isRight()).toBe(true);
    expect((await caso.executar('alto-volume')).isLeft()).toBe(true);
    expect(falso.apagarPerfil).toHaveBeenCalledTimes(1);
  });

  it('salva distribuição em 100% e recusa fora de 100 com DISTRIBUICAO_FORA_DE_100', async () => {
    const { falso, repositorio } = montar();
    const caso = new SalvarDistribuicao(repositorio);
    expect((await caso.executar({ nome: 'd', distribuicao })).isRight()).toBe(true);
    expect(falso.salvarDistribuicao).toHaveBeenCalledWith({ nome: 'd', distribuicao });

    const fora = await caso.executar({ nome: 'd', distribuicao: { ...distribuicao, bancos: { b: 50 } } });
    expect(fora.value).toEqual({ tipo: 'validacao', erros: ['DISTRIBUICAO_FORA_DE_100'] });
    expect(falso.salvarDistribuicao).toHaveBeenCalledTimes(1);
  });

  it('apaga distribuição', async () => {
    const { falso, repositorio } = montar();
    expect((await new ApagarDistribuicao(repositorio).executar('d')).isRight()).toBe(true);
    expect(falso.apagarDistribuicao).toHaveBeenCalledWith('d');
  });

  it('gera lote com quantidade válida e recusa quantidade 0', async () => {
    const { falso, repositorio } = montar();
    const caso = new GerarLote(repositorio);
    expect((await caso.executar({ quantidade: 10, distribuicaoNome: 'd' })).value).toEqual(lote);
    expect(falso.gerarLote).toHaveBeenCalledWith({ quantidade: 10, distribuicaoNome: 'd' });
    expect((await caso.executar({ quantidade: 0 })).value).toEqual({
      tipo: 'validacao',
      erros: ['QUANTIDADE_INVALIDA'],
    });
    expect(falso.gerarLote).toHaveBeenCalledTimes(1);
  });

  it('cancelar devolve a recusa da API (Left)', async () => {
    const { repositorio } = montar();
    const resultado = await new CancelarLote(repositorio).executar('lote-0');
    expect(resultado.isLeft()).toBe(true);
    expect(resultado.value).toEqual({ tipo: 'conflito', status: 409, erros: ['lote já terminou'] });
  });

  it('pagina os clientes do lote e recusa página inválida', async () => {
    const { falso, repositorio } = montar();
    const caso = new ClientesDoLote(repositorio);
    expect((await caso.executar({ id: 'lote-0', inicio: 20, quantidade: 20 })).isRight()).toBe(true);
    expect(falso.clientesDoLote).toHaveBeenCalledWith('lote-0', { inicio: 20, quantidade: 20 });
    expect((await caso.executar({ id: 'lote-0', inicio: -1, quantidade: 20 })).value).toEqual({
      tipo: 'validacao',
      erros: ['PAGINA_INVALIDA'],
    });
  });

  it('cria cliente: sem banco recusa antes; com banco repassa a recusa da API', async () => {
    const { falso, repositorio } = montar();
    const caso = new CriarClienteAvulso(repositorio);
    expect((await caso.executar({ tipo: 'PF', bancos: [] })).value).toEqual({
      tipo: 'validacao',
      erros: ['BANCO_OBRIGATORIO'],
    });
    expect(falso.criarCliente).not.toHaveBeenCalled();
    expect((await caso.executar({ tipo: 'PF', bancos: ['b'], perfil: 'x' })).value).toEqual(recusa);
  });

  it('obtém cliente pelo documento aparado e exige documento', async () => {
    const { falso, repositorio } = montar();
    const caso = new ObterClienteDaMassa(repositorio);
    expect((await caso.executar(' 1 ')).value).toEqual(massa);
    expect(falso.obterCliente).toHaveBeenCalledWith('1');
    expect((await caso.executar('')).value).toEqual({ tipo: 'validacao', erros: ['DOCUMENTO_OBRIGATORIO'] });
  });

  it('prévia manda confirmar falso; confirmação manda verdadeiro', async () => {
    const { falso, repositorio } = montar();
    expect((await new PreverLimpeza(repositorio).executar({ alvo: 'tudo', id: '' })).value).toEqual(limpeza);
    expect(falso.limpar).toHaveBeenLastCalledWith({ alvo: 'tudo' }, false);
    expect((await new ConfirmarLimpeza(repositorio).executar({ alvo: 'lote', id: 'lote-0' })).isRight()).toBe(true);
    expect(falso.limpar).toHaveBeenLastCalledWith({ alvo: 'lote', id: 'lote-0' }, true);
  });

  it('prévia e confirmação recusam lote sem id', async () => {
    const { falso, repositorio } = montar();
    const sem = { alvo: 'lote', id: ' ' } as const;
    expect((await new PreverLimpeza(repositorio).executar(sem)).value).toEqual({
      tipo: 'validacao',
      erros: ['ID_OBRIGATORIO'],
    });
    expect((await new ConfirmarLimpeza(repositorio).executar(sem)).isLeft()).toBe(true);
    expect(falso.limpar).not.toHaveBeenCalled();
  });

  it('registra um caso por token', () => {
    const { repositorio } = montar();
    const pares = casosDaMassa(repositorio);
    expect(pares).toHaveLength(Object.keys(TOKENS_DA_MASSA).length);
    expect(new Set(pares.map(([token]) => token)).size).toBe(pares.length);
  });
});
