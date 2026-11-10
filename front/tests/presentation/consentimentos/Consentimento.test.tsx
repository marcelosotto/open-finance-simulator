import { screen, waitFor, within } from '@testing-library/react';
import { abrir } from '../../apoio/renderizar';
import { capturar, comAmbiente, responder } from '../../apoio/api';
import { visaoPJ } from '../../apoio/consentimentos';

const VISAO = '/admin/consentimentos/urn%3Ahml%3Ap-c5x0';
const TELA = '/consentimentos/urn%3Ahml%3Ap-c5x0';

describe('Visão do consentimento e alçadas (UI14)', () => {
  beforeEach(() => comAmbiente());

  it('duas aprovações levam os recursos a AVAILABLE (RN98)', async () => {
    responder('get', VISAO, visaoPJ(2));
    const aprovacoes = capturar('post', `${VISAO}/aprovar`, visaoPJ(1));
    const { usuario } = abrir(TELA);

    expect(await screen.findByText('Faltam 2 aprovações')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Aguardando aprovação' })).toBeInTheDocument();

    responder('get', VISAO, visaoPJ(1));
    await usuario.click(screen.getByRole('button', { name: 'Aprovar' }));
    expect(await screen.findByText('Falta 1 aprovação')).toBeInTheDocument();

    responder(
      'get',
      VISAO,
      visaoPJ(0, { decisoes: [{ autor: 'Bruno', decisao: 'aprovada', em: '2026-11-10T11:00:00Z' }] }),
    );
    await usuario.click(screen.getByRole('button', { name: 'Aprovar' }));
    expect(await screen.findByText('Nenhuma aprovação pendente')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Disponível' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Aprovar' })).not.toBeInTheDocument();
    expect(within(screen.getByRole('table', { name: 'Decisões' })).getByText('Bruno')).toBeInTheDocument();
    expect(aprovacoes).toHaveLength(2);
  });

  it('recusa leva os recursos a UNAVAILABLE e tira os botões (RN98)', async () => {
    const recusada = visaoPJ(1, {
      alcada: { situacao: 'recusada', aprovacoesPendentes: 1, prazo: '2026-11-25T10:50:03Z' },
      recursos: [{ resourceId: 'hml-p-r1x1', tipo: 'ACCOUNT', estado: 'UNAVAILABLE' }],
    });
    responder('get', VISAO, visaoPJ(1));
    responder('post', `${VISAO}/recusar`, recusada);
    const { usuario } = abrir(TELA);

    await screen.findByText('Falta 1 aprovação');
    responder('get', VISAO, recusada);
    await usuario.click(screen.getByRole('button', { name: 'Recusar' }));

    expect(await screen.findByRole('cell', { name: 'Indisponível' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Recusar' })).not.toBeInTheDocument();
  });

  it('alçada vencida (16 dias) não oferece decisão (RN98)', async () => {
    responder(
      'get',
      VISAO,
      visaoPJ(1, {
        alcada: { situacao: 'vencida', aprovacoesPendentes: 1, prazo: '2026-11-25T10:50:03Z' },
        recursos: [{ resourceId: 'hml-p-r1x1', tipo: 'ACCOUNT', estado: 'UNAVAILABLE' }],
      }),
    );
    abrir(TELA);
    expect(await screen.findByText('Situação: Vencida (passaram os 15 dias)')).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Indisponível' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Aprovar' })).not.toBeInTheDocument();
  });

  it('409 na decisão mostra o motivo e relê a visão', async () => {
    responder('get', VISAO, visaoPJ(1));
    responder('post', `${VISAO}/aprovar`, { erros: ['escrita concorrente'] }, 409);
    const { usuario } = abrir(TELA);

    await usuario.click(await screen.findByRole('button', { name: 'Aprovar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('escrita concorrente');
    responder('get', VISAO, visaoPJ(0));
    await usuario.click(screen.getByRole('button', { name: 'Tentar de novo' }));
    expect(await screen.findByText('Nenhuma aprovação pendente')).toBeInTheDocument();
  });

  it('revogar pede confirmação; REJECTED mostra quem rejeitou e não oferece revogar', async () => {
    responder('get', VISAO, visaoPJ(0, { alcada: null, expiraEm: '2027-11-10T10:50:03Z' }));
    const revogacoes = capturar('post', `${VISAO}/revogar`, { estado: 'REJECTED' });
    const { usuario } = abrir(TELA);

    expect(await screen.findByText('Sem alçada.')).toBeInTheDocument();
    await usuario.click(screen.getByRole('button', { name: 'Revogar' }));
    responder(
      'get',
      VISAO,
      visaoPJ(0, { alcada: null, estado: 'REJECTED', rejeitadoPor: 'USER', razao: 'CUSTOMER_MANUALLY_REVOKED' }),
    );
    await usuario.click(within(await screen.findByRole('dialog')).getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByText('Pelo cliente')).toHaveAttribute('title', 'USER');
    expect(screen.getByText(/(CUSTOMER_MANUALLY_REVOKED)/)).toBeInTheDocument();
    expect(screen.getByTestId('estado')).toHaveTextContent('Rejeitado');
    expect(screen.queryByRole('button', { name: 'Revogar' })).not.toBeInTheDocument();
    await waitFor(() => expect(revogacoes).toHaveLength(1));
  });

  it('busca por consentId abre a visão (Q7)', async () => {
    responder('get', VISAO, visaoPJ(2));
    const { usuario } = abrir('/consentimentos');
    expect(screen.getByRole('button', { name: 'Abrir' })).toBeDisabled();
    await usuario.type(screen.getByRole('textbox', { name: 'consentId' }), ' urn:hml:p-c5x0 ');
    await usuario.click(screen.getByRole('button', { name: 'Abrir' }));
    expect(await screen.findByRole('heading', { name: 'Consentimento urn:hml:p-c5x0' })).toBeInTheDocument();
  });

  it('receptoras: lista, mostra as credenciais de teste uma vez e a recusa', async () => {
    responder('get', '/admin/receptoras', [{ organisationId: 'hml-p-org', nome: 'Coletor' }]);
    responder(
      'post',
      '/admin/receptoras',
      { organisationId: 'org2', nome: 'Outra', usuario: 'teste-1', senha: 'teste-senha' },
      201,
    );
    const { usuario } = abrir('/receptoras');

    expect(await screen.findByRole('cell', { name: 'Coletor' })).toBeInTheDocument();
    await usuario.type(screen.getByRole('textbox', { name: 'Nome' }), 'Outra');
    await usuario.click(screen.getByRole('button', { name: 'Criar receptora' }));
    expect(await screen.findByText(/usuário teste-1, senha teste-senha/)).toBeInTheDocument();
    expect(screen.getByRole('textbox', { name: 'Nome' })).toHaveValue('');

    responder('post', '/admin/receptoras', { erros: ['receptora repetida'] }, 409);
    await usuario.type(screen.getByRole('textbox', { name: 'Nome' }), 'Outra');
    await usuario.click(screen.getByRole('button', { name: 'Criar receptora' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('receptora repetida');
  });
});
