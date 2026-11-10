import { screen, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { comAmbiente, responder } from '../../apoio/api';
import { resumo } from '../../apoio/dados';

const XP = { organisationId: 'hml-p-receptora-padrao', nome: 'XP', usuario: 'teste-ab12', senha: 'teste-senha-cd34' };

const situacaoDoPasso = (titulo: RegExp) => within(screen.getByRole('heading', { name: titulo }));

describe('Primeiros passos (UI18)', () => {
  beforeEach(() => {
    comAmbiente();
    responder('get', '/admin/instituicoes', [
      resumo('banco-estavel', 'Banco Estável'),
      resumo('velho', 'Velho', [], 'inativa'),
    ]);
  });

  it('é a página inicial e marca o que ainda falta', async () => {
    responder('get', '/admin/receptoras', []);
    abrir('/');

    expect(await screen.findByRole('heading', { name: 'Primeiros passos', level: 1 })).toBeInTheDocument();
    expect(await screen.findByText(/Já vêm 1 bancos prontos/)).toBeInTheDocument();
    expect(situacaoDoPasso(/^1\. Criar um banco/).getByText('Feito')).toBeInTheDocument();
    expect(situacaoDoPasso(/^2\. Gerar clientes/).getByText('A fazer')).toBeInTheDocument();
    expect(situacaoDoPasso(/^3\. Criar um consentimento/).getByText('A fazer')).toBeInTheDocument();
    expect(situacaoDoPasso(/^4\. Ver, aprovar ou revogar/).getByText('Quando quiser')).toBeInTheDocument();
    expect(screen.getByText('Ainda não há receptora. Crie uma em Receptoras.')).toBeInTheDocument();
    expect(screen.getByLabelText('Exemplo de pedido de token')).toHaveTextContent('client_id=<usuario>');
  });

  it('com massa e consentimento, marca como feito e mostra as credenciais da receptora padrão no exemplo', async () => {
    responder('get', '/admin/massa/resumo', { clientes: 12, consentimentos: 3, recursos: 40, lotes: 1 });
    responder('get', '/admin/receptoras', [XP]);
    abrir('/primeiros-passos');

    expect(await screen.findByText(/Agora há 12 clientes/)).toBeInTheDocument();
    expect(situacaoDoPasso(/^2\. Gerar clientes/).getByText('Feito')).toBeInTheDocument();
    expect(situacaoDoPasso(/^3\. Criar um consentimento/).getByText('Feito')).toBeInTheDocument();
    expect(await screen.findByText('Receptora XP: usuário teste-ab12, senha teste-senha-cd34.')).toBeInTheDocument();
    const exemplo = screen.getByLabelText('Exemplo de pedido de token');
    expect(exemplo).toHaveTextContent('/banco-estavel/token');
    expect(exemplo).toHaveTextContent('client_secret=teste-senha-cd34');
  });

  it('cada passo leva à tela certa e a tabela diz para que serve cada tela', async () => {
    responder('get', '/admin/receptoras', []);
    abrir('/primeiros-passos');

    const passos = within(await screen.findByRole('list', { name: 'Primeiros passos' }));
    const destinos = passos.getAllByRole('link', { name: 'Abrir a tela' }).map((link) => link.getAttribute('href'));
    expect(destinos).toEqual([
      '/bancos/novo',
      '/massa/lotes',
      '/consentimentos/novo',
      '/consentimentos',
      '/receptoras',
    ]);

    const tabela = within(screen.getByRole('table', { name: 'Tela → para que serve' }));
    expect(tabela.getByRole('link', { name: 'Novo consentimento' })).toHaveAttribute('href', '/consentimentos/novo');
    expect(tabela.getByText(/Simula um cliente autorizando outra instituição/)).toBeInTheDocument();
  });
});

describe('Ajuda no topo das telas (UI17)', () => {
  beforeEach(() => comAmbiente());

  it.each([
    ['/bancos', 'Os bancos simulados.', 'Não precisa de nada antes.'],
    ['/massa/lotes', 'Gera muitos clientes de uma vez', 'Precisa existir antes: um banco'],
    ['/cenarios/pacotes', 'Agrupa regras', 'Não precisa de nada antes.'],
  ])('%s diz para que serve, o que precisa antes e leva a Primeiros passos', async (caminho, para, precisa) => {
    responder('get', '/admin/instituicoes', []);
    responder('get', '/admin/massa/lotes', []);
    responder('get', '/admin/massa/distribuicoes', []);
    responder('get', '/admin/cenarios/pacotes', []);
    abrir(caminho);

    const ajuda = within(await screen.findByRole('complementary', { name: 'Primeiros passos' }));
    expect(ajuda.getByText(new RegExp(para))).toBeInTheDocument();
    expect(ajuda.getByText(new RegExp(precisa))).toBeInTheDocument();
    expect(ajuda.getByRole('link', { name: 'Primeiros passos' })).toHaveAttribute('href', '/primeiros-passos');
  });
});
