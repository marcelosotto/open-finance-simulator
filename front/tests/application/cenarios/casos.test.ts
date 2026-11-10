import { left, right } from '@/core/comum/either';
import { falhaDaResposta } from '@/core/comum/falha';
import { formularioVazio } from '@/core/cenarios/formulario';
import type { PacoteDeCenario, RepositorioDeCenarios, Vencedoras } from '@/core/cenarios/regra';
import {
  AlterarRegra,
  ApagarPacote,
  ApagarRegra,
  CatalogoDeDefeitos,
  CriarPacote,
  CriarRegra,
  LigarPacote,
  LigarRegra,
  ListarPacotes,
  ListarRegras,
  ObterRegra,
  OperacoesDoCatalogo,
  TOKENS_DOS_CENARIOS,
  VencedoraDaChamada,
  casosDosCenarios,
} from '@/application/cenarios/casos';
import { regra } from '../../core/cenarios/regras';

const pacote: PacoteDeCenario = { nome: 'natal', ligado: false, criadoEm: '2026-11-05T12:00:00+00:00' };
const vencedoras: Vencedoras = {
  instante: '2026-11-05T12:00:00+00:00',
  comportamento: regra(),
  estado: null,
  defeito: null,
};
const recusa = falhaDaResposta(422, ['endpoint desconhecido']);

/** Repositório falso: cada método é um jest.fn com resposta de sucesso. */
const repositorioFalso = () => {
  const r = {
    listarRegras: jest.fn().mockResolvedValue(right([regra()])),
    obterRegra: jest.fn().mockResolvedValue(right(regra())),
    criarRegra: jest.fn().mockResolvedValue(right(regra())),
    alterarRegra: jest.fn().mockResolvedValue(right(regra())),
    apagarRegra: jest.fn().mockResolvedValue(right(undefined)),
    ligarRegra: jest.fn().mockResolvedValue(right(regra({ ligada: false }))),
    defeitos: jest.fn().mockResolvedValue(right([{ nome: 'formato-invalido', descricao: 'Reescreve a data.' }])),
    operacoes: jest.fn().mockResolvedValue(right(['accountsGetAccounts'])),
    listarPacotes: jest.fn().mockResolvedValue(right([pacote])),
    criarPacote: jest.fn().mockResolvedValue(right(pacote)),
    ligarPacote: jest.fn().mockResolvedValue(right({ ...pacote, ligado: true })),
    apagarPacote: jest.fn().mockResolvedValue(left(falhaDaResposta(409, ['pacote tem regras']))),
    vencedoras: jest.fn().mockResolvedValue(right(vencedoras)),
  } satisfies RepositorioDeCenarios;
  return r;
};

