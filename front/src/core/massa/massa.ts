import { left, right, type Either } from '../comum/either';
import { falhaDeValidacao, type Falha } from '../comum/falha';

// Massa como a administração da 003 devolve (data-model da 010, seção Massa).

/** Perfis que nascem com o ambiente; o servidor recusa alterar ou apagar (RN55). */
export const PERFIS_PRONTOS: readonly string[] = ['cliente-comum', 'sem-movimento', 'alto-volume'];

export interface PerfilDeDados {
  readonly nome: string;
  readonly transacoesMin: number;
  readonly transacoesMax: number;
  readonly valorMin: number;
  readonly valorMax: number;
  readonly saldoInicial: number;
  readonly tiposDeLancamento: readonly string[];
  readonly produtos: readonly string[];
}

export const perfilPronto = (nome: string): boolean => PERFIS_PRONTOS.includes(nome);

const numerosDoPerfil = ['transacoesMin', 'transacoesMax', 'valorMin', 'valorMax', 'saldoInicial'] as const;

/** Nome obrigatório, perfil pronto recusado e números finitos não negativos; o resto o servidor confere. */
export const perfilParaSalvar = (perfil: PerfilDeDados): Either<Falha, PerfilDeDados> => {
  const nome = perfil.nome.trim();
  if (nome.length === 0) return left(falhaDeValidacao('NOME_OBRIGATORIO'));
  if (perfilPronto(nome)) return left(falhaDeValidacao('PERFIL_PRONTO'));
  if (numerosDoPerfil.some((campo) => !Number.isFinite(perfil[campo]) || perfil[campo] < 0)) {
    return left(falhaDeValidacao('NUMERO_INVALIDO'));
  }
  return right({ ...perfil, nome });
};

export type Porcentagens = Readonly<Record<string, number>>;

export interface Distribuicao {
  readonly bancos: Porcentagens;
  readonly perfis: Porcentagens;
  readonly produtos: Porcentagens;
  readonly pjPct: number;
}

export interface DistribuicaoNomeada {
  readonly nome: string;
  readonly distribuicao: Distribuicao;
}

// Arredonda para a soma de 33,3 + 33,3 + 33,4 dar 100, como no decimal do servidor.
const somar = (porcentagens: Porcentagens): number =>
  Math.round(Object.values(porcentagens).reduce((total, pct) => total + pct, 0) * 1e6) / 1e6;

export const somaDeBancos = (distribuicao: Pick<Distribuicao, 'bancos'>): number => somar(distribuicao.bancos);

export const somaDePerfis = (distribuicao: Pick<Distribuicao, 'perfis'>): number => somar(distribuicao.perfis);

export const porcentagemValida = (pct: number): boolean => Number.isFinite(pct) && pct >= 0 && pct <= 100;

/** Bancos e perfis somam 100; cada % de 0 a 100, inclusive produtos (sem soma) e pjPct (UI07, RN56). */
export const podeSalvar = (distribuicao: Distribuicao): boolean =>
  somaDeBancos(distribuicao) === 100 &&
  somaDePerfis(distribuicao) === 100 &&
  [distribuicao.bancos, distribuicao.perfis, distribuicao.produtos].every((grupo) =>
    Object.values(grupo).every(porcentagemValida),
  ) &&
  porcentagemValida(distribuicao.pjPct);

export const distribuicaoParaSalvar = (
  nome: string,
  distribuicao: Distribuicao,
): Either<Falha, DistribuicaoNomeada> => {
  const aparado = nome.trim();
  if (aparado.length === 0) return left(falhaDeValidacao('NOME_OBRIGATORIO'));
  if (!podeSalvar(distribuicao)) return left(falhaDeValidacao('DISTRIBUICAO_FORA_DE_100'));
  return right({ nome: aparado, distribuicao });
};

export type SituacaoDoLote = 'em_andamento' | 'concluido' | 'parcial';

