import type { ClienteAdmin } from '../http/cliente-admin';

export class RepositorioDeCenariosHttp {
  constructor(readonly cliente: ClienteAdmin) {}
}
