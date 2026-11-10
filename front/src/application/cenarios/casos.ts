import { left, type Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import { montarPedido, nomeDePacote, pedidoDeVencedora, type FormularioDeRegra } from '@/core/cenarios/formulario';
import type {
  ItemDoCatalogo,
  PacoteDeCenario,
  RegraDeCenario,
  RepositorioDeCenarios,
  Vencedoras,
} from '@/core/cenarios/regra';
import { token } from '../tokens';

export class ListarRegras {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(): Promise<Either<Falha, readonly RegraDeCenario[]>> {
    return this.repositorio.listarRegras();
  }
}

export class ObterRegra {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(id: string): Promise<Either<Falha, RegraDeCenario>> {
    return this.repositorio.obterRegra(id);
  }
}

/** Valida formato e faixas no core; o resto a administração recusa (UC21). */
export class CriarRegra {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  async executar(formulario: FormularioDeRegra): Promise<Either<Falha, RegraDeCenario>> {
    const pedido = montarPedido(formulario);
    if (pedido.isLeft()) return left(pedido.value);
    return this.repositorio.criarRegra(pedido.value);
  }
}

/** O PUT substitui a regra inteira, inclusive `ligada` (contrato da 007). */
export class AlterarRegra {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  async executar(dados: {
    readonly id: string;
    readonly formulario: FormularioDeRegra;
  }): Promise<Either<Falha, RegraDeCenario>> {
    const pedido = montarPedido(dados.formulario);
    if (pedido.isLeft()) return left(pedido.value);
    return this.repositorio.alterarRegra(dados.id, pedido.value);
  }
}

export class ApagarRegra {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(id: string): Promise<Either<Falha, void>> {
    return this.repositorio.apagarRegra(id);
  }
}

export class LigarRegra {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(pedido: { readonly id: string; readonly ligada: boolean }): Promise<Either<Falha, RegraDeCenario>> {
    return this.repositorio.ligarRegra(pedido.id, pedido.ligada);
  }
}

export class CatalogoDeDefeitos {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(): Promise<Either<Falha, readonly ItemDoCatalogo[]>> {
    return this.repositorio.defeitos();
  }
}

/** Os `operationId` do catálogo, para sugerir o endpoint. */
export class OperacoesDoCatalogo {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(): Promise<Either<Falha, readonly string[]>> {
    return this.repositorio.operacoes();
  }
}

export class ListarPacotes {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(): Promise<Either<Falha, readonly PacoteDeCenario[]>> {
    return this.repositorio.listarPacotes();
  }
}

/** O pacote nasce desligado (RN19). */
export class CriarPacote {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  async executar(nome: string): Promise<Either<Falha, PacoteDeCenario>> {
    const valido = nomeDePacote(nome);
    if (valido.isLeft()) return left(valido.value);
    return this.repositorio.criarPacote(valido.value);
  }
}

export class LigarPacote {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(pedido: { readonly nome: string; readonly ligado: boolean }): Promise<Either<Falha, PacoteDeCenario>> {
    return this.repositorio.ligarPacote(pedido.nome, pedido.ligado);
  }
}

/** Pacote com regras é recusado com 409 (contrato da 007). */
export class ApagarPacote {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  executar(nome: string): Promise<Either<Falha, void>> {
    return this.repositorio.apagarPacote(nome);
  }
}

/** Qual regra vale para a chamada: a resposta vem pronta da administração (Q9, UI06). */
export class VencedoraDaChamada {
  constructor(private readonly repositorio: RepositorioDeCenarios) {}
  async executar(dados: {
    readonly instituicao: string;
    readonly endpoint: string;
    readonly cliente: string;
    readonly recurso: string;
  }): Promise<Either<Falha, Vencedoras>> {
    const pedido = pedidoDeVencedora(dados);
    if (pedido.isLeft()) return left(pedido.value);
    return this.repositorio.vencedoras(pedido.value);
  }
}

export const TOKENS_DOS_CENARIOS = {
  ListarRegras: token<ListarRegras>('ListarRegras'),
  ObterRegra: token<ObterRegra>('ObterRegra'),
  CriarRegra: token<CriarRegra>('CriarRegra'),
  AlterarRegra: token<AlterarRegra>('AlterarRegra'),
  ApagarRegra: token<ApagarRegra>('ApagarRegra'),
  LigarRegra: token<LigarRegra>('LigarRegra'),
  Defeitos: token<CatalogoDeDefeitos>('CatalogoDeDefeitos'),
  Operacoes: token<OperacoesDoCatalogo>('OperacoesDoCatalogo'),
  ListarPacotes: token<ListarPacotes>('ListarPacotes'),
  CriarPacote: token<CriarPacote>('CriarPacote'),
  LigarPacote: token<LigarPacote>('LigarPacote'),
  ApagarPacote: token<ApagarPacote>('ApagarPacote'),
  Vencedora: token<VencedoraDaChamada>('VencedoraDaChamada'),
};

/** Todos os casos sobre um repositório: o container registra cada um pelo seu token. */
export const casosDosCenarios = (repositorio: RepositorioDeCenarios) =>
  [
    [TOKENS_DOS_CENARIOS.ListarRegras, new ListarRegras(repositorio)],
    [TOKENS_DOS_CENARIOS.ObterRegra, new ObterRegra(repositorio)],
    [TOKENS_DOS_CENARIOS.CriarRegra, new CriarRegra(repositorio)],
    [TOKENS_DOS_CENARIOS.AlterarRegra, new AlterarRegra(repositorio)],
    [TOKENS_DOS_CENARIOS.ApagarRegra, new ApagarRegra(repositorio)],
    [TOKENS_DOS_CENARIOS.LigarRegra, new LigarRegra(repositorio)],
    [TOKENS_DOS_CENARIOS.Defeitos, new CatalogoDeDefeitos(repositorio)],
    [TOKENS_DOS_CENARIOS.Operacoes, new OperacoesDoCatalogo(repositorio)],
    [TOKENS_DOS_CENARIOS.ListarPacotes, new ListarPacotes(repositorio)],
    [TOKENS_DOS_CENARIOS.CriarPacote, new CriarPacote(repositorio)],
    [TOKENS_DOS_CENARIOS.LigarPacote, new LigarPacote(repositorio)],
    [TOKENS_DOS_CENARIOS.ApagarPacote, new ApagarPacote(repositorio)],
    [TOKENS_DOS_CENARIOS.Vencedora, new VencedoraDaChamada(repositorio)],
  ] as const;