export interface Lote {
  readonly id: string;
  readonly quantidade: number;
  readonly situacao: SituacaoDoLote;
  readonly processados: number;
  readonly consentimentos: number;
  readonly recursos: number;
  readonly data: string;
  readonly motivo: string | null;
}

export const emAndamento = (lote: Pick<Lote, 'situacao'>): boolean => lote.situacao === 'em_andamento';

export interface PedidoDeLote {
  readonly quantidade: number;
  readonly distribuicaoNome?: string;
  readonly consentimentosPorCliente?: number;
  readonly permissoes?: readonly string[];
  readonly semente?: number;
  readonly historicoMeses?: number;
}

const inteiroPositivo = (valor: number | undefined): boolean =>
  valor === undefined || (Number.isInteger(valor) && valor >= 1);

/** Só faixas: quantidade, consentimentos e meses inteiros positivos; semente inteira (contrato da 003). */
export const pedidoDeLote = (pedido: PedidoDeLote): Either<Falha, PedidoDeLote> => {
  if (!Number.isInteger(pedido.quantidade) || pedido.quantidade < 1) {
    return left(falhaDeValidacao('QUANTIDADE_INVALIDA'));
  }
  if (!inteiroPositivo(pedido.consentimentosPorCliente) || !inteiroPositivo(pedido.historicoMeses)) {
    return left(falhaDeValidacao('NUMERO_INVALIDO'));
  }
  if (pedido.semente !== undefined && !Number.isInteger(pedido.semente)) {
    return left(falhaDeValidacao('NUMERO_INVALIDO'));
  }
  return right(pedido);
};

export interface Pagina {
  readonly inicio: number;
  readonly quantidade: number;
}

export const paginaValida = (pagina: Pagina): boolean =>
  Number.isInteger(pagina.inicio) &&
  pagina.inicio >= 0 &&
  Number.isInteger(pagina.quantidade) &&
  pagina.quantidade >= 1;

export type TipoDePessoa = 'PF' | 'PJ';

export interface ClienteDaMassa {
  readonly documento: string;
  readonly tipo: TipoDePessoa;
  readonly nome: string;
  readonly marca: string;
  readonly natureza: string;
  readonly loteId: string | null;
}

export interface RecursoDaMassa {
  readonly resourceId: string;
  readonly banco: string;
  readonly tipo: string;
  readonly produto: string;
  readonly estado: string;
  readonly perfil: string;
}

export interface ConsentimentoDaMassa {
  readonly consentId: string;
  readonly banco: string;
  readonly estado: string;
  readonly permissoes: readonly string[];
  readonly criadoEm: string;
  readonly expiraEm: string | null;
}

export interface MassaDoCliente {
  readonly cliente: ClienteDaMassa;
  readonly consentimentos: readonly ConsentimentoDaMassa[];
  readonly recursos: readonly RecursoDaMassa[];
}

export interface PedidoDeCliente {
  readonly tipo: TipoDePessoa;
  readonly bancos: readonly string[];
  readonly perfil?: string;
  readonly nome?: string;
  readonly semente?: number;
}

/** Pelo menos um banco e semente inteira; `documento` não é aceito pelo contrato. */
export const pedidoDeCliente = (pedido: PedidoDeCliente): Either<Falha, PedidoDeCliente> => {
  const bancos = pedido.bancos.map((banco) => banco.trim()).filter((banco) => banco.length > 0);
  if (bancos.length === 0) return left(falhaDeValidacao('BANCO_OBRIGATORIO'));
  if (pedido.semente !== undefined && !Number.isInteger(pedido.semente)) {
    return left(falhaDeValidacao('NUMERO_INVALIDO'));
  }
  return right({ ...pedido, bancos });
};

export type AlvoDaLimpeza = 'lote' | 'instituicao' | 'ajustes-mundo' | 'tudo';

export const ALVOS_DA_LIMPEZA: readonly AlvoDaLimpeza[] = ['lote', 'instituicao', 'ajustes-mundo', 'tudo'];

