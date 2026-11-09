using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Simulador.Consentimentos.Contratos;

namespace Simulador.Consentimentos.Dominio;

/// <summary>Tipos de token do modo simples (RN120).</summary>
internal enum TipoDeToken
{
    Receptora,
    Consentimento,
}

/// <summary>Conteúdo de um token. O texto emitido é assinado e nada é gravado.</summary>
internal sealed record TokenDeAcesso(TipoDeToken Tipo, string OrganisationId, string Banco, string? ConsentId, DateTimeOffset ExpiraEm);

/// <summary>Emite e lê tokens assinados com HMAC.</summary>
internal static class CodecDeToken
{
    public const int ValidadeSegundos = 900;
    private const char Separador = '|';

    public static string Emitir(TokenDeAcesso token, byte[] chave)
    {
        var conteudo = string.Join(
            Separador,
            token.Tipo.ToString(),
            token.OrganisationId,
            token.Banco,
            token.ExpiraEm.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            token.ConsentId ?? string.Empty);
        var corpo = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(conteudo));
        return $"{corpo}.{Assinar(corpo, chave)}";
    }

    public static TokenDeAcesso? Ler(string? texto, byte[] chave)
    {
        var partes = (texto ?? string.Empty).Split('.');
        if (partes.Length != 2 || !Iguais(partes[1], Assinar(partes[0], chave)))
        {
            return null;
        }

        var campos = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(partes[0])).Split(Separador);
        return new TokenDeAcesso(
            Enum.Parse<TipoDeToken>(campos[0]),
            campos[1],
            campos[2],
            campos[4].Length == 0 ? null : campos[4],
            DateTimeOffset.FromUnixTimeSeconds(long.Parse(campos[3], CultureInfo.InvariantCulture)));
    }

    private static string Assinar(string corpo, byte[] chave) =>
        Base64Url.EncodeToString(HMACSHA256.HashData(chave, Encoding.UTF8.GetBytes(corpo)));

    /// <summary>Compara em tempo constante.</summary>
    public static bool Iguais(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}

/// <summary>Resultado do portão.</summary>
internal enum VereditoDoPortao
{
    Passa,
    NaoAutorizado,
    Proibido,
}

/// <summary>Regras de acesso às operações protegidas (RN88, RN122, Q1, Q4).</summary>
internal static class RegrasDoPortao
{
    public const string EscopoDaReceptora = "consents";
    public const string ContratoDeConsentimentos = "consents";

    private static readonly Dictionary<string, string> Familias = new(StringComparer.Ordinal)
    {
        ["accounts"] = "ACCOUNTS_",
        ["credit-cards-accounts"] = "CREDIT_CARDS_ACCOUNTS_",
        ["customers"] = "CUSTOMERS_",
        ["loans"] = "LOANS_",
        ["financings"] = "FINANCINGS_",
        ["unarranged-accounts-overdraft"] = "UNARRANGED_ACCOUNTS_OVERDRAFT_",
        ["invoice-financings"] = "INVOICE_FINANCINGS_",
        ["bank-fixed-incomes"] = "BANK_FIXED_INCOMES_",
        ["credit-fixed-incomes"] = "CREDIT_FIXED_INCOMES_",
        ["funds"] = "FUNDS_",
        ["variable-incomes"] = "VARIABLE_INCOMES_",
        ["treasure-titles"] = "TREASURE_TITLES_",
        ["exchanges"] = "EXCHANGES_",
    };

    /// <summary>Se a rota confere o estado do consentimento: a renovação responde 422 (RN87).</summary>
    public static bool ConfereEstado(OperacaoProtegida operacao) => operacao.Contrato != ContratoDeConsentimentos;

    public static bool ExigeTokenDaReceptora(OperacaoProtegida operacao) => operacao.Escopos.Contains(EscopoDaReceptora);

    public static VereditoDoPortao Avaliar(
        TokenDeAcesso? token,
        string banco,
        DateTimeOffset agora,
        OperacaoProtegida operacao,
        string? consentIdDaRota,
        DadosDoConsentimento? consentimento)
    {
        if (token is null || token.ExpiraEm <= agora || token.Banco != banco)
        {
            return VereditoDoPortao.NaoAutorizado;
        }

        var daReceptora = token.Tipo == TipoDeToken.Receptora;
        if (daReceptora != ExigeTokenDaReceptora(operacao))
        {
            return VereditoDoPortao.Proibido;
        }

        if (daReceptora)
        {
            return VereditoDoPortao.Passa;
        }

        if (!ConfereEstado(operacao))
        {
            return token.ConsentId == consentIdDaRota ? VereditoDoPortao.Passa : VereditoDoPortao.Proibido;
        }

        if (consentimento is null || consentimento.Banco != banco || Ciclo.Efetivo(consentimento, agora).Estado != Ciclo.Autorizado)
        {
            return VereditoDoPortao.NaoAutorizado;
        }

        return TemPermissao(operacao, consentimento.Permissoes) ? VereditoDoPortao.Passa : VereditoDoPortao.Proibido;
    }

    /// <summary>Se o consentimento serve a essa receptora (Q4).</summary>
    public static bool ServeA(DadosDoConsentimento consentimento, string organisationId) =>
        consentimento.Receptora is null || consentimento.Receptora == organisationId;

    private static bool TemPermissao(OperacaoProtegida operacao, IReadOnlyList<string> concedidas)
    {
        if (operacao.Permissoes.Count > 0)
        {
            return operacao.Permissoes.Any(concedidas.Contains);
        }

        return Familias.TryGetValue(operacao.Contrato, out var prefixo)
            && concedidas.Any(p => p.StartsWith(prefixo, StringComparison.Ordinal));
    }
}
