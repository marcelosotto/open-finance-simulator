// Retorno de todo caso de uso: Left para falha esperada, Right para sucesso (guia de front-end, Either).
export class Left<L> {
  readonly tipo = 'left' as const;
  constructor(readonly value: L) {}
  isLeft(): this is Left<L> {
    return true;
  }
  isRight(): false {
    return false;
  }
}

export class Right<R> {
  readonly tipo = 'right' as const;
  constructor(readonly value: R) {}
  isLeft(): false {
    return false;
  }
  isRight(): this is Right<R> {
    return true;
  }
}

export type Either<L, R> = Left<L> | Right<R>;

export const left = <L, R = never>(valor: L): Either<L, R> => new Left(valor);
export const right = <R, L = never>(valor: R): Either<L, R> => new Right(valor);

/** Aplica `f` só no sucesso. */
export const mapear = <L, R, S>(e: Either<L, R>, f: (valor: R) => S): Either<L, S> =>
  e.isLeft() ? left(e.value) : right(f(e.value));