describe('casos de uso dos cenários', () => {
  it('listar, obter, apagar e ligar repassam ao repositório', async () => {
    const repo = repositorioFalso();

    expect((await new ListarRegras(repo).executar()).isRight()).toBe(true);
    expect((await new ObterRegra(repo).executar('r-1')).isRight()).toBe(true);
    expect((await new ApagarRegra(repo).executar('r-1')).isRight()).toBe(true);
    const ligada = await new LigarRegra(repo).executar({ id: 'r-1', ligada: false });

    expect(repo.obterRegra).toHaveBeenCalledWith('r-1');
    expect(repo.apagarRegra).toHaveBeenCalledWith('r-1');
    expect(repo.ligarRegra).toHaveBeenCalledWith('r-1', false);
    expect(ligada.isRight() && ligada.value.ligada).toBe(false);
  });

  it('criar monta o corpo só com os campos do efeito', async () => {
    const repo = repositorioFalso();

    const criada = await new CriarRegra(repo).executar({
      ...formularioVazio(),
      instituicao: 'banco-estavel',
      tipo: 'status',
      status: '500',
      latenciaMs: '99',
    });

    expect(criada.isRight()).toBe(true);
    expect(repo.criarRegra).toHaveBeenCalledWith({
      instituicao: 'banco-estavel',
      efeito: { tipo: 'status', status: 500 },
      frequenciaPct: 100,
      ligada: true,
    });
  });

  it('criar com formato inválido é Left e não chama o repositório', async () => {
    const repo = repositorioFalso();

    const criada = await new CriarRegra(repo).executar({ ...formularioVazio(), latenciaMs: 'x' });

    expect(criada.isLeft() && criada.value.erros).toEqual(['LATENCIA_INVALIDA']);
    expect(repo.criarRegra).not.toHaveBeenCalled();
  });

  it('a recusa da administração chega como Left', async () => {
    const repo = repositorioFalso();
    repo.criarRegra.mockResolvedValue(left(recusa));

    const criada = await new CriarRegra(repo).executar({ ...formularioVazio(), endpoint: 'nada', latenciaMs: '1' });

    expect(criada.isLeft() && criada.value).toEqual(recusa);
  });

  it('alterar manda o pedido inteiro pelo id, inclusive ligada', async () => {
    const repo = repositorioFalso();

    await new AlterarRegra(repo).executar({
      id: 'r-1',
      formulario: { ...formularioVazio(), cliente: '761', latenciaMs: '10', ligada: false },
    });
    const invalida = await new AlterarRegra(repo).executar({
      id: 'r-1',
      formulario: { ...formularioVazio(), latenciaMs: '' },
    });

    expect(repo.alterarRegra).toHaveBeenCalledTimes(1);
    expect(repo.alterarRegra).toHaveBeenCalledWith('r-1', {
      cliente: '761',
      efeito: { tipo: 'latencia', latenciaMs: 10 },
      frequenciaPct: 100,
      ligada: false,
    });
    expect(invalida.isLeft()).toBe(true);
  });

  it('catálogo de defeitos e operações', async () => {
    const repo = repositorioFalso();

    const defeitos = await new CatalogoDeDefeitos(repo).executar();
    const operacoes = await new OperacoesDoCatalogo(repo).executar();

    expect(defeitos.isRight() && defeitos.value.map((d) => d.nome)).toEqual(['formato-invalido']);
    expect(operacoes.isRight() && operacoes.value).toEqual(['accountsGetAccounts']);
  });

  it('pacotes: listar, criar com nome aparado, ligar e a recusa ao apagar', async () => {
    const repo = repositorioFalso();

    expect((await new ListarPacotes(repo).executar()).isRight()).toBe(true);
    expect((await new CriarPacote(repo).executar('  natal ')).isRight()).toBe(true);
    const ligado = await new LigarPacote(repo).executar({ nome: 'natal', ligado: true });
    const apagado = await new ApagarPacote(repo).executar('natal');

    expect(repo.criarPacote).toHaveBeenCalledWith('natal');
    expect(repo.ligarPacote).toHaveBeenCalledWith('natal', true);
    expect(ligado.isRight() && ligado.value.ligado).toBe(true);
    expect(apagado.isLeft() && apagado.value).toEqual({ tipo: 'conflito', status: 409, erros: ['pacote tem regras'] });
  });

  it('pacote sem nome é Left e não chama o repositório', async () => {
    const repo = repositorioFalso();

    const criado = await new CriarPacote(repo).executar(' ');

    expect(criado.isLeft() && criado.value.erros).toEqual(['NOME_DO_PACOTE_OBRIGATORIO']);
    expect(repo.criarPacote).not.toHaveBeenCalled();
  });

  it('vencedora: devolve o que a administração respondeu, sem decidir nada', async () => {
    const repo = repositorioFalso();

    const resposta = await new VencedoraDaChamada(repo).executar({
      instituicao: 'banco-estavel',
      endpoint: 'accountsGetAccounts',
      cliente: '',
      recurso: 'acc-1',
    });

    expect(resposta.isRight() && resposta.value).toEqual(vencedoras);
    expect(repo.vencedoras).toHaveBeenCalledWith({
      instituicao: 'banco-estavel',
      endpoint: 'accountsGetAccounts',
      recurso: 'acc-1',
    });
  });

  it('vencedora sem endpoint é Left e não chama o repositório', async () => {
    const repo = repositorioFalso();

    const resposta = await new VencedoraDaChamada(repo).executar({
      instituicao: 'banco-estavel',
      endpoint: '',
      cliente: '',
      recurso: '',
    });

    expect(resposta.isLeft() && resposta.value.erros).toEqual(['ENDPOINT_DA_CHAMADA_OBRIGATORIO']);
    expect(repo.vencedoras).not.toHaveBeenCalled();
  });

  it('registra um caso por token', () => {
    const pares = casosDosCenarios(repositorioFalso());
    expect(pares.map(([token]) => token)).toEqual(Object.values(TOKENS_DOS_CENARIOS));
  });
});
