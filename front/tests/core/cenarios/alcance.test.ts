import { agruparPorAlcance, alcance } from '@/core/cenarios/alcance';
import { aceitaCampo, aceitaQuantidade } from '@/core/cenarios/regra';
import { regra } from './regras';

describe('alcance da regra (data-model da 010)', () => {
  it.each([
    [{ recurso: 'acc-1', cliente: '76109277673', endpoint: 'accountsGetAccounts' }, 'recurso'],
    [{ cliente: '76109277673', endpoint: 'accountsGetAccounts' }, 'cliente'],
    [{ endpoint: 'accountsGetAccounts' }, 'endpoint'],
    [{}, 'instituicao'],
    [{ instituicao: null }, 'instituicao'],
  ])('%o é de alcance %s', (filtros, esperado) => {
    expect(alcance(regra(filtros))).toBe(esperado);
  });

  it('agrupa na ordem recurso, cliente, endpoint, instituição, só os grupos com regra', () => {
    const banco = regra({ id: 'r-banco' });
    const endpoint = regra({ id: 'r-endpoint', endpoint: 'accountsGetAccounts' });
    const recurso = regra({ id: 'r-recurso', recurso: 'acc-1' });
    const outroBanco = regra({ id: 'r-banco-2', instituicao: 'banco-instavel' });

    const grupos = agruparPorAlcance([banco, endpoint, recurso, outroBanco]);

    expect(grupos.map((g) => g.alcance)).toEqual(['recurso', 'endpoint', 'instituicao']);
    expect(grupos[2]?.regras.map((r) => r.id)).toEqual(['r-banco', 'r-banco-2']);
  });

  it('lista vazia não tem grupo', () => {
    expect(agruparPorAlcance([])).toEqual([]);
  });

  it('só os defeitos de campo aceitam campo, e só dado-muda-na-paginacao aceita quantidade', () => {
    expect(aceitaCampo('campo-obrigatorio-ausente')).toBe(true);
    expect(aceitaCampo('total-records-incoerente')).toBe(false);
    expect(aceitaQuantidade('dado-muda-na-paginacao')).toBe(true);
    expect(aceitaQuantidade('formato-invalido')).toBe(false);
  });
});
