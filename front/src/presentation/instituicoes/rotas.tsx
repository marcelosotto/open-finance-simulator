import type { RouteObject } from 'react-router-dom';
import { ListaDeBancos } from './ListaDeBancos';
import { NovoBanco } from './NovoBanco';
import { EdicaoDoBanco } from './EdicaoDoBanco';

export const rotasDasInstituicoes: RouteObject[] = [
  { path: 'bancos', element: <ListaDeBancos /> },
  { path: 'bancos/novo', element: <NovoBanco /> },
  { path: 'bancos/:endereco', element: <EdicaoDoBanco /> },
];
