import { expect, type APIRequestContext, type APIResponse, type Page } from '@playwright/test';

// Cada teste cria os seus dados pela administração, com nomes únicos, para não depender de ordem (R10).
const sufixo = () => `${Date.now().toString(36)}${Math.floor(Math.random() * 1e4)}`;

export interface Massa {
  readonly banco: string;
  readonly receptora: { readonly organisationId: string; readonly nome: string };
  readonly documento: string;
}

const json = async <T>(pedido: Promise<APIResponse>): Promise<T> => {
  const resposta = await pedido;
  expect(resposta.ok(), `${resposta.url()} respondeu ${resposta.status()}: ${await resposta.text()}`).toBeTruthy();
  return (await resposta.json()) as T;
};

/** Um banco próprio do teste, para mexer no relógio sem afetar os outros. */
export const criarBanco = async (api: APIRequestContext): Promise<string> =>
  (await json<{ endereco: string }>(api.post('/admin/instituicoes', { data: { nome: `Banco E2E ${sufixo()}` } })))
    .endereco;

export const criarCliente = async (api: APIRequestContext, tipo: 'PF' | 'PJ', banco: string): Promise<string> => {
  const nome = tipo === 'PF' ? 'Mariana Alves' : 'Indústria Aurora';
  const corpo = { tipo, perfil: 'cliente-comum', bancos: [banco], nome };
  return (await json<{ cliente: { documento: string } }>(api.post('/admin/massa/clientes', { data: corpo }))).cliente
    .documento;
};

export const prepararMassa = async (
  api: APIRequestContext,
  tipo: 'PF' | 'PJ',
  banco = 'banco-estavel',
): Promise<Massa> => {
  const receptora = await json<{ organisationId: string; nome: string }>(
    api.post('/admin/receptoras', { data: { nome: `Coletor ${sufixo()}` } }),
  );
  return { banco, receptora, documento: await criarCliente(api, tipo, banco) };
};

export const fixarRelogio = (api: APIRequestContext, banco: string, fixadoEm: string) =>
  json<unknown>(api.put(`/admin/instituicoes/${banco}/relogio`, { data: { fixadoEm } }));

/** Percorre os dois primeiros passos do menu (UI10) e para nas permissões. */
export const abrirPermissoes = async (pagina: Page, massa: Massa) => {
  await pagina.goto('/consentimentos/novo');
  await pagina.getByRole('button', { name: 'Recomeçar' }).click();
  await pagina.getByRole('textbox', { name: /Documento do cliente/ }).fill(massa.documento);
  await avancar(pagina);
  await pagina.getByRole('combobox', { name: 'Banco' }).selectOption(massa.banco);
  await pagina.getByRole('combobox', { name: 'Receptora' }).selectOption(massa.receptora.organisationId);
  await avancar(pagina);
  await expect(pagina.getByRole('group', { name: 'Grupos de permissões' })).toBeVisible();
};

export const marcarGrupo = (pagina: Page, nome: string) =>
  pagina.getByRole('checkbox', { name: `Grupo ${nome}` }).check();

export const avancar = (pagina: Page) => pagina.getByRole('button', { name: 'Avançar' }).click();

/** Confirma no último passo e espera a visão do consentimento. */
export const confirmar = async (pagina: Page) => {
  await pagina.getByRole('button', { name: 'Criar consentimento' }).click();
  await expect(pagina.getByRole('heading', { name: /^Consentimento urn:/ })).toBeVisible();
};

export const recursos = (pagina: Page) => pagina.getByRole('table', { name: 'Recursos' });
