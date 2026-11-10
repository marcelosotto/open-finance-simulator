import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { castDraft } from 'immer';
import { rascunhoVazio, type Rascunho } from '@/core/consentimentos/consentimento';

// Passo e rascunho do menu (estado da tela, Zustand com Immer). As opções ficam no TanStack Query.
export const PASSOS = ['banco-e-cliente', 'permissoes', 'prazo', 'confirmacao'] as const;
export type Passo = (typeof PASSOS)[number];

interface EstadoDoMenu {
  passo: number;
  rascunho: Rascunho;
  avancar: () => void;
  voltar: () => void;
  atualizar: (mudar: (rascunho: Rascunho) => Rascunho) => void;
  recomecar: () => void;
}

export const useMenu = create<EstadoDoMenu>()(
  immer((set) => ({
    passo: 0,
    rascunho: rascunhoVazio,
    avancar: () =>
      set((estado) => {
        estado.passo = Math.min(estado.passo + 1, PASSOS.length - 1);
      }),
    voltar: () =>
      set((estado) => {
        estado.passo = Math.max(estado.passo - 1, 0);
      }),
    atualizar: (mudar) =>
      set((estado) => {
        estado.rascunho = castDraft(mudar(estado.rascunho));
      }),
    recomecar: () =>
      set((estado) => {
        estado.passo = 0;
        estado.rascunho = castDraft(rascunhoVazio);
      }),
  })),
);
