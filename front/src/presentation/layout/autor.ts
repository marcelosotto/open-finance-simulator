import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';
import { createJSONStorage, persist } from 'zustand/middleware';

// Quem opera a tela, sem login (Q8). Guardado no navegador e enviado em x-sim-autor nas escritas.
interface EstadoDoAutor {
  nome: string;
  definir: (nome: string) => void;
}

export const useAutor = create<EstadoDoAutor>()(
  persist(
    immer((set) => ({
      nome: '',
      definir: (nome) =>
        set((estado) => {
          estado.nome = nome;
        }),
    })),
    { name: 'sim-autor', storage: createJSONStorage(() => localStorage) },
  ),
);

export const autorAtual = (): string => useAutor.getState().nome;
