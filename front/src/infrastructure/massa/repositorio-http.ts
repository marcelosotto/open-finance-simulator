import type { ClienteAdmin } from '../http/cliente-admin';

export class RepositorioDaMassaHttp {
  constructor(readonly cliente: ClienteAdmin) {}
}
