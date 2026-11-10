import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { comBase, pacoteNatal } from './apoio';

const abrirPacotes = async () => {
  const tela = abrir('/cenarios/pacotes');
  await screen.findByRole('table', { name: 'Pacotes' });
  return tela;
};

describe('Pacotes de cenário (RN19)', () => {
  it('lista e cria um pacote, que nasce desligado', async () => {
    comBase();
    const criacao = capturar('put', '/admin/cenarios/pacotes/black-friday', {
      nome: 'black-friday',
      ligado: false,
      criadoEm: '2026-11-05T12:00:00+00:00',
    });
    const { usuario } = await abrirPacotes();
    expect(screen.getByRole('row', { name: /natal/ })).toHaveTextContent('Desligado');

    responder('get', '/admin/cenarios/pacotes', [
      pacoteNatal,
      { nome: 'black-friday', ligado: false, criadoEm: '2026-11-05T12:00:00+00:00' },
    ]);
    await usuario.type(screen.getByRole('textbox', { name: 'Nome do pacote' }), ' black-friday ');
    await usuario.click(screen.getByRole('button', { name: 'Criar pacote' }));

    expect(await screen.findByRole('row', { name: /black-friday/ })).toHaveTextContent('Desligado');
    expect(criacao).toHaveLength(1);
    expect(screen.getByRole('textbox', { name: 'Nome do pacote' })).toHaveValue('');
  });

  it('pacote sem nome não é criado', async () => {
    comBase();
    const { usuario } = await abrirPacotes();

    await usuario.click(screen.getByRole('button', { name: 'Criar pacote' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Informe o nome do pacote.');
  });

  it('liga e desliga o pacote', async () => {
    comBase();
    const ligar = capturar('post', '/admin/cenarios/pacotes/natal/ligar', { ...pacoteNatal, ligado: true });
    const { usuario } = await abrirPacotes();

    responder('get', '/admin/cenarios/pacotes', [{ ...pacoteNatal, ligado: true }]);
    await usuario.click(screen.getByRole('button', { name: 'Ligar natal' }));

    expect(await screen.findByRole('button', { name: 'Desligar natal' })).toBeInTheDocument();
    expect(screen.getByRole('row', { name: /natal/ })).toHaveTextContent('Ligado');
    expect(ligar).toHaveLength(1);

    const desligar = capturar('post', '/admin/cenarios/pacotes/natal/desligar', pacoteNatal);
    responder('get', '/admin/cenarios/pacotes', [pacoteNatal]);
    await usuario.click(screen.getByRole('button', { name: 'Desligar natal' }));

    expect(await screen.findByRole('button', { name: 'Ligar natal' })).toBeInTheDocument();
    expect(desligar).toHaveLength(1);
  });

  it('mostra a recusa ao apagar pacote com regras', async () => {
    comBase();
    const remocao = capturar('delete', '/admin/cenarios/pacotes/natal', { erros: ['pacote tem regras'] }, 409);
    const { usuario } = await abrirPacotes();

    await usuario.click(within(screen.getByRole('row', { name: /natal/ })).getByRole('button', { name: 'Apagar' }));
    await usuario.click(
      within(screen.getByRole('dialog', { name: 'Apagar o pacote natal?' })).getByRole('button', { name: 'Confirmar' }),
    );

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('O pedido conflita com o estado atual.');
    expect(alerta).toHaveTextContent('pacote tem regras');
    expect(remocao).toHaveLength(1);
    expect(screen.getByRole('row', { name: /natal/ })).toBeInTheDocument();
  });

  it('lista vazia', async () => {
    comBase({ pacotes: [] });
    abrir('/cenarios/pacotes');
    expect(await screen.findByText('Nenhum pacote.')).toBeInTheDocument();
  });
});
