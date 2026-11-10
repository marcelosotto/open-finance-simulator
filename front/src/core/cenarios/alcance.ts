import type { RegraDeCenario } from './regra';

/** Do filtro mais específico ao mais amplo; é só a ordem da lista (UI06). */
export const ALCANCES = ['recurso', 'cliente', 'endpoint', 'instituicao'] as const;
export type Alcance = (typeof ALCANCES)[number];

type Filtros = Pick<RegraDeCenario, 'instituicao' | 'endpoint' | 'cliente' | 'recurso'>;

/**
 * O filtro mais específico informado, só para agrupar a lista (data-model da 010).
 * Não diz quem vence: a vencedora vem da administração (Q9), que segue RN15.
 */
export const alcance = (regra: Filtros): Alcance => {
  if (regra.recurso !== null) return 'recurso';
  if (regra.cliente !== null) return 'cliente';
  if (regra.endpoint !== null) return 'endpoint';
  return 'instituicao';
};

export interface GrupoDeRegras {
  readonly alcance: Alcance;
  readonly regras: readonly RegraDeCenario[];
}

/** Os grupos com alguma regra, na ordem recurso, cliente, endpoint, instituição, mantendo a ordem da API dentro de cada um. */
export const agruparPorAlcance = (regras: readonly RegraDeCenario[]): readonly GrupoDeRegras[] =>
  ALCANCES.map((a) => ({ alcance: a, regras: regras.filter((regra) => alcance(regra) === a) })).filter(
    (grupo) => grupo.regras.length > 0,
  );
