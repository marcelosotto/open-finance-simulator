import { z } from 'zod';
import { mapear, type Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import type {
  ItemDoCatalogo,
  PacoteDeCenario,
  PedidoDeRegra,
  PedidoDeVencedora,
  RegraDeCenario,
  RepositorioDeCenarios,
  Vencedoras,
} from '@/core/cenarios/regra';
import type { ClienteAdmin } from '../http/cliente-admin';

// DTOs dos contratos da 007 (regras e pacotes), da 009 (efeito `defeito`) e da 010 (vencedora). O zod tira campos desconhecidos.
const texto = z.string().nullable().default(null);
const numero = z.number().nullable().default(null);

const Efeito = z.object({
  tipo: z.string(),
  status: numero,
  latenciaMs: numero,
  esperaMs: numero,
  resultado: texto,
  falha: texto,
  estado: texto,
  defeito: texto,
  campo: texto,
  quantidade: numero,
});

const Regra = z.object({
  id: z.string(),
  nome: texto,
  instituicao: texto,
  endpoint: texto,
  cliente: texto,
  recurso: texto,
  efeito: Efeito,
  frequenciaPct: z.number(),
  inicio: texto,
  fim: texto,
  semente: numero,
  ligada: z.boolean(),
  pacote: texto,
  criadaEm: z.string(),
  atualizadaEm: z.string(),
});

const Pacote = z.object({ nome: z.string(), ligado: z.boolean(), criadoEm: z.string() });

const Vencedora = z.object({
  instante: z.string(),
  comportamento: Regra.nullable(),
  estado: Regra.nullable(),
  defeito: Regra.nullable(),
});

const Catalogo = z.array(z.object({ nome: z.string(), descricao: z.string() }));
const Operacoes = z.object({ operacoes: z.array(z.object({ operationId: z.string() })) });
const Vazio = z.void();

const REGRAS = '/admin/cenarios/regras';
const PACOTES = '/admin/cenarios/pacotes';
const regra = (id: string, resto = '') => `${REGRAS}/${encodeURIComponent(id)}${resto}`;
const pacote = (nome: string, resto = '') => `${PACOTES}/${encodeURIComponent(nome)}${resto}`;

export class RepositorioDeCenariosHttp implements RepositorioDeCenarios {
  constructor(private readonly cliente: ClienteAdmin) {}

  listarRegras(): Promise<Either<Falha, readonly RegraDeCenario[]>> {
    return this.cliente.get(REGRAS, z.array(Regra));
  }

  obterRegra(id: string): Promise<Either<Falha, RegraDeCenario>> {
    return this.cliente.get(regra(id), Regra);
  }

  criarRegra(pedido: PedidoDeRegra): Promise<Either<Falha, RegraDeCenario>> {
    return this.cliente.post(REGRAS, Regra, pedido);
  }

  alterarRegra(id: string, pedido: PedidoDeRegra): Promise<Either<Falha, RegraDeCenario>> {
    return this.cliente.put(regra(id), Regra, pedido);
  }

  apagarRegra(id: string): Promise<Either<Falha, void>> {
    return this.cliente.delete(regra(id), Vazio);
  }

  ligarRegra(id: string, ligada: boolean): Promise<Either<Falha, RegraDeCenario>> {
    return this.cliente.post(regra(id, ligada ? '/ligar' : '/desligar'), Regra);
  }

  defeitos(): Promise<Either<Falha, readonly ItemDoCatalogo[]>> {
    return this.cliente.get('/admin/cenarios/defeitos', Catalogo);
  }

  async operacoes(): Promise<Either<Falha, readonly string[]>> {
    return mapear(await this.cliente.get('/admin/catalogo', Operacoes), (catalogo) =>
      [...new Set(catalogo.operacoes.map((o) => o.operationId))].sort(),
    );
  }

  listarPacotes(): Promise<Either<Falha, readonly PacoteDeCenario[]>> {
    return this.cliente.get(PACOTES, z.array(Pacote));
  }

  criarPacote(nome: string): Promise<Either<Falha, PacoteDeCenario>> {
    return this.cliente.put(pacote(nome), Pacote);
  }

  ligarPacote(nome: string, ligado: boolean): Promise<Either<Falha, PacoteDeCenario>> {
    return this.cliente.post(pacote(nome, ligado ? '/ligar' : '/desligar'), Pacote);
  }

  apagarPacote(nome: string): Promise<Either<Falha, void>> {
    return this.cliente.delete(pacote(nome), Vazio);
  }

  vencedoras(pedido: PedidoDeVencedora): Promise<Either<Falha, Vencedoras>> {
    return this.cliente.get(`${REGRAS}/vencedora`, Vencedora, {
      instituicao: pedido.instituicao,
      endpoint: pedido.endpoint,
      cliente: pedido.cliente,
      recurso: pedido.recurso,
    });
  }
}
