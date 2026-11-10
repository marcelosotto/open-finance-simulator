import type { RouteObject } from 'react-router-dom';
import { Layout } from '@/presentation/layout/Layout';
import { NaoEncontrada } from '@/presentation/layout/NaoEncontrada';
import { PrimeirosPassos } from '@/presentation/inicio/PrimeirosPassos';
import { rotasDasInstituicoes } from '@/presentation/instituicoes/rotas';
import { rotasDosConsentimentos } from '@/presentation/consentimentos/rotas';
import { rotasDaMassa } from '@/presentation/massa/rotas';
import { rotasDosCenarios } from '@/presentation/cenarios/rotas';

export const rotas: RouteObject[] = [
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <PrimeirosPassos /> },
      { path: 'primeiros-passos', element: <PrimeirosPassos /> },
      ...rotasDasInstituicoes,
      ...rotasDosConsentimentos,
      ...rotasDaMassa,
      ...rotasDosCenarios,
      { path: '*', element: <NaoEncontrada /> },
    ],
  },
];

// Comportamento do React Router 7 já ligado, para a troca de versão não mudar a tela.
export const FUTURO_DO_ROTEADOR = {
  v7_relativeSplatPath: true,
  v7_fetcherPersist: true,
  v7_normalizeFormMethod: true,
  v7_partialHydration: true,
  v7_skipActionErrorRevalidation: true,
} as const;
