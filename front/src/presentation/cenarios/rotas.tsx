import type { RouteObject } from 'react-router-dom';
import { Regras } from './Regras';
import { EdicaoDeRegra, NovaRegra } from './EdicaoDeRegra';
import { Pacotes } from './Pacotes';

export const rotasDosCenarios: RouteObject[] = [
  { path: 'cenarios', element: <Regras /> },
  { path: 'cenarios/regras/nova', element: <NovaRegra /> },
  { path: 'cenarios/regras/:id', element: <EdicaoDeRegra /> },
  { path: 'cenarios/pacotes', element: <Pacotes /> },
];
