import { right } from '@/core/comum/either';
import type { RepositorioDaMassa } from '@/core/massa/massa';
import { casosDaMassa, ClientesDoBanco, ResumoDaMassaPersistida, TOKENS_DA_MASSA } from '@/application/massa/casos';

const pagina = { clientes: [], inicio: 0, quantidade: 10, temMais: false };

const repositorio = () => {
  const clientesDoBanco = jest.fn().mockResolvedValue(right(pagina));
  const resumo = jest.fn().mockResolvedValue(right({ clientes: 3, consentimentos: 1, recursos: 5, lotes: 1 }));
  return {
    falso: { clientesDoBanco, resumo },
    repositorio: { clientesDoBanco, resumo } as unknown as RepositorioDaMassa,
  };
};

describe('Clientes do banco e resumo (UI18, UI19)', () => {
  it('repassa banco aparado e página válida; banco vazio vira todos', async () => {
    const { falso, repositorio: r } = repositorio();
    const caso = new ClientesDoBanco(r);

    expect((await caso.executar({ banco: ' banco-estavel ', pagina: { inicio: 0, quantidade: 1000 } })).value).toEqual(
      pagina,
    );
    expect(falso.clientesDoBanco).toHaveBeenCalledWith('banco-estavel', { inicio: 0, quantidade: 1000 });

    await caso.executar({ banco: '', pagina: { inicio: 5, quantidade: 1 } });
    await caso.executar({ banco: null, pagina: { inicio: 0, quantidade: 1 } });
    expect(falso.clientesDoBanco).toHaveBeenNthCalledWith(2, null, { inicio: 5, quantidade: 1 });
    expect(falso.clientesDoBanco).toHaveBeenNthCalledWith(3, null, { inicio: 0, quantidade: 1 });
  });

  it('recusa página fora da faixa sem chamar a administração', async () => {
    const { falso, repositorio: r } = repositorio();
    const caso = new ClientesDoBanco(r);
    expect((await caso.executar({ banco: 'b', pagina: { inicio: 0, quantidade: 1001 } })).value).toEqual({
      tipo: 'validacao',
      erros: ['PAGINA_INVALIDA'],
    });
    expect((await caso.executar({ banco: 'b', pagina: { inicio: -1, quantidade: 1 } })).isLeft()).toBe(true);
    expect(falso.clientesDoBanco).not.toHaveBeenCalled();
  });

  it('resumo e registro no container', async () => {
    const { repositorio: r } = repositorio();
    expect((await new ResumoDaMassaPersistida(r).executar()).value).toEqual({
      clientes: 3,
      consentimentos: 1,
      recursos: 5,
      lotes: 1,
    });
    const casos = new Map<symbol, unknown>(casosDaMassa(r));
    expect(casos.get(TOKENS_DA_MASSA.ClientesDoBanco)).toBeInstanceOf(ClientesDoBanco);
    expect(casos.get(TOKENS_DA_MASSA.Resumo)).toBeInstanceOf(ResumoDaMassaPersistida);
  });
});
