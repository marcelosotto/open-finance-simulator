import { z } from 'zod';
import { mapear, type Either } from '@/core/comum/either';
import type { Falha } from '@/core/comum/falha';
import { Ambiente, type RepositorioDoAmbiente } from '@/core/ambiente/ambiente';
import type { ClienteAdmin } from '../http/cliente-admin';

const AmbienteDto = z.object({ marca: z.string() });

export class RepositorioDoAmbienteHttp implements RepositorioDoAmbiente {
  constructor(private readonly cliente: ClienteAdmin) {}

  async obter(): Promise<Either<Falha, Ambiente>> {
    return mapear(await this.cliente.get('/admin/ambiente', AmbienteDto), (dto) => new Ambiente(dto.marca));
  }
}
