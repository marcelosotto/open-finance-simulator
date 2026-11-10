import { screen, waitFor } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { resumo } from '../../apoio/dados';
import { useAutor } from '@/presentation/layout/autor';

describe('Layout (UI01, UI16)', () => {
  beforeEach(() => {
    responder('get', '/admin/instituicoes', [resumo('banco-estavel', 'Banco Estável')]);
    useAutor.setState({ nome: '' });
  });

  it('mostra a marca hml em destaque e a faixa de simulação (RN72, PR03)', async () => {
    comAmbiente('hml');
    abrir('/');
    expect(await screen.findByLabelText('Ambiente hml')).toHaveTextContent('hml');
    expect(screen.getByRole('note')).toHaveTextContent('Ambiente simulado, dados fictícios');
  });

  it('com a marca vazia não mostra marca, mas a faixa continua', async () => {
    comAmbiente('');
    abrir('/bancos');
    expect(await screen.findByRole('link', { name: 'Banco Estável' })).toBeInTheDocument();
    expect(screen.queryByLabelText(/^Ambiente /)).not.toBeInTheDocument();
    expect(screen.getByRole('note')).toHaveTextContent('Ambiente simulado, dados fictícios');
  });

  it('o nome digitado vai em x-sim-autor e fica guardado no navegador (Q8)', async () => {
    comAmbiente();
    const pedidos = capturar(
      'post',
      '/admin/receptoras',
      {
        organisationId: 'org',
        nome: 'Coletor',
        usuario: 'u',
        senha: 's',
      },
      201,
    );
    responder('get', '/admin/receptoras', []);
    const { usuario } = abrir('/receptoras');

    await usuario.type(screen.getByRole('textbox', { name: 'Seu nome' }), 'Diego');
    await usuario.type(screen.getByRole('textbox', { name: 'Nome' }), 'Coletor');
    await usuario.click(screen.getByRole('button', { name: 'Criar receptora' }));

    await waitFor(() => expect(pedidos).toHaveLength(1));
    expect(pedidos[0]!.cabecalhos.get('x-sim-autor')).toBe('Diego');
    expect(localStorage.getItem('sim-autor')).toContain('Diego');
  });

  it('rota desconhecida', async () => {
    comAmbiente();
    abrir('/nao-existe');
    expect(
      await screen.findByRole('heading', { name: 'Esta página não existe. Comece por Primeiros passos.' }),
    ).toBeInTheDocument();
  });
});
