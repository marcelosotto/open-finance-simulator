using Simulador.Instituicoes.Contratos;

namespace Simulador.Instituicoes.Dominio;

/// <summary>Valores oficiais usados como padrão e como referência (RN11, RN21, RN26, RN29, RN31, RN35).</summary>
internal static class ValoresOficiais
{
    public const string PerfilPadrao = "estavel";
    public const string PublicoPadrao = "ambos";

    /// <summary>Classe de frequência e SLA oficial em milissegundos (RN11).</summary>
    public static IReadOnlyList<(string Classe, int SlaMs, decimal Fator)> Classes { get; } =
    [
        ("alta", 1500, 1m),
        ("media-alta", 1500, 1m),
        ("media", 2000, 1.33m),
        ("baixa", 4000, 2.67m),
    ];

    public static IReadOnlyList<string> Produtos { get; } =
        ["contas", "cartoes", "emprestimos", "financiamentos", "adiantamentos", "direitos-creditorios", "investimentos", "cambio"];

    /// <summary>Status que só aparecem por limite ou por regra de cenário (RN13).</summary>
    public static IReadOnlySet<int> StatusExclusivos { get; } = new HashSet<int> { 429, 423, 529 };

    /// <summary>TPM oficial da alta frequência pelos consentimentos ativos (RN21).</summary>
    public static int TpmDaAlta(long qca) => qca switch
    {
        <= 1_000_000 => 2_500,
        <= 2_000_000 => 5_000,
        <= 3_000_000 => 8_000,
        <= 6_000_000 => 10_000,
        _ => 10_000 + (2_000 * (int)Math.Ceiling((qca - 6_000_000) / 2_000_000d)),
    };

    public static Limites Limites() => new(
        Qca: 0,
        QcaPorReceptora: new Dictionary<string, long>(),
        Tpm: new Tpm(MediaAlta: 2_000, Media: 1_500, Baixa: 1_000),
        LimiteOperacionalLigado: true,
        LimiteMensal: new LimiteMensal(Alta: 240, SaldosELimites: 420, MediaAlta: 120, Media: 30, Baixa: 8),
        CapacidadeTps: 300,
        PageSizeMaximo: 1_000,
        ValidadeDaChaveMin: 60,
        RetryAfterLigado: true,
        RetryAfterSeg: 60,
        PreparacaoDeRecursosSeg: 0);
}

/// <summary>Os cinco perfis prontos (<c>docs/03-design/perfis-de-comportamento.md</c>).</summary>
internal static class CatalogoDePerfis
{
    public static IReadOnlyList<PerfilDeComportamento> Todos() =>
    [
        Perfil("estavel", 200, 800, 0m, 0.2m, [500], 0.2m, null, ValoresOficiais.Limites()),
        Perfil("lento", 1_200, 2_500, 0.1m, 0.5m, [504], 0.2m, null, ValoresOficiais.Limites() with { PageSizeMaximo = 500, PreparacaoDeRecursosSeg = 30 }),
        Perfil("instavel", 300, 1_400, 0.5m, 8m, [500, 504], 2m, new Janela(2, 60), ValoresOficiais.Limites() with { PreparacaoDeRecursosSeg = 60 }),
        Perfil("timeout", 300, 1_400, 5m, 1m, [504], 0.2m, null, ValoresOficiais.Limites() with { PreparacaoDeRecursosSeg = 180 }),
        Perfil("restritivo", 200, 800, 0m, 0.5m, [500], 1m, null, ValoresOficiais.Limites() with
        {
            Tpm = new Tpm(MediaAlta: 1_000, Media: 750, Baixa: 500, Alta: 1_250),
            LimiteMensal = new LimiteMensal(Alta: 120, SaldosELimites: 210, MediaAlta: 60, Media: 15, Baixa: 4),
            PageSizeMaximo = 200,
            ValidadeDaChaveMin = 10,
        }),
    ];

    public static PerfilDeComportamento? Obter(string nome) => Todos().FirstOrDefault(p => p.Nome == nome);

    private static PerfilDeComportamento Perfil(
        string nome, int p50, int p95, decimal cauda, decimal taxa, int[] status, decimal recurso, Janela? janela, Limites limites) =>
        new(
            nome,
            new Comportamento(
                ValoresOficiais.Classes.ToDictionary(
                    c => c.Classe,
                    c => new Latencia(Escalar(p50, c.Fator), Escalar(p95 - p50, c.Fator), cauda)),
                taxa,
                status.ToDictionary(s => s, _ => 1m),
                recurso,
                janela),
            limites);

    private static int Escalar(int valor, decimal fator) => (int)Math.Round(valor * fator, MidpointRounding.AwayFromZero);
}

/// <summary>Os 16 bancos que nascem com o ambiente (RN02, personas).</summary>
internal static class BancosProntos
{
    public static IReadOnlyList<(string Nome, string Perfil, long Qca, bool NomeReal)> Todos { get; } =
    [
        ("Banco Estável", "estavel", 0, false),
        ("Banco Lento", "lento", 0, false),
        ("Banco Instável", "instavel", 0, false),
        ("Banco Timeout", "timeout", 0, false),
        ("Banco Restritivo", "restritivo", 0, false),
        ("Itaú", "estavel", 7_000_000, true),
        ("Nubank", "estavel", 6_500_000, true),
        ("Bradesco", "estavel", 5_000_000, true),
        ("Banco do Brasil", "estavel", 4_500_000, true),
        ("Caixa", "estavel", 4_000_000, true),
        ("Santander", "estavel", 3_000_000, true),
        ("Inter", "estavel", 2_000_000, true),
        ("C6 Bank", "estavel", 1_500_000, true),
        ("BTG Pactual", "estavel", 800_000, true),
        ("Sicoob", "estavel", 700_000, true),
        ("Sicredi", "estavel", 600_000, true),
    ];
}
