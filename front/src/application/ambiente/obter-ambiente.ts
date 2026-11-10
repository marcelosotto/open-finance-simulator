import type { Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import type { Ambiente, RepositorioDoAmbiente } from '@/core/ambiente/ambiente';
import { token } from '../tokens';

export class ObterAmbiente {
  constructor(private readonly repositorio: RepositorioDoAmbiente) {}

  executar(): Promise<Either<Falha, Ambiente>> {
    return this.repositorio.obter();
  }
}

export const TOKENS_DO_AMBIENTE = { ObterAmbiente: token<ObterAmbiente>('ObterAmbiente') };
