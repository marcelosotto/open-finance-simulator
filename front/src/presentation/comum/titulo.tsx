import type { ReactNode } from 'react';
import { Ajuda } from '../ajuda/Ajuda';
import type { TelaComAjuda } from '../ajuda/textos';

interface PropsDoTitulo {
  readonly children: ReactNode;
  readonly acoes?: ReactNode;
  /** A ajuda do topo da tela (UI17). */
  readonly ajuda?: TelaComAjuda;
}

export const Titulo = ({ children, acoes, ajuda }: PropsDoTitulo) => (
  <>
    <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
      <h1 className="text-xl font-semibold">{children}</h1>
      {acoes !== undefined && <div className="flex gap-2">{acoes}</div>}
    </div>
    {ajuda !== undefined && <Ajuda tela={ajuda} />}
  </>
);

export const Secao = ({ titulo, children }: { readonly titulo: string; readonly children: ReactNode }) => (
  <section aria-label={titulo} className="mb-6 rounded border border-slate-200 bg-white p-4">
    <h2 className="mb-3 text-base font-semibold">{titulo}</h2>
    {children}
  </section>
);

export const Tabela = ({ rotulo, children }: { readonly rotulo: string; readonly children: ReactNode }) => (
  <div className="overflow-x-auto">
    <table
      aria-label={rotulo}
      className="w-full border-collapse text-left text-sm [&_td]:border-t [&_td]:px-2 [&_td]:py-1.5 [&_th]:px-2 [&_th]:py-1.5"
    >
      {children}
    </table>
  </div>
);