/** Lote e instituição pedem o id; os outros alvos não. */
export const alvoPedeId = (alvo: AlvoDaLimpeza): boolean => alvo === 'lote' || alvo === 'instituicao';

export interface PedidoDeLimpeza {
  readonly alvo: AlvoDaLimpeza;
  readonly id?: string;
}

export const pedidoDeLimpeza = (alvo: AlvoDaLimpeza, id: string): Either<Falha, PedidoDeLimpeza> => {
  if (!alvoPedeId(alvo)) return right({ alvo });
  const aparado = id.trim();
  if (aparado.length === 0) return left(falhaDeValidacao('ID_OBRIGATORIO'));
  return right({ alvo, id: aparado });
};

/** O que será (prévia) ou foi (removido) apagado. */
export interface ResultadoDaLimpeza {
  readonly alvo: AlvoDaLimpeza;
  readonly id: string | null;
  readonly removido: boolean;
  readonly clientes: number;
  readonly consentimentos: number;
  readonly recursos: number;
  readonly ajustes: number;
  readonly lotes: number;
  readonly perfis: number;
  readonly instituicoes: number;
}

export const CONTAGENS_DA_LIMPEZA = [
  'clientes',
  'consentimentos',
  'recursos',
  'ajustes',
  'lotes',
  'perfis',
  'instituicoes',
] as const;

/** Uma página de clientes persistidos de um banco (UI19). */
export interface PaginaDeClientes {
  readonly clientes: readonly ClienteDaMassa[];
  readonly inicio: number;
  readonly quantidade: number;
  readonly temMais: boolean;
}

/** O que já existe na massa persistida, para Primeiros passos (UI18). */
export interface ResumoDaMassa {
  readonly clientes: number;
  readonly consentimentos: number;
  readonly recursos: number;
  readonly lotes: number;
}

/** Rótulo do cliente nas listas de escolha: nome, documento e PF ou PJ (UI19). */
export const rotuloDoCliente = (cliente: Pick<ClienteDaMassa, 'nome' | 'documento' | 'tipo'>): string =>
  `${cliente.nome} — ${cliente.documento} (${cliente.tipo})`;

export interface RepositorioDaMassa {
  perfis(): Promise<Either<Falha, readonly PerfilDeDados[]>>;
  salvarPerfil(perfil: PerfilDeDados): Promise<Either<Falha, PerfilDeDados>>;
  apagarPerfil(nome: string): Promise<Either<Falha, void>>;
  distribuicoes(): Promise<Either<Falha, readonly DistribuicaoNomeada[]>>;
  salvarDistribuicao(distribuicao: DistribuicaoNomeada): Promise<Either<Falha, DistribuicaoNomeada>>;
  apagarDistribuicao(nome: string): Promise<Either<Falha, void>>;
  gerarLote(pedido: PedidoDeLote): Promise<Either<Falha, Lote>>;
  lotes(): Promise<Either<Falha, readonly Lote[]>>;
  obterLote(id: string): Promise<Either<Falha, Lote>>;
  cancelarLote(id: string): Promise<Either<Falha, Lote>>;
  clientesDoLote(id: string, pagina: Pagina): Promise<Either<Falha, readonly ClienteDaMassa[]>>;
  criarCliente(pedido: PedidoDeCliente): Promise<Either<Falha, MassaDoCliente>>;
  obterCliente(documento: string): Promise<Either<Falha, MassaDoCliente>>;
  /** Sem banco, todos os persistidos. */
  clientesDoBanco(banco: string | null, pagina: Pagina): Promise<Either<Falha, PaginaDeClientes>>;
  resumo(): Promise<Either<Falha, ResumoDaMassa>>;
  limpar(pedido: PedidoDeLimpeza, confirmar: boolean): Promise<Either<Falha, ResultadoDaLimpeza>>;
}
