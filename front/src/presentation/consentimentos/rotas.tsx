import type { RouteObject } from 'react-router-dom';
import { NovoConsentimento } from './NovoConsentimento';
import { Consentimento } from './Consentimento';
import { BuscaDeConsentimento } from './BuscaDeConsentimento';
import { Receptoras } from './Receptoras';

export const rotasDosConsentimentos: RouteObject[] = [
  { path: 'consentimentos', element: <BuscaDeConsentimento /> },
  { path: 'consentimentos/novo', element: <NovoConsentimento /> },
  { path: 'consentimentos/:consentId', element: <Consentimento /> },
  { path: 'receptoras', element: <Receptoras /> },
];
