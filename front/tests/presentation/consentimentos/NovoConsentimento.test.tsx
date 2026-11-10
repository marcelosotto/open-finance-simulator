import { screen, waitFor, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { resumo } from '../../apoio/dados';
import { opcoesPF, visaoPJ } from '../../apoio/consentimentos';
import { useMenu } from '@/presentation/consentimentos/menu';

type Usuario = ReturnType<typeof abrir>['usuario'];

const XP = { organisationId: 'hml-p-receptora-padrao', nome: 'XP', usuario: 'teste-1', senha: 'teste-senha-1' };
const MARIANA = {
  documento: '95000000080',
  tipo: 'PF',
  nome: '[HML] Mariana Alves',
  marca: 'hml',
  natureza: 'persistido',
  loteId: null,
};
const AURORA = {
  documento: 'P0000001000182',
  tipo: 'PJ',
  nome: '[HML] Indústria Aurora',
  marca: 'hml',
  natureza: 'persistido',
  loteId: null,
};

const comClientes = (...clientes: object[]) =>
  responder('get', '/admin/massa/clientes', { clientes, inicio: 0, quantidade: 1000, temMais: false });

/** Passo 1: banco, cliente da lista e receptora (UI10, UI19); para nas permissões. */
const chegarAsPermissoes = async (usuario: Usuario, cliente = MARIANA.documento) => {
  await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
  await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Cliente' }), cliente);
  await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
  await screen.findByRole('group', { name: 'Permissões de contas-saldos' });
};

describe('Novo consentimento (UI10, UI13, UI19)', () => {
  beforeEach(() => {
    useMenu.getState().recomecar();
    comAmbiente();
    responder('get', '/admin/instituicoes', [
      resumo('banco-estavel', 'Banco Estável'),
      resumo('velho', 'Velho', [], 'inativa'),
    ]);
    responder('get', '/admin/receptoras', [XP]);
    responder('get', '/admin/consentimentos/opcoes', opcoesPF);
    comClientes(MARIANA, AURORA);
  });

  it('escolhe banco e cliente na lista, usa a receptora única e cria com contas, cartões e 12 meses (RN80, RN95, RN124)', async () => {
    const criados = capturar(
      'post',
      '/admin/consentimentos',
      visaoPJ(0, { cliente: '95000000080', alcada: null }),
      201,
    );
    responder('get', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0', visaoPJ(0, { alcada: null }));
    const { usuario } = abrir('/consentimentos/novo');

    expect(await screen.findByText('1. Banco, cliente e receptora')).toHaveAttribute('aria-current', 'step');
    expect(screen.getByRole('complementary', { name: 'Primeiros passos' })).toHaveTextContent(
      'Precisa existir antes: um banco e clientes nesse banco',
    );
    expect(await screen.findByRole('combobox', { name: 'Cliente' })).toBeDisabled();
    expect(screen.getByText('Receptora: XP (a única cadastrada).')).toBeInTheDocument();
    expect(screen.queryByRole('combobox', { name: 'Receptora' })).not.toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Velho' })).not.toBeInTheDocument();

    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    expect(await screen.findByRole('option', { name: '[HML] Mariana Alves — 95000000080 (PF)' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: '[HML] Indústria Aurora — P0000001000182 (PJ)' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeDisabled();
    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Cliente' }), MARIANA.documento);
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));

    await usuario.click(await screen.findByRole('checkbox', { name: 'Grupo contas-saldos' }));
    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo cartoes-limites' }));
    expect(screen.getByRole('checkbox', { name: 'hml-p-r0x1' })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: 'hml-p-r0x0' })).toBeChecked();
    expect(screen.getByText('Compartilhados por inteiro, sem seleção: LOAN')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));

    await usuario.type(screen.getByRole('spinbutton', { name: /Prazo em meses/ }), '12');
    expect(screen.queryByRole('spinbutton', { name: /Aprovações pendentes/ })).not.toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Criar consentimento' }));

    expect(await screen.findByTestId('estado')).toHaveTextContent('AUTHORISED');
    expect(criados[0]!.corpo).toEqual({
      cliente: '95000000080',
      banco: 'banco-estavel',
      receptora: 'hml-p-receptora-padrao',
      permissoes: [
        'ACCOUNTS_READ',
        'ACCOUNTS_BALANCES_READ',
        'CREDIT_CARDS_ACCOUNTS_READ',
        'CREDIT_CARDS_ACCOUNTS_LIMITS_READ',
        'RESOURCES_READ',
      ],
      contas: ['hml-p-r0x1'],
      cartoes: ['hml-p-r0x0'],
      prazoMeses: 12,
    });
  });

  it('com mais de uma receptora, pede a escolha', async () => {
    responder('get', '/admin/receptoras', [XP, { organisationId: 'org-2', nome: 'Outro app' }]);
    const { usuario } = abrir('/consentimentos/novo');

    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Cliente' }), MARIANA.documento);
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeDisabled();
    await usuario.selectOptions(screen.getByRole('combobox', { name: 'Receptora' }), 'org-2');
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeEnabled();
  });

  it('banco sem clientes manda gerar massa (UI19)', async () => {
    comClientes();
    const { usuario } = abrir('/consentimentos/novo');

    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');

    const aviso = await screen.findByRole('alert');
    expect(aviso).toHaveTextContent('O Banco Estável ainda não tem clientes.');
    expect(screen.getByRole('link', { name: 'Gerar massa de clientes' })).toHaveAttribute('href', '/massa/lotes');
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeDisabled();
  });

  it('sem prazo vai sem data final (RN85)', async () => {
    const criados = capturar('post', '/admin/consentimentos', visaoPJ(0, { alcada: null }), 201);
    responder('get', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0', visaoPJ(0, { alcada: null }));
    const { usuario } = abrir('/consentimentos/novo');

    await chegarAsPermissoes(usuario);
    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo cadastro-identificacao' }));
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    expect(screen.getByText('Indeterminado')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Criar consentimento' }));

    expect(await screen.findByText('Sem data final')).toBeInTheDocument();
    expect(criados[0]!.corpo).not.toHaveProperty('prazoMeses');
    expect(criados[0]!.corpo).not.toHaveProperty('contas');
  });

  it('desmarcar todas as contas impede confirmar (RN95)', async () => {
    const { usuario } = abrir('/consentimentos/novo');
    await chegarAsPermissoes(usuario);

    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' }));
    await usuario.click(screen.getByRole('checkbox', { name: 'hml-p-r0x1' }));

    expect(screen.getByRole('alert')).toHaveTextContent('Marque pelo menos uma: Contas.');
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeDisabled();
  });

  it('crédito não tem seleção de origem e cliente sem conta não vê a seção (UI13)', async () => {
    responder('get', '/admin/consentimentos/opcoes', { ...opcoesPF, contas: [] });
    const { usuario } = abrir('/consentimentos/novo');
    await chegarAsPermissoes(usuario);

    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo operacoes-de-credito' }));
    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' }));

    expect(screen.queryByRole('group', { name: 'Contas' })).not.toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'hml-p-r0x1' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeEnabled();
  });

  it('grupo incompleto é recusado pela API com o código oficial (RN83, Q6)', async () => {
    responder('post', '/admin/consentimentos', { erros: ['COMBINACAO_PERMISSOES_INCORRETA'] }, 422);
    const { usuario } = abrir('/consentimentos/novo');
    await chegarAsPermissoes(usuario);

    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' }));
    await usuario.click(screen.getByRole('checkbox', { name: 'ACCOUNTS_BALANCES_READ' }));
    expect(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' })).not.toBeChecked();
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Criar consentimento' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Grupo de permissões incompleto (RN83).');
    expect(screen.getByRole('button', { name: 'Criar consentimento' })).toBeInTheDocument();
  });

  it('cliente PF vê só grupos PF; PJ informa as alçadas (RN84, RN99)', async () => {
    responder('get', '/admin/consentimentos/opcoes', {
      ...opcoesPF,
      tipo: 'PJ',
      grupos: [
        { nome: 'cadastro-identificacao', permissoes: ['CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ'] },
        opcoesPF.grupos[1],
      ],
    });
    const criados = capturar('post', '/admin/consentimentos', visaoPJ(2), 201);
    responder('get', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0', visaoPJ(2));
    const { usuario } = abrir('/consentimentos/novo');
    await chegarAsPermissoes(usuario, AURORA.documento);

    expect(screen.getByText('Cliente PJ')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ' })).toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ' })).not.toBeInTheDocument();
    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' }));
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.type(screen.getByRole('spinbutton', { name: /Aprovações pendentes/ }), '2');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Criar consentimento' }));

    expect(await screen.findByText('Faltam 2 aprovações')).toBeInTheDocument();
    expect(criados[0]!.corpo).toEqual(expect.objectContaining({ cliente: AURORA.documento, aprovacoesPendentes: 2 }));
  });

  it('alçada fora da faixa é barrada na tela (RN99)', async () => {
    responder('get', '/admin/consentimentos/opcoes', { ...opcoesPF, tipo: 'PJ' });
    const { usuario } = abrir('/consentimentos/novo');
    await chegarAsPermissoes(usuario);
    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo cadastro-identificacao' }));
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.type(screen.getByRole('spinbutton', { name: /Aprovações pendentes/ }), '11');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Criar consentimento' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Alçadas só em PJ, de 1 a 10 aprovações (RN99).');
  });

  it('documento digitado que não está na massa: diz o que faltou e deixa voltar', async () => {
    responder('get', '/admin/consentimentos/opcoes', '', 404);
    const { usuario } = abrir('/consentimentos/novo');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    await usuario.type(await screen.findByRole('textbox', { name: 'Documento do cliente' }), '12345678900');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));

    const aviso = await screen.findByRole('alert');
    expect(aviso).toHaveTextContent('Esse cliente não existe na massa deste banco');
    expect(aviso).not.toHaveTextContent('HTTP_404');
    await usuario.click(screen.getByRole('button', { name: 'Voltar' }));
    expect(await screen.findByRole('combobox', { name: 'Banco' })).toBeInTheDocument();
  });

  it('cliente sem produto no banco é avisado antes das permissões (RN95)', async () => {
    responder('get', '/admin/consentimentos/opcoes', { ...opcoesPF, semProduto: true });
    const { usuario } = abrir('/consentimentos/novo');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Cliente' }), MARIANA.documento);
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('O cliente não tem produto neste banco (RN95).');
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeDisabled();
  });

  it('sem receptora, avisa com link; recomeçar limpa a escolha', async () => {
    responder('get', '/admin/receptoras', []);
    const { usuario } = abrir('/consentimentos/novo');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    expect(await screen.findByText(/Nenhuma receptora cadastrada/)).toBeInTheDocument();
    expect(within(screen.getByRole('alert')).getByRole('link', { name: 'Receptoras' })).toHaveAttribute(
      'href',
      '/receptoras',
    );

    await usuario.click(screen.getByRole('button', { name: 'Recomeçar' }));
    await waitFor(() => expect(screen.getByRole('combobox', { name: 'Banco' })).toHaveValue(''));
  });
});
