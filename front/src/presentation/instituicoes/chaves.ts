// Chaves do TanStack Query desta área.
export const CHAVES = {
  lista: ['instituicoes'] as const,
  perfis: ['instituicoes', 'perfis'] as const,
  detalhe: (endereco: string) => ['instituicoes', endereco] as const,
  historico: (endereco: string) => ['instituicoes', endereco, 'historico'] as const,
  anomalias: (endereco: string) => ['instituicoes', endereco, 'anomalias'] as const,
};
