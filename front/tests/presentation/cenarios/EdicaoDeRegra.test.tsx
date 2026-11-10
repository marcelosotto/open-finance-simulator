import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { efeito, regra } from '../../core/cenarios/regras';
import { catalogoDeDefeitos, comBase } from './apoio';

const abrirNova = async () => {
  const tela = abrir('/cenarios/regras/nova');
  await screen.findByRole('button', { name: 'Salvar regra' });
  return tela;
};

const campo = (nome: string) => screen.getByRole('textbox', { name: nome });
const lista = (nome: string) => screen.getByRole('combobox', { name: nome });

describe('Nova regra (UC21)', () => {
  it('latência no banco: manda só os campos do efeito e volta à lista', async () => {
    comBase();
    const criacao = capturar('post', '/admin/cenarios/regras', regra(), 201);
    const { usuario, roteador } = await abrirNova();

    await usuario.selectOptions(lista('Banco'), 'Banco Estável');
    await usuario.type(campo('Latência (ms)'), '3000');
    await usuario.clear(campo('Frequência (%)'));
    await usuario.type(campo('Frequência (%)'), '50');
    await usuario.type(campo('Semente (opcional)'), '42');
    await usuario.selectOptions(lista('Pacote'), 'natal');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    expect(await screen.findByRole('heading', { name: 'Cenários' })).toBeInTheDocument();
    expect(roteador.state.location.pathname).toBe('/cenarios');
    expect(criacao[0]?.corpo).toEqual({
      instituicao: 'banco-estavel',
      efeito: { tipo: 'latencia', latenciaMs: 3000 },
      frequenciaPct: 50,
      semente: 42,
      ligada: true,
      pacote: 'natal',
    });
  });

  it('mostra só os campos do tipo escolhido', async () => {
    comBase();
    const { usuario } = await abrirNova();

    expect(campo('Latência (ms)')).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Status HTTP' })).not.toBeInTheDocument();

    await usuario.selectOptions(lista('Tipo de efeito'), 'Status');
    expect(campo('Status HTTP')).toBeInTheDocument();
    expect(campo('Espera (ms, opcional)')).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Latência (ms)' })).not.toBeInTheDocument();

    await usuario.selectOptions(lista('Tipo de efeito'), 'Indisponibilidade');
    expect(
      within(lista('Resultado'))
        .getAllByRole('option')
        .map((o) => o.getAttribute('value')),
    ).toEqual(['', '503', '504', 'corte']);

    await usuario.selectOptions(lista('Tipo de efeito'), 'Falha de transporte');
    expect(lista('Falha')).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Espera (ms, opcional)' })).not.toBeInTheDocument();

    await usuario.selectOptions(lista('Tipo de efeito'), 'Estado do recurso');
    expect(within(lista('Estado')).getAllByRole('option')).toHaveLength(5);
    expect(screen.getByText('Regra de estado exige 100%.')).toBeInTheDocument();
  });

  it('status com espera, no endpoint', async () => {
    comBase();
    const criacao = capturar('post', '/admin/cenarios/regras', regra(), 201);
    const { usuario } = await abrirNova();

    await usuario.selectOptions(lista('Banco'), 'Banco Instável');
    await usuario.type(lista('Endpoint (operationId)'), 'accountsGetAccounts');
    await usuario.selectOptions(lista('Tipo de efeito'), 'Status');
    await usuario.type(campo('Status HTTP'), '504');
    await usuario.type(campo('Espera (ms, opcional)'), '2000');
    await usuario.type(campo('Nome (opcional)'), 'gateway lento');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    await screen.findByRole('heading', { name: 'Cenários' });
    expect(criacao[0]?.corpo).toEqual({
      nome: 'gateway lento',
      instituicao: 'banco-instavel',
      endpoint: 'accountsGetAccounts',
      efeito: { tipo: 'status', status: 504, esperaMs: 2000 },
      frequenciaPct: 100,
      ligada: true,
    });
  });

  it('transporte e indisponibilidade levam a escolha da lista', async () => {
    comBase();
    const criacao = capturar('post', '/admin/cenarios/regras', regra(), 201);
    const { usuario } = await abrirNova();

    await usuario.type(campo('Documento do cliente'), '76109277673');
    await usuario.selectOptions(lista('Tipo de efeito'), 'Falha de transporte');
    await usuario.selectOptions(lista('Falha'), 'JSON inválido');
    await usuario.click(screen.getByRole('checkbox', { name: 'Regra ligada' }));
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    await screen.findByRole('heading', { name: 'Cenários' });
    expect(criacao[0]?.corpo).toEqual({
      cliente: '76109277673',
      efeito: { tipo: 'transporte', falha: 'json-invalido' },
      frequenciaPct: 100,
      ligada: false,
    });
  });

  it('defeito: só os nomes do catálogo, campo-alvo só nos defeitos de campo e quantidade só na paginação (RN111)', async () => {
    comBase();
    const criacao = capturar('post', '/admin/cenarios/regras', regra(), 201);
    const { usuario } = await abrirNova();

    await usuario.selectOptions(lista('Banco'), 'Banco Estável');
    await usuario.type(lista('Endpoint (operationId)'), 'accountsGetAccountsAccountIdTransactions');
    await usuario.selectOptions(lista('Tipo de efeito'), 'Defeito de dado');

    const nomes = within(lista('Defeito'))
      .getAllByRole('option')
      .map((o) => o.getAttribute('value'))
      .filter((valor) => valor !== '');
    expect(nomes).toEqual(catalogoDeDefeitos.map((d) => d.nome));
    expect(screen.queryByRole('textbox', { name: 'Campo-alvo (opcional)' })).not.toBeInTheDocument();

    await usuario.selectOptions(lista('Defeito'), 'total-records-incoerente');
    expect(screen.queryByRole('textbox', { name: 'Campo-alvo (opcional)' })).not.toBeInTheDocument();
    expect(screen.getByText('Soma 1 a meta.totalRecords.')).toBeInTheDocument();

    await usuario.selectOptions(lista('Defeito'), 'dado-muda-na-paginacao');
    expect(campo('Quantidade (opcional, -1000 a 1000, diferente de 0)')).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Campo-alvo (opcional)' })).not.toBeInTheDocument();

    await usuario.selectOptions(lista('Defeito'), 'campo-obrigatorio-ausente');
    expect(
      screen.queryByRole('textbox', { name: 'Quantidade (opcional, -1000 a 1000, diferente de 0)' }),
    ).not.toBeInTheDocument();
    await usuario.type(campo('Campo-alvo (opcional)'), 'transactionAmount');
    await usuario.clear(campo('Frequência (%)'));
    await usuario.type(campo('Frequência (%)'), '10');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    await screen.findByRole('heading', { name: 'Cenários' });
    expect(criacao[0]?.corpo).toEqual({
      instituicao: 'banco-estavel',
      endpoint: 'accountsGetAccountsAccountIdTransactions',
      efeito: { tipo: 'defeito', defeito: 'campo-obrigatorio-ausente', campo: 'transactionAmount' },
      frequenciaPct: 10,
      ligada: true,
    });
  });

  it('dado-muda-na-paginacao leva a quantidade', async () => {
    comBase();
    const criacao = capturar('post', '/admin/cenarios/regras', regra(), 201);
    const { usuario } = await abrirNova();

    await usuario.selectOptions(lista('Banco'), 'Banco Estável');
    await usuario.selectOptions(lista('Tipo de efeito'), 'Defeito de dado');
    await usuario.selectOptions(lista('Defeito'), 'dado-muda-na-paginacao');
    await usuario.type(campo('Quantidade (opcional, -1000 a 1000, diferente de 0)'), '-3');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    await screen.findByRole('heading', { name: 'Cenários' });
    expect(criacao[0]?.corpo).toEqual({
      instituicao: 'banco-estavel',
      efeito: { tipo: 'defeito', defeito: 'dado-muda-na-paginacao', quantidade: -3 },
      frequenciaPct: 100,
      ligada: true,
    });
  });

  it('formato inválido não chega à administração', async () => {
    comBase();
    const criacao = capturar('post', '/admin/cenarios/regras', regra(), 201);
    const { usuario } = await abrirNova();

    await usuario.selectOptions(lista('Banco'), 'Banco Estável');
    await usuario.selectOptions(lista('Tipo de efeito'), 'Estado do recurso');
    await usuario.selectOptions(lista('Estado'), 'UNAVAILABLE');
    await usuario.clear(campo('Frequência (%)'));
    await usuario.type(campo('Frequência (%)'), '50');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Regra de estado exige frequência de 100%.');
    expect(criacao).toHaveLength(0);
  });

  it('mostra a recusa da administração sem perder o que foi digitado', async () => {
    comBase();
    responder(
      'post',
      '/admin/cenarios/regras',
      { erros: ['endpoint desconhecido', 'início deve vir antes do fim'] },
      422,
    );
    const { usuario, roteador } = await abrirNova();

    await usuario.type(lista('Endpoint (operationId)'), 'nada');
    await usuario.type(campo('Latência (ms)'), '100');
    await usuario.type(campo('Início (UTC, ISO 8601, opcional)'), '2026-12-31T00:00:00Z');
    await usuario.type(campo('Fim (UTC, ISO 8601, opcional)'), '2026-12-01T00:00:00Z');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('O pedido foi recusado.');
    expect(alerta).toHaveTextContent('endpoint desconhecido');
    expect(alerta).toHaveTextContent('início deve vir antes do fim');
    expect(lista('Endpoint (operationId)')).toHaveValue('nada');
    expect(campo('Latência (ms)')).toHaveValue('100');
    expect(roteador.state.location.pathname).toBe('/cenarios/regras/nova');
  });
});

