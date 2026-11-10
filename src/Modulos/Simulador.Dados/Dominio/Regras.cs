using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Simulador.Dados.Dominio;

/// <summary>Uma família de dados: contrato, tipo de recurso e filtro de data.</summary>
internal sealed record Familia(string Contrato, string? TipoDeRecurso, string FiltroDeData, bool AceitaFuturo);

/// <summary>As 13 famílias de dados (RN41, tabela de endpoints).</summary>
internal static class Familias
{
    private const string Transacao = "TransactionDate";

    private static readonly Dictionary<string, Familia> Todas = new Familia[]
    {
        new("customers", null, Transacao, false),
        new("accounts", "ACCOUNT", "BookingDate", true),
        new("credit-cards-accounts", "CREDIT_CARD_ACCOUNT", Transacao, false),
        new("loans", "LOAN", Transacao, false),
        new("financings", "FINANCING", Transacao, false),
        new("unarranged-accounts-overdraft", "UNARRANGED_ACCOUNT_OVERDRAFT", Transacao, false),
        new("invoice-financings", "INVOICE_FINANCING", Transacao, false),
        new("bank-fixed-incomes", "BANK_FIXED_INCOME", Transacao, false),
        new("credit-fixed-incomes", "CREDIT_FIXED_INCOME", Transacao, false),
        new("variable-incomes", "VARIABLE_INCOME", Transacao, false),
        new("treasure-titles", "TREASURE_TITLE", Transacao, false),
        new("funds", "FUND", "TransactionConversionDate", false),
        new("exchanges", "EXCHANGE", Transacao, false),
    }.ToDictionary(f => f.Contrato, StringComparer.Ordinal);

    public static Familia? De(string contrato) => Todas.GetValueOrDefault(contrato);
}

/// <summary>Tipo da transação de cartão, coerente com o sentido, e os campos que o contrato exige por condição (RN42).</summary>
internal static class TiposDoCartao
{
    private const string Pagamento = "PAGAMENTO";
    private const string Tarifa = "TARIFA";
    private const string Operacoes = "OPERACOES_CREDITO_CONTRATADAS_CARTAO";
    private const string Outros = "OUTROS";

    private static readonly string[] DeCredito = ["PAGAMENTO_FATURA", "ESTORNO", "CASHBACK", Outros];
    private static readonly string[] DeDebito = [Pagamento, Tarifa, Operacoes, Outros];
    private static readonly string[] Tarifas = ["ANUIDADE", "SAQUE_CARTAO_BRASIL", "SAQUE_CARTAO_EXTERIOR"];
    private static readonly string[] OutrosCreditos = ["CREDITO_ROTATIVO", "PARCELAMENTO_FATURA", "EMPRESTIMO"];

    public static void Preencher(IDictionary<string, string> valores, bool credito, uint semente)
    {
        var opcoes = credito ? DeCredito : DeDebito;
        var tipo = opcoes[semente % (uint)opcoes.Length];
        valores["transactionType"] = tipo;
        switch (tipo)
        {
            case Tarifa:
                valores["feeType"] = Tarifas[semente % (uint)Tarifas.Length];
                break;
            case Operacoes:
                valores["otherCreditsType"] = OutrosCreditos[semente % (uint)OutrosCreditos.Length];
                break;
            case Outros:
                valores["transactionalAdditionalInfo"] = "Outra transação";
                break;
        }
    }
}

/// <summary>Filtros opcionais do contrato e o campo da resposta que cada um compara (RN42).</summary>
internal static class FiltrosOficiais
{
    public static IReadOnlyDictionary<string, string> Campos { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["creditDebitIndicator"] = "creditDebitType",
        ["accountType"] = "type",
        ["transactionType"] = "transactionType",
        ["payeeMCC"] = "payeeMCC",
    };
}

/// <summary>O que uma operação entrega, deduzido do caminho oficial.</summary>
internal enum TipoDeOperacao
{
    Cadastral,
    ListaDeRecursos,
    Recurso,
    NotaDeNegociacao,
    Transacoes,
    TransacoesRecentes,
    Faturas,
    TransacoesDaFatura,
    Sublista,
    Detalhe,
}

/// <summary>Classifica o caminho e acha o nome do parâmetro do recurso.</summary>
internal static class Classificador
{
    public static (TipoDeOperacao Tipo, string? Parametro) De(string caminho)
    {
        var partes = caminho.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var parametro = partes.Length > 1 && partes[1].StartsWith('{') ? partes[1][1..^1] : null;
        if (parametro is null)
        {
            return (partes.Length == 1 ? TipoDeOperacao.ListaDeRecursos : TipoDeOperacao.Cadastral, null);
        }

        if (partes.Length == 2)
        {
            return (partes[0] == "broker-notes" ? TipoDeOperacao.NotaDeNegociacao : TipoDeOperacao.Recurso, parametro);
        }

        var tipo = partes[^1] switch
        {
            "transactions" when partes.Length > 3 => TipoDeOperacao.TransacoesDaFatura,
            "transactions" => TipoDeOperacao.Transacoes,
            "transactions-current" => TipoDeOperacao.TransacoesRecentes,
            "bills" => TipoDeOperacao.Faturas,
            "warranties" or "events" => TipoDeOperacao.Sublista,
            _ => TipoDeOperacao.Detalhe,
        };
        return (tipo, parametro);
    }
}

