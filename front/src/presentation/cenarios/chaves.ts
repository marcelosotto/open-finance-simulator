// Chaves do TanStack Query desta área. A lista de bancos usa a mesma chave da área de instituições.
export const CHAVES = {
  regras: ['cenarios', 'regras'] as const,
  regra: (id: string) => ['cenarios', 'regras', id] as const,
  pacotes: ['cenarios', 'pacotes'] as const,
  defeitos: ['cenarios', 'defeitos'] as const,
  operacoes: ['cenarios', 'operacoes'] as const,
  instituicoes: ['instituicoes'] as const,
};
