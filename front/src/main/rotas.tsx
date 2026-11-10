import { Navigate, type RouteObject } from 'react-router-dom';
import { Layout } from '@/presentation/layout/Layout';
import { NaoEncontrada } from '@/presentation/layout/NaoEncontrada';
import { rotasDasInstituicoes } from '@/presentation/instituicoes/rotas';
import { rotasDosConsentimentos } from '@/presentation/consentimentos/rotas';
import { rotasDaMassa } from '@/presentation/massa/rotas';
import { rotasDosCenarios } from '@/presentation/cenarios/rotas';

export const rotas: RouteObject[] = [
  {
    path: '/',
    element: <Layout />,
    children: [
      { index: true, element: <Navigate to="/bancos" replace /> },
      ...rotasDasInstituicoes,
      ...rotasDosConsentimentos,
      ...rotasDaMassa,
      ...rotasDosCenarios,
      { path: '*', element: <NaoEncontrada /> },
    ],
  },
];
