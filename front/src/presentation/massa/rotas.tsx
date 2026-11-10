import type { RouteObject } from 'react-router-dom';
import { IndiceDaMassa } from './IndiceDaMassa';
import { PerfisDeDados } from './PerfisDeDados';
import { Distribuicoes } from './Distribuicoes';
import { Lotes } from './Lotes';
import { Lote } from './Lote';
import { Clientes } from './Clientes';
import { Cliente } from './Cliente';
import { Limpeza } from './Limpeza';

export const rotasDaMassa: RouteObject[] = [
  { path: 'massa', element: <IndiceDaMassa /> },
  { path: 'massa/perfis', element: <PerfisDeDados /> },
  { path: 'massa/distribuicoes', element: <Distribuicoes /> },
  { path: 'massa/lotes', element: <Lotes /> },
  { path: 'massa/lotes/:id', element: <Lote /> },
  { path: 'massa/clientes', element: <Clientes /> },
  { path: 'massa/clientes/:documento', element: <Cliente /> },
  { path: 'massa/limpeza', element: <Limpeza /> },
];
