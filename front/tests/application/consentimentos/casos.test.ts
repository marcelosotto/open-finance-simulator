import { right } from '@/core/comum/either';
import {
  rascunhoVazio,
  type OpcoesDoMenu,
  type RepositorioDeConsentimentos,
} from '@/core/consentimentos/consentimento';
import {
  casosDosConsentimentos,
  CriarConsentimento,
  CriarReceptora,
  ObterOpcoesDoMenu,
  TOKENS_DOS_CONSENTIMENTOS,
} from '@/application/consentimentos/casos';

const repositorioFalso = (): jest.Mocked<RepositorioDeConsentimentos> => ({
  opcoes: jest.fn().mockResolvedValue(right({})),
  criar: jest.fn().mockResolvedValue(right({ consentId: 'urn:hml:p-c0x0' })),
  obter: jest.fn().mockResolvedValue(right({})),
  decidir: jest.fn().mockResolvedValue(right({})),
  revogar: jest.fn().mockResolvedValue(right(true)),
  receptoras: jest.fn().mockResolvedValue(right([])),
  criarReceptora: jest.fn().mockResolvedValue(right({})),
});

const opcoes: OpcoesDoMenu = {
  cliente: '1',
  tipo: 'PF',
  banco: 'b',
  semProduto: false,
  grupos: [],
  contas: [],
  cartoes: [],
  agrupados: [],
  receptoras: [],
};

describe('Casos de uso do consentimento', () => {
  it('opções exigem cliente e banco', async () => {
    const repositorio = repositorioFalso();
    const caso = new ObterOpcoesDoMenu(repositorio);
    expect((await caso.executar({ cliente: ' ', banco: 'b' })).value).toEqual({
      tipo: 'validacao',
      erros: ['PEDIDO_INVALIDO'],
    });
    expect((await caso.executar({ cliente: '1', banco: '' })).isLeft()).toBe(true);
    await caso.executar({ cliente: ' 1 ', banco: 'b' });
    expect(repositorio.opcoes).toHaveBeenCalledWith('1', 'b');
  });

  it('criar não chama a API com rascunho inválido e chama com o pedido montado', async () => {
    const repositorio = repositorioFalso();
    const caso = new CriarConsentimento(repositorio);
    expect((await caso.executar({ rascunho: rascunhoVazio, opcoes })).isLeft()).toBe(true);
    expect(repositorio.criar).not.toHaveBeenCalled();

    const rascunho = { ...rascunhoVazio, cliente: '1', banco: 'b', receptora: 'org', permissoes: ['ACCOUNTS_READ'] };
    expect((await caso.executar({ rascunho, opcoes })).isRight()).toBe(true);
    expect(repositorio.criar).toHaveBeenCalledWith(
      expect.objectContaining({ permissoes: ['ACCOUNTS_READ', 'RESOURCES_READ'] }),
    );
  });

  it('receptora exige nome', async () => {
    const repositorio = repositorioFalso();
    const caso = new CriarReceptora(repositorio);
    expect((await caso.executar('  ')).isLeft()).toBe(true);
    await caso.executar(' Coletor ');
    expect(repositorio.criarReceptora).toHaveBeenCalledWith('Coletor');
  });

  it('os demais casos repassam ao repositório', async () => {
    const repositorio = repositorioFalso();
    const casos = new Map<symbol, unknown>(casosDosConsentimentos(repositorio));
    type Executa = { executar: (...args: unknown[]) => Promise<unknown> };
    const caso = (token: symbol) => casos.get(token) as Executa;

    await caso(TOKENS_DOS_CONSENTIMENTOS.Obter).executar('id');
    await caso(TOKENS_DOS_CONSENTIMENTOS.Decidir).executar({ consentId: 'id', decisao: 'recusar' });
    await caso(TOKENS_DOS_CONSENTIMENTOS.Revogar).executar('id');
    await caso(TOKENS_DOS_CONSENTIMENTOS.Receptoras).executar();

    expect(repositorio.obter).toHaveBeenCalledWith('id');
    expect(repositorio.decidir).toHaveBeenCalledWith('id', 'recusar');
    expect(repositorio.revogar).toHaveBeenCalledWith('id');
    expect(repositorio.receptoras).toHaveBeenCalled();
    expect(casos.size).toBe(Object.keys(TOKENS_DOS_CONSENTIMENTOS).length);
  });
});
