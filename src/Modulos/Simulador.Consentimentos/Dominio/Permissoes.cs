namespace Simulador.Consentimentos.Dominio;

/// <summary>Agrupamentos da tabela do contrato <c>consents-3.3.1</c> e regras RN08, RN83 e RN84.</summary>
internal static class Permissoes
{
    public const string Recursos = "RESOURCES_READ";
    public const string SemFuncionais = "SEM_PERMISSOES_FUNCIONAIS_RESTANTES";
    public const string Combinacao = "COMBINACAO_PERMISSOES_INCORRETA";
    public const string PfPjJuntas = "PERMISSAO_PF_PJ_EM_CONJUNTO";
    public const string PjSemDados = "INFORMACOES_PJ_NAO_INFORMADAS";
    public const string PjIncorretas = "PERMISSOES_PJ_INCORRETAS";

    private const string Pessoal = "CUSTOMERS_PERSONAL_";
    private const string Empresarial = "CUSTOMERS_BUSINESS_";
    private const string Contas = "ACCOUNTS_";
    private const string Cartoes = "CREDIT_CARDS_ACCOUNTS_";
    private const string ContaBase = "ACCOUNTS_READ";
    private const string CartaoBase = "CREDIT_CARDS_ACCOUNTS_READ";

    /// <summary>Grupos oficiais com nome para o menu do painel e o tipo de pessoa (nulo: PF e PJ) (RN96).</summary>
    public static IReadOnlyList<(string Nome, string? Pessoa, string[] Permissoes)> GruposNomeados { get; } =
    [
        ("cadastro-identificacao", "PF", ["CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ"]),
        ("cadastro-complementar", "PF", ["CUSTOMERS_PERSONAL_ADITTIONALINFO_READ"]),
        ("cadastro-identificacao", "PJ", ["CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ"]),
        ("cadastro-complementar", "PJ", ["CUSTOMERS_BUSINESS_ADITTIONALINFO_READ"]),
        ("contas-saldos", null, [ContaBase, "ACCOUNTS_BALANCES_READ"]),
        ("contas-limites", null, [ContaBase, "ACCOUNTS_OVERDRAFT_LIMITS_READ"]),
        ("contas-transacoes", null, [ContaBase, "ACCOUNTS_TRANSACTIONS_READ"]),
        ("cartoes-limites", null, [CartaoBase, "CREDIT_CARDS_ACCOUNTS_LIMITS_READ"]),
        ("cartoes-transacoes", null, [CartaoBase, "CREDIT_CARDS_ACCOUNTS_TRANSACTIONS_READ"]),
        ("cartoes-faturas", null, [CartaoBase, "CREDIT_CARDS_ACCOUNTS_BILLS_READ", "CREDIT_CARDS_ACCOUNTS_BILLS_TRANSACTIONS_READ"]),
        ("operacoes-de-credito", null,
        [
            "LOANS_READ", "LOANS_WARRANTIES_READ", "LOANS_SCHEDULED_INSTALMENTS_READ", "LOANS_PAYMENTS_READ",
            "FINANCINGS_READ", "FINANCINGS_WARRANTIES_READ", "FINANCINGS_SCHEDULED_INSTALMENTS_READ", "FINANCINGS_PAYMENTS_READ",
            "UNARRANGED_ACCOUNTS_OVERDRAFT_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_WARRANTIES_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_SCHEDULED_INSTALMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_PAYMENTS_READ",
            "INVOICE_FINANCINGS_READ", "INVOICE_FINANCINGS_WARRANTIES_READ", "INVOICE_FINANCINGS_SCHEDULED_INSTALMENTS_READ", "INVOICE_FINANCINGS_PAYMENTS_READ",
        ]),
        ("investimentos", null, ["BANK_FIXED_INCOMES_READ", "CREDIT_FIXED_INCOMES_READ", "FUNDS_READ", "VARIABLE_INCOMES_READ", "TREASURE_TITLES_READ"]),
        ("cambio", null, ["EXCHANGES_READ"]),
    ];

    private static readonly string[][] Grupos = [.. GruposNomeados.Select(g => g.Permissoes)];

    private static readonly HashSet<string> Conhecidas = [.. Grupos.SelectMany(g => g), Recursos];

    private static readonly Dictionary<string, string> PrefixoPorTipo = new(StringComparer.Ordinal)
    {
        ["ACCOUNT"] = Contas,
        ["CREDIT_CARD_ACCOUNT"] = Cartoes,
        ["LOAN"] = "LOANS_",
        ["FINANCING"] = "FINANCINGS_",
        ["UNARRANGED_ACCOUNT_OVERDRAFT"] = "UNARRANGED_ACCOUNTS_OVERDRAFT_",
        ["INVOICE_FINANCING"] = "INVOICE_FINANCINGS_",
        ["BANK_FIXED_INCOME"] = "BANK_FIXED_INCOMES_",
        ["CREDIT_FIXED_INCOME"] = "CREDIT_FIXED_INCOMES_",
        ["VARIABLE_INCOME"] = "VARIABLE_INCOMES_",
        ["TREASURE_TITLE"] = "TREASURE_TITLES_",
        ["FUND"] = "FUNDS_",
        ["EXCHANGE"] = "EXCHANGES_",
    };

    public static bool TodasConhecidas(IReadOnlyList<string> permissoes) => permissoes.Count > 0 && permissoes.All(Conhecidas.Contains);

    /// <summary>Código do erro 422 de RN84 e RN83; nulo se o pedido é válido.</summary>
    public static string? Validar(IReadOnlyList<string> permissoes, bool temDadosPj)
    {
        var pf = permissoes.Any(p => p.StartsWith(Pessoal, StringComparison.Ordinal));
        var pj = permissoes.Any(p => p.StartsWith(Empresarial, StringComparison.Ordinal));
        if (pf && pj)
        {
            return PfPjJuntas;
        }

        if (pj && !temDadosPj)
        {
            return PjSemDados;
        }

        if (pf && temDadosPj)
        {
            return PjIncorretas;
        }

        return permissoes.Contains(Recursos) && GruposCompletos(permissoes) ? null : Combinacao;
    }

    /// <summary>Tira contas e cartões que o banco não oferece; os agrupados ficam (RN08, RN83).</summary>
    public static IReadOnlyList<string> Aplicaveis(IReadOnlyList<string> permissoes, IReadOnlyList<string> produtosDoBanco) =>
        [.. permissoes.Where(p => !(
            (p.StartsWith(Contas, StringComparison.Ordinal) && !produtosDoBanco.Contains("contas"))
            || (p.StartsWith(Cartoes, StringComparison.Ordinal) && !produtosDoBanco.Contains("cartoes"))))];

    public static bool TemFuncional(IReadOnlyList<string> permissoes) => permissoes.Any(p => p != Recursos);

    public static bool Cobre(string tipoDoRecurso, IReadOnlyList<string> permissoes) =>
        PrefixoPorTipo.TryGetValue(tipoDoRecurso, out var prefixo) && permissoes.Any(p => p.StartsWith(prefixo, StringComparison.Ordinal));

    /// <summary>Contas e cartões têm seleção individual; o resto é agrupado (RN95, RN106).</summary>
    public static bool Agrupado(string tipoDoRecurso) => tipoDoRecurso is not ("ACCOUNT" or "CREDIT_CARD_ACCOUNT");

    private static bool GruposCompletos(IReadOnlyList<string> permissoes)
    {
        var completas = Grupos.Where(g => g.All(permissoes.Contains)).SelectMany(g => g).ToHashSet();
        return permissoes.Where(p => p != Recursos).All(completas.Contains) && completas.Count > 0;
    }
}
