import { left, right, type Either } from '../comum/either';
import { falhaDeValidacao, type Falha } from '../comum/falha';

// Instituição simulada como a administração devolve (contrato da 002, data-model da 010).
export type Situacao = 'ativa' | 'inativa';
export type Publico = 'PF' | 'PJ' | 'ambos';
export type Classe = 'alta' | 'media-alta' | 'media' | 'baixa';

export const CLASSES: readonly Classe[] = ['alta', 'media-alta', 'media', 'baixa'];

export interface ResumoDeInstituicao {
  readonly endereco: string;
  readonly nome: string;
  readonly perfil: string;
  readonly situacao: Situacao;
  readonly anomaliasLigadas: readonly string[];
}

/** O aviso da lista aparece quando há alguma anomalia ligada (UI02, RN114). */
export const temAnomalia = (instituicao: Pick<ResumoDeInstituicao, 'anomaliasLigadas'>): boolean =>
  instituicao.anomaliasLigadas.length > 0;

export interface LatenciaDaClasse {
  readonly tipicoMs: number;
  readonly variacaoMs: number;
  readonly caudaLentaPct: number;
}

export interface Comportamento {
  readonly latenciaPorClasse: Readonly<Record<string, LatenciaDaClasse>>;
  readonly taxaDeErroPct: number;
  readonly pesosPorStatus: Readonly<Record<string, number>>;
  readonly recursoIndisponivelPct: number;
  readonly janela: { readonly duracaoMin: number; readonly intervaloMin: number } | null;
}

export interface Limites {
  readonly qca: number;
  readonly qcaPorReceptora: Readonly<Record<string, number>>;
  readonly tpm: {
    readonly alta: number | null;
    readonly mediaAlta: number;
    readonly media: number;
    readonly baixa: number;
  };
  readonly limiteOperacionalLigado: boolean;
  readonly limiteMensal: {
    readonly alta: number;
    readonly saldosELimites: number;
    readonly mediaAlta: number;
    readonly media: number;
    readonly baixa: number;
  };
  readonly capacidadeTps: number;
  readonly pageSizeMaximo: number;
  readonly validadeDaChaveMin: number;
  readonly retryAfterLigado: boolean;
  readonly retryAfterSeg: number;
  readonly preparacaoDeRecursosSeg: number;
}

export interface Relogio {
  readonly baseSimulada: string;
  readonly baseReal: string;
  readonly fator: number;
}

/** Fator 0 é relógio parado na hora fixada (UC28). */
export const relogioFixado = (relogio: Relogio): boolean => relogio.fator === 0;

export interface SlaDaClasse {
  readonly classe: string;
  readonly slaMs: number;
  readonly p95Ms: number;
  readonly dentroDoSla: boolean;
}

export interface AvisoDeLimite {
  readonly campo: string;
  readonly oficial: number;
  readonly configurado: number;
}

export interface Instituicao extends ResumoDeInstituicao {
  readonly organisationId: string;
  readonly marca: string;
  readonly publico: Publico;
  readonly produtos: readonly string[];
  readonly pronta: boolean;
  readonly comportamento: Comportamento;
  readonly limites: Limites;
  readonly relogio: Relogio | null;
  readonly sla: readonly SlaDaClasse[];
  readonly avisos: readonly AvisoDeLimite[];
}

/** O aviso de um campo de limite, se ele estiver abaixo do oficial (UI05). */
export const avisoDoCampo = (instituicao: Pick<Instituicao, 'avisos'>, campo: string): AvisoDeLimite | undefined =>
  instituicao.avisos.find((aviso) => aviso.campo === campo);

export interface NovaInstituicao {
  readonly nome: string;
  readonly perfil: string;
}

/** Só nome e perfil (UI03, RN03); o resto vem do servidor. */
export const novaInstituicao = (nome: string, perfil: string): Either<Falha, NovaInstituicao> => {
  const aparado = nome.trim();
  if (aparado.length === 0) return left(falhaDeValidacao('NOME_OBRIGATORIO'));
  if (perfil.trim().length === 0) return left(falhaDeValidacao('PERFIL_OBRIGATORIO'));
  return right({ nome: aparado, perfil });
};

/** Campos que o PATCH aceita (JSON Merge Patch do contrato da 002). */
export interface AlteracaoDeInstituicao {
  readonly nome?: string;
  readonly marca?: string;
  readonly publico?: Publico;
  readonly produtos?: readonly string[];
  readonly comportamento?: Partial<Comportamento>;
  readonly limites?: Partial<Limites>;
}

export type ResultadoDaRemocao = 'apagada' | 'inativada';

export interface Mudanca {
  readonly campo: string;
  readonly antes: string | null;
  readonly depois: string | null;
}

export interface EntradaDeHistorico {
  readonly data: string;
  readonly autor: string;
  readonly acao: string;
  readonly mudancas: readonly Mudanca[];
}

export interface EstadoDaAnomalia {
  readonly nome: string;
  readonly descricao: string;
  readonly ligada: boolean;
}

export type PedidoDeRelogio = { readonly fixadoEm: string } | { readonly fator: number };

export interface RepositorioDeInstituicoes {
  listar(): Promise<Either<Falha, readonly ResumoDeInstituicao[]>>;
  obter(endereco: string): Promise<Either<Falha, Instituicao>>;
  perfis(): Promise<Either<Falha, readonly string[]>>;
  criar(nova: NovaInstituicao): Promise<Either<Falha, Instituicao>>;
  alterar(endereco: string, alteracao: AlteracaoDeInstituicao): Promise<Either<Falha, Instituicao>>;
  apagar(endereco: string): Promise<Either<Falha, ResultadoDaRemocao>>;
  reativar(endereco: string): Promise<Either<Falha, Instituicao>>;
  historico(endereco: string): Promise<Either<Falha, readonly EntradaDeHistorico[]>>;
  anomalias(endereco: string): Promise<Either<Falha, readonly EstadoDaAnomalia[]>>;
  ligarAnomalia(endereco: string, nome: string, ligada: boolean): Promise<Either<Falha, EstadoDaAnomalia>>;
  mudarRelogio(endereco: string, pedido: PedidoDeRelogio | null): Promise<Either<Falha, Instituicao>>;
}
