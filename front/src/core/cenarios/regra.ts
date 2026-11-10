import type { Either } from '../comum/either';
import type { Falha } from '../comum/falha';

// Regra de cenário como a administração devolve (data-model da 007, efeito `defeito` da 009, data-model da 010).
export const TIPOS_DE_EFEITO = [
  'latencia',
  'status',
  'timeout',
  'indisponibilidade',
  'transporte',
  'estado',
  'defeito',
] as const;
export type TipoDeEfeito = (typeof TIPOS_DE_EFEITO)[number];

export const RESULTADOS_DE_INDISPONIBILIDADE = ['503', '504', 'corte'] as const;
export const FALHAS_DE_TRANSPORTE = ['conexao-cortada', 'corpo-truncado', 'json-invalido'] as const;
export const ESTADOS_DE_RECURSO = [
  'AVAILABLE',
  'TEMPORARILY_UNAVAILABLE',
  'UNAVAILABLE',
  'PENDING_AUTHORISATION',
] as const;

/** Os defeitos que aceitam `campo` (RN117); o catálogo da administração só traz nome e descrição. */
export const DEFEITOS_DE_CAMPO: readonly string[] = ['campo-obrigatorio-ausente', 'formato-invalido', 'enum-invalido'];

/** O único defeito que aceita `quantidade` (RN115). */
export const DEFEITO_COM_QUANTIDADE = 'dado-muda-na-paginacao';

export const QUANTIDADE_MAXIMA = 1000;

export const aceitaCampo = (defeito: string): boolean => DEFEITOS_DE_CAMPO.includes(defeito);

export const aceitaQuantidade = (defeito: string): boolean => defeito === DEFEITO_COM_QUANTIDADE;

export interface EfeitoDaRegra {
  readonly tipo: string;
  readonly status: number | null;
  readonly latenciaMs: number | null;
  readonly esperaMs: number | null;
  readonly resultado: string | null;
  readonly falha: string | null;
  readonly estado: string | null;
  readonly defeito: string | null;
  readonly campo: string | null;
  readonly quantidade: number | null;
}

export interface RegraDeCenario {
  readonly id: string;
  readonly nome: string | null;
  readonly instituicao: string | null;
  readonly endpoint: string | null;
  readonly cliente: string | null;
  readonly recurso: string | null;
  readonly efeito: EfeitoDaRegra;
  readonly frequenciaPct: number;
  readonly inicio: string | null;
  readonly fim: string | null;
  readonly semente: number | null;
  readonly ligada: boolean;
  readonly pacote: string | null;
  readonly criadaEm: string;
  readonly atualizadaEm: string;
}

export interface PacoteDeCenario {
  readonly nome: string;
  readonly ligado: boolean;
  readonly criadoEm: string;
}

/** A regra que venceria em cada grupo, na hora do banco (UI06, Q9). Vem pronta da administração. */
export interface Vencedoras {
  readonly instante: string;
  readonly comportamento: RegraDeCenario | null;
  readonly estado: RegraDeCenario | null;
  readonly defeito: RegraDeCenario | null;
}

export interface ItemDoCatalogo {
  readonly nome: string;
  readonly descricao: string;
}

/** Efeito do corpo de criação e alteração: só os campos do tipo. */
export interface EfeitoDoPedido {
  readonly tipo: TipoDeEfeito;
  readonly status?: number;
  readonly latenciaMs?: number;
  readonly esperaMs?: number;
  readonly resultado?: string;
  readonly falha?: string;
  readonly estado?: string;
  readonly defeito?: string;
  readonly campo?: string;
  readonly quantidade?: number;
}

/** Corpo de `POST` e `PUT /admin/cenarios/regras`: campos da regra, exceto os gerados; vazios ficam de fora. */
export interface PedidoDeRegra {
  readonly nome?: string;
  readonly instituicao?: string;
  readonly endpoint?: string;
  readonly cliente?: string;
  readonly recurso?: string;
  readonly efeito: EfeitoDoPedido;
  readonly frequenciaPct?: number;
  readonly inicio?: string;
  readonly fim?: string;
  readonly semente?: number;
  readonly ligada: boolean;
  readonly pacote?: string;
}

export interface PedidoDeVencedora {
  readonly instituicao: string;
  readonly endpoint: string;
  readonly cliente?: string;
  readonly recurso?: string;
}

export interface RepositorioDeCenarios {
  listarRegras(): Promise<Either<Falha, readonly RegraDeCenario[]>>;
  obterRegra(id: string): Promise<Either<Falha, RegraDeCenario>>;
  criarRegra(pedido: PedidoDeRegra): Promise<Either<Falha, RegraDeCenario>>;
  alterarRegra(id: string, pedido: PedidoDeRegra): Promise<Either<Falha, RegraDeCenario>>;
  apagarRegra(id: string): Promise<Either<Falha, void>>;
  ligarRegra(id: string, ligada: boolean): Promise<Either<Falha, RegraDeCenario>>;
  defeitos(): Promise<Either<Falha, readonly ItemDoCatalogo[]>>;
  operacoes(): Promise<Either<Falha, readonly string[]>>;
  listarPacotes(): Promise<Either<Falha, readonly PacoteDeCenario[]>>;
  criarPacote(nome: string): Promise<Either<Falha, PacoteDeCenario>>;
  ligarPacote(nome: string, ligado: boolean): Promise<Either<Falha, PacoteDeCenario>>;
  apagarPacote(nome: string): Promise<Either<Falha, void>>;
  vencedoras(pedido: PedidoDeVencedora): Promise<Either<Falha, Vencedoras>>;
}
