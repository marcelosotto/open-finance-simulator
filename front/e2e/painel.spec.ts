import { expect, test } from '@playwright/test';
import {
  abrirPermissoes,
  avancar,
  confirmar,
  criarBanco,
  fixarRelogio,
  marcarGrupo,
  prepararMassa,
  recursos,
} from './apoio';

// docs/01-requisitos/gherkin/consentimento-no-painel.feature, no navegador, contra o simulador real (Q10).
test.describe('Funcionalidade: Criar consentimento no painel', () => {
  // RN80, RN95
  test('Cenário: Criar consentimento', async ({ page, request }) => {
    const massa = await prepararMassa(request, 'PF');
    await abrirPermissoes(page, massa);
    await marcarGrupo(page, 'contas-saldos');
    await marcarGrupo(page, 'cartoes-limites');
    await avancar(page);
    await page.getByRole('spinbutton', { name: /Prazo em meses/ }).fill('12');
    await avancar(page);
    await confirmar(page);

    await expect(page.getByTestId('estado')).toHaveText('AUTHORISED');
    const linhas = recursos(page).locator('tbody tr');
    await expect(linhas).toHaveCount(2);
    for (const linha of await linhas.all()) await expect(linha.getByRole('cell').nth(2)).toHaveText('AVAILABLE');
  });

  // RN85
  test('Cenário: Prazo indeterminado', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PF'));
    await marcarGrupo(page, 'contas-saldos');
    await avancar(page);
    await avancar(page);
    await confirmar(page);
    await expect(page.getByText('Sem data final')).toBeVisible();
  });

  // RN95
  test('Cenário: Pelo menos uma origem', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PF'));
    await marcarGrupo(page, 'contas-saldos');
    const contas = page.getByRole('group', { name: 'Contas' }).getByRole('checkbox');
    for (const conta of await contas.all()) await conta.uncheck();

    await expect(page.getByRole('alert')).toContainText('Marque pelo menos uma: Contas.');
    await expect(page.getByRole('button', { name: 'Avançar' })).toBeDisabled();
  });

  // RN95
  test('Cenário: Crédito não tem seleção de origem', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PF'));
    await marcarGrupo(page, 'operacoes-de-credito');
    await expect(page.getByText(/Compartilhados por inteiro, sem seleção: .*LOAN/)).toBeVisible();
    await expect(page.getByRole('group', { name: 'Contas' })).toHaveCount(0);
    await avancar(page);
    await avancar(page);
    await confirmar(page);

    await expect(recursos(page).getByRole('cell', { name: 'LOAN' })).toBeVisible();
  });

  // RN83
  test('Cenário: Grupo de permissões incompleto', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PF'));
    // Abre o grupo e marca só uma das duas permissões dele (Q6).
    await page.getByText('contas-saldos', { exact: true }).click();
    await page
      .getByRole('group', { name: 'Permissões de contas-saldos' })
      .getByRole('checkbox', { name: 'ACCOUNTS_READ' })
      .check();
    await avancar(page);
    await avancar(page);
    await page.getByRole('button', { name: 'Criar consentimento' }).click();

    await expect(page.getByRole('alert')).toContainText('Grupo de permissões incompleto (RN83).');
    await expect(page.getByRole('heading', { name: /^Consentimento urn:/ })).toHaveCount(0);
  });

  // RN84
  test('Cenário: Permissões de pessoa física e jurídica', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PF'));
    await expect(page.getByText('Cliente PF')).toBeVisible();
    await expect(page.getByRole('checkbox', { name: 'CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ' })).toBeAttached();
    await expect(page.getByRole('checkbox', { name: /CUSTOMERS_BUSINESS_/ })).toHaveCount(0);
  });

  // RN98
  test('Cenário: Duas aprovações pendentes', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PJ'));
    await marcarGrupo(page, 'contas-saldos');
    await avancar(page);
    await page.getByRole('spinbutton', { name: /Aprovações pendentes/ }).fill('2');
    await avancar(page);
    await confirmar(page);

    await expect(page.getByText('Faltam 2 aprovações')).toBeVisible();
    await page.getByRole('button', { name: 'Aprovar' }).click();
    await expect(page.getByText('Faltam 1 aprovações')).toBeVisible();
    await page.getByRole('button', { name: 'Aprovar' }).click();
    await expect(page.getByText('Faltam 0 aprovações')).toBeVisible();
    await expect(recursos(page).getByRole('cell', { name: 'AVAILABLE' }).first()).toBeVisible();
    await expect(recursos(page).getByRole('cell', { name: 'PENDING_AUTHORISATION' })).toHaveCount(0);
  });

  // RN98
  test('Cenário: Recusa na alçada', async ({ page, request }) => {
    await abrirPermissoes(page, await prepararMassa(request, 'PJ'));
    await marcarGrupo(page, 'contas-saldos');
    await avancar(page);
    await page.getByRole('spinbutton', { name: /Aprovações pendentes/ }).fill('1');
    await avancar(page);
    await confirmar(page);

    await page.getByRole('button', { name: 'Recusar' }).click();
    await expect(recursos(page).getByRole('cell', { name: 'UNAVAILABLE' }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Aprovar' })).toHaveCount(0);
  });

  // RN98
  test('Cenário: Fim do prazo da alçada', async ({ page, request }) => {
    const banco = await criarBanco(request);
    await abrirPermissoes(page, await prepararMassa(request, 'PJ', banco));
    await marcarGrupo(page, 'contas-saldos');
    await avancar(page);
    await page.getByRole('spinbutton', { name: /Aprovações pendentes/ }).fill('1');
    await avancar(page);
    await confirmar(page);

    // A alçada vence 15 dias corridos depois da criação, pelo relógio do banco (RN99).
    await fixarRelogio(request, banco, new Date(Date.now() + 16 * 24 * 3600 * 1000).toISOString());
    await page.reload();

    await expect(page.getByText('Situação: vencida')).toBeVisible();
    await expect(recursos(page).getByRole('cell', { name: 'UNAVAILABLE' }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Aprovar' })).toHaveCount(0);
  });
});
