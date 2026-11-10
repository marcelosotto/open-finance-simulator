using Simulador.Consentimentos.Contratos;

namespace Simulador.Consentimentos.Dominio;

/// <summary>Ciclo do consentimento (RN82, RN87).</summary>
internal static class Ciclo
{
    public const string Autorizado = "AUTHORISED";
    public const string Rejeitado = "REJECTED";
    public const string PorTpp = "TPP";
    public const string PorUsuario = "USER";
    public const string PorInstituicao = "ASPSP";
    public const string Revogado = "CUSTOMER_MANUALLY_REVOKED";
    public const string Vencido = "CONSENT_MAX_DATE_REACHED";
    public const string Fraude = "INTERNAL_SECURITY_REASON";

    public const string EstadoInvalido = "ESTADO_CONSENTIMENTO_INVALIDO";
    public const string DependeAlcada = "DEPENDE_MULTIPLA_ALCADA";
    public const string DataInvalida = "DATA_EXPIRACAO_INVALIDA";
    public const string NaoMapeado = "ERRO_NAO_MAPEADO";

    /// <summary>O consentimento vencido vale como rejeitado em toda leitura (RN82).</summary>
    public static DadosDoConsentimento Efetivo(DadosDoConsentimento consentimento, DateTimeOffset agora) =>
        consentimento.Estado == Autorizado && consentimento.ExpiraEm is { } expira && expira <= agora
            ? consentimento with { Estado = Rejeitado, RejeitadoPor = PorInstituicao, Razao = Vencido, AtualizadoEm = expira }
            : consentimento;

    public static DadosDoConsentimento Rejeitar(DadosDoConsentimento consentimento, string quem, string razao, DateTimeOffset agora) =>
        consentimento with { Estado = Rejeitado, RejeitadoPor = quem, Razao = razao, AtualizadoEm = agora };

    /// <summary>Código do erro 422 da renovação; nulo se pode renovar (RN87).</summary>
    public static string? ErroDaRenovacao(DadosDoConsentimento efetivo, string? novaValidade, DateTimeOffset? nova, DateTimeOffset agora)
    {
        if (efetivo.Estado != Autorizado)
        {
            return EstadoInvalido;
        }

        if (efetivo.AprovacoesPendentes > 0)
        {
            return DependeAlcada;
        }

        if (novaValidade is null)
        {
            return null;
        }

        return nova is { } data && data > agora && efetivo.ExpiraEm is { } atual && data > atual ? null : DataInvalida;
    }

    /// <summary>PF: só o CPF criador. PJ: qualquer CPF simulado, com o mesmo CNPJ (RN87).</summary>
    public static bool PodeRenovar(DadosDoConsentimento consentimento, string tipoDoCliente, string cpf, bool cpfSimulado, string? cnpj) =>
        tipoDoCliente == "PJ"
            ? cpfSimulado && cnpj == consentimento.Cliente
            : cpf == (consentimento.UsuarioLogado ?? consentimento.Cliente);
}

/// <summary>Situação da preparação de recursos.</summary>
internal enum SituacaoDaPreparacao
{
    Pronta,
    Preparando,
    Esgotada,
}

/// <summary>Preparação da lista de recursos (RN103).</summary>
internal static class Preparacao
{
    public const int LimiteSegundos = 300;

    public static SituacaoDaPreparacao Situacao(DateTimeOffset criadoEm, int duracaoSegundos, DateTimeOffset agora)
    {
        var passados = Math.Max(0, (agora - criadoEm).TotalSeconds);
        if (passados >= LimiteSegundos)
        {
            return duracaoSegundos > LimiteSegundos ? SituacaoDaPreparacao.Esgotada : SituacaoDaPreparacao.Pronta;
        }

        return passados < duracaoSegundos ? SituacaoDaPreparacao.Preparando : SituacaoDaPreparacao.Pronta;
    }
}

/// <summary>Estados e transições de recurso (RN100, RN101).</summary>
internal static class EstadosDeRecurso
{
    public const string Disponivel = "AVAILABLE";
    public const string Temporario = "TEMPORARILY_UNAVAILABLE";
    public const string Indisponivel = "UNAVAILABLE";
    public const string Pendente = "PENDING_AUTHORISATION";

    public static bool Existe(string estado) => estado is Disponivel or Temporario or Indisponivel or Pendente;

    public static bool Permitida(string tipoDoRecurso, string de, string para) => (de, para) switch
    {
        _ when de == para => true,
        (_, Pendente) => false,
        (Indisponivel, Disponivel) => tipoDoRecurso is "FUND" or "VARIABLE_INCOME",
        (Indisponivel, _) => false,
        _ => true,
    };
}
