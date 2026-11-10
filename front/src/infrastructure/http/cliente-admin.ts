import axios, { type AxiosInstance, type Method } from 'axios';
import type { ZodType, ZodTypeDef } from 'zod';
import { left, right, type Either } from '@/core/comum/either';
import { falhaDaResposta, falhaDeRede, type Falha } from '@/core/comum/falha';

const CABECALHO_DO_AUTOR = 'x-sim-autor';

// O tipo é o da saída do esquema (com defaults e transforms), não o da entrada.
type Esquema<T> = ZodType<T, ZodTypeDef, unknown>;

interface Pedido {
  readonly corpo?: unknown;
  readonly parametros?: Record<string, string | number | undefined>;
  readonly tipo?: string;
}

/** Fala só com a API de administração e transforma toda resposta em Either (R7). */
export class ClienteAdmin {
  private readonly http: AxiosInstance;

  constructor(
    url: string,
    private readonly autor: () => string,
  ) {
    this.http = axios.create({ baseURL: url, validateStatus: () => true, timeout: 30_000 });
  }

  get<T>(caminho: string, esquema: Esquema<T>, parametros?: Pedido['parametros']): Promise<Either<Falha, T>> {
    return this.enviar('GET', caminho, esquema, { parametros });
  }

  post<T>(caminho: string, esquema: Esquema<T>, corpo?: unknown): Promise<Either<Falha, T>> {
    return this.enviar('POST', caminho, esquema, { corpo });
  }

  put<T>(caminho: string, esquema: Esquema<T>, corpo?: unknown): Promise<Either<Falha, T>> {
    return this.enviar('PUT', caminho, esquema, { corpo });
  }

  patch<T>(caminho: string, esquema: Esquema<T>, corpo: unknown): Promise<Either<Falha, T>> {
    return this.enviar('PATCH', caminho, esquema, { corpo, tipo: 'application/merge-patch+json' });
  }

  delete<T>(caminho: string, esquema: Esquema<T>): Promise<Either<Falha, T>> {
    return this.enviar('DELETE', caminho, esquema, {});
  }

  private async enviar<T>(
    metodo: Method,
    caminho: string,
    esquema: Esquema<T>,
    pedido: Pedido,
  ): Promise<Either<Falha, T>> {
    const autor = this.autor().trim();
    const cabecalhos: Record<string, string> = {};
    if (metodo !== 'GET' && autor.length > 0) cabecalhos[CABECALHO_DO_AUTOR] = autor;
    if (pedido.corpo !== undefined) cabecalhos['Content-Type'] = pedido.tipo ?? 'application/json';

    let resposta;
    try {
      resposta = await this.http.request({
        method: metodo,
        url: caminho,
        data: pedido.corpo,
        params: pedido.parametros,
        headers: cabecalhos,
      });
    } catch {
      return left(falhaDeRede());
    }

    if (resposta.status >= 400) return left(falhaDaResposta(resposta.status, errosDoCorpo(resposta.data)));
    const lido = esquema.safeParse(resposta.status === 204 ? undefined : resposta.data);
    return lido.success
      ? right(lido.data)
      : left({ tipo: 'servidor', status: resposta.status, erros: ['RESPOSTA_INESPERADA'] });
  }
}

const errosDoCorpo = (corpo: unknown): string[] => {
  if (typeof corpo === 'object' && corpo !== null && 'erros' in corpo && Array.isArray(corpo.erros)) {
    return corpo.erros.filter((e): e is string => typeof e === 'string');
  }
  return [];
};
