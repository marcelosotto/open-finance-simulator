import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { massaDaMariana, perfisProntos } from '../../infrastructure/massa/dados';
import { resumo } from '../../apoio/dados';

describe('Clientes da massa (US3, cenário 3)', () => {
  beforeEach(() => {
    comAmbiente();
    responder('get', '/admin/instituicoes', [
      resumo('banco-estavel', 'Banco Estável'),
      resumo('banco-lento', 'Banco Lento'),
    ]);
  });

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
    expect(within(consentimentos).getByText('Autorizado')).toBeInTheDocument();
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

  it('404 diz o que faltou e como resolver (UI19)', async () => {
    responder('get', '/admin/massa/clientes/0', { erros: ['cliente não encontrado'] }, 404);
    abrir('/massa/clientes/0');
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Não há cliente com o documento 0 na massa. Escolha o cliente na lista',
    );
  });

  it('abre o cliente escolhido na lista do banco (UI19)', async () => {
    responder('get', '/admin/massa/perfis', perfisProntos);
    const listas = capturar('get', '/admin/massa/clientes', {
      clientes: [massaDaMariana.cliente],
      inicio: 0,
      quantidade: 1000,
      temMais: false,
    });
    responder('get', '/admin/massa/clientes/12345678909', massaDaMariana);
    const { usuario } = abrir('/massa/clientes');

    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Cliente' }), '12345678909');
    expect(listas.at(-1)?.url.searchParams.get('banco')).toBe('banco-estavel');
    await usuario.click(screen.getByRole('button', { name: 'Abrir cliente' }));

    expect(await screen.findByRole('heading', { name: 'Cliente 12345678909' })).toBeInTheDocument();
  });

  it('cria cliente avulso e abre o cliente criado', async () => {
    responder('get', '/admin/massa/perfis', perfisProntos);
    const pedidos = capturar('post', '/admin/massa/clientes', massaDaMariana, 201);
    responder('get', '/admin/massa/clientes/12345678909', massaDaMariana);
    const { usuario } = abrir('/massa/clientes');

    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Perfil de dados' }), 'sem-movimento');
    await usuario.selectOptions(screen.getByRole('listbox', { name: /^Bancos/ }), ['banco-estavel', 'banco-lento']);
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
    responder('post', '/admin/massa/clientes', { erros: ['perfil desconhecido: x'] }, 422);
    const { usuario } = abrir('/massa/clientes');

    await usuario.selectOptions(await screen.findByRole('listbox', { name: /^Bancos/ }), 'banco-estavel');
    await usuario.click(screen.getByRole('button', { name: 'Criar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('perfil desconhecido: x');
  });
});
