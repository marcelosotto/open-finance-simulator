import { left, right, type Either } from '../comum/either';
import { falhaDeValidacao, type Falha } from '../comum/falha';

/** "500:1, 504:2" ⇄ { "500": 1, "504": 2 }. Só o formato; o servidor confere os status declarados. */
export const lerPesos = (texto: string): Either<Falha, Record<string, number>> => {
  const pesos: Record<string, number> = {};
  for (const parte of texto
    .split(',')
    .map((p) => p.trim())
    .filter((p) => p.length > 0)) {
    const casou = /^(\d{3})\s*:\s*(\d+(?:\.\d+)?)$/.exec(parte);
    if (casou === null) return left(falhaDeValidacao('PESOS_INVALIDOS'));
    const [, status = '', peso = ''] = casou;
    pesos[status] = Number(peso);
  }
  return right(pesos);
};

export const escreverPesos = (pesos: Readonly<Record<string, number>>): string =>
  Object.entries(pesos)
    .map(([status, peso]) => `${status}:${peso}`)
    .join(', ');
