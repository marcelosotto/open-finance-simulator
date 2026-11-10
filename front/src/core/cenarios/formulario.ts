import { left, right, type Either } from '../comum/either';
import { falhaDeValidacao, type Falha } from '../comum/falha';
import {
  ESTADOS_DE_RECURSO,
  FALHAS_DE_TRANSPORTE,
  QUANTIDADE_MAXIMA,
  RESULTADOS_DE_INDISPONIBILIDADE,
  TIPOS_DE_EFEITO,
  aceitaCampo,
  aceitaQuantidade,
  type EfeitoDoPedido,
  type PedidoDeRegra,
  type PedidoDeVencedora,
  type RegraDeCenario,
  type TipoDeEfeito,
} from './regra';

/** O que a tela de regra digita: tudo texto, como no formulário. */
export interface FormularioDeRegra {
  nome: string;
  instituicao: string;
  endpoint: string;
  cliente: string;
  recurso: string;
  tipo: TipoDeEfeito;
  latenciaMs: string;
  status: string;
  esperaMs: string;
  resultado: string;
  falha: string;
  estado: string;
  defeito: string;
  campo: string;
  quantidade: string;
  frequenciaPct: string;
  inicio: string;
  fim: string;
  semente: string;
  pacote: string;
  ligada: boolean;
}

export const ehTipoDeEfeito = (tipo: string): tipo is TipoDeEfeito =>
  (TIPOS_DE_EFEITO as readonly string[]).includes(tipo);

export const formularioVazio = (): FormularioDeRegra => ({
  nome: '',
  instituicao: '',
  endpoint: '',
  cliente: '',
  recurso: '',
  tipo: 'latencia',
  latenciaMs: '',
  status: '',
  esperaMs: '',
  resultado: '',
  falha: '',
  estado: '',
  defeito: '',
  campo: '',
  quantidade: '',
  frequenciaPct: '100',
  inicio: '',
  fim: '',
  semente: '',
  pacote: '',
  ligada: true,
});

const texto = (valor: string | number | null): string => (valor === null ? '' : String(valor));

/** A regra do servidor no formato do formulário, para a edição. */
export const formularioDaRegra = (regra: RegraDeCenario): FormularioDeRegra => {
  const e = regra.efeito;
  return {
    nome: texto(regra.nome),
    instituicao: texto(regra.instituicao),
    endpoint: texto(regra.endpoint),
    cliente: texto(regra.cliente),
    recurso: texto(regra.recurso),
    tipo: ehTipoDeEfeito(e.tipo) ? e.tipo : 'latencia',
    latenciaMs: texto(e.latenciaMs),
    status: texto(e.status),
    esperaMs: texto(e.esperaMs),
    resultado: texto(e.resultado),
    falha: texto(e.falha),
    estado: texto(e.estado),
    defeito: texto(e.defeito),
    campo: texto(e.campo),
    quantidade: texto(e.quantidade),
    frequenciaPct: String(regra.frequenciaPct),
    inicio: texto(regra.inicio),
    fim: texto(regra.fim),
    semente: texto(regra.semente),
    pacote: texto(regra.pacote),
    ligada: regra.ligada,
  };
};

const aparado = (valor: string): string | undefined => {
  const t = valor.trim();
  return t === '' ? undefined : t;
};

/** `undefined` quando vazio, `null` quando não é inteiro. */
const inteiro = (valor: string): number | null | undefined => {
  const t = aparado(valor);
  if (t === undefined) return undefined;
  return /^-?\d+$/.test(t) ? Number(t) : null;
};

/** Aceita vírgula ou ponto; `undefined` quando vazio, `null` quando não é número. */
const decimal = (valor: string): number | null | undefined => {
  const t = aparado(valor);
  if (t === undefined) return undefined;
  return /^-?\d+([.,]\d+)?$/.test(t) ? Number(t.replace(',', '.')) : null;
};

const naFaixa = (valor: number | null | undefined, min: number, max: number): number | undefined =>
  valor !== null && valor !== undefined && valor >= min && valor <= max ? valor : undefined;

const umDe = (valor: string, opcoes: readonly string[]): string | undefined =>
  opcoes.includes(valor.trim()) ? valor.trim() : undefined;

/** Tira as chaves sem valor: o corpo leva só o que foi informado. */
const semVazios = <T extends object>(objeto: T): T =>
  Object.fromEntries(Object.entries(objeto).filter(([, valor]) => valor !== undefined)) as T;

type Erros = string[];

const espera = (f: FormularioDeRegra, erros: Erros): number | undefined => {
  const ms = inteiro(f.esperaMs);
  if (ms === null || (ms !== undefined && ms <= 0)) {
    erros.push('ESPERA_INVALIDA');
    return undefined;
  }
  return ms;
};

const exigir = <T>(valor: T | undefined, codigo: string, erros: Erros): T | undefined => {
  if (valor === undefined) erros.push(codigo);
  return valor;
};

const quantidade = (f: FormularioDeRegra, erros: Erros): number | undefined => {
  const q = inteiro(f.quantidade);
  if (q === undefined) return undefined;
  if (q === null || q === 0 || Math.abs(q) > QUANTIDADE_MAXIMA) {
    erros.push('QUANTIDADE_DO_DEFEITO_INVALIDA');
    return undefined;
  }
  return q;
};