/// <summary>Página pedida e tamanho aplicado (RN31, RN32, PA21).</summary>
internal sealed record PedidoDePagina(int Pagina, int Tamanho)
{
    public const int TamanhoPadrao = 25;

    /// <summary>Nulo quando <c>page</c> ou <c>page-size</c> são inválidos.</summary>
    public static PedidoDePagina? Ler(string? pagina, string? tamanho, int minimo, int maximo)
    {
        var numero = 1;
        if (!string.IsNullOrEmpty(pagina) && (!int.TryParse(pagina, NumberStyles.None, CultureInfo.InvariantCulture, out numero) || numero < 1))
        {
            return null;
        }

        var pedido = TamanhoPadrao;
        if (!string.IsNullOrEmpty(tamanho) && (!int.TryParse(tamanho, NumberStyles.None, CultureInfo.InvariantCulture, out pedido) || pedido < 1))
        {
            return null;
        }

        return new PedidoDePagina(numero, Math.Min(Math.Max(pedido, minimo), maximo));
    }

    public int TotalDePaginas(int registros) => registros == 0 ? 0 : ((registros - 1) / Tamanho) + 1;

    public bool ForaDaFaixa(int registros) => Pagina > Math.Max(TotalDePaginas(registros), 1);
}

/// <summary>Erro de filtro de data (RN41, RN42).</summary>
internal enum ErroDeData
{
    Nenhum,
    NaoInformado,
    Invalido,
}

/// <summary>Período resolvido de uma consulta.</summary>
internal readonly record struct Periodo(DateOnly De, DateOnly Ate, ErroDeData Erro = ErroDeData.Nenhum)
{
    public static Periodo Falha(ErroDeData erro) => new(default, default, erro);
}

/// <summary>Janelas oficiais de data (RN41, RN42).</summary>
internal static class JanelaDeDatas
{
    public const int MesesDeHistorico = 12;
    private const int DiasRecentes = 6;

    public static Periodo Transacoes(string? de, string? ate, DateOnly hoje, bool recente, bool aceitaFuturo)
    {
        if (string.IsNullOrEmpty(de) && string.IsNullOrEmpty(ate))
        {
            return new Periodo(hoje, hoje);
        }

        var par = Par(de, ate);
        if (par.Erro != ErroDeData.Nenhum)
        {
            return par;
        }

        var minimo = recente ? hoje.AddDays(-DiasRecentes) : hoje.AddMonths(-MesesDeHistorico);
        var maximo = aceitaFuturo ? hoje.AddMonths(MesesDeHistorico) : hoje;
        return par.De < minimo || par.Ate > maximo ? Periodo.Falha(ErroDeData.Invalido) : par;
    }

    /// <summary><c>/bills</c>: datas isoladas; sem datas, 12 meses, inclusive a fatura fechada que ainda vai vencer.</summary>
    public static Periodo Faturas(string? de, string? ate, DateOnly hoje)
    {
        var inicio = hoje.AddMonths(-MesesDeHistorico);
        if (!Data(de, inicio, out var primeira) || !Data(ate, hoje.AddMonths(1), out var ultima) || primeira > ultima)
        {
            return Periodo.Falha(ErroDeData.Invalido);
        }

        return new Periodo(primeira < inicio ? inicio : primeira, ultima);
    }

    /// <summary><c>/bills/{billId}/transactions</c>: exige o par e, sem datas, a fatura inteira.</summary>
    public static Periodo DaFatura(string? de, string? ate, DateOnly inicioDaFatura, DateOnly fimDaFatura)
    {
        if (string.IsNullOrEmpty(de) && string.IsNullOrEmpty(ate))
        {
            return new Periodo(inicioDaFatura, fimDaFatura);
        }

        var par = Par(de, ate);
        if (par.Erro != ErroDeData.Nenhum)
        {
            return par;
        }

        return new Periodo(par.De > inicioDaFatura ? par.De : inicioDaFatura, par.Ate < fimDaFatura ? par.Ate : fimDaFatura);
    }

    private static Periodo Par(string? de, string? ate)
    {
        if (string.IsNullOrEmpty(de) || string.IsNullOrEmpty(ate))
        {
            return Periodo.Falha(ErroDeData.NaoInformado);
        }

        return Data(de, default, out var primeira) && Data(ate, default, out var ultima) && primeira <= ultima
            ? new Periodo(primeira, ultima)
            : Periodo.Falha(ErroDeData.Invalido);
    }

