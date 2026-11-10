import { format, parseISO } from 'date-fns';

/** Data no fuso do navegador (Assumptions da spec). */
export const dataHora = (iso: string): string => format(parseISO(iso), 'dd/MM/yyyy HH:mm:ss');
