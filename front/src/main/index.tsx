import 'reflect-metadata';
import '@/presentation/estilos/base.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider, createBrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { lerConfiguracao } from '@/infrastructure/config';
import { ClienteAdmin } from '@/infrastructure/http/cliente-admin';
import { iniciarIdioma } from '@/presentation/i18n';
import { autorAtual } from '@/presentation/layout/autor';
import { registrar } from './registros';
import { FUTURO_DO_ROTEADOR, rotas } from './rotas';

const configuracao = lerConfiguracao({ SIM_ADMIN_API_URL: process.env.SIM_ADMIN_API_URL });
registrar(new ClienteAdmin(configuracao.urlDaAdministracao, autorAtual));
iniciarIdioma();

const consultas = new QueryClient({ defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false } } });

const raiz = document.getElementById('raiz');
if (raiz === null) throw new Error('Elemento #raiz ausente em index.html');

createRoot(raiz).render(
  <StrictMode>
    <QueryClientProvider client={consultas}>
      <RouterProvider
        router={createBrowserRouter(rotas, { future: FUTURO_DO_ROTEADOR })}
        future={{ v7_startTransition: true }}
      />
    </QueryClientProvider>
  </StrictMode>,
);
