import { left, mapear, right } from '@/core/comum/either';
import { falhaDaResposta, falhaDeRede, falhaDeValidacao } from '@/core/comum/falha';

describe('Either', () => {
  it('Left e Right dizem o que são', () => {
    const erro = left<string, number>('x');
    const ok = right<number, string>(1);
    expect(erro.isLeft()).toBe(true);
    expect(erro.isRight()).toBe(false);
    expect(ok.isRight()).toBe(true);
    expect(ok.isLeft()).toBe(false);
  });

  it('mapear transforma só o sucesso', () => {
    expect(mapear(right<number, string>(2), (n) => n * 3).value).toBe(6);
    expect(mapear(left<string, number>('falhou'), (n) => n * 3).value).toBe('falhou');
  });
});

describe('Falha', () => {
  it('validação e rede', () => {
    expect(falhaDeValidacao('A', 'B')).toEqual({ tipo: 'validacao', erros: ['A', 'B'] });
    expect(falhaDeRede()).toEqual({ tipo: 'rede', erros: ['SEM_CONEXAO'] });
  });

  it.each([
    [404, 'inexistente'],
    [409, 'conflito'],
    [400, 'recusa'],
    [422, 'recusa'],
    [500, 'servidor'],
  ])('status %i vira %s', (status, tipo) => {
    expect(falhaDaResposta(status, ['X'])).toEqual({ tipo, status, erros: ['X'] });
  });

  it('sem corpo de erros, usa o status', () => {
    expect(falhaDaResposta(503, []).erros).toEqual(['HTTP_503']);
  });
});
