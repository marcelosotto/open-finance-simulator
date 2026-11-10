import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { RepositorioDeConsentimentosHttp } from '@/infrastructure/consentimentos/repositorio-http';
import { API } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { opcoesPF, visaoPJ } from '../../apoio/consentimentos';

const repositorio = () => new RepositorioDeConsentimentosHttp(new ClienteAdmin(API, () => 'Bruno'));

describe('Repositório HTTP dos consentimentos', () => {
  it('opções com cliente e banco na consulta', async () => {
    const pedidos = capturar('get', '/admin/consentimentos/opcoes', opcoesPF);
    expect((await repositorio().opcoes('95000000080', 'banco-estavel')).isRight()).toBe(true);
    expect(pedidos[0]!.url.searchParams.get('cliente')).toBe('95000000080');
    expect(pedidos[0]!.url.searchParams.get('banco')).toBe('banco-estavel');
  });

  it('cria, consulta, decide e revoga', async () => {
    const criados = capturar('post', '/admin/consentimentos', visaoPJ(2), 201);
    responder('get', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0', visaoPJ(2));
    responder('post', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0/aprovar', visaoPJ(1));
    responder('post', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0/recusar', { erros: ['sem aprovação pendente'] }, 409);
    responder('post', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0/revogar', { estado: 'REJECTED' });

    const r = repositorio();
    const pedido = { cliente: 'P1', banco: 'b', receptora: 'org', permissoes: ['ACCOUNTS_READ'] };
    expect((await r.criar(pedido)).isRight()).toBe(true);
    expect(criados[0]!.corpo).toEqual(pedido);
    const visao = await r.obter('urn:hml:p-c5x0');
    expect(visao.isRight() && visao.value.alcada?.aprovacoesPendentes).toBe(2);
    const aprovada = await r.decidir('urn:hml:p-c5x0', 'aprovar');
    expect(aprovada.isRight() && aprovada.value.alcada?.aprovacoesPendentes).toBe(1);
    expect((await r.decidir('urn:hml:p-c5x0', 'recusar')).value).toEqual({
      tipo: 'conflito',
      status: 409,
      erros: ['sem aprovação pendente'],
    });
    expect((await r.revogar('urn:hml:p-c5x0')).value).toBe(true);
  });

  it('visão sem campos opcionais', async () => {
    responder('get', '/admin/consentimentos/x', {
      consentId: 'x',
      cliente: 'c',
      banco: 'b',
      estado: 'AUTHORISED',
      permissoes: [],
      criadoEm: '2026-11-10T00:00:00Z',
    });
    const visao = await repositorio().obter('x');
    expect(visao.value).toEqual(
      expect.objectContaining({ receptora: null, expiraEm: null, alcada: null, decisoes: [], recursos: [] }),
    );
  });

  it('receptoras', async () => {
    responder('get', '/admin/receptoras', [{ organisationId: 'org', nome: 'Coletor', criadaEm: 'x' }]);
    const criadas = capturar(
      'post',
      '/admin/receptoras',
      {
        organisationId: 'org2',
        nome: 'Outra',
        usuario: 'u',
        senha: 's',
        credencialDeTeste: true,
      },
      201,
    );

    const r = repositorio();
    expect((await r.receptoras()).value).toEqual([{ organisationId: 'org', nome: 'Coletor' }]);
    expect((await r.criarReceptora('Outra')).isRight()).toBe(true);
    expect(criadas[0]!.corpo).toEqual({ nome: 'Outra' });
  });
});
