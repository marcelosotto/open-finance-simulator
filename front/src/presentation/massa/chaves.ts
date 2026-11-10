// Chaves do TanStack Query desta área.
export const CHAVES = {
  perfis: ['massa', 'perfis'] as const,
  distribuicoes: ['massa', 'distribuicoes'] as const,
  lotes: ['massa', 'lotes'] as const,
  lote: (id: string) => ['massa', 'lotes', id] as const,
  clientesDoLote: (id: string, inicio: number) => ['massa', 'lotes', id, 'clientes', inicio] as const,
  cliente: (documento: string) => ['massa', 'clientes', documento] as const,
  clientesDoBanco: (banco: string) => ['massa', 'clientes-do-banco', banco] as const,
  resumo: ['massa', 'resumo'] as const,
};

/** "a, b , c" → ["a", "b", "c"]. */
export const separar = (texto: string): string[] =>
  texto
    .split(',')
    .map((parte) => parte.trim())
    .filter((parte) => parte.length > 0);

/** Campo numérico opcional: vazio fica de fora; o resto o caso de uso confere. */
export const numeroOpcional = (texto: string): number | undefined => (texto.trim() === '' ? undefined : Number(texto));

export const classeDoLink = 'text-teal-800 underline';
