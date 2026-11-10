import { left, type Either } from '@/core/comum/either';
import { falhaDeValidacao, type Falha } from '@/core/comum/falha';
import {
  distribuicaoParaSalvar,
  paginaValida,
  pedidoDeCliente,
  pedidoDeLimpeza,
  pedidoDeLote,
  perfilPronto,
  perfilParaSalvar,
  type AlvoDaLimpeza,
  type ClienteDaMassa,
  type Distribuicao,
  type DistribuicaoNomeada,
  type Lote,
  type MassaDoCliente,
  type Pagina,
  type PedidoDeCliente,
  type PedidoDeLote,
  type PerfilDeDados,
  type RepositorioDaMassa,
  type ResultadoDaLimpeza,
} from '@/core/massa/massa';
import { token } from '../tokens';

export class ListarPerfisDeDados {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  executar(): Promise<Either<Falha, readonly PerfilDeDados[]>> {
    return this.repositorio.perfis();
  }
}

/** Cria ou altera um perfil criado; os prontos o caso já recusa (RN55). */
export class SalvarPerfilDeDados {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(perfil: PerfilDeDados): Promise<Either<Falha, PerfilDeDados>> {
    const valido = perfilParaSalvar(perfil);
    if (valido.isLeft()) return left(valido.value);
    return this.repositorio.salvarPerfil(valido.value);
  }
}

export class ApagarPerfilDeDados {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(nome: string): Promise<Either<Falha, void>> {
    if (perfilPronto(nome)) return left(falhaDeValidacao('PERFIL_PRONTO'));
    return this.repositorio.apagarPerfil(nome);
  }
}

export class ListarDistribuicoes {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  executar(): Promise<Either<Falha, readonly DistribuicaoNomeada[]>> {
    return this.repositorio.distribuicoes();
  }
}

/** Recusa fora de 100% antes de chamar a API (UI07, RN56). */
export class SalvarDistribuicao {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(pedido: {
    readonly nome: string;
    readonly distribuicao: Distribuicao;
  }): Promise<Either<Falha, DistribuicaoNomeada>> {
    const valida = distribuicaoParaSalvar(pedido.nome, pedido.distribuicao);
    if (valida.isLeft()) return left(valida.value);
    return this.repositorio.salvarDistribuicao(valida.value);
  }
}

export class ApagarDistribuicao {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  executar(nome: string): Promise<Either<Falha, void>> {
    return this.repositorio.apagarDistribuicao(nome);
  }
}

export class GerarLote {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(pedido: PedidoDeLote): Promise<Either<Falha, Lote>> {
    const valido = pedidoDeLote(pedido);
    if (valido.isLeft()) return left(valido.value);
    return this.repositorio.gerarLote(valido.value);
  }
}

export class ListarLotes {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  executar(): Promise<Either<Falha, readonly Lote[]>> {
    return this.repositorio.lotes();
  }
}

export class ObterLote {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  executar(id: string): Promise<Either<Falha, Lote>> {
    return this.repositorio.obterLote(id);
  }
}

export class CancelarLote {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  executar(id: string): Promise<Either<Falha, Lote>> {
    return this.repositorio.cancelarLote(id);
  }
}

export class ClientesDoLote {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(pedido: { readonly id: string } & Pagina): Promise<Either<Falha, readonly ClienteDaMassa[]>> {
    const pagina = { inicio: pedido.inicio, quantidade: pedido.quantidade };
    if (!paginaValida(pagina)) return left(falhaDeValidacao('PAGINA_INVALIDA'));
    return this.repositorio.clientesDoLote(pedido.id, pagina);
  }
}

export class CriarClienteAvulso {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(pedido: PedidoDeCliente): Promise<Either<Falha, MassaDoCliente>> {
    const valido = pedidoDeCliente(pedido);
    if (valido.isLeft()) return left(valido.value);
    return this.repositorio.criarCliente(valido.value);
  }
}

export class ObterClienteDaMassa {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(documento: string): Promise<Either<Falha, MassaDoCliente>> {
    const aparado = documento.trim();
    if (aparado.length === 0) return left(falhaDeValidacao('DOCUMENTO_OBRIGATORIO'));
    return this.repositorio.obterCliente(aparado);
  }
}

