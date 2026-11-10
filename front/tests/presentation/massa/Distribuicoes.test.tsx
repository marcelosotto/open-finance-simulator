import { screen } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { distribuicaoSalva } from '../../infrastructure/massa/dados';

const preencherBancos = async (usuario: ReturnType<typeof abrir>['usuario']) => {
  await usuario.type(await screen.findByRole('textbox', { name: 'Endereço do banco 1' }), 'banco-estavel');
  await usuario.type(screen.getByRole('spinbutton', { name: '% do banco 1' }), '60');
};

describe('Distribuições (UI07, RN56)', () => {
  beforeEach(() => comAmbiente());

  it('mostra a soma e bloqueia salvar fora de 100%; produtos não somam', async () => {
    responder('get', '/admin/massa/distribuicoes', []);
    const { usuario } = abrir('/massa/distribuicoes');

    expect(await screen.findByText('Nenhuma distribuição salva.')).toBeInTheDocument();
    await preencherBancos(usuario);

    expect(screen.getByText('Soma dos bancos: 60%')).toBeInTheDocument();
    expect(screen.getByText('Soma dos perfis: 100%')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Salvar' })).toBeDisabled();
    expect(screen.getByText(/precisam somar 100%/)).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: 'Adicionar linha em Bancos' }));
    await usuario.type(screen.getByRole('textbox', { name: 'Endereço do banco 2' }), 'banco-lento');
    await usuario.type(screen.getByRole('spinbutton', { name: '% do banco 2' }), '40');

    await usuario.click(screen.getByRole('button', { name: 'Adicionar linha em Produtos' }));
    await usuario.type(screen.getByRole('textbox', { name: 'Produto 1' }), 'contas');
    await usuario.type(screen.getByRole('spinbutton', { name: '% do produto 1' }), '90');

    expect(screen.getByText('Soma dos bancos: 100%')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Salvar' })).toBeEnabled();
    expect(screen.queryByText(/precisam somar 100%/)).not.toBeInTheDocument();
  });

  it('salva a distribuição com o PUT do contrato', async () => {
    responder('get', '/admin/massa/distribuicoes', []);
    const pedidos = capturar('put', '/admin/massa/distribuicoes/minha', {
      nome: 'minha',
      distribuicao: { ...distribuicaoSalva.distribuicao, produtos: {}, pjPct: 0 },
    });
    const { usuario } = abrir('/massa/distribuicoes');

    await preencherBancos(usuario);
    await usuario.click(screen.getByRole('button', { name: 'Adicionar linha em Bancos' }));
    await usuario.type(screen.getByRole('textbox', { name: 'Endereço do banco 2' }), 'banco-lento');
    await usuario.type(screen.getByRole('spinbutton', { name: '% do banco 2' }), '40');
    await usuario.type(screen.getByRole('textbox', { name: 'Nome' }), 'minha');
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByText('Distribuição salva.')).toBeInTheDocument();
    expect(pedidos[0]?.corpo).toEqual({
      bancos: { 'banco-estavel': 60, 'banco-lento': 40 },
      perfis: { 'cliente-comum': 100 },
      produtos: {},
      pjPct: 0,
    });
  });

  it('abre uma salva para editar e mostra a recusa da API', async () => {
    responder('get', '/admin/massa/distribuicoes', [distribuicaoSalva]);
    responder('put', '/admin/massa/distribuicoes/meia-meia', { erros: ['banco desconhecido: banco-lento'] }, 422);
    const { usuario } = abrir('/massa/distribuicoes');

    await usuario.click(await screen.findByRole('button', { name: 'Editar meia-meia' }));
    expect(screen.getByRole('textbox', { name: 'Nome' })).toHaveValue('meia-meia');
    expect(screen.getByRole('textbox', { name: 'Endereço do banco 2' })).toHaveValue('banco-lento');
    expect(screen.getByRole('spinbutton', { name: '% do produto 1' })).toHaveValue(90);
    await usuario.click(screen.getByRole('button', { name: 'Salvar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('banco desconhecido: banco-lento');
  });
});
