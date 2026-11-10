import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider, createMemoryRouter, type RouteObject } from 'react-router-dom';
import { container } from 'tsyringe';
import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { iniciarIdioma } from '@/presentation/i18n';
import { autorAtual } from '@/presentation/layout/autor';
import { registrar } from '@/main/registros';
import { FUTURO_DO_ROTEADOR, rotas } from '@/main/rotas';

export const API = 'http://simulador.test';

/** Container real com os repositórios HTTP apontando para o msw (tests/apoio/servidor.ts). */
export const montarContainer = () => {
  container.clearInstances();
  registrar(new ClienteAdmin(API, autorAtual));
  iniciarIdioma();
};

const novoQueryClient = () =>
  new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: 0 }, mutations: { retry: false } } });

/** Abre o app inteiro numa rota, com layout e navegação de verdade. */
export const abrir = (caminho: string) => {
  montarContainer();
  const roteador = createMemoryRouter(rotas, { initialEntries: [caminho], future: FUTURO_DO_ROTEADOR });
  const usuario = userEvent.setup();
  const tela = render(
    <QueryClientProvider client={novoQueryClient()}>
      <RouterProvider router={roteador} future={{ v7_startTransition: true }} />
    </QueryClientProvider>,
  );
  return { ...tela, usuario, roteador };
};

/** Renderiza um componente solto, com as rotas extras que ele precisar. */
export const renderizar = (elemento: ReactElement, extras: RouteObject[] = []) => {
  montarContainer();
  const roteador = createMemoryRouter([{ path: '/', element: elemento }, ...extras], {
    initialEntries: ['/'],
    future: FUTURO_DO_ROTEADOR,
  });
  const usuario = userEvent.setup();
  const tela = render(
    <QueryClientProvider client={novoQueryClient()}>
      <RouterProvider router={roteador} future={{ v7_startTransition: true }} />
    </QueryClientProvider>,
  );
  return { ...tela, usuario, roteador };
};
