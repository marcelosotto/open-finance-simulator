using Simulador.Massa.Contratos;

namespace Simulador.Massa.Dominio;

/// <summary>Perfis de dados que nascem com o ambiente (RN55).</summary>
internal static class PerfisProntos
{
    public const string Padrao = "cliente-comum";

    private static readonly string[] Lancamentos = ["PIX", "TED", "BOLETO", "CARTAO"];

    public static IReadOnlyList<PerfilDeDados> Todos { get; } =
    [
        new(Padrao, 0, 4, 10m, 2_000m, 5_000m, Lancamentos, ["contas", "cartoes", "emprestimos"]),
        new("sem-movimento", 0, 0, 0m, 0m, 1_000m, Lancamentos, ["contas"]),
        new("alto-volume", 2_000, 3_000, 1m, 50_000m, 1_000_000m, Lancamentos, ["contas", "cartoes"]),
    ];

    public static bool Eh(string nome) => Todos.Any(p => p.Nome == nome);
}

/// <summary>Produtos, tipos oficiais de recurso e permissões das specs travadas (data-model).</summary>
internal static class Produtos
{
    private static readonly Dictionary<string, (string Tipo, string Permissao)> Mapa = new(StringComparer.Ordinal)
    {
        ["contas"] = ("ACCOUNT", "ACCOUNTS_"),
        ["cartoes"] = ("CREDIT_CARD_ACCOUNT", "CREDIT_CARDS_ACCOUNTS_"),
        ["emprestimos"] = ("LOAN", "LOANS_"),
        ["financiamentos"] = ("FINANCING", "FINANCINGS_"),
        ["adiantamentos"] = ("UNARRANGED_ACCOUNT_OVERDRAFT", "UNARRANGED_ACCOUNTS_OVERDRAFT_"),
        ["direitos-creditorios"] = ("INVOICE_FINANCING", "INVOICE_FINANCINGS_"),
        ["investimentos"] = ("BANK_FIXED_INCOME", "BANK_FIXED_INCOMES_"),
        ["cambio"] = ("EXCHANGE", "EXCHANGES_"),
    };

    public static bool Existe(string produto) => Mapa.ContainsKey(produto);

    public static string TipoDe(string produto) => Mapa[produto].Tipo;

    public static bool Coberto(string produto, IEnumerable<string> permissoes) =>
        permissoes.Any(p => p.StartsWith(Mapa[produto].Permissao, StringComparison.Ordinal));
}

/// <summary>Permissões oficiais de consentimento (RN83, RN84).</summary>
internal static class Permissoes
{
    private const string Pessoal = "CUSTOMERS_PERSONAL_";
    private const string Empresarial = "CUSTOMERS_BUSINESS_";

    public static IReadOnlyList<string> Todas { get; } =
    [
        "ACCOUNTS_BALANCES_READ", "ACCOUNTS_OVERDRAFT_LIMITS_READ", "ACCOUNTS_READ", "ACCOUNTS_TRANSACTIONS_READ",
        "BANK_FIXED_INCOMES_READ",
        "CREDIT_CARDS_ACCOUNTS_BILLS_READ", "CREDIT_CARDS_ACCOUNTS_BILLS_TRANSACTIONS_READ", "CREDIT_CARDS_ACCOUNTS_LIMITS_READ",
        "CREDIT_CARDS_ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_TRANSACTIONS_READ",
        "CREDIT_FIXED_INCOMES_READ",
        "CUSTOMERS_BUSINESS_ADITTIONALINFO_READ", "CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ",
        "CUSTOMERS_PERSONAL_ADITTIONALINFO_READ", "CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ",
        "EXCHANGES_READ",
        "FINANCINGS_PAYMENTS_READ", "FINANCINGS_READ", "FINANCINGS_SCHEDULED_INSTALMENTS_READ", "FINANCINGS_WARRANTIES_READ",
        "FUNDS_READ",
        "INVOICE_FINANCINGS_PAYMENTS_READ", "INVOICE_FINANCINGS_READ", "INVOICE_FINANCINGS_SCHEDULED_INSTALMENTS_READ", "INVOICE_FINANCINGS_WARRANTIES_READ",
        "LOANS_PAYMENTS_READ", "LOANS_READ", "LOANS_SCHEDULED_INSTALMENTS_READ", "LOANS_WARRANTIES_READ",
        "RESOURCES_READ", "TREASURE_TITLES_READ",
        "UNARRANGED_ACCOUNTS_OVERDRAFT_PAYMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_READ",
        "UNARRANGED_ACCOUNTS_OVERDRAFT_SCHEDULED_INSTALMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_WARRANTIES_READ",
        "VARIABLE_INCOMES_READ",
    ];