describe('Edição de regra', () => {
  const existente = regra({
    id: 'r-1',
    nome: 'natal lento',
    endpoint: 'accountsGetAccounts',
    efeito: efeito({ latenciaMs: 800 }),
    frequenciaPct: 30,
    ligada: false,
    pacote: 'natal',
  });

  it('abre preenchida e o PUT leva a regra inteira, inclusive desligada e o pacote', async () => {
    comBase();
    responder('get', '/admin/cenarios/regras/r-1', existente);
    const alteracao = capturar('put', '/admin/cenarios/regras/r-1', existente);
    const { usuario } = abrir('/cenarios/regras/r-1');

    expect(await screen.findByRole('heading', { name: 'Regra natal lento' })).toBeInTheDocument();
    await screen.findByRole('button', { name: 'Salvar regra' });
    expect(lista('Banco')).toHaveValue('banco-estavel');
    expect(lista('Endpoint (operationId)')).toHaveValue('accountsGetAccounts');
    expect(campo('Latência (ms)')).toHaveValue('800');
    expect(lista('Pacote')).toHaveValue('natal');
    expect(screen.getByRole('checkbox', { name: 'Regra ligada' })).not.toBeChecked();

    await usuario.clear(campo('Latência (ms)'));
    await usuario.type(campo('Latência (ms)'), '1200');
    await usuario.click(screen.getByRole('button', { name: 'Salvar regra' }));

    await screen.findByRole('heading', { name: 'Cenários' });
    expect(alteracao[0]?.corpo).toEqual({
      nome: 'natal lento',
      instituicao: 'banco-estavel',
      endpoint: 'accountsGetAccounts',
      efeito: { tipo: 'latencia', latenciaMs: 1200 },
      frequenciaPct: 30,
      ligada: false,
      pacote: 'natal',
    });
  });

  it('mantém na lista o banco e o pacote que já não existem', async () => {
    comBase({ pacotes: [] });
    responder('get', '/admin/cenarios/regras/r-1', { ...existente, instituicao: 'banco-apagado' });
    abrir('/cenarios/regras/r-1');

    await screen.findByRole('button', { name: 'Salvar regra' });
    expect(lista('Banco')).toHaveValue('banco-apagado');
    expect(lista('Pacote')).toHaveValue('natal');
  });

  it('regra inexistente', async () => {
    comBase();
    responder('get', '/admin/cenarios/regras/r-9', { erros: ['regra não encontrada'] }, 404);
    abrir('/cenarios/regras/r-9');

    expect(await screen.findByRole('alert')).toHaveTextContent('regra não encontrada');
  });
});
