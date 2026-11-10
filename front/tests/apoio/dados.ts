// Respostas reais da administração (capturadas do simulador), usadas como base nos testes.
export const latencia = { tipicoMs: 200, variacaoMs: 600, caudaLentaPct: 0 };

export const bancoEstavel = {
  endereco: 'banco-estavel',
  nome: 'Banco Estável',
  organisationId: 'hml-p-89fbkdngtsjf8nb8',
  perfil: 'estavel',
  marca: 'Banco Estável',
  publico: 'ambos',
  produtos: ['contas', 'cartoes'],
  situacao: 'ativa',
  pronta: true,
  comportamento: {
    latenciaPorClasse: { alta: latencia, 'media-alta': latencia, media: latencia, baixa: latencia },
    taxaDeErroPct: 0.2,
    pesosPorStatus: { '500': 1 },
    recursoIndisponivelPct: 0.2,
    janela: null,
  },
  limites: {
    qca: 0,
    qcaPorReceptora: {},
    tpm: { mediaAlta: 2000, media: 1500, baixa: 1000, alta: null },
    limiteOperacionalLigado: true,
    limiteMensal: { alta: 240, saldosELimites: 420, mediaAlta: 120, media: 30, baixa: 8 },
    capacidadeTps: 300,
    pageSizeMaximo: 1000,
    validadeDaChaveMin: 60,
    retryAfterLigado: true,
    retryAfterSeg: 60,
    preparacaoDeRecursosSeg: 0,
  },
  relogio: null,
  sla: [
    { classe: 'alta', slaMs: 1500, p95Ms: 800, dentroDoSla: true },
    { classe: 'media-alta', slaMs: 1500, p95Ms: 800, dentroDoSla: true },
    { classe: 'media', slaMs: 2000, p95Ms: 1064, dentroDoSla: true },
    { classe: 'baixa', slaMs: 4000, p95Ms: 2136, dentroDoSla: true },
  ],
  avisos: [],
  anomaliasLigadas: [],
};

export const resumo = (endereco: string, nome: string, anomaliasLigadas: string[] = [], situacao = 'ativa') => ({
  endereco,
  nome,
  perfil: 'estavel',
  situacao,
  anomaliasLigadas,
});
