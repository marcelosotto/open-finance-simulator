import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { clienteDoLote, distribuicaoSalva, lote } from '../../infrastructure/massa/dados';

const vinte = Array.from({ length: 20 }, (_, i) => clienteDoLote(i + 1));

describe('Lotes (US3, cenários 2 e 3)', () => {
  beforeEach(() => comAmbiente());

  it('gera lote com a distribuição escolhida e abre o andamento', async () => {
    responder('get', '/admin/massa/lotes', [lote('concluido', 10)]);
    responder('get', '/admin/massa/distribuicoes', [distribuicaoSalva]);
    const pedidos = capturar('post', '/admin/massa/lotes', lote(), 202);
    responder('get', '/admin/massa/lotes/lote-0', lote());
    responder('get', '/admin/massa/lotes/lote-0/clientes', []);
    const { usuario } = abrir('/massa/lotes');

    expect(await screen.findByRole('link', { name: 'lote-0' })).toHaveAttribute('href', '/massa/lotes/lote-0');
    expect(screen.getByText('Concluído')).toBeInTheDocument();
    const quantidade = await screen.findByRole('spinbutton', { name: 'Quantidade de clientes' });
    await usuario.clear(quantidade);
    await usuario.type(quantidade, '5');
    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Distribuição' }), 'meia-meia');
    await usuario.click(screen.getByRole('button', { name: 'Gerar lote' }));

    expect(await screen.findByRole('heading', { name: 'Lote lote-0' })).toBeInTheDocument();
    expect(pedidos[0]?.corpo).toEqual({
      quantidade: 5,
      distribuicaoNome: 'meia-meia',
      consentimentosPorCliente: 1,
      historicoMeses: 12,
    });
  });

  it('mostra a recusa da API ao gerar', async () => {
    responder('get', '/admin/massa/lotes', []);
    responder('get', '/admin/massa/distribuicoes', []);
    responder('post', '/admin/massa/lotes', { erros: ['quantidade acima do saldo de documentos'] }, 422);
    const { usuario } = abrir('/massa/lotes');

    expect(await screen.findByText('Nenhum lote.')).toBeInTheDocument();
    await usuario.click(await screen.findByRole('button', { name: 'Gerar lote' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('quantidade acima do saldo de documentos');
  });

  it('relê o andamento enquanto roda e para quando termina', async () => {
    responder('get', '/admin/massa/lotes/lote-0', lote('em_andamento', 3));
    responder('get', '/admin/massa/lotes/lote-0/clientes', []);
    abrir('/massa/lotes/lote-0');

    expect(await screen.findByText('Em andamento')).toBeInTheDocument();
    expect(screen.getByText('3 de 10')).toBeInTheDocument();
    expect(screen.getByRole('progressbar', { name: 'Progresso do lote' })).toHaveAttribute('value', '3');
    expect(screen.getByRole('button', { name: 'Cancelar lote' })).toBeInTheDocument();

    responder('get', '/admin/massa/lotes/lote-0', lote('concluido', 10));
    expect(await screen.findByText('Concluído', {}, { timeout: 5000 })).toBeInTheDocument();
    expect(screen.getByText('10 de 10')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cancelar lote' })).not.toBeInTheDocument();
  }, 10000);

  it('cancela com confirmação', async () => {
    responder('get', '/admin/massa/lotes/lote-0', lote());
    responder('get', '/admin/massa/lotes/lote-0/clientes', []);
    const pedidos = capturar('post', '/admin/massa/lotes/lote-0/cancelar', lote(), 202);
    const { usuario } = abrir('/massa/lotes/lote-0');

    await usuario.click(await screen.findByRole('button', { name: 'Cancelar lote' }));
    const dialogo = screen.getByRole('dialog', { name: 'Cancelar o lote lote-0?' });
    await usuario.click(within(dialogo).getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByText('Cancelamento pedido.')).toBeInTheDocument();
    expect(pedidos).toHaveLength(1);
  });

  it('mostra a recusa da API ao cancelar lote já terminado', async () => {
    responder('get', '/admin/massa/lotes/lote-0', lote());
    responder('get', '/admin/massa/lotes/lote-0/clientes', []);
    responder('post', '/admin/massa/lotes/lote-0/cancelar', { erros: ['lote já terminou'] }, 409);
    const { usuario } = abrir('/massa/lotes/lote-0');

    await usuario.click(await screen.findByRole('button', { name: 'Cancelar lote' }));
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('lote já terminou');
  });

  it('pagina os clientes com inicio e quantidade e leva ao cliente', async () => {
    responder('get', '/admin/massa/lotes/lote-0', lote('concluido', 10));
    const pedidos = capturar('get', '/admin/massa/lotes/lote-0/clientes', vinte);
    const { usuario } = abrir('/massa/lotes/lote-0');

    const link = await screen.findByRole('link', { name: '00000000001' });
    expect(link).toHaveAttribute('href', '/massa/clientes/00000000001');
    expect(screen.getByText('Clientes 1 a 20')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Página anterior' })).toBeDisabled();
    expect(pedidos[0]?.url.searchParams.get('inicio')).toBe('0');
    expect(pedidos[0]?.url.searchParams.get('quantidade')).toBe('20');

    await usuario.click(screen.getByRole('button', { name: 'Próxima página' }));

    expect(await screen.findByText('Clientes 21 a 40')).toBeInTheDocument();
    expect(pedidos.at(-1)?.url.searchParams.get('inicio')).toBe('20');
    expect(screen.getByRole('button', { name: 'Página anterior' })).toBeEnabled();
  });

  it('última página desabilita a próxima', async () => {
    responder('get', '/admin/massa/lotes/lote-0', lote('parcial', 2, 'cancelado'));
    responder('get', '/admin/massa/lotes/lote-0/clientes', [clienteDoLote(1)]);
    abrir('/massa/lotes/lote-0');

    expect(await screen.findByText('cancelado')).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: 'Próxima página' })).toBeDisabled();
  });
});
