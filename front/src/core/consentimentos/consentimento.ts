import { left, right, type Either } from '../comum/either';
import { falhaDeValidacao, type Falha } from '../comum/falha';

// Menu de consentimento do painel (UI10, UI13, UI14), sobre a API da 008.
export type TipoDeCliente = 'PF' | 'PJ';

export interface GrupoDePermissoes {
  readonly nome: string;
  readonly permissoes: readonly string[];
}

export interface OrigemDoMenu {
  readonly resourceId: string;
  readonly marcada: boolean;
}

export interface Receptora {
  readonly organisationId: string;
  readonly nome: string;
  /** Credenciais de teste (RN123): vêm na lista da administração, não nas opções do menu. */
  readonly usuario?: string;
  readonly senha?: string;
}

export interface ReceptoraCriada extends Receptora {
  readonly usuario: string;
  readonly senha: string;
}

export interface OpcoesDoMenu {
  readonly cliente: string;
  readonly tipo: TipoDeCliente;
  readonly banco: string;
  readonly semProduto: boolean;
  readonly grupos: readonly GrupoDePermissoes[];
  readonly contas: readonly OrigemDoMenu[];
  readonly cartoes: readonly OrigemDoMenu[];
  readonly agrupados: readonly string[];
  readonly receptoras: readonly Receptora[];
}

export const PERMISSAO_DE_RECURSOS = 'RESOURCES_READ';
const PERMISSAO_DE_CONTAS = 'ACCOUNTS_READ';
const PERMISSAO_DE_CARTOES = 'CREDIT_CARDS_ACCOUNTS_READ';

export type Origem = 'contas' | 'cartoes';

/**
 * O que Bruno escolheu. As origens guardam só o que ele inverteu em relação ao `marcada` da API,
 * para a tela não copiar a resposta da API (guia, estado).
 */
export interface Rascunho {
  readonly cliente: string;
  readonly banco: string;
  readonly receptora: string;
  readonly permissoes: readonly string[];
  readonly invertidas: readonly string[];
  readonly prazoMeses: string;
  readonly aprovacoesPendentes: string;
}

export const rascunhoVazio: Rascunho = {
  cliente: '',
  banco: '',
  receptora: '',
  permissoes: [],
  invertidas: [],
  prazoMeses: '',
  aprovacoesPendentes: '',
};

export type EstadoDoGrupo = 'marcado' | 'parcial' | 'desmarcado';

export const estadoDoGrupo = (rascunho: Rascunho, grupo: GrupoDePermissoes): EstadoDoGrupo => {
  const presentes = grupo.permissoes.filter((p) => rascunho.permissoes.includes(p)).length;
  if (presentes === grupo.permissoes.length) return 'marcado';
  return presentes === 0 ? 'desmarcado' : 'parcial';
};

/** Liga o grupo inteiro, ou desliga o que só ele usa entre os grupos marcados. */
export const alternarGrupo = (
  rascunho: Rascunho,
  grupo: GrupoDePermissoes,
  grupos: readonly GrupoDePermissoes[],
): Rascunho => {
  if (estadoDoGrupo(rascunho, grupo) !== 'marcado') {
    return { ...rascunho, permissoes: [...new Set([...rascunho.permissoes, ...grupo.permissoes])] };
  }
  const deOutros = new Set(
    grupos
      .filter((g) => g.nome !== grupo.nome && estadoDoGrupo(rascunho, g) === 'marcado')
      .flatMap((g) => g.permissoes),
  );
  return {
    ...rascunho,
    permissoes: rascunho.permissoes.filter((p) => !grupo.permissoes.includes(p) || deOutros.has(p)),
  };
};

/** Permite montar grupo incompleto; quem recusa é a API, com o código oficial (RN83, Q6). */
export const alternarPermissao = (rascunho: Rascunho, permissao: string): Rascunho => ({
  ...rascunho,
  permissoes: rascunho.permissoes.includes(permissao)
    ? rascunho.permissoes.filter((p) => p !== permissao)
    : [...rascunho.permissoes, permissao],
});

export const alternarOrigem = (rascunho: Rascunho, resourceId: string): Rascunho => ({
  ...rascunho,
  invertidas: rascunho.invertidas.includes(resourceId)
    ? rascunho.invertidas.filter((id) => id !== resourceId)
    : [...rascunho.invertidas, resourceId],
});

export const origemMarcada = (rascunho: Rascunho, origem: OrigemDoMenu): boolean =>
  origem.marcada !== rascunho.invertidas.includes(origem.resourceId);

const pedeOrigem = (rascunho: Rascunho, origem: Origem): boolean =>
  rascunho.permissoes.includes(origem === 'contas' ? PERMISSAO_DE_CONTAS : PERMISSAO_DE_CARTOES);

/** A seção de origem só aparece quando há opção e o grupo foi pedido (UI13). */
export const mostraOrigem = (rascunho: Rascunho, opcoes: OpcoesDoMenu, origem: Origem): boolean =>
  opcoes[origem].length > 0 && pedeOrigem(rascunho, origem);

const selecionadas = (rascunho: Rascunho, opcoes: OpcoesDoMenu, origem: Origem): string[] =>
  opcoes[origem].filter((o) => origemMarcada(rascunho, o)).map((o) => o.resourceId);

