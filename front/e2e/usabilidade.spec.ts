import { expect, test } from '@playwright/test';
import { criarBanco } from './apoio';

// Melhorias de uso da 010 (Q11 a Q14): receptora padrão, cliente da lista, ajuda e Primeiros passos.
test.describe('Funcionalidade: Usar o back-office sem saber os identificadores', () => {
  // RN124
  test('Cenário: O ambiente sobe com a receptora padrão XP', async ({ request }) => {
    const receptoras = (await (await request.get('/admin/receptoras')).json()) as { nome: string; organisationId: string }[];
    const xp = receptoras.find((r) => r.nome === 'XP');
    expect(xp, 'a receptora padrão deveria existir').toBeDefined();
    expect(xp?.organisationId).toMatch(/p-receptora-padrao$/);
    expect(receptoras.filter((r) => r.organisationId === xp?.organisationId)).toHaveLength(1);
  });

  // UI19
  test('Cenário: Banco sem clientes manda gerar massa', async ({ page, request }) => {
    const banco = await criarBanco(request);
    await page.goto('/consentimentos/novo');
    await page.getByRole('button', { name: 'Recomeçar' }).click();
    await page.getByRole('combobox', { name: 'Banco' }).selectOption(banco);

    await expect(page.getByRole('alert')).toContainText('ainda não tem clientes');
    await page.getByRole('link', { name: 'Gerar massa de clientes' }).click();
    await expect(page.getByRole('heading', { name: 'Lotes', level: 1 })).toBeVisible();
  });

  // UI17, UI18
  test('Cenário: Primeiros passos mostra o caminho e cada tela explica para que serve', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Primeiros passos', level: 1 })).toBeVisible();
    await expect(page.getByRole('heading', { name: /^1\. Criar um banco/ })).toContainText('Feito');
    await expect(page.getByLabel('Exemplo de pedido de token')).toContainText('scope=consents');

    await page.getByRole('list', { name: 'Primeiros passos' }).getByRole('link', { name: 'Abrir a tela' }).nth(2).click();
    await expect(page.getByRole('complementary', { name: 'Primeiros passos' })).toContainText(
      'Simula um cliente autorizando outra instituição a ler os dados dele.',
    );
  });
});
