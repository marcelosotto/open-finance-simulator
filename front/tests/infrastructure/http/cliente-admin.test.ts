import { http, HttpResponse } from 'msw';
import { z } from 'zod';
import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { lerConfiguracao } from '@/infrastructure/config';
import { servidor } from '../../apoio/servidor';
import { API } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';

const Item = z.object({ nome: z.string() });

describe('ClienteAdmin', () => {
  it('envia x-sim-autor só nas escritas e só com autor (RN71, Q8)', async () => {
    let autor = 'Bruno';
    const cliente = new ClienteAdmin(API, () => autor);
    const leituras = capturar('get', '/admin/x', { nome: 'a' });
    const escritas = capturar('post', '/admin/x', { nome: 'b' });

    await cliente.get('/admin/x', Item);
    await cliente.post('/admin/x', Item, { a: 1 });
    autor = '  ';
    await cliente.post('/admin/x', Item, { a: 2 });

    expect(leituras[0]!.cabecalhos.get('x-sim-autor')).toBeNull();
    expect(escritas[0]!.cabecalhos.get('x-sim-autor')).toBe('Bruno');
    expect(escritas[0]!.corpo).toEqual({ a: 1 });
    expect(escritas[1]!.cabecalhos.get('x-sim-autor')).toBeNull();
  });

  it('PATCH é JSON Merge Patch e PUT e DELETE passam', async () => {
    const cliente = new ClienteAdmin(API, () => '');
    const patches = capturar('patch', '/admin/x', { nome: 'p' });
    responder('put', '/admin/x', { nome: 'u' });
    servidor.use(http.delete(`${API}/admin/x`, () => new HttpResponse(null, { status: 204 })));

    expect((await cliente.patch('/admin/x', Item, { nome: 'p' })).value).toEqual({ nome: 'p' });
    expect(patches[0]!.cabecalhos.get('content-type')).toBe('application/merge-patch+json');
    expect((await cliente.put('/admin/x', Item, {})).value).toEqual({ nome: 'u' });
    expect((await cliente.delete('/admin/x', z.undefined())).isRight()).toBe(true);
  });

  it('erro da administração vira Falha com os códigos', async () => {
    const cliente = new ClienteAdmin(API, () => '');
    responder('post', '/admin/x', { erros: ['PRAZO_INVALIDO', 'ORIGEM_INVALIDA'] }, 422);
    responder('get', '/admin/y', 'sem erros', 404);

    expect((await cliente.post('/admin/x', Item, {})).value).toEqual({
      tipo: 'recusa',
      status: 422,
      erros: ['PRAZO_INVALIDO', 'ORIGEM_INVALIDA'],
    });
    expect((await cliente.get('/admin/y', Item)).value).toEqual({
      tipo: 'inexistente',
      status: 404,
      erros: ['HTTP_404'],
    });
  });

  it('resposta fora do formato e rede fora', async () => {
    const cliente = new ClienteAdmin(API, () => '');
    responder('get', '/admin/x', { outro: 1 });
    servidor.use(http.get(`${API}/admin/fora`, () => HttpResponse.error()));

    expect((await cliente.get('/admin/x', Item)).value).toEqual({
      tipo: 'servidor',
      status: 200,
      erros: ['RESPOSTA_INESPERADA'],
    });
    expect((await cliente.get('/admin/fora', Item)).value).toEqual({ tipo: 'rede', erros: ['SEM_CONEXAO'] });
  });
});

describe('Configuração', () => {
  it('vazio é a mesma origem; URL perde a barra final; outro texto é recusado', () => {
    expect(lerConfiguracao({}).urlDaAdministracao).toBe('');
    expect(lerConfiguracao({ SIM_ADMIN_API_URL: 'http://localhost:8081/' }).urlDaAdministracao).toBe(
      'http://localhost:8081',
    );
    expect(() => lerConfiguracao({ SIM_ADMIN_API_URL: 'nao-e-url' })).toThrow();
  });
});
