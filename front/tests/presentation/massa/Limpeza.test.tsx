import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { previaDoLote } from '../../infrastructure/massa/dados';

describe('Limpeza (US3, cenário 5)', () => {
  beforeEach(() => comAmbiente());

  it('mostra a prévia antes de confirmar e só então apaga', async () => {
    const pedidos = capturar('post', '/admin/massa/limpezas', previaDoLote);
    const { usuario } = abrir('/massa/limpeza');

    await usuario.type(await screen.findByRole('textbox', { name: 'Id do lote ou endereço do banco' }), 'lote-0');
    expect(screen.queryByRole('button', { name: 'Apagar' })).not.toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Ver prévia' }));

    const previa = await screen.findByRole('region', { name: 'O que será apagado' });
    expect(within(previa).getByText('Clientes: 10')).toBeInTheDocument();
    expect(within(previa).getByText('Recursos: 20')).toBeInTheDocument();
    expect(within(previa).getByText('Instituições: 0')).toBeInTheDocument();
    expect(pedidos).toHaveLength(1);
    expect(pedidos[0]?.corpo).toEqual({ alvo: 'lote', id: 'lote-0', confirmar: false });

    await usuario.click(screen.getByRole('button', { name: 'Apagar' }));
    await usuario.click(within(screen.getByRole('dialog')).getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByText('Limpeza feita.')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'O que foi apagado' })).toBeInTheDocument();
    expect(pedidos[1]?.corpo).toEqual({ alvo: 'lote', id: 'lote-0', confirmar: true });
  });

  it('mudar o alvo descarta a prévia; tudo não pede id', async () => {
    capturar('post', '/admin/massa/limpezas', previaDoLote);
    const { usuario } = abrir('/massa/limpeza');

    await usuario.type(await screen.findByRole('textbox', { name: 'Id do lote ou endereço do banco' }), 'lote-0');
    await usuario.click(screen.getByRole('button', { name: 'Ver prévia' }));
    expect(await screen.findByRole('button', { name: 'Apagar' })).toBeInTheDocument();

    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Alvo' }), 'tudo');

    expect(screen.queryByRole('button', { name: 'Apagar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Id do lote ou endereço do banco' })).not.toBeInTheDocument();
  });

  it('sem id, recusa na tela; com lote em andamento, mostra a recusa da API', async () => {
    responder('post', '/admin/massa/limpezas', { erros: ['lote em andamento'] }, 409);
    const { usuario } = abrir('/massa/limpeza');

    await usuario.click(await screen.findByRole('button', { name: 'Ver prévia' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Informe o id do lote ou o endereço do banco.');

    await usuario.type(screen.getByRole('textbox', { name: 'Id do lote ou endereço do banco' }), 'lote-0');
    await usuario.click(screen.getByRole('button', { name: 'Ver prévia' }));
    expect(await screen.findByText('lote em andamento')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Apagar' })).not.toBeInTheDocument();
  });
});
