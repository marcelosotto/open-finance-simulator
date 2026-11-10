import { http, HttpResponse } from 'msw';
import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { RepositorioDaMassaHttp } from '@/infrastructure/massa/repositorio-http';
import { API } from '../../apoio/renderizar';
import { servidor } from '../../apoio/servidor';
import { capturar, responder } from '../../apoio/api';
import {
  clienteDoLote,
  distribuicaoSalva,
  lote,
  massaDaMariana,
  perfilCriado,
  perfisProntos,
  previaDoLote,
} from './dados';

const repositorio = () => new RepositorioDaMassaHttp(new ClienteAdmin(API, () => 'diego'));

const semConteudo = (metodo: 'delete', caminho: string) => {
  const urls: string[] = [];
  servidor.use(
    http[metodo](`${API}${caminho}`, ({ request }) => {
      urls.push(request.url);
      return new HttpResponse(null, { status: 204 });
    }),
  );
  return urls;
};

describe('Repositório HTTP da massa', () => {
  it('lista os perfis', async () => {
    responder('get', '/admin/massa/perfis', perfisProntos);
    const resultado = await repositorio().perfis();
    expect(resultado.isRight()).toBe(true);
    expect(resultado.value).toEqual(perfisProntos);
  });

  it('salva o perfil com o nome só no caminho e o autor no cabeçalho', async () => {
    const pedidos = capturar('put', '/admin/massa/perfis/meu-perfil', perfilCriado);
    const resultado = await repositorio().salvarPerfil(perfilCriado);
    expect(resultado.value).toEqual(perfilCriado);
    const { nome: _nome, ...corpo } = perfilCriado;
    expect(pedidos[0]?.corpo).toEqual(corpo);
    expect(pedidos[0]?.cabecalhos.get('x-sim-autor')).toBe('diego');
  });

  it('apaga perfil (204) e traduz o 409 de perfil pronto', async () => {
    const urls = semConteudo('delete', '/admin/massa/perfis/meu-perfil');
    expect((await repositorio().apagarPerfil('meu-perfil')).isRight()).toBe(true);
    expect(urls).toHaveLength(1);

    responder('delete', '/admin/massa/perfis/cliente-comum', { erros: ['perfil pronto não pode ser apagado'] }, 409);
    const recusa = await repositorio().apagarPerfil('cliente-comum');
    expect(recusa.value).toEqual({ tipo: 'conflito', status: 409, erros: ['perfil pronto não pode ser apagado'] });
  });

  it('lista distribuições, aceitando produtos nulo', async () => {
    responder('get', '/admin/massa/distribuicoes', [
      distribuicaoSalva,
      { nome: 'sem-produtos', distribuicao: { bancos: { b: 100 }, perfis: { p: 100 }, produtos: null, pjPct: 0 } },
    ]);
    const resultado = await repositorio().distribuicoes();
    expect(resultado.value).toEqual([
      distribuicaoSalva,
      { nome: 'sem-produtos', distribuicao: { bancos: { b: 100 }, perfis: { p: 100 }, produtos: {}, pjPct: 0 } },
    ]);
  });

  it('salva a distribuição mandando só o objeto distribuicao', async () => {
    const pedidos = capturar('put', '/admin/massa/distribuicoes/meia-meia', distribuicaoSalva);
    const resultado = await repositorio().salvarDistribuicao(distribuicaoSalva);
    expect(resultado.value).toEqual(distribuicaoSalva);
    expect(pedidos[0]?.corpo).toEqual(distribuicaoSalva.distribuicao);
  });

  it('apaga distribuição com o nome codificado', async () => {
    const urls = semConteudo('delete', '/admin/massa/distribuicoes/:nome');
    expect((await repositorio().apagarDistribuicao('a b')).isRight()).toBe(true);
    expect(urls[0]).toBe(`${API}/admin/massa/distribuicoes/a%20b`);
  });

  it('gera lote (202) e lê só os campos do core', async () => {
    const pedidos = capturar('post', '/admin/massa/lotes', lote(), 202);
    const resultado = await repositorio().gerarLote({ quantidade: 10, distribuicaoNome: 'meia-meia' });
    expect(pedidos[0]?.corpo).toEqual({ quantidade: 10, distribuicaoNome: 'meia-meia' });
    expect(resultado.value).toEqual({
      id: 'lote-0',
      quantidade: 10,
      situacao: 'em_andamento',
      processados: 3,
      consentimentos: 3,
      recursos: 6,
      data: '2026-11-10T10:00:00+00:00',
      motivo: null,
    });
  });

  it('lista, obtém e cancela lote', async () => {
    responder('get', '/admin/massa/lotes', [lote('concluido', 10)]);
    responder('get', '/admin/massa/lotes/lote-0', lote('parcial', 5, 'cancelado'));
    responder('post', '/admin/massa/lotes/lote-0/cancelar', { erros: ['lote já terminou'] }, 409);
    const r = repositorio();
    expect((await r.lotes()).isRight()).toBe(true);
    const obtido = await r.obterLote('lote-0');
    expect(obtido.isRight() && obtido.value.motivo).toBe('cancelado');
    expect((await r.cancelarLote('lote-0')).value).toEqual({
      tipo: 'conflito',
      status: 409,
      erros: ['lote já terminou'],
    });
  });

  it('pede os clientes do lote com inicio e quantidade', async () => {
    const pedidos = capturar('get', '/admin/massa/lotes/lote-0/clientes', [clienteDoLote(1)]);
    const resultado = await repositorio().clientesDoLote('lote-0', { inicio: 20, quantidade: 20 });
    expect(resultado.value).toEqual([clienteDoLote(1)]);
    expect(pedidos[0]?.url.searchParams.get('inicio')).toBe('20');
    expect(pedidos[0]?.url.searchParams.get('quantidade')).toBe('20');
  });

  it('cria cliente (201) e obtém por documento', async () => {
    const pedidos = capturar('post', '/admin/massa/clientes', massaDaMariana, 201);
    const criado = await repositorio().criarCliente({ tipo: 'PF', bancos: ['banco-estavel'], nome: 'Mariana Alves' });
    expect(pedidos[0]?.corpo).toEqual({ tipo: 'PF', bancos: ['banco-estavel'], nome: 'Mariana Alves' });
    expect(criado.isRight() && criado.value.cliente.documento).toBe('12345678909');

    responder('get', '/admin/massa/clientes/12345678909', massaDaMariana);
    const obtido = await repositorio().obterCliente('12345678909');
    expect(obtido.isRight() && obtido.value.consentimentos[0]).toEqual({
      consentId: 'urn:hml:p-c0x0',
      banco: 'banco-estavel',
      estado: 'AUTHORISED',
      permissoes: ['ACCOUNTS_READ', 'RESOURCES_READ'],
      criadoEm: '2026-11-10T10:00:00+00:00',
      expiraEm: null,
    });
  });

  it('traduz o 404 do cliente', async () => {
    responder('get', '/admin/massa/clientes/0', { erros: ['cliente não encontrado'] }, 404);
    expect((await repositorio().obterCliente('0')).value).toEqual({
      tipo: 'inexistente',
      status: 404,
      erros: ['cliente não encontrado'],
    });
  });

  it('limpa com confirmar no corpo', async () => {
    const pedidos = capturar('post', '/admin/massa/limpezas', previaDoLote);
    const resultado = await repositorio().limpar({ alvo: 'lote', id: 'lote-0' }, false);
    expect(resultado.value).toEqual(previaDoLote);
    expect(pedidos[0]?.corpo).toEqual({ alvo: 'lote', id: 'lote-0', confirmar: false });
  });

  it('resposta fora do formato vira RESPOSTA_INESPERADA', async () => {
    responder('get', '/admin/massa/lotes', [{ id: 'lote-0', situacao: 'desconhecida' }]);
    expect((await repositorio().lotes()).value).toEqual({
      tipo: 'servidor',
      status: 200,
      erros: ['RESPOSTA_INESPERADA'],
    });
  });
});
