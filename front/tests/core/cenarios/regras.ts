import type { EfeitoDaRegra, RegraDeCenario } from '@/core/cenarios/regra';

// Regra no formato que a administração devolve (resposta real de POST /admin/cenarios/regras).
export const efeito = (parcial: Partial<EfeitoDaRegra> = {}): EfeitoDaRegra => ({
  tipo: 'latencia',
  status: null,
  latenciaMs: 3000,
  esperaMs: null,
  resultado: null,
  falha: null,
  estado: null,
  defeito: null,
  campo: null,
  quantidade: null,
  ...parcial,
});

export const regra = (parcial: Partial<RegraDeCenario> = {}): RegraDeCenario => ({
  id: 'r-i00dnwpny63e',
  nome: null,
  instituicao: 'banco-estavel',
  endpoint: null,
  cliente: null,
  recurso: null,
  efeito: efeito(),
  frequenciaPct: 100,
  inicio: null,
  fim: null,
  semente: null,
  ligada: true,
  pacote: null,
  criadaEm: '2026-11-05T12:00:00+00:00',
  atualizadaEm: '2026-11-05T12:00:00+00:00',
  ...parcial,
});
