import { z } from 'zod';
import { left, mapear, right, type Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import type {
  AlteracaoDeInstituicao,
  EntradaDeHistorico,
  EstadoDaAnomalia,
  Instituicao,
  NovaInstituicao,
  PedidoDeRelogio,
  RepositorioDeInstituicoes,
  ResultadoDaRemocao,
  ResumoDeInstituicao,
} from '@/core/instituicoes/instituicao';
import type { ClienteAdmin } from '../http/cliente-admin';

// DTOs do contrato da 002 (com anomaliasLigadas da 009). O zod tira campos desconhecidos.
const Resumo = z.object({
  endereco: z.string(),
  nome: z.string(),
  perfil: z.string(),
  situacao: z.enum(['ativa', 'inativa']),
  anomaliasLigadas: z.array(z.string()).default([]),
});

const Latencia = z.object({ tipicoMs: z.number(), variacaoMs: z.number(), caudaLentaPct: z.number() });

const Detalhe = Resumo.extend({
  organisationId: z.string(),
  marca: z.string(),
  publico: z.enum(['PF', 'PJ', 'ambos']),
  produtos: z.array(z.string()),
  pronta: z.boolean(),
  comportamento: z.object({
    latenciaPorClasse: z.record(z.string(), Latencia),
    taxaDeErroPct: z.number(),
    pesosPorStatus: z.record(z.string(), z.number()),
    recursoIndisponivelPct: z.number(),
    janela: z.object({ duracaoMin: z.number(), intervaloMin: z.number() }).nullable(),
  }),
  limites: z.object({
    qca: z.number(),
    qcaPorReceptora: z.record(z.string(), z.number()),
    tpm: z.object({ alta: z.number().nullable(), mediaAlta: z.number(), media: z.number(), baixa: z.number() }),
    limiteOperacionalLigado: z.boolean(),
    limiteMensal: z.object({
      alta: z.number(),
      saldosELimites: z.number(),
      mediaAlta: z.number(),
      media: z.number(),
      baixa: z.number(),
    }),
    capacidadeTps: z.number(),
    pageSizeMaximo: z.number(),
    validadeDaChaveMin: z.number(),
    retryAfterLigado: z.boolean(),
    retryAfterSeg: z.number(),
    preparacaoDeRecursosSeg: z.number(),
  }),
  relogio: z.object({ baseSimulada: z.string(), baseReal: z.string(), fator: z.number() }).nullable(),
  sla: z
    .array(z.object({ classe: z.string(), slaMs: z.number(), p95Ms: z.number(), dentroDoSla: z.boolean() }))
    .default([]),
  avisos: z.array(z.object({ campo: z.string(), oficial: z.number(), configurado: z.number() })).default([]),
});

const Perfis = z.array(z.object({ nome: z.string() }));
const Remocao = z.object({ resultado: z.enum(['apagada', 'inativada']) });
const Historico = z.array(
  z.object({
    data: z.string(),
    autor: z.string(),
    acao: z.string(),
    mudancas: z
      .array(z.object({ campo: z.string(), antes: z.string().nullable(), depois: z.string().nullable() }))
      .default([]),
  }),
);
const Catalogo = z.array(z.object({ nome: z.string(), descricao: z.string() }));
const Estados = z.array(z.object({ nome: z.string(), ligada: z.boolean() }));
const Estado = z.object({ nome: z.string(), ligada: z.boolean() });

const caminho = (endereco: string, resto = '') => `/admin/instituicoes/${encodeURIComponent(endereco)}${resto}`;

export class RepositorioDeInstituicoesHttp implements RepositorioDeInstituicoes {
  constructor(private readonly cliente: ClienteAdmin) {}

  listar(): Promise<Either<Falha, readonly ResumoDeInstituicao[]>> {
    return this.cliente.get('/admin/instituicoes', z.array(Resumo));
  }

  obter(endereco: string): Promise<Either<Falha, Instituicao>> {
    return this.cliente.get(caminho(endereco), Detalhe);
  }

  async perfis(): Promise<Either<Falha, readonly string[]>> {
    return mapear(await this.cliente.get('/admin/perfis-de-comportamento', Perfis), (lista) =>
      lista.map((p) => p.nome),
    );
  }

  criar(nova: NovaInstituicao): Promise<Either<Falha, Instituicao>> {
    return this.cliente.post('/admin/instituicoes', Detalhe, nova);
  }

  alterar(endereco: string, alteracao: AlteracaoDeInstituicao): Promise<Either<Falha, Instituicao>> {
    return this.cliente.patch(caminho(endereco), Detalhe, alteracao);
  }

  async apagar(endereco: string): Promise<Either<Falha, ResultadoDaRemocao>> {
    return mapear(await this.cliente.delete(caminho(endereco), Remocao), (r) => r.resultado);
  }

  reativar(endereco: string): Promise<Either<Falha, Instituicao>> {
    return this.cliente.post(caminho(endereco, '/reativar'), Detalhe);
  }

  historico(endereco: string): Promise<Either<Falha, readonly EntradaDeHistorico[]>> {
    return this.cliente.get(caminho(endereco, '/historico'), Historico);
  }

  /** Junta o catálogo (descrição) ao estado do banco. */
  async anomalias(endereco: string): Promise<Either<Falha, readonly EstadoDaAnomalia[]>> {
    const [catalogo, estados] = await Promise.all([
      this.cliente.get('/admin/cenarios/anomalias', Catalogo),
      this.cliente.get(`/admin/cenarios/anomalias/${encodeURIComponent(endereco)}`, Estados),
    ]);
    if (estados.isLeft()) return left(estados.value);
    if (catalogo.isLeft()) return left(catalogo.value);
    const descricoes = new Map(catalogo.value.map((item) => [item.nome, item.descricao]));
    return right(estados.value.map((e) => ({ ...e, descricao: descricoes.get(e.nome) ?? '' })));
  }

  async ligarAnomalia(endereco: string, nome: string, ligada: boolean): Promise<Either<Falha, EstadoDaAnomalia>> {
    const acao = ligada ? 'ligar' : 'desligar';
    const rota = `/admin/cenarios/anomalias/${encodeURIComponent(endereco)}/${encodeURIComponent(nome)}/${acao}`;
    return mapear(await this.cliente.post(rota, Estado), (e) => ({ ...e, descricao: '' }));
  }

  mudarRelogio(endereco: string, pedido: PedidoDeRelogio | null): Promise<Either<Falha, Instituicao>> {
    return pedido === null
      ? this.cliente.delete(caminho(endereco, '/relogio'), Detalhe)
      : this.cliente.put(caminho(endereco, '/relogio'), Detalhe, pedido);
  }
}
