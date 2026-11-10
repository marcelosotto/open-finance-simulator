import { left, type Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import {
  novaInstituicao,
  type AlteracaoDeInstituicao,
  type EntradaDeHistorico,
  type EstadoDaAnomalia,
  type Instituicao,
  type PedidoDeRelogio,
  type RepositorioDeInstituicoes,
  type ResultadoDaRemocao,
  type ResumoDeInstituicao,
} from '@/core/instituicoes/instituicao';
import { token } from '../tokens';

export class ListarInstituicoes {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(): Promise<Either<Falha, readonly ResumoDeInstituicao[]>> {
    return this.repositorio.listar();
  }
}

export class ObterInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(endereco: string): Promise<Either<Falha, Instituicao>> {
    return this.repositorio.obter(endereco);
  }
}

export class ListarPerfisDeComportamento {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(): Promise<Either<Falha, readonly string[]>> {
    return this.repositorio.perfis();
  }
}

/** Cria só com nome e perfil; a tela abre a edição com o que o servidor devolveu (UI03, Q5). */
export class CriarInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  async executar(dados: { readonly nome: string; readonly perfil: string }): Promise<Either<Falha, Instituicao>> {
    const nova = novaInstituicao(dados.nome, dados.perfil);
    if (nova.isLeft()) return left(nova.value);
    return this.repositorio.criar(nova.value);
  }
}

export class AlterarInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(pedido: {
    readonly endereco: string;
    readonly alteracao: AlteracaoDeInstituicao;
  }): Promise<Either<Falha, Instituicao>> {
    return this.repositorio.alterar(pedido.endereco, pedido.alteracao);
  }
}

export class ApagarInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(endereco: string): Promise<Either<Falha, ResultadoDaRemocao>> {
    return this.repositorio.apagar(endereco);
  }
}

export class ReativarInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(endereco: string): Promise<Either<Falha, Instituicao>> {
    return this.repositorio.reativar(endereco);
  }
}

export class HistoricoDaInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(endereco: string): Promise<Either<Falha, readonly EntradaDeHistorico[]>> {
    return this.repositorio.historico(endereco);
  }
}

export class AnomaliasDaInstituicao {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(endereco: string): Promise<Either<Falha, readonly EstadoDaAnomalia[]>> {
    return this.repositorio.anomalias(endereco);
  }
}

export class LigarAnomalia {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(pedido: {
    readonly endereco: string;
    readonly nome: string;
    readonly ligada: boolean;
  }): Promise<Either<Falha, EstadoDaAnomalia>> {
    return this.repositorio.ligarAnomalia(pedido.endereco, pedido.nome, pedido.ligada);
  }
}

/** Fixa, acelera ou volta ao relógio real (null) (UC28). */
export class MudarRelogio {
  constructor(private readonly repositorio: RepositorioDeInstituicoes) {}
  executar(pedido: {
    readonly endereco: string;
    readonly relogio: PedidoDeRelogio | null;
  }): Promise<Either<Falha, Instituicao>> {
    return this.repositorio.mudarRelogio(pedido.endereco, pedido.relogio);
  }
}

export const TOKENS_DAS_INSTITUICOES = {
  Listar: token<ListarInstituicoes>('ListarInstituicoes'),
  Obter: token<ObterInstituicao>('ObterInstituicao'),
  Perfis: token<ListarPerfisDeComportamento>('ListarPerfisDeComportamento'),
  Criar: token<CriarInstituicao>('CriarInstituicao'),
  Alterar: token<AlterarInstituicao>('AlterarInstituicao'),
  Apagar: token<ApagarInstituicao>('ApagarInstituicao'),
  Reativar: token<ReativarInstituicao>('ReativarInstituicao'),
  Historico: token<HistoricoDaInstituicao>('HistoricoDaInstituicao'),
  Anomalias: token<AnomaliasDaInstituicao>('AnomaliasDaInstituicao'),
  LigarAnomalia: token<LigarAnomalia>('LigarAnomalia'),
  MudarRelogio: token<MudarRelogio>('MudarRelogio'),
};

/** Todos os casos sobre um repositório: o container registra cada um pelo seu token. */
export const casosDasInstituicoes = (repositorio: RepositorioDeInstituicoes) =>
  [
    [TOKENS_DAS_INSTITUICOES.Listar, new ListarInstituicoes(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Obter, new ObterInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Perfis, new ListarPerfisDeComportamento(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Criar, new CriarInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Alterar, new AlterarInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Apagar, new ApagarInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Reativar, new ReativarInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Historico, new HistoricoDaInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.Anomalias, new AnomaliasDaInstituicao(repositorio)],
    [TOKENS_DAS_INSTITUICOES.LigarAnomalia, new LigarAnomalia(repositorio)],
    [TOKENS_DAS_INSTITUICOES.MudarRelogio, new MudarRelogio(repositorio)],
  ] as const;
