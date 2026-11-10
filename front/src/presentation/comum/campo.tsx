import { forwardRef, useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from 'react';
import { cn } from './cn';

interface PropsDoCampo {
  readonly rotulo: string;
  readonly erro?: string | undefined;
  readonly ajuda?: ReactNode;
}

const controle =
  'mt-1 block w-full rounded border border-slate-300 px-2 py-1 text-sm focus:border-teal-600 focus:outline-none';

export const Campo = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & PropsDoCampo>(
  ({ rotulo, erro, ajuda, className, ...props }, ref) => {
    const id = useId();
    return (
      <div className={className}>
        <label htmlFor={id} className="block text-sm font-medium text-slate-700">
          {rotulo}
        </label>
        <input
          id={id}
          ref={ref}
          aria-invalid={erro !== undefined}
          className={cn(controle, erro !== undefined && 'border-red-500')}
          {...props}
        />
        {ajuda !== undefined && <div className="mt-1 text-xs text-slate-600">{ajuda}</div>}
        {erro !== undefined && <p className="mt-1 text-xs text-red-700">{erro}</p>}
      </div>
    );
  },
);
Campo.displayName = 'Campo';

export const Selecao = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement> & PropsDoCampo>(
  ({ rotulo, erro, children, className, ...props }, ref) => {
    const id = useId();
    return (
      <div className={className}>
        <label htmlFor={id} className="block text-sm font-medium text-slate-700">
          {rotulo}
        </label>
        <select id={id} ref={ref} className={controle} {...props}>
          {children}
        </select>
        {erro !== undefined && <p className="mt-1 text-xs text-red-700">{erro}</p>}
      </div>
    );
  },
);
Selecao.displayName = 'Selecao';
