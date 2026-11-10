import { useState } from 'react';
import { screen } from '@testing-library/react';
import { renderizar } from '../../apoio/renderizar';
import { capturar, responder } from '../../apoio/api';
import { SeletorDeCliente } from '@/presentation/massa/SeletorDeCliente';

const MARIANA = {
  documento: '95000000080',
  tipo: 'PF',
  nome: 'Mariana Alves',
  marca: 'hml',
  natureza: 'persistido',
  loteId: null,
};

const Teste = ({ banco, opcional = false }: { readonly banco: string; readonly opcional?: boolean }) => {
  const [valor, setValor] = useState('');
  return (
    <>
      <SeletorDeCliente
        banco={banco}
        nomeDoBanco="Banco Azul"
        rotulo="Cliente"
        valor={valor}
        aoMudar={setValor}
        opcional={opcional}
      />
      <output aria-label="escolhido">{valor}</output>
    </>
  );
};

describe('Seletor de cliente (UI19)', () => {
  it('sem banco, pede o banco primeiro e não chama a administração', () => {
    const pedidos = capturar('get', '/admin/massa/clientes', {
      clientes: [],
      inicio: 0,
      quantidade: 1000,
      temMais: false,
    });
    renderizar(<Teste banco="" />);
    expect(screen.getByRole('combobox', { name: 'Cliente' })).toBeDisabled();
    expect(screen.getByRole('option', { name: 'Escolha o banco primeiro.' })).toBeInTheDocument();
    expect(pedidos).toHaveLength(0);
  });

  it('escolhe da lista ou digita um documento fora dela; avisa quando há mais de 1000', async () => {
    responder('get', '/admin/massa/clientes', { clientes: [MARIANA], inicio: 0, quantidade: 1000, temMais: true });
    const { usuario } = renderizar(<Teste banco="banco-azul" />);

    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Cliente' }), '95000000080');
    expect(screen.getByRole('status', { name: 'escolhido' })).toHaveTextContent('95000000080');
    expect(screen.getByText(/Mostrando os 1000 primeiros/)).toBeInTheDocument();

    await usuario.type(screen.getByRole('textbox', { name: 'Documento do cliente' }), ' 111 ');
    expect(screen.getByRole('status', { name: 'escolhido' })).toHaveTextContent('111');
    expect(screen.getByRole('combobox', { name: 'Cliente' })).toHaveValue('');
  });

  it('banco inexistente diz o que fazer', async () => {
    responder('get', '/admin/massa/clientes', { erros: ['banco não encontrado: banco-azul'] }, 404);
    renderizar(<Teste banco="banco-azul" />);
    expect(await screen.findByRole('alert')).toHaveTextContent(
      'O banco banco-azul não existe mais. Escolha outro banco',
    );
  });

  it('opcional e sem clientes: sem alerta, só o campo para digitar', async () => {
    renderizar(<Teste banco="" opcional />);
    expect(await screen.findByRole('textbox', { name: 'Documento do cliente' })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });

  it('opcional sem banco lista todos, com "qualquer cliente" primeiro', async () => {
    const pedidos = capturar('get', '/admin/massa/clientes', {
      clientes: [MARIANA],
      inicio: 0,
      quantidade: 1000,
      temMais: false,
    });
    renderizar(<Teste banco="" opcional />);
    expect(await screen.findByRole('option', { name: 'Qualquer cliente' })).toBeInTheDocument();
    expect(pedidos[0]!.url.searchParams.has('banco')).toBe(false);
  });
});