    /// <summary>As pedidas, ou todas, sem as do outro tipo de pessoa (RN84).</summary>
    public static IReadOnlyList<string> Para(string tipo, IReadOnlyList<string>? pedidas)
    {
        var proibido = tipo == TiposDePessoa.Pf ? Empresarial : Pessoal;
        return [.. (pedidas ?? Todas).Where(p => !p.StartsWith(proibido, StringComparison.Ordinal))];
    }
}

/// <summary>Regras de cadastro da massa (RN55, RN56).</summary>
internal static class Validacao
{
    public static IReadOnlyList<string> Validar(PerfilDeDados perfil)
    {
        var erros = new List<string>();
        Exigir(erros, !string.IsNullOrWhiteSpace(perfil.Nome), "nome é obrigatório");
        Exigir(erros, perfil.TransacoesMin >= 0 && perfil.TransacoesMin <= perfil.TransacoesMax, "faixa de transações inválida");
        Exigir(erros, perfil.ValorMin >= 0 && perfil.ValorMin <= perfil.ValorMax, "faixa de valores inválida");
        Exigir(erros, perfil.SaldoInicial >= 0, "saldo inicial inválido");
        Exigir(erros, perfil.TransacoesMax == 0 || perfil.TiposDeLancamento.Count > 0, "tipos de lançamento são obrigatórios");
        Exigir(erros, perfil.Produtos.Count > 0, "produtos são obrigatórios");
        erros.AddRange(perfil.Produtos.Where(p => !Produtos.Existe(p)).Select(p => $"produto desconhecido: {p}"));
        return erros;
    }

    public static IReadOnlyList<string> Validar(Distribuicao distribuicao)
    {
        var erros = new List<string>();
        ExigirSoma100(erros, "bancos", distribuicao.Bancos);
        ExigirSoma100(erros, "perfis", distribuicao.Perfis);
        var produtos = distribuicao.Produtos ?? new Dictionary<string, decimal>();
        erros.AddRange(produtos.Keys.Where(p => !Produtos.Existe(p)).Select(p => $"produto desconhecido: {p}"));
        Exigir(erros, produtos.Values.All(EhPorcentagem), "porcentagem de produto fora de 0 a 100");
        Exigir(erros, EhPorcentagem(distribuicao.PjPct), "porcentagem de PJ fora de 0 a 100");
        return erros;
    }

    private static void ExigirSoma100(List<string> erros, string nome, IReadOnlyDictionary<string, decimal> valores)
    {
        Exigir(erros, valores.Values.All(EhPorcentagem), $"porcentagem de {nome} fora de 0 a 100");
        Exigir(erros, valores.Values.All(v => decimal.Round(v, 2) == v), $"{nome} aceita até 2 casas decimais");
        Exigir(erros, valores.Values.Sum() == 100, $"a soma de {nome} precisa dar 100%");
    }

    private static bool EhPorcentagem(decimal valor) => valor is >= 0 and <= 100;

    private static void Exigir(List<string> erros, bool condicao, string mensagem)
    {
        if (!condicao)
        {
            erros.Add(mensagem);
        }
    }
}
