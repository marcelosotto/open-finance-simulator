// Falha esperada que a tela mostra: nunca uma exceção (guia de front-end, Either).
export type TipoDeFalha = 'rede' | 'servidor' | 'recusa' | 'inexistente' | 'conflito' | 'validacao';

export interface Falha {
  readonly tipo: TipoDeFalha;
  readonly status?: number;
  readonly erros: readonly string[];
}

export const falhaDeValidacao = (...erros: string[]): Falha => ({ tipo: 'validacao', erros });

export const falhaDeRede = (): Falha => ({ tipo: 'rede', erros: ['SEM_CONEXAO'] });

/** Traduz o status e o corpo `{ erros }` da administração. */
export const falhaDaResposta = (status: number, erros: readonly string[]): Falha => {
  const lista = erros.length > 0 ? erros : [`HTTP_${status}`];
  if (status === 404) return { tipo: 'inexistente', status, erros: lista };
  if (status === 409) return { tipo: 'conflito', status, erros: lista };
  if (status >= 400 && status < 500) return { tipo: 'recusa', status, erros: lista };
  return { tipo: 'servidor', status, erros: lista };
};
