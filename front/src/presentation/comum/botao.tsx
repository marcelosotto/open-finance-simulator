import type { ButtonHTMLAttributes } from 'react';
import { cn } from './cn';

type Variante = 'primario' | 'secundario' | 'perigo';

const ESTILOS: Readonly<Record<Variante, string>> = {
  primario: 'bg-teal-700 text-white hover:bg-teal-800',
  secundario: 'border border-slate-300 bg-white text-slate-800 hover:bg-slate-100',
  perigo: 'bg-red-700 text-white hover:bg-red-800',
};

export const Botao = ({
  variante = 'primario',
  className,
  type = 'button',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { readonly variante?: Variante }) => (
  <button
    type={type}
    className={cn(
      'rounded px-3 py-1.5 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50',
      ESTILOS[variante],
      className,
    )}
    {...props}
  />
);