/** Com opção e o grupo pedido, pelo menos uma origem fica marcada (UI13, RN95). */
export const origensFaltando = (rascunho: Rascunho, opcoes: OpcoesDoMenu): Origem[] =>
  (['contas', 'cartoes'] as const).filter(
    (origem) => mostraOrigem(rascunho, opcoes, origem) && selecionadas(rascunho, opcoes, origem).length === 0,
  );

export interface PedidoDeConsentimento {
  readonly cliente: string;
  readonly banco: string;
  readonly receptora: string;
  readonly permissoes: readonly string[];
  readonly contas?: readonly string[];
  readonly cartoes?: readonly string[];
  readonly prazoMeses?: number;
  readonly aprovacoesPendentes?: number;
}

const inteiroNaFaixa = (texto: string, minimo: number, maximo: number): number | null => {
  const valor = Number(texto);
  return Number.isInteger(valor) && valor >= minimo && valor <= maximo ? valor : null;
};

/**
 * Monta o corpo de `POST /admin/consentimentos`. Valida só as faixas declaradas no contrato e a regra de tela da
 * origem; as regras de permissão ficam com a API (FR-007). `RESOURCES_READ` entra sempre (RN96).
 */
export const montarPedido = (rascunho: Rascunho, opcoes: OpcoesDoMenu): Either<Falha, PedidoDeConsentimento> => {
  const erros: string[] = [];
  if (rascunho.receptora === '') erros.push('RECEPTORA_OBRIGATORIA');
  if (rascunho.permissoes.length === 0) erros.push('PERMISSOES_OBRIGATORIAS');
  if (origensFaltando(rascunho, opcoes).length > 0) erros.push('SEM_ORIGEM_SELECIONADA');
  const prazo = rascunho.prazoMeses.trim() === '' ? undefined : inteiroNaFaixa(rascunho.prazoMeses, 1, 12);
  if (prazo === null) erros.push('PRAZO_INVALIDO');
  const temAlcada = opcoes.tipo === 'PJ' && rascunho.aprovacoesPendentes.trim() !== '';
  const alcadas = temAlcada ? inteiroNaFaixa(rascunho.aprovacoesPendentes, 1, 10) : undefined;
  if (alcadas === null) erros.push('ALCADA_INVALIDA');
  if (erros.length > 0) return left(falhaDeValidacao(...erros));

  return right({
    cliente: rascunho.cliente,
    banco: rascunho.banco,
    receptora: rascunho.receptora,
    permissoes: [...new Set([...rascunho.permissoes, PERMISSAO_DE_RECURSOS])],
    ...(mostraOrigem(rascunho, opcoes, 'contas') ? { contas: selecionadas(rascunho, opcoes, 'contas') } : {}),
    ...(mostraOrigem(rascunho, opcoes, 'cartoes') ? { cartoes: selecionadas(rascunho, opcoes, 'cartoes') } : {}),
    ...(typeof prazo === 'number' ? { prazoMeses: prazo } : {}),
    ...(typeof alcadas === 'number' ? { aprovacoesPendentes: alcadas } : {}),
  });
};

export type SituacaoDaAlcada = 'sem-alcada' | 'pendente' | 'aprovada' | 'recusada' | 'vencida' | 'encerrada';

export interface VisaoDoConsentimento {
  readonly consentId: string;
  readonly cliente: string;
  readonly banco: string;
  readonly receptora: string | null;
  readonly estado: string;
  readonly permissoes: readonly string[];
  readonly criadoEm: string;
  readonly expiraEm: string | null;
  readonly rejeitadoPor: string | null;
  readonly razao: string | null;
  readonly alcada: {
    readonly situacao: string;
    readonly aprovacoesPendentes: number;
    readonly prazo: string | null;
  } | null;
  readonly decisoes: readonly { readonly autor: string; readonly decisao: string; readonly em: string }[];
  readonly recursos: readonly { readonly resourceId: string; readonly tipo: string; readonly estado: string }[];
}

/** Aprovar e recusar só com pendência válida; a visão admin já zera pendências em REJECTED (UI14, RN99). */
export const podeDecidir = (visao: VisaoDoConsentimento): boolean =>
  visao.estado !== 'REJECTED' && visao.alcada?.situacao === 'pendente' && visao.alcada.aprovacoesPendentes > 0;

export const podeRevogar = (visao: VisaoDoConsentimento): boolean => visao.estado !== 'REJECTED';

export interface RepositorioDeConsentimentos {
  opcoes(cliente: string, banco: string): Promise<Either<Falha, OpcoesDoMenu>>;
  criar(pedido: PedidoDeConsentimento): Promise<Either<Falha, VisaoDoConsentimento>>;
  obter(consentId: string): Promise<Either<Falha, VisaoDoConsentimento>>;
  decidir(consentId: string, decisao: 'aprovar' | 'recusar'): Promise<Either<Falha, VisaoDoConsentimento>>;
  revogar(consentId: string): Promise<Either<Falha, true>>;
  receptoras(): Promise<Either<Falha, readonly Receptora[]>>;
  criarReceptora(nome: string): Promise<Either<Falha, ReceptoraCriada>>;
}
