import { http, HttpResponse } from 'msw';
import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { RepositorioDeCenariosHttp } from '@/infrastructure/cenarios/repositorio-http';
import { servidor } from '../../apoio/servidor';
import { API } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { regra } from '../../core/cenarios/regras';

const repositorio = () => new RepositorioDeCenariosHttp(new ClienteAdmin(API, () => 'bruno'));

// Resposta real de POST /admin/cenarios/regras (contrato da 007).
const respostaReal = {
  id: 'r-i00dnwpny63e',
  nome: null,
  instituicao: 'banco-estavel',
  endpoint: null,
  cliente: null,
  recurso: null,
  efeito: {
    tipo: 'latencia',
    status: null,
    latenciaMs: 3000,
    esperaMs: null,
    resultado: null,
    falha: null,
    estado: null,
    defeito: null,
    campo: null,
    quantidade: null,
  },
  frequenciaPct: 100,
  inicio: null,
  fim: null,
  semente: null,
  ligada: true,
  pacote: null,
  criadaEm: '2026-11-05T12:00:00+00:00',
  atualizadaEm: '2026-11-05T12:00:00+00:00',
};

const pacote = { nome: 'natal', ligado: false, criadoEm: '2026-11-05T12:00:00+00:00' };

describe('RepositorioDeCenariosHttp', () => {
  it('lê a regra real e completa os campos ausentes com null', async () => {
    const { efeito, ...semEfeito } = respostaReal;
    responder('get', '/admin/cenarios/regras', [
      respostaReal,
      { ...semEfeito, efeito: { tipo: efeito.tipo }, extra: 1 },
    ]);

    const lista = await repositorio().listarRegras();

    expect(lista.isRight() && lista.value).toEqual([
      regra(),
      regra({ efeito: { ...regra().efeito, latenciaMs: null } }),
    ]);
  });

  it('obter com 404 vira inexistente', async () => {
    responder('get', '/admin/cenarios/regras/r%2F1', { erros: ['regra não encontrada'] }, 404);

    const obtida = await repositorio().obterRegra('r/1');

    expect(obtida.isLeft() && obtida.value).toEqual({
      tipo: 'inexistente',
      status: 404,
      erros: ['regra não encontrada'],
    });
  });

  it('cria com POST e o autor, e altera com PUT', async () => {
    const criacao = capturar('post', '/admin/cenarios/regras', respostaReal, 201);
    const alteracao = capturar('put', '/admin/cenarios/regras/r-1', respostaReal);
    const pedido = {
      instituicao: 'banco-estavel',
      efeito: { tipo: 'latencia', latenciaMs: 3000 },
      ligada: true,
    } as const;

    const criada = await repositorio().criarRegra(pedido);
    const alterada = await repositorio().alterarRegra('r-1', pedido);

    expect(criada.isRight() && criada.value.id).toBe('r-i00dnwpny63e');
    expect(alterada.isRight()).toBe(true);
    expect(criacao[0]?.corpo).toEqual(pedido);
    expect(criacao[0]?.cabecalhos.get('x-sim-autor')).toBe('bruno');
    expect(alteracao[0]?.corpo).toEqual(pedido);
  });

  it('a recusa 422 traz os erros da administração', async () => {
    responder(
      'post',
      '/admin/cenarios/regras',
      { erros: ['informe ao menos um filtro de alcance', 'efeito inválido'] },
      422,
    );

    const criada = await repositorio().criarRegra({ efeito: { tipo: 'latencia' }, ligada: true });

    expect(criada.isLeft() && criada.value).toEqual({
      tipo: 'recusa',
      status: 422,
      erros: ['informe ao menos um filtro de alcance', 'efeito inválido'],
    });
  });

  it('apaga com 204 e liga e desliga pela rota de cada ação', async () => {
    servidor.use(http.delete(`${API}/admin/cenarios/regras/r-1`, () => new HttpResponse(null, { status: 204 })));
    const ligar = capturar('post', '/admin/cenarios/regras/r-1/ligar', respostaReal);
    const desligar = capturar('post', '/admin/cenarios/regras/r-1/desligar', { ...respostaReal, ligada: false });

    const apagada = await repositorio().apagarRegra('r-1');
    await repositorio().ligarRegra('r-1', true);
    const desligada = await repositorio().ligarRegra('r-1', false);

    expect(apagada.isRight()).toBe(true);
    expect(ligar).toHaveLength(1);
    expect(desligar).toHaveLength(1);
    expect(desligada.isRight() && desligada.value.ligada).toBe(false);
  });

  it('catálogo de defeitos e operationIds do catálogo, sem repetição e em ordem', async () => {
    responder('get', '/admin/cenarios/defeitos', [{ nome: 'formato-invalido', descricao: 'Reescreve a data.' }]);
    responder('get', '/admin/catalogo', {
      commit: 'abc',
      operacoes: [
        { operationId: 'consentsPostConsents', contrato: 'consents', metodo: 'POST', caminho: '/consents' },
        { operationId: 'accountsGetAccounts', contrato: 'accounts', metodo: 'GET', caminho: '/accounts' },
        { operationId: 'accountsGetAccounts', contrato: 'accounts', metodo: 'GET', caminho: '/accounts' },
      ],
    });

    const defeitos = await repositorio().defeitos();
    const operacoes = await repositorio().operacoes();

    expect(defeitos.isRight() && defeitos.value).toEqual([
      { nome: 'formato-invalido', descricao: 'Reescreve a data.' },
    ]);
    expect(operacoes.isRight() && operacoes.value).toEqual(['accountsGetAccounts', 'consentsPostConsents']);
  });

  it('pacotes: lista, cria com PUT sem corpo, liga e desliga, e a recusa 409 ao apagar', async () => {
    responder('get', '/admin/cenarios/pacotes', [pacote]);
    const criacao = capturar('put', '/admin/cenarios/pacotes/natal%20de%202026', { ...pacote, nome: 'natal de 2026' });
    responder('post', '/admin/cenarios/pacotes/natal/ligar', { ...pacote, ligado: true });
    responder('post', '/admin/cenarios/pacotes/natal/desligar', pacote);
    responder('delete', '/admin/cenarios/pacotes/natal', { erros: ['pacote tem regras'] }, 409);

    const lista = await repositorio().listarPacotes();
    const criado = await repositorio().criarPacote('natal de 2026');
    const ligado = await repositorio().ligarPacote('natal', true);
    const desligado = await repositorio().ligarPacote('natal', false);
    const apagado = await repositorio().apagarPacote('natal');

    expect(lista.isRight() && lista.value).toEqual([pacote]);
    expect(criado.isRight() && criado.value.nome).toBe('natal de 2026');
    expect(criacao[0]?.corpo).toBeUndefined();
    expect(ligado.isRight() && ligado.value.ligado).toBe(true);
    expect(desligado.isRight() && desligado.value.ligado).toBe(false);
    expect(apagado.isLeft() && apagado.value).toEqual({ tipo: 'conflito', status: 409, erros: ['pacote tem regras'] });
  });

  it('pacote apagado com 204', async () => {
    servidor.use(http.delete(`${API}/admin/cenarios/pacotes/natal`, () => new HttpResponse(null, { status: 204 })));
    expect((await repositorio().apagarPacote('natal')).isRight()).toBe(true);
  });

  it('vencedora manda só os parâmetros informados e lê as três vencedoras', async () => {
    const endpoint = { ...respostaReal, id: 'r-endpoint', endpoint: 'accountsGetAccounts' };
    const pedidos = capturar('get', '/admin/cenarios/regras/vencedora', {
      instante: '2026-11-05T12:00:00+00:00',
      comportamento: endpoint,
      estado: null,
      defeito: null,
    });

    const resposta = await repositorio().vencedoras({ instituicao: 'banco-estavel', endpoint: 'accountsGetAccounts' });

    const parametros = pedidos[0]?.url.searchParams;
    expect(parametros?.get('instituicao')).toBe('banco-estavel');
    expect(parametros?.get('endpoint')).toBe('accountsGetAccounts');
    expect(parametros?.has('cliente')).toBe(false);
    expect(parametros?.has('recurso')).toBe(false);
    expect(resposta.isRight() && resposta.value).toEqual({
      instante: '2026-11-05T12:00:00+00:00',
      comportamento: regra({ id: 'r-endpoint', endpoint: 'accountsGetAccounts' }),
      estado: null,
      defeito: null,
    });
  });

  it('vencedora com banco inexistente é 404', async () => {
    responder('get', '/admin/cenarios/regras/vencedora', { erros: ['instituição não encontrada'] }, 404);

    const resposta = await repositorio().vencedoras({
      instituicao: 'nada',
      endpoint: 'accountsGetAccounts',
      cliente: '1',
    });

    expect(resposta.isLeft() && resposta.value.tipo).toBe('inexistente');
  });
});
