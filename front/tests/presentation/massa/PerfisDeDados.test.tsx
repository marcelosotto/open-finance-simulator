import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { perfilCriado, perfisProntos } from '../../infrastructure/massa/dados';

describe('Perfis de dados (US3, cenário 4)', () => {
  beforeEach(() => {
    comAmbiente();
    responder('get', '/admin/massa/perfis', [...perfisProntos, perfilCriado]);
  });

  it('o índice da massa leva às sub-telas', async () => {
    abrir('/massa');
    expect(await screen.findByRole('link', { name: 'Perfis de dados' })).toHaveAttribute('href', '/massa/perfis');
    expect(screen.getByRole('link', { name: 'Limpeza' })).toHaveAttribute('href', '/massa/limpeza');
  });

  it('lista prontos e criados; os prontos não têm editar nem apagar', async () => {
    abrir('/massa/perfis');

    const tabela = await screen.findByRole('table', { name: 'Perfis prontos e criados' });
    expect(within(tabela).getAllByText('Pronto')).toHaveLength(3);
    expect(within(tabela).getByText('Criado')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Editar cliente-comum' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Apagar alto-volume' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Editar meu-perfil' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Apagar meu-perfil' })).toBeInTheDocument();
  });

  it('edita um perfil criado e manda o PUT sem o nome no corpo', async () => {
    const pedidos = capturar('put', '/admin/massa/perfis/meu-perfil', { ...perfilCriado, saldoInicial: 250 });
    const { usuario } = abrir('/massa/perfis');

    await usuario.click(await screen.findByRole('button', { name: 'Editar meu-perfil' }));
    expect(screen.getByRole('textbox', { name: 'Nome' })).toHaveValue('meu-perfil');
    const saldo = screen.getByRole('spinbutton', { name: 'Saldo inicial' });
    await usuario.clear(saldo);
    await usuario.type(saldo, '250');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('Salvo.')).toBeInTheDocument();
    expect(pedidos[0]?.corpo).toEqual({
      transacoesMin: 1,
      transacoesMax: 2,
      valorMin: 5,
      valorMax: 50,
      saldoInicial: 250,
      tiposDeLancamento: ['PIX'],
      produtos: ['contas'],
    });
  });

  it('recusa na tela um nome de perfil pronto, sem chamar a API', async () => {
    const pedidos = capturar('put', '/admin/massa/perfis/cliente-comum', perfisProntos[0] ?? {});
    const { usuario } = abrir('/massa/perfis');

    await usuario.type(await screen.findByRole('textbox', { name: 'Nome' }), 'cliente-comum');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Perfil pronto não pode ser alterado nem apagado');
    expect(pedidos).toHaveLength(0);
  });

  it('mostra a recusa da API ao apagar perfil em uso', async () => {
    responder('delete', '/admin/massa/perfis/meu-perfil', { erros: ['perfil em uso não pode ser apagado'] }, 409);
    const { usuario } = abrir('/massa/perfis');

    await usuario.click(await screen.findByRole('button', { name: 'Apagar meu-perfil' }));
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('perfil em uso não pode ser apagado');
  });
});
