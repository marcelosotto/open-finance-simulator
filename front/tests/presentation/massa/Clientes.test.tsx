import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { massaDaMariana, perfisProntos } from '../../infrastructure/massa/dados';

describe('Clientes da massa (US3, cenário 3)', () => {
  beforeEach(() => comAmbiente());

  it('mostra recursos e consentimentos, com link para a visão do consentimento', async () => {
    responder('get', '/admin/massa/clientes/12345678909', massaDaMariana);
    abrir('/massa/clientes/12345678909');

    expect(await screen.findByText('Mariana Alves')).toBeInTheDocument();
    expect(screen.getByText('Avulso')).toBeInTheDocument();
    const recursos = screen.getByRole('table', { name: 'Recursos' });
    expect(within(recursos).getByText('hml-p-r0x0')).toBeInTheDocument();
    expect(within(recursos).getByText('ACCOUNT')).toBeInTheDocument();
    const consentimentos = screen.getByRole('table', { name: 'Consentimentos' });
    expect(within(consentimentos).getByRole('link', { name: 'urn:hml:p-c0x0' })).toHaveAttribute(
      'href',
      `/consentimentos/${encodeURIComponent('urn:hml:p-c0x0')}`,
    );
    expect(within(consentimentos).getByText('AUTHORISED')).toBeInTheDocument();
  });

  it('cliente de lote leva ao lote; sem consentimento avisa', async () => {
    responder('get', '/admin/massa/clientes/1', {
      ...massaDaMariana,
      cliente: { ...massaDaMariana.cliente, documento: '1', loteId: 'lote-0' },
      consentimentos: [],
      recursos: [],
    });
    abrir('/massa/clientes/1');

    expect(await screen.findByRole('link', { name: 'lote-0' })).toHaveAttribute('href', '/massa/lotes/lote-0');
    expect(screen.getByText('Nenhum consentimento.')).toBeInTheDocument();
    expect(screen.getByText('Nenhum recurso.')).toBeInTheDocument();
  });

  it('mostra o 404 da API', async () => {
    responder('get', '/admin/massa/clientes/0', { erros: ['cliente não encontrado'] }, 404);
    abrir('/massa/clientes/0');
    expect(await screen.findByRole('alert')).toHaveTextContent('cliente não encontrado');
  });

  it('busca por documento', async () => {
    responder('get', '/admin/massa/perfis', perfisProntos);
    responder('get', '/admin/massa/clientes/12345678909', massaDaMariana);
    const { usuario } = abrir('/massa/clientes');

    await usuario.type(await screen.findByRole('textbox', { name: 'Documento (CPF ou CNPJ)' }), '12345678909');
    await usuario.click(screen.getByRole('button', { name: 'Abrir cliente' }));

    expect(await screen.findByRole('heading', { name: 'Cliente 12345678909' })).toBeInTheDocument();
  });

  it('cria cliente avulso e abre o cliente criado', async () => {
    responder('get', '/admin/massa/perfis', perfisProntos);
    const pedidos = capturar('post', '/admin/massa/clientes', massaDaMariana, 201);
    responder('get', '/admin/massa/clientes/12345678909', massaDaMariana);
    const { usuario } = abrir('/massa/clientes');

    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Perfil de dados' }), 'sem-movimento');
    await usuario.type(screen.getByRole('textbox', { name: /^Bancos/ }), 'banco-estavel, banco-lento');
    await usuario.type(screen.getByRole('textbox', { name: 'Nome (opcional)' }), 'Mariana Alves');
    await usuario.click(screen.getByRole('button', { name: 'Criar' }));

    expect(await screen.findByRole('heading', { name: 'Cliente 12345678909' })).toBeInTheDocument();
    expect(pedidos[0]?.corpo).toEqual({
      tipo: 'PF',
      perfil: 'sem-movimento',
      bancos: ['banco-estavel', 'banco-lento'],
      nome: 'Mariana Alves',
    });
  });

  it('mostra a recusa da API ao criar', async () => {
    responder('get', '/admin/massa/perfis', perfisProntos);
    responder('post', '/admin/massa/clientes', { erros: ['banco desconhecido: banco-x'] }, 422);
    const { usuario } = abrir('/massa/clientes');

    await usuario.type(await screen.findByRole('textbox', { name: /^Bancos/ }), 'banco-x');
    await usuario.click(screen.getByRole('button', { name: 'Criar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('banco desconhecido: banco-x');
  });
});
