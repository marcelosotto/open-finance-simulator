import { forwardRef, type ButtonHTMLAttributes } from 'react';
import { cn } from './cn';

type Variante = 'primario' | 'secundario' | 'perigo';

const ESTILOS: Readonly<Record<Variante, string>> = {
  primario: 'bg-teal-700 text-white hover:bg-teal-800',
  secundario: 'border border-slate-300 bg-white text-slate-800 hover:bg-slate-100',
  perigo: 'bg-red-700 text-white hover:bg-red-800',
};

/** Com ref, para o Radix (`asChild`) poder usar o botão como gatilho. */
export const Botao = forwardRef<
  HTMLButtonElement,
  ButtonHTMLAttributes<HTMLButtonElement> & { readonly variante?: Variante }
>(({ variante = 'primario', className, type = 'button', ...props }, ref) => (
  <button
    ref={ref}
    type={type}
    className={cn(
      'rounded px-3 py-1.5 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50',
      ESTILOS[variante],
      className,
    )}
    {...props}
  />
));
Botao.displayName = 'Botao';
