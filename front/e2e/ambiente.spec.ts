import { expect, test } from '@playwright/test';

// docs/01-requisitos/gherkin/marcacao.feature, cenário @tela (RN72). O Compose sobe com a marca hml por padrão.
test.describe('Funcionalidade: Marcação de ambiente', () => {
  test('Cenário: Ambiente visível nas telas', async ({ page, request }) => {
    const { marca } = (await (await request.get('/admin/ambiente')).json()) as { marca: string };
    test.skip(marca !== 'hml', 'O cenário pede a marca "hml".');

    for (const caminho of ['/bancos', '/consentimentos/novo', '/massa', '/cenarios']) {
      await page.goto(caminho);
      await expect(page.getByLabel('Ambiente hml')).toHaveText('hml');
      await expect(page.getByRole('note')).toHaveText('Ambiente simulado, dados fictícios');
    }
  });
});