const defeito = (f: FormularioDeRegra, erros: Erros): Omit<EfeitoDoPedido, 'tipo'> => {
  const nome = exigir(aparado(f.defeito), 'DEFEITO_OBRIGATORIO', erros);
  if (nome === undefined) return {};
  return {
    defeito: nome,
    campo: aceitaCampo(nome) ? aparado(f.campo) : undefined,
    quantidade: aceitaQuantidade(nome) ? quantidade(f, erros) : undefined,
  };
};

/** Só os campos de cada tipo (data-model da 007 e contrato da 009). */
const CAMPOS_DO_EFEITO: Readonly<
  Record<TipoDeEfeito, (f: FormularioDeRegra, erros: Erros) => Omit<EfeitoDoPedido, 'tipo'>>
> = {
  latencia: (f, erros) => ({
    latenciaMs: exigir(naFaixa(inteiro(f.latenciaMs), 0, Number.MAX_SAFE_INTEGER), 'LATENCIA_INVALIDA', erros),
  }),
  status: (f, erros) => ({
    status: exigir(naFaixa(inteiro(f.status), 100, 599), 'STATUS_INVALIDO', erros),
    esperaMs: espera(f, erros),
  }),
  timeout: (f, erros) => ({ esperaMs: espera(f, erros) }),
  indisponibilidade: (f, erros) => ({
    resultado: exigir(umDe(f.resultado, RESULTADOS_DE_INDISPONIBILIDADE), 'RESULTADO_OBRIGATORIO', erros),
    esperaMs: espera(f, erros),
  }),
  transporte: (f, erros) => ({ falha: exigir(umDe(f.falha, FALHAS_DE_TRANSPORTE), 'FALHA_OBRIGATORIA', erros) }),
  estado: (f, erros) => ({ estado: exigir(umDe(f.estado, ESTADOS_DE_RECURSO), 'ESTADO_OBRIGATORIO', erros) }),
  defeito,
};

const frequencia = (f: FormularioDeRegra, erros: Erros): number | undefined => {
  const pct = decimal(f.frequenciaPct);
  if (pct === null || (pct !== undefined && (pct < 0 || pct > 100))) {
    erros.push('FREQUENCIA_DA_REGRA_INVALIDA');
    return undefined;
  }
  // Vazia, o servidor usa 100 (data-model da 007).
  if (f.tipo === 'estado' && pct !== undefined && pct !== 100) erros.push('ESTADO_EXIGE_FREQUENCIA_100');
  return pct;
};

const instante = (valor: string, codigo: string, erros: Erros): string | undefined => {
  const t = aparado(valor);
  if (t !== undefined && Number.isNaN(Date.parse(t))) {
    erros.push(codigo);
    return undefined;
  }
  return t;
};

const semente = (f: FormularioDeRegra, erros: Erros): number | undefined => {
  const valor = inteiro(f.semente);
  if (valor === null) {
    erros.push('SEMENTE_DA_REGRA_INVALIDA');
    return undefined;
  }
  return valor;
};

/**
 * Corpo do pedido a partir do formulário: valida só formato e faixas.
 * O resto (alcance, endpoint conhecido, status declarado, início antes do fim, pacote existente) a administração recusa.
 */
export const montarPedido = (f: FormularioDeRegra): Either<Falha, PedidoDeRegra> => {
  if (!ehTipoDeEfeito(f.tipo)) return left(falhaDeValidacao('TIPO_DE_EFEITO_INVALIDO'));
  const erros: Erros = [];
  const efeito: EfeitoDoPedido = semVazios({ tipo: f.tipo, ...CAMPOS_DO_EFEITO[f.tipo](f, erros) });
  const pedido: PedidoDeRegra = semVazios({
    nome: aparado(f.nome),
    instituicao: aparado(f.instituicao),
    endpoint: aparado(f.endpoint),
    cliente: aparado(f.cliente),
    recurso: aparado(f.recurso),
    efeito,
    frequenciaPct: frequencia(f, erros),
    inicio: instante(f.inicio, 'INICIO_INVALIDO', erros),
    fim: instante(f.fim, 'FIM_INVALIDO', erros),
    semente: semente(f, erros),
    ligada: f.ligada,
    pacote: aparado(f.pacote),
  });
  return erros.length > 0 ? left(falhaDeValidacao(...erros)) : right(pedido);
};

/** Banco e endpoint obrigatórios; cliente e recurso opcionais (contrato da 010). */
export const pedidoDeVencedora = (dados: {
  readonly instituicao: string;
  readonly endpoint: string;
  readonly cliente: string;
  readonly recurso: string;
}): Either<Falha, PedidoDeVencedora> => {
  const instituicao = aparado(dados.instituicao);
  const endpoint = aparado(dados.endpoint);
  const erros: Erros = [];
  if (instituicao === undefined) erros.push('BANCO_DA_CHAMADA_OBRIGATORIO');
  if (endpoint === undefined) erros.push('ENDPOINT_DA_CHAMADA_OBRIGATORIO');
  if (instituicao === undefined || endpoint === undefined) return left(falhaDeValidacao(...erros));
  return right(semVazios({ instituicao, endpoint, cliente: aparado(dados.cliente), recurso: aparado(dados.recurso) }));
};

export const nomeDePacote = (nome: string): Either<Falha, string> => {
  const t = aparado(nome);
  return t === undefined ? left(falhaDeValidacao('NOME_DO_PACOTE_OBRIGATORIO')) : right(t);
};
