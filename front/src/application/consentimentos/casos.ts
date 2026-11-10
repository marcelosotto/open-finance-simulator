import { left, type Either } from '@/core/comum/either';
import { falhaDeValidacao, type Falha } from '@/core/comum/falha';
import {
  montarPedido,
  type OpcoesDoMenu,
  type Rascunho,
  type Receptora,
  type ReceptoraCriada,
  type RepositorioDeConsentimentos,
  type VisaoDoConsentimento,
} from '@/core/consentimentos/consentimento';
import { token } from '../tokens';

export class ObterOpcoesDoMenu {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  executar(pedido: { readonly cliente: string; readonly banco: string }): Promise<Either<Falha, OpcoesDoMenu>> {
    if (pedido.cliente.trim() === '' || pedido.banco === '')
      return Promise.resolve(left(falhaDeValidacao('PEDIDO_INVALIDO')));
    return this.repositorio.opcoes(pedido.cliente.trim(), pedido.banco);
  }
}

/** Valida o rascunho no core e cria pela API (UI10, RN96). */
export class CriarConsentimento {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  async executar(dados: {
    readonly rascunho: Rascunho;
    readonly opcoes: OpcoesDoMenu;
  }): Promise<Either<Falha, VisaoDoConsentimento>> {
    const pedido = montarPedido(dados.rascunho, dados.opcoes);
    if (pedido.isLeft()) return left(pedido.value);
    return this.repositorio.criar(pedido.value);
  }
}

export class ObterConsentimento {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  executar(consentId: string): Promise<Either<Falha, VisaoDoConsentimento>> {
    return this.repositorio.obter(consentId);
  }
}

export class DecidirAlcada {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  executar(pedido: {
    readonly consentId: string;
    readonly decisao: 'aprovar' | 'recusar';
  }): Promise<Either<Falha, VisaoDoConsentimento>> {
    return this.repositorio.decidir(pedido.consentId, pedido.decisao);
  }
}

export class RevogarConsentimento {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  executar(consentId: string): Promise<Either<Falha, true>> {
    return this.repositorio.revogar(consentId);
  }
}

export class ListarReceptoras {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  executar(): Promise<Either<Falha, readonly Receptora[]>> {
    return this.repositorio.receptoras();
  }
}

export class CriarReceptora {
  constructor(private readonly repositorio: RepositorioDeConsentimentos) {}
  executar(nome: string): Promise<Either<Falha, ReceptoraCriada>> {
    if (nome.trim() === '') return Promise.resolve(left(falhaDeValidacao('NOME_OBRIGATORIO')));
    return this.repositorio.criarReceptora(nome.trim());
  }
}

export const TOKENS_DOS_CONSENTIMENTOS = {
  Opcoes: token<ObterOpcoesDoMenu>('ObterOpcoesDoMenu'),
  Criar: token<CriarConsentimento>('CriarConsentimento'),
  Obter: token<ObterConsentimento>('ObterConsentimento'),
  Decidir: token<DecidirAlcada>('DecidirAlcada'),
  Revogar: token<RevogarConsentimento>('RevogarConsentimento'),
  Receptoras: token<ListarReceptoras>('ListarReceptoras'),
  CriarReceptora: token<CriarReceptora>('CriarReceptora'),
};

export const casosDosConsentimentos = (repositorio: RepositorioDeConsentimentos) =>
  [
    [TOKENS_DOS_CONSENTIMENTOS.Opcoes, new ObterOpcoesDoMenu(repositorio)],
    [TOKENS_DOS_CONSENTIMENTOS.Criar, new CriarConsentimento(repositorio)],
    [TOKENS_DOS_CONSENTIMENTOS.Obter, new ObterConsentimento(repositorio)],
    [TOKENS_DOS_CONSENTIMENTOS.Decidir, new DecidirAlcada(repositorio)],
    [TOKENS_DOS_CONSENTIMENTOS.Revogar, new RevogarConsentimento(repositorio)],
    [TOKENS_DOS_CONSENTIMENTOS.Receptoras, new ListarReceptoras(repositorio)],
    [TOKENS_DOS_CONSENTIMENTOS.CriarReceptora, new CriarReceptora(repositorio)],
  ] as const;