interface PedidoDaTela {
  readonly alvo: AlvoDaLimpeza;
  readonly id: string;
}

/** Sem `confirmar`: só mostra o que será apagado. */
export class PreverLimpeza {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(pedido: PedidoDaTela): Promise<Either<Falha, ResultadoDaLimpeza>> {
    const valido = pedidoDeLimpeza(pedido.alvo, pedido.id);
    if (valido.isLeft()) return left(valido.value);
    return this.repositorio.limpar(valido.value, false);
  }
}

export class ConfirmarLimpeza {
  constructor(private readonly repositorio: RepositorioDaMassa) {}
  async executar(pedido: PedidoDaTela): Promise<Either<Falha, ResultadoDaLimpeza>> {
    const valido = pedidoDeLimpeza(pedido.alvo, pedido.id);
    if (valido.isLeft()) return left(valido.value);
    return this.repositorio.limpar(valido.value, true);
  }
}

export const TOKENS_DA_MASSA = {
  Perfis: token<ListarPerfisDeDados>('ListarPerfisDeDados'),
  SalvarPerfil: token<SalvarPerfilDeDados>('SalvarPerfilDeDados'),
  ApagarPerfil: token<ApagarPerfilDeDados>('ApagarPerfilDeDados'),
  Distribuicoes: token<ListarDistribuicoes>('ListarDistribuicoes'),
  SalvarDistribuicao: token<SalvarDistribuicao>('SalvarDistribuicao'),
  ApagarDistribuicao: token<ApagarDistribuicao>('ApagarDistribuicao'),
  GerarLote: token<GerarLote>('GerarLote'),
  Lotes: token<ListarLotes>('ListarLotes'),
  Lote: token<ObterLote>('ObterLote'),
  CancelarLote: token<CancelarLote>('CancelarLote'),
  ClientesDoLote: token<ClientesDoLote>('ClientesDoLote'),
  CriarCliente: token<CriarClienteAvulso>('CriarClienteAvulso'),
  Cliente: token<ObterClienteDaMassa>('ObterClienteDaMassa'),
  PreverLimpeza: token<PreverLimpeza>('PreverLimpeza'),
  ConfirmarLimpeza: token<ConfirmarLimpeza>('ConfirmarLimpeza'),
};

/** Todos os casos sobre um repositório: o container registra cada um pelo seu token. */
export const casosDaMassa = (repositorio: RepositorioDaMassa) =>
  [
    [TOKENS_DA_MASSA.Perfis, new ListarPerfisDeDados(repositorio)],
    [TOKENS_DA_MASSA.SalvarPerfil, new SalvarPerfilDeDados(repositorio)],
    [TOKENS_DA_MASSA.ApagarPerfil, new ApagarPerfilDeDados(repositorio)],
    [TOKENS_DA_MASSA.Distribuicoes, new ListarDistribuicoes(repositorio)],
    [TOKENS_DA_MASSA.SalvarDistribuicao, new SalvarDistribuicao(repositorio)],
    [TOKENS_DA_MASSA.ApagarDistribuicao, new ApagarDistribuicao(repositorio)],
    [TOKENS_DA_MASSA.GerarLote, new GerarLote(repositorio)],
    [TOKENS_DA_MASSA.Lotes, new ListarLotes(repositorio)],
    [TOKENS_DA_MASSA.Lote, new ObterLote(repositorio)],
    [TOKENS_DA_MASSA.CancelarLote, new CancelarLote(repositorio)],
    [TOKENS_DA_MASSA.ClientesDoLote, new ClientesDoLote(repositorio)],
    [TOKENS_DA_MASSA.CriarCliente, new CriarClienteAvulso(repositorio)],
    [TOKENS_DA_MASSA.Cliente, new ObterClienteDaMassa(repositorio)],
    [TOKENS_DA_MASSA.PreverLimpeza, new PreverLimpeza(repositorio)],
    [TOKENS_DA_MASSA.ConfirmarLimpeza, new ConfirmarLimpeza(repositorio)],
  ] as const;
