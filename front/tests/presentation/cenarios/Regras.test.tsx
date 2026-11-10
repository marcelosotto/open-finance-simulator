import { screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { abrir, API } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { servidor } from '../../apoio/servidor';
import { dataHora } from '@/presentation/comum/datas';
import { efeito, regra } from '../../core/cenarios/regras';
import { comBase } from './apoio';

const ALCANCES = ['Recurso', 'Cliente', 'Endpoint', 'Instituição'];

const doBanco = regra({ id: 'r-banco', nome: 'banco lento' });
const doEndpoint = regra({
  id: 'r-endpoint',
  endpoint: 'accountsGetAccounts',
  efeito: efeito({ tipo: 'status', latenciaMs: null, status: 500, esperaMs: 2000 }),
  frequenciaPct: 25,
  pacote: 'natal',
});

describe('Regras de cenário (UI06)', () => {
  it('agrupa por alcance, na ordem recurso, cliente, endpoint, instituição, com efeito, frequência, período e pacote', async () => {
    comBase({
      regras: [
        doBanco,
        doEndpoint,
        regra({ id: 'r-cliente', cliente: '76109277673', ligada: false }),
        regra({
          id: 'r-recurso',
          recurso: 'acc-1',
          efeito: efeito({ tipo: 'defeito', latenciaMs: null, defeito: 'formato-invalido', campo: 'bookingDate' }),
        }),
      ],
    });

    abrir('/cenarios');

    expect(await screen.findByRole('region', { name: 'Recurso' })).toBeInTheDocument();
    const ordem = screen
      .getAllByRole('region')
      .map((secao) => secao.getAttribute('aria-label') ?? '')
      .filter((nome) => ALCANCES.includes(nome));
    expect(ordem).toEqual(ALCANCES);

    const endpoint = within(screen.getByRole('region', { name: 'Endpoint' }));
    const linha = endpoint.getByRole('row', { name: /r-endpoint/ });
    expect(linha).toHaveTextContent('Status: HTTP 500, espera 2000 ms');
    expect(linha).toHaveTextContent('25%');
    expect(linha).toHaveTextContent('Sempre');
    expect(linha).toHaveTextContent('natal');
    expect(linha).toHaveTextContent('Ligada');
    expect(within(linha).getByRole('link', { name: 'r-endpoint' })).toHaveAttribute(
      'href',
      '/cenarios/regras/r-endpoint',
    );

    const banco = within(screen.getByRole('region', { name: 'Instituição' }));
    expect(banco.getByRole('link', { name: 'banco lento' })).toBeInTheDocument();
    expect(banco.getByRole('row', { name: /banco lento/ })).toHaveTextContent('Latência: 3000 ms');

    const cliente = within(screen.getByRole('region', { name: 'Cliente' }));
    expect(cliente.getByRole('row', { name: /r-cliente/ })).toHaveTextContent('Desligada');
    expect(
      within(screen.getByRole('region', { name: 'Recurso' })).getByRole('row', { name: /r-recurso/ }),
    ).toHaveTextContent('Defeito de dado: formato-invalido, campo bookingDate');
    expect(screen.getByRole('link', { name: 'Nova regra' })).toHaveAttribute('href', '/cenarios/regras/nova');
  });

  it('mostra o período informado', async () => {
    const inicio = '2026-12-01T00:00:00+00:00';
    const fim = '2026-12-31T00:00:00+00:00';
    comBase({
      regras: [regra({ id: 'r-entre', inicio, fim }), regra({ id: 'r-desde', inicio }), regra({ id: 'r-ate', fim })],
    });

    abrir('/cenarios');

    expect(await screen.findByRole('row', { name: /r-entre/ })).toHaveTextContent(
      `${dataHora(inicio)} a ${dataHora(fim)}`,
    );
    expect(screen.getByRole('row', { name: /r-desde/ })).toHaveTextContent(`De ${dataHora(inicio)}`);
    expect(screen.getByRole('row', { name: /r-ate/ })).toHaveTextContent(`Até ${dataHora(fim)}`);
  });

  it('lista vazia', async () => {
    comBase();
    abrir('/cenarios');
    expect(await screen.findByText('Nenhuma regra.')).toBeInTheDocument();
  });

  it('desliga e liga a regra e relê a lista', async () => {
    comBase({ regras: [doBanco] });
    const desligar = capturar('post', '/admin/cenarios/regras/r-banco/desligar', { ...doBanco, ligada: false });
    const { usuario } = abrir('/cenarios');
    await screen.findByRole('row', { name: /banco lento/ });

    responder('get', '/admin/cenarios/regras', [{ ...doBanco, ligada: false }]);
    await usuario.click(screen.getByRole('button', { name: 'Desligar banco lento' }));

    expect(await screen.findByRole('button', { name: 'Ligar banco lento' })).toBeInTheDocument();
    expect(desligar).toHaveLength(1);
    expect(screen.getByRole('row', { name: /banco lento/ })).toHaveTextContent('Desligada');

    const ligar = capturar('post', '/admin/cenarios/regras/r-banco/ligar', doBanco);
    responder('get', '/admin/cenarios/regras', [doBanco]);
    await usuario.click(screen.getByRole('button', { name: 'Ligar banco lento' }));

    expect(await screen.findByRole('button', { name: 'Desligar banco lento' })).toBeInTheDocument();
    expect(ligar).toHaveLength(1);
  });

  it('apaga a regra depois de confirmar', async () => {
    comBase({ regras: [doBanco, doEndpoint] });
    let apagadas = 0;
    servidor.use(
      http.delete(`${API}/admin/cenarios/regras/r-banco`, () => {
        apagadas += 1;
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const { usuario } = abrir('/cenarios');
    const linha = await screen.findByRole('row', { name: /banco lento/ });

    responder('get', '/admin/cenarios/regras', [doEndpoint]);
    await usuario.click(within(linha).getByRole('button', { name: 'Apagar' }));
    const dialogo = screen.getByRole('dialog', { name: 'Apagar a regra banco lento?' });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Confirmar' }));

    await waitFor(() => expect(screen.queryByText('banco lento')).not.toBeInTheDocument());
    expect(screen.getByText('r-endpoint')).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Instituição' })).not.toBeInTheDocument();
    expect(apagadas).toBe(1);
  });

  it('mostra a recusa ao ligar', async () => {
    comBase({ regras: [{ ...doBanco, ligada: false }] });
    responder('post', '/admin/cenarios/regras/r-banco/ligar', { erros: ['regra não encontrada'] }, 404);
    const { usuario } = abrir('/cenarios');

    await usuario.click(await screen.findByRole('button', { name: 'Ligar banco lento' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('regra não encontrada');
  });
});

describe('Qual regra vale para esta chamada? (Q9)', () => {
  const instante = '2026-11-05T12:00:00+00:00';

  it('mostra as vencedoras que a administração respondeu: a de endpoint vence a de banco', async () => {
    comBase({ regras: [doBanco, doEndpoint] });
    const pedidos = capturar('get', '/admin/cenarios/regras/vencedora', {
      instante,
      comportamento: doEndpoint,
      estado: null,
      defeito: null,
    });
    const { usuario } = abrir('/cenarios');
    const painel = within(await screen.findByRole('region', { name: 'Qual regra vale para esta chamada?' }));

    await usuario.selectOptions(await painel.findByRole('combobox', { name: 'Banco' }), 'Banco Estável');
    await usuario.type(painel.getByRole('combobox', { name: 'Endpoint (operationId)' }), 'accountsGetAccounts');
    await usuario.type(painel.getByRole('textbox', { name: 'Recurso (opcional)' }), 'acc-1');
    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));

    const tabela = within(await painel.findByRole('table', { name: 'Regras que valem' }));
    const comportamento = tabela.getByRole('row', { name: /Comportamento/ });
    expect(within(comportamento).getByRole('link', { name: 'r-endpoint' })).toBeInTheDocument();
    expect(comportamento).toHaveTextContent('(Endpoint) Status: HTTP 500');
    expect(comportamento).not.toHaveTextContent('banco lento');
    expect(tabela.getByRole('row', { name: /Estado/ })).toHaveTextContent('Nenhuma');
    expect(tabela.getByRole('row', { name: /Defeito/ })).toHaveTextContent('Nenhuma');
    expect(painel.getByText(`Hora do banco: ${dataHora(instante)}`)).toBeInTheDocument();

    const parametros = pedidos[0]?.url.searchParams;
    expect(parametros?.get('instituicao')).toBe('banco-estavel');
    expect(parametros?.get('endpoint')).toBe('accountsGetAccounts');
    expect(parametros?.get('recurso')).toBe('acc-1');
    expect(parametros?.has('cliente')).toBe(false);
  });

  it('refaz a resposta depois de desligar uma regra, sem mostrar a vencedora velha (revisão da 010)', async () => {
    comBase({ regras: [doBanco, doEndpoint] });
    const pedidos = capturar('get', '/admin/cenarios/regras/vencedora', {
      instante,
      comportamento: doEndpoint,
      estado: null,
      defeito: null,
    });
    responder('post', '/admin/cenarios/regras/r-endpoint/desligar', { ...doEndpoint, ligada: false });
    const { usuario } = abrir('/cenarios');
    const painel = within(await screen.findByRole('region', { name: 'Qual regra vale para esta chamada?' }));
    await usuario.selectOptions(await painel.findByRole('combobox', { name: 'Banco' }), 'Banco Estável');
    await usuario.type(painel.getByRole('combobox', { name: 'Endpoint (operationId)' }), 'accountsGetAccounts');
    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));
    expect(await painel.findByRole('link', { name: 'r-endpoint' })).toBeInTheDocument();

    responder('get', '/admin/cenarios/regras', [doBanco, { ...doEndpoint, ligada: false }]);
    responder('get', '/admin/cenarios/regras/vencedora', {
      instante,
      comportamento: doBanco,
      estado: null,
      defeito: null,
    });
    await usuario.click(screen.getByRole('button', { name: /^Desligar r-endpoint/ }));

    expect(await painel.findByRole('link', { name: 'banco lento' })).toBeInTheDocument();
    expect(painel.queryByRole('link', { name: 'r-endpoint' })).not.toBeInTheDocument();
    expect(pedidos).toHaveLength(1);
  });

  it('consultar de novo com os mesmos dados refaz a consulta e atualiza a hora do banco', async () => {
    comBase({ regras: [doBanco] });
    const depois = '2026-11-05T12:05:00+00:00';
    const pedidos = capturar('get', '/admin/cenarios/regras/vencedora', {
      instante,
      comportamento: doBanco,
      estado: null,
      defeito: null,
    });
    const { usuario } = abrir('/cenarios');
    const painel = within(await screen.findByRole('region', { name: 'Qual regra vale para esta chamada?' }));
    await usuario.selectOptions(await painel.findByRole('combobox', { name: 'Banco' }), 'Banco Estável');
    await usuario.type(painel.getByRole('combobox', { name: 'Endpoint (operationId)' }), 'accountsGetAccounts');
    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));
    expect(await painel.findByText(`Hora do banco: ${dataHora(instante)}`)).toBeInTheDocument();

    const segundos = capturar('get', '/admin/cenarios/regras/vencedora', {
      instante: depois,
      comportamento: doBanco,
      estado: null,
      defeito: null,
    });
    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));

    expect(await painel.findByText(`Hora do banco: ${dataHora(depois)}`)).toBeInTheDocument();
    expect(pedidos).toHaveLength(1);
    expect(segundos).toHaveLength(1);
  });

  it('mostra as três vencedoras quando há estado e defeito', async () => {
    const deEstado = regra({
      id: 'r-estado',
      efeito: efeito({ tipo: 'estado', latenciaMs: null, estado: 'UNAVAILABLE' }),
    });
    const deDefeito = regra({
      id: 'r-defeito',
      cliente: '761',
      efeito: efeito({ tipo: 'defeito', latenciaMs: null, defeito: 'dado-muda-na-paginacao', quantidade: -2 }),
    });
    comBase({ regras: [doBanco] });
    responder('get', '/admin/cenarios/regras/vencedora', {
      instante,
      comportamento: doBanco,
      estado: deEstado,
      defeito: deDefeito,
    });
    const { usuario } = abrir('/cenarios');
    const painel = within(await screen.findByRole('region', { name: 'Qual regra vale para esta chamada?' }));

    await usuario.selectOptions(await painel.findByRole('combobox', { name: 'Banco' }), 'Banco Estável');
    await usuario.type(painel.getByRole('combobox', { name: 'Endpoint (operationId)' }), 'accountsGetAccounts');
    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));

    const tabela = within(await painel.findByRole('table', { name: 'Regras que valem' }));
    expect(tabela.getByRole('row', { name: /Comportamento/ })).toHaveTextContent(
      'banco lento (Instituição) Latência: 3000 ms',
    );
    expect(tabela.getByRole('row', { name: /Estado/ })).toHaveTextContent('Estado do recurso: UNAVAILABLE');
    expect(tabela.getByRole('row', { name: /Defeito/ })).toHaveTextContent(
      '(Cliente) Defeito de dado: dado-muda-na-paginacao, quantidade -2',
    );
  });

  it('exige banco e endpoint antes de perguntar', async () => {
    comBase();
    const pedidos = capturar('get', '/admin/cenarios/regras/vencedora', {});
    const { usuario } = abrir('/cenarios');
    const painel = within(await screen.findByRole('region', { name: 'Qual regra vale para esta chamada?' }));

    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));

    const alerta = await painel.findByRole('alert');
    expect(alerta).toHaveTextContent('Escolha o banco da chamada.');
    expect(alerta).toHaveTextContent('Informe o endpoint (operationId) da chamada.');
    expect(pedidos).toHaveLength(0);
  });

  it('mostra a recusa de endpoint desconhecido', async () => {
    comBase();
    responder('get', '/admin/cenarios/regras/vencedora', { erros: ['endpoint desconhecido'] }, 422);
    const { usuario } = abrir('/cenarios');
    const painel = within(await screen.findByRole('region', { name: 'Qual regra vale para esta chamada?' }));

    await usuario.selectOptions(await painel.findByRole('combobox', { name: 'Banco' }), 'Banco Estável');
    await usuario.type(painel.getByRole('combobox', { name: 'Endpoint (operationId)' }), 'nada');
    await usuario.click(painel.getByRole('button', { name: 'Consultar' }));

    expect(await painel.findByRole('alert')).toHaveTextContent('endpoint desconhecido');
    expect(painel.getByRole('combobox', { name: 'Endpoint (operationId)' })).toHaveValue('nada');
  });
});
