import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { RepositorioDaMassaHttp } from '@/infrastructure/massa/repositorio-http';
import { API } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { clienteDoLote } from './dados';

const repositorio = () => new RepositorioDaMassaHttp(new ClienteAdmin(API, () => ''));

describe('Repositório HTTP: clientes do banco e resumo', () => {
  it('pede a página com banco, início e quantidade; sem banco, não manda o parâmetro', async () => {
    const pedidos = capturar('get', '/admin/massa/clientes', {
      clientes: [clienteDoLote(1)],
      inicio: 0,
      quantidade: 2,
      temMais: true,
    });

    const pagina = await repositorio().clientesDoBanco('banco-estavel', { inicio: 0, quantidade: 2 });
    await repositorio().clientesDoBanco(null, { inicio: 0, quantidade: 2 });

    expect(pagina.isRight() && pagina.value.temMais).toBe(true);
    expect(pedidos[0]!.url.searchParams.get('banco')).toBe('banco-estavel');
    expect(pedidos[0]!.url.searchParams.get('quantidade')).toBe('2');
    expect(pedidos[1]!.url.searchParams.has('banco')).toBe(false);
  });

  it('404 de banco vira falha inexistente; resumo lido', async () => {
    responder('get', '/admin/massa/clientes', { erros: ['banco não encontrado: x'] }, 404);
    responder('get', '/admin/massa/resumo', { clientes: 2, consentimentos: 1, recursos: 4, lotes: 1 });

    expect((await repositorio().clientesDoBanco('x', { inicio: 0, quantidade: 1 })).value).toEqual(
      expect.objectContaining({ tipo: 'inexistente' }),
    );
    expect((await repositorio().resumo()).value).toEqual({ clientes: 2, consentimentos: 1, recursos: 4, lotes: 1 });
  });
});
