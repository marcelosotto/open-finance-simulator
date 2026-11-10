import { z } from 'zod';
import { mapear, type Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import type {
  OpcoesDoMenu,
  PedidoDeConsentimento,
  Receptora,
  ReceptoraCriada,
  RepositorioDeConsentimentos,
  VisaoDoConsentimento,
} from '@/core/consentimentos/consentimento';
import type { ClienteAdmin } from '../http/cliente-admin';

// Contratos da 008 (painel) e da 004 (receptoras e revogação).
const Origem = z.object({ resourceId: z.string(), marcada: z.boolean() });
const ReceptoraDto = z.object({ organisationId: z.string(), nome: z.string() });

const Opcoes = z.object({
  cliente: z.string(),
  tipo: z.enum(['PF', 'PJ']),
  banco: z.string(),
  semProduto: z.boolean(),
  grupos: z.array(z.object({ nome: z.string(), permissoes: z.array(z.string()) })),
  contas: z.array(Origem),
  cartoes: z.array(Origem),
  agrupados: z.array(z.string()),
  receptoras: z.array(ReceptoraDto),
});

const Visao = z.object({
  consentId: z.string(),
  cliente: z.string(),
  banco: z.string(),
  receptora: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  estado: z.string(),
  permissoes: z.array(z.string()),
  criadoEm: z.string(),
  expiraEm: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  rejeitadoPor: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  razao: z
    .string()
    .nullish()
    .transform((v) => v ?? null),
  alcada: z
    .object({
      situacao: z.string(),
      aprovacoesPendentes: z.number(),
      prazo: z
        .string()
        .nullish()
        .transform((v) => v ?? null),
    })
    .nullish()
    .transform((v) => v ?? null),
  decisoes: z.array(z.object({ autor: z.string(), decisao: z.string(), em: z.string() })).default([]),
  recursos: z.array(z.object({ resourceId: z.string(), tipo: z.string(), estado: z.string() })).default([]),
});

const Criada = ReceptoraDto.extend({ usuario: z.string(), senha: z.string() });
const Qualquer = z.unknown();

const base = (consentId: string) => `/admin/consentimentos/${encodeURIComponent(consentId)}`;

export class RepositorioDeConsentimentosHttp implements RepositorioDeConsentimentos {
  constructor(private readonly cliente: ClienteAdmin) {}

  opcoes(cliente: string, banco: string): Promise<Either<Falha, OpcoesDoMenu>> {
    return this.cliente.get('/admin/consentimentos/opcoes', Opcoes, { cliente, banco });
  }

  criar(pedido: PedidoDeConsentimento): Promise<Either<Falha, VisaoDoConsentimento>> {
    return this.cliente.post('/admin/consentimentos', Visao, pedido);
  }

  obter(consentId: string): Promise<Either<Falha, VisaoDoConsentimento>> {
    return this.cliente.get(base(consentId), Visao);
  }

  decidir(consentId: string, decisao: 'aprovar' | 'recusar'): Promise<Either<Falha, VisaoDoConsentimento>> {
    return this.cliente.post(`${base(consentId)}/${decisao}`, Visao);
  }

  async revogar(consentId: string): Promise<Either<Falha, true>> {
    return mapear(await this.cliente.post(`${base(consentId)}/revogar`, Qualquer, {}), () => true as const);
  }

  receptoras(): Promise<Either<Falha, readonly Receptora[]>> {
    return this.cliente.get('/admin/receptoras', z.array(ReceptoraDto));
  }

  criarReceptora(nome: string): Promise<Either<Falha, ReceptoraCriada>> {
    return this.cliente.post('/admin/receptoras', Criada, { nome });
  }
}
