import { Ambiente } from '@/core/ambiente/ambiente';

describe('Ambiente (UI01, RN72)', () => {
  it.each([
    ['dev', 'dev'],
    ['HML', 'hml'],
    ['stage', 'stage'],
    ['qa-2', 'neutra'],
  ])('a marca %s tem a cor %s', (marca, cor) => {
    const ambiente = new Ambiente(marca);
    expect(ambiente.temMarca).toBe(true);
    expect(ambiente.cor).toBe(cor);
  });

  it('guarda a porta das APIs Open Finance, nula quando não informada', () => {
    expect(new Ambiente('hml', 8080).portaOpenFinance).toBe(8080);
    expect(new Ambiente('hml').portaOpenFinance).toBeNull();
  });

  it('marca vazia ou só espaços não mostra nada', () => {
    expect(new Ambiente('').temMarca).toBe(false);
    expect(new Ambiente('   ').temMarca).toBe(false);
    expect(new Ambiente(' hml ').marca).toBe('hml');
  });
});