    private static bool Data(string? texto, DateOnly padrao, out DateOnly valor)
    {
        valor = padrao;
        return string.IsNullOrEmpty(texto) || DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out valor);
    }
}

/// <summary>
/// O que uma chave válida leva: o instante de emissão no banco, o instante real da emissão e,
/// nas listas de recursos, o conjunto da emissão.
/// </summary>
internal sealed record LeituraDaChave(DateTimeOffset Emissao, DateTimeOffset EmissaoReal, string? Recursos);

/// <summary>
/// Chave de paginação assinada: instante do banco, instante real, consulta e, nas listas de recursos,
/// o conjunto da emissão, sem gravar nada (RN35 a RN37). Chave sem o instante real é inválida (RN36).
/// </summary>
internal static class ChaveDePaginacao
{
    private const char Separador = '.';

    public static string Emitir(DateTimeOffset instante, DateTimeOffset real, string consulta, byte[] segredo, string? recursos = null)
    {
        var corpo = string.Create(CultureInfo.InvariantCulture, $"{instante.ToUnixTimeMilliseconds()}{Separador}{real.ToUnixTimeMilliseconds()}");
        corpo = recursos is null ? corpo : $"{corpo}{Separador}{recursos}";
        return $"{corpo}{Separador}{Assinar(corpo, consulta, segredo)}";
    }

    /// <summary>O que a chave leva, ou nulo se ela for inválida ou expirada.</summary>
    public static LeituraDaChave? Abrir(string? texto, string consulta, byte[] segredo, DateTimeOffset agora, int validadeMin)
    {
        var chave = texto ?? string.Empty;
        var fim = chave.LastIndexOf(Separador);
        var corpo = fim < 0 ? string.Empty : chave[..fim];
        var partes = corpo.Split(Separador);
        if (fim < 0 || partes.Length is < 2 or > 3 || Instante(partes[0]) is not { } emissao || Instante(partes[1]) is not { } real
            || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(chave[(fim + 1)..]), Encoding.UTF8.GetBytes(Assinar(corpo, consulta, segredo))))
        {
            return null;
        }

        return agora <= emissao.AddMinutes(validadeMin) ? new LeituraDaChave(emissao, real, partes.Length == 3 ? partes[2] : null) : null;
    }

    private static DateTimeOffset? Instante(string texto) =>
        long.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var milissegundos) && milissegundos <= DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()
            ? DateTimeOffset.FromUnixTimeMilliseconds(milissegundos)
            : null;

    private static string Assinar(string corpo, string consulta, byte[] segredo) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(segredo, Encoding.UTF8.GetBytes($"{corpo}|{consulta}")));
}

/// <summary>
/// Quais posições da lista de recursos do consentimento entraram na lista na emissão (RN37).
/// O consentimento só ganha recursos no fim, então as posições da emissão não mudam.
/// </summary>
internal static class RecursosDaChave
{
    public static string Marcar(IEnumerable<int> posicoes, int total)
    {
        ArgumentNullException.ThrowIfNull(posicoes);
        var bytes = new byte[(total + 7) / 8];
        foreach (var posicao in posicoes)
        {
            bytes[posicao / 8] |= (byte)(1 << (posicao % 8));
        }

        return Base64Url.EncodeToString(bytes);
    }

    public static HashSet<int> Posicoes(string marcados)
    {
        var bytes = Base64Url.DecodeFromChars(marcados);
        return [.. Enumerable.Range(0, bytes.Length * 8).Where(p => (bytes[p / 8] & (1 << (p % 8))) != 0)];
    }
}

/// <summary>Identificadores derivados do recurso dono (RN105).</summary>
internal static class IdsDerivados
{
    private const string Fatura = "-b";
    private const string Nota = "-n1";

    public static string DaFatura(string recursoId, DateOnly mes) => string.Create(CultureInfo.InvariantCulture, $"{recursoId}{Fatura}{mes:yyyyMM}");

    public static string DaNota(string recursoId) => recursoId + Nota;

    /// <summary>Primeiro dia do mês da fatura, se ela for do recurso.</summary>
    public static DateOnly? MesDaFatura(string billId, string recursoId) =>
        billId.StartsWith(recursoId + Fatura, StringComparison.Ordinal)
        && DateOnly.TryParseExact(billId[(recursoId.Length + Fatura.Length)..], "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var mes)
            ? mes
            : null;

    /// <summary>Base dos IDs de Dados Cadastrais, no padrão oficial de prefixo (RN62).</summary>
    public static string BaseDoConsentimento(string consentId) => consentId.Replace("urn:", string.Empty, StringComparison.Ordinal).Replace(':', '-');

    public static string? RecursoDaNota(string notaId) => notaId.EndsWith(Nota, StringComparison.Ordinal) ? notaId[..^Nota.Length] : null;
}
