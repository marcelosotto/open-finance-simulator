import { left, right } from '@/core/comum/either';
import type { RepositorioDeInstituicoes } from '@/core/instituicoes/instituicao';
import { casosDasInstituicoes, CriarInstituicao, TOKENS_DAS_INSTITUICOES } from '@/application/instituicoes/casos';
import { ObterAmbiente } from '@/application/ambiente/obter-ambiente';
import { Ambiente } from '@/core/ambiente/ambiente';

const falha = { tipo: 'recusa' as const, erros: ['X'] };

const repositorioFalso = (): jest.Mocked<RepositorioDeInstituicoes> => ({
  listar: jest.fn().mockResolvedValue(right([])),
  obter: jest.fn().mockResolvedValue(right({ endereco: 'b' })),
  perfis: jest.fn().mockResolvedValue(right(['estavel'])),
  criar: jest.fn().mockResolvedValue(right({ endereco: 'banco-azul' })),
  alterar: jest.fn().mockResolvedValue(left(falha)),
  apagar: jest.fn().mockResolvedValue(right('apagada')),
  reativar: jest.fn().mockResolvedValue(right({ endereco: 'b' })),
  historico: jest.fn().mockResolvedValue(right([])),
  anomalias: jest.fn().mockResolvedValue(right([])),
  ligarAnomalia: jest.fn().mockResolvedValue(right({ nome: 'n', ligada: true, descricao: '' })),
  mudarRelogio: jest.fn().mockResolvedValue(right({ endereco: 'b' })),
});

describe('Casos de uso das instituições', () => {
  it('criar valida antes de chamar o repositório (Left) e cria com o nome aparado (Right)', async () => {
    const repositorio = repositorioFalso();
    const criar = new CriarInstituicao(repositorio);

    expect((await criar.executar({ nome: ' ', perfil: 'estavel' })).isLeft()).toBe(true);
    expect(repositorio.criar).not.toHaveBeenCalled();

    expect((await criar.executar({ nome: ' Banco Azul ', perfil: 'lento' })).isRight()).toBe(true);
    expect(repositorio.criar).toHaveBeenCalledWith({ nome: 'Banco Azul', perfil: 'lento' });
  });

  it('cada token tem um caso que repassa ao repositório', async () => {
    const repositorio = repositorioFalso();
    const casos = new Map<symbol, unknown>(casosDasInstituicoes(repositorio));
    const caso = <T>(token: symbol) => casos.get(token) as T;
    type Executa = { executar: (...args: unknown[]) => Promise<{ isLeft: () => boolean }> };

    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Listar).executar();
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Obter).executar('b');
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Perfis).executar();
    const alterado = await caso<Executa>(TOKENS_DAS_INSTITUICOES.Alterar).executar({
      endereco: 'b',
      alteracao: { nome: 'x' },
    });
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Apagar).executar('b');
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Reativar).executar('b');
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Historico).executar('b');
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.Anomalias).executar('b');
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.LigarAnomalia).executar({ endereco: 'b', nome: 'n', ligada: true });
    await caso<Executa>(TOKENS_DAS_INSTITUICOES.MudarRelogio).executar({ endereco: 'b', relogio: { fator: 60 } });

    expect(alterado.isLeft()).toBe(true);
    expect(repositorio.alterar).toHaveBeenCalledWith('b', { nome: 'x' });
    expect(repositorio.ligarAnomalia).toHaveBeenCalledWith('b', 'n', true);
    expect(repositorio.mudarRelogio).toHaveBeenCalledWith('b', { fator: 60 });
    expect(casos.size).toBe(Object.keys(TOKENS_DAS_INSTITUICOES).length);
  });

  it('obter ambiente repassa ao repositório', async () => {
    const caso = new ObterAmbiente({ obter: jest.fn().mockResolvedValue(right(new Ambiente('hml'))) });
    const resultado = await caso.executar();
    expect(resultado.isRight() && resultado.value.marca).toBe('hml');
  });
});
