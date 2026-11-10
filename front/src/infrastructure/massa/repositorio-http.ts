import { z } from 'zod';
import type { Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import type {
  ClienteDaMassa,
  DistribuicaoNomeada,
  Lote,
  MassaDoCliente,
  Pagina,
  PedidoDeCliente,
  PedidoDeLimpeza,
  PedidoDeLote,
  PerfilDeDados,
  RepositorioDaMassa,
  ResultadoDaLimpeza,
} from '@/core/massa/massa';
import type { ClienteAdmin } from '../http/cliente-admin';

// DTOs do contrato da 003 (admin-massa.md). O zod tira campos desconhecidos.
const Perfil = z.object({
  nome: z.string(),
  transacoesMin: z.number(),
  transacoesMax: z.number(),
  valorMin: z.number(),
  valorMax: z.number(),
  saldoInicial: z.number(),
  tiposDeLancamento: z.array(z.string()),
  produtos: z.array(z.string()),
});

const Porcentagens = z.record(z.string(), z.number());

// `produtos` é opcional no C# (pode vir null).
const Distribuicao = z.object({
  bancos: Porcentagens,
  perfis: Porcentagens,
  produtos: Porcentagens.nullish().transform((produtos) => produtos ?? {}),
  pjPct: z.number().default(0),
});

const Nomeada = z.object({ nome: z.string(), distribuicao: Distribuicao });

const LoteDto = z.object({
  id: z.string(),
  quantidade: z.number(),
  situacao: z.enum(['em_andamento', 'concluido', 'parcial']),
  processados: z.number(),
  consentimentos: z.number(),
  recursos: z.number(),
  data: z.string(),
  motivo: z
    .string()
    .nullish()
    .transform((motivo) => motivo ?? null),
});

const Cliente = z.object({
  documento: z.string(),
  tipo: z.enum(['PF', 'PJ']),
  nome: z.string(),
  marca: z.string(),
  natureza: z.string(),
  loteId: z
    .string()
    .nullish()
    .transform((id) => id ?? null),
});

const Recurso = z.object({
  resourceId: z.string(),
  banco: z.string(),
  tipo: z.string(),
  produto: z.string(),
  estado: z.string(),
  perfil: z.string(),
});

const Consentimento = z.object({
  consentId: z.string(),
  banco: z.string(),
  estado: z.string(),
  permissoes: z.array(z.string()),
  criadoEm: z.string(),
  expiraEm: z
    .string()
    .nullish()
    .transform((data) => data ?? null),
});

const Massa = z.object({
  cliente: Cliente,
  consentimentos: z.array(Consentimento).default([]),
  recursos: z.array(Recurso).default([]),
});

const Limpeza = z.object({
  alvo: z.enum(['lote', 'instituicao', 'ajustes-mundo', 'tudo']),
  id: z
    .string()
    .nullish()
    .transform((id) => id ?? null),
  removido: z.boolean(),
  clientes: z.number(),
  consentimentos: z.number(),
  recursos: z.number(),
  ajustes: z.number(),
  lotes: z.number(),
  perfis: z.number(),
  instituicoes: z.number(),
});

const SemCorpo = z.void();

const BASE = '/admin/massa';
const nomeado = (recurso: string, nome: string, resto = '') => `${BASE}/${recurso}/${encodeURIComponent(nome)}${resto}`;

export class RepositorioDaMassaHttp implements RepositorioDaMassa {
  constructor(private readonly cliente: ClienteAdmin) {}

  perfis(): Promise<Either<Falha, readonly PerfilDeDados[]>> {
    return this.cliente.get(`${BASE}/perfis`, z.array(Perfil));
  }

  /** O nome vai no caminho; o corpo não aceita campo a mais (UnmappedMemberHandling.Disallow). */
  salvarPerfil({ nome, ...corpo }: PerfilDeDados): Promise<Either<Falha, PerfilDeDados>> {
    return this.cliente.put(nomeado('perfis', nome), Perfil, corpo);
  }

  apagarPerfil(nome: string): Promise<Either<Falha, void>> {
    return this.cliente.delete(nomeado('perfis', nome), SemCorpo);
  }

  distribuicoes(): Promise<Either<Falha, readonly DistribuicaoNomeada[]>> {
    return this.cliente.get(`${BASE}/distribuicoes`, z.array(Nomeada));
  }

  salvarDistribuicao({ nome, distribuicao }: DistribuicaoNomeada): Promise<Either<Falha, DistribuicaoNomeada>> {
    return this.cliente.put(nomeado('distribuicoes', nome), Nomeada, distribuicao);
  }

  apagarDistribuicao(nome: string): Promise<Either<Falha, void>> {
    return this.cliente.delete(nomeado('distribuicoes', nome), SemCorpo);
  }

  gerarLote(pedido: PedidoDeLote): Promise<Either<Falha, Lote>> {
    return this.cliente.post(`${BASE}/lotes`, LoteDto, pedido);
  }

  lotes(): Promise<Either<Falha, readonly Lote[]>> {
    return this.cliente.get(`${BASE}/lotes`, z.array(LoteDto));
  }

  obterLote(id: string): Promise<Either<Falha, Lote>> {
    return this.cliente.get(nomeado('lotes', id), LoteDto);
  }

  cancelarLote(id: string): Promise<Either<Falha, Lote>> {
    return this.cliente.post(nomeado('lotes', id, '/cancelar'), LoteDto);
  }

  clientesDoLote(id: string, pagina: Pagina): Promise<Either<Falha, readonly ClienteDaMassa[]>> {
    return this.cliente.get(nomeado('lotes', id, '/clientes'), z.array(Cliente), {
      inicio: pagina.inicio,
      quantidade: pagina.quantidade,
    });
  }

  criarCliente(pedido: PedidoDeCliente): Promise<Either<Falha, MassaDoCliente>> {
    return this.cliente.post(`${BASE}/clientes`, Massa, pedido);
  }

  obterCliente(documento: string): Promise<Either<Falha, MassaDoCliente>> {
    return this.cliente.get(nomeado('clientes', documento), Massa);
  }

  /** Sem `confirmar: true` a administração só devolve a prévia. */
  limpar(pedido: PedidoDeLimpeza, confirmar: boolean): Promise<Either<Falha, ResultadoDaLimpeza>> {
    return this.cliente.post(`${BASE}/limpezas`, Limpeza, { ...pedido, confirmar });
  }
}
