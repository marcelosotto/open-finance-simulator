import type { Either } from '../comum/either';
import type { Falha } from '../comum/falha';

// A marca vem da administração (UI01, RN72, Q2). Valor livre; vazio, a tela não mostra marca.
export type CorDaMarca = 'dev' | 'hml' | 'stage' | 'neutra';

const CORES: Readonly<Record<string, CorDaMarca>> = { dev: 'dev', hml: 'hml', stage: 'stage' };

export class Ambiente {
  readonly marca: string;

  constructor(marca: string) {
    this.marca = marca.trim();
  }

  get temMarca(): boolean {
    return this.marca.length > 0;
  }

  /** Cor fixa para dev, hml e stage; qualquer outro valor fica neutro. */
  get cor(): CorDaMarca {
    return CORES[this.marca.toLowerCase()] ?? 'neutra';
  }
}

export interface RepositorioDoAmbiente {
  obter(): Promise<Either<Falha, Ambiente>>;
}
