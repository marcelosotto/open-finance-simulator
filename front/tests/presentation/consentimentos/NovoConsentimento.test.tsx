import { screen, waitFor } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { resumo } from '../../apoio/dados';
import { opcoesPF, visaoPJ } from '../../apoio/consentimentos';
import { useMenu } from '@/presentation/consentimentos/menu';

type Usuario = ReturnType<typeof abrir>['usuario'];

const chegarAsPermissoes = async (usuario: Usuario, documento = '95000000080') => {
  await usuario.type(await screen.findByRole('textbox', { name: /Documento do cliente/ }), documento);
  await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
  await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
  await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Receptora' }), 'hml-p-org');
  await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
  await screen.findByRole('group', { name: 'Permissões de contas-saldos' });
};

describe('Novo consentimento (UI10, UI13)', () => {
  beforeEach(() => {
    useMenu.getState().recomecar();
    comAmbiente();
    responder('get', '/admin/instituicoes', [
      resumo('banco-estavel', 'Banco Estável'),
      resumo('velho', 'Velho', [], 'inativa'),
    ]);
    responder('get', '/admin/receptoras', [{ organisationId: 'hml-p-org', nome: 'Coletor' }]);
    responder('get', '/admin/consentimentos/opcoes', opcoesPF);
  });

  it('cria em passos com contas, cartões e 12 meses, e abre a visão em AUTHORISED (RN80, RN95)', async () => {
    const criados = capturar(
      'post',
      '/admin/consentimentos',
      visaoPJ(0, { cliente: '95000000080', alcada: null }),
      201,
    );
    responder('get', '/admin/consentimentos/urn%3Ahml%3Ap-c5x0', visaoPJ(0, { alcada: null }));
    const { usuario } = abrir('/consentimentos/novo');

    expect(await screen.findByText('1. Cliente')).toHaveAttribute('aria-current', 'step');
    await chegarAsPermissoes(usuario);
    expect(screen.queryByRole('option', { name: 'Velho' })).not.toBeInTheDocument();

    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' }));
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
      receptora: 'hml-p-org',
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
    await chegarAsPermissoes(usuario, 'P0000001000182');

    expect(screen.getByText('Cliente PJ')).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ' })).toBeInTheDocument();
    expect(screen.queryByRole('checkbox', { name: 'CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ' })).not.toBeInTheDocument();
    await usuario.click(screen.getByRole('checkbox', { name: 'Grupo contas-saldos' }));
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.type(screen.getByRole('spinbutton', { name: /Aprovações pendentes/ }), '2');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.click(screen.getByRole('button', { name: 'Criar consentimento' }));

    expect(await screen.findByText('Faltam 2 aprovações')).toBeInTheDocument();
    expect(criados[0]!.corpo).toEqual(expect.objectContaining({ aprovacoesPendentes: 2 }));
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

  it('cliente inexistente: mostra o erro e deixa voltar', async () => {
    responder('get', '/admin/consentimentos/opcoes', { erros: ['cliente não encontrado'] }, 404);
    const { usuario } = abrir('/consentimentos/novo');
    await usuario.type(await screen.findByRole('textbox', { name: /Documento do cliente/ }), '000');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Receptora' }), 'hml-p-org');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('cliente não encontrado');
    await usuario.click(screen.getByRole('button', { name: 'Voltar' }));
    expect(await screen.findByRole('combobox', { name: 'Banco' })).toBeInTheDocument();
  });

  it('cliente sem produto no banco é avisado antes das permissões (RN95)', async () => {
    responder('get', '/admin/consentimentos/opcoes', { ...opcoesPF, semProduto: true });
    const { usuario } = abrir('/consentimentos/novo');
    await usuario.type(await screen.findByRole('textbox', { name: /Documento do cliente/ }), '1');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Banco' }), 'banco-estavel');
    await usuario.selectOptions(await screen.findByRole('combobox', { name: 'Receptora' }), 'hml-p-org');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('O cliente não tem produto neste banco (RN95).');
    expect(screen.getByRole('button', { name: 'Avançar' })).toBeDisabled();
  });

  it('sem receptora, avisa; recomeçar volta ao primeiro passo', async () => {
    responder('get', '/admin/receptoras', []);
    const { usuario } = abrir('/consentimentos/novo');
    await usuario.type(await screen.findByRole('textbox', { name: /Documento do cliente/ }), '1');
    await usuario.click(screen.getByRole('button', { name: 'Avançar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('Nenhuma receptora.');

    await usuario.click(screen.getByRole('button', { name: 'Recomeçar' }));
    await waitFor(() => expect(screen.getByRole('textbox', { name: /Documento do cliente/ })).toHaveValue(''));
  });
});
