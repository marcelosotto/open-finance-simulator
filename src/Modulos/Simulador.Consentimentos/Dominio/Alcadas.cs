using Simulador.Consentimentos.Contratos;

namespace Simulador.Consentimentos.Dominio;

/// <summary>Situação da múltipla alçada de um consentimento (data-model.md).</summary>
internal enum SituacaoDaAlcada
{
    SemAlcada,
    Pendente,
    Vencida,
    Recusada,
    Aprovada,
    Encerrada,
}

/// <summary>Múltiplas alçadas e estado do recurso no consentimento (RN97, RN98, RN99).</summary>
internal static class Alcadas
{
    public const int Maximo = 10;
    public const string Aprovada = "aprovada";
    public const string Recusada = "recusada";

    public static readonly TimeSpan Prazo = TimeSpan.FromDays(15);

    public static SituacaoDaAlcada Situacao(DadosDoConsentimento consentimento, DateTimeOffset agora)
    {
        if (consentimento.AlcadaRecusada)
        {
            return SituacaoDaAlcada.Recusada;
        }

        if (consentimento.AprovacoesPendentes > 0)
        {
            return agora - consentimento.CriadoEm >= Prazo ? SituacaoDaAlcada.Vencida : SituacaoDaAlcada.Pendente;
        }

        return consentimento.Decisoes is { Count: > 0 } ? SituacaoDaAlcada.Aprovada : SituacaoDaAlcada.SemAlcada;
    }

    public static string Nome(SituacaoDaAlcada situacao) => situacao switch
    {
        SituacaoDaAlcada.Pendente => "pendente",
        SituacaoDaAlcada.Vencida => "vencida",
        SituacaoDaAlcada.Recusada => Recusada,
        SituacaoDaAlcada.Aprovada => Aprovada,
        SituacaoDaAlcada.Encerrada => "encerrada",
        _ => "sem-alcada",
    };

    /// <summary>O que a visão da administração mostra: consentimento <c>REJECTED</c> não tem aprovação pendente (RN99).</summary>
    public static (SituacaoDaAlcada Situacao, int Pendentes) NaVisao(DadosDoConsentimento efetivo, DateTimeOffset agora)
    {
        var situacao = Situacao(efetivo, agora);
        return efetivo.Estado == Ciclo.Rejeitado && situacao is SituacaoDaAlcada.Pendente or SituacaoDaAlcada.Vencida
            ? (SituacaoDaAlcada.Encerrada, 0)
            : (situacao, efetivo.AprovacoesPendentes);
    }

    /// <summary>Fim do prazo da alçada; nulo sem alçada (RN99).</summary>
    public static DateTimeOffset? FimDoPrazo(DadosDoConsentimento consentimento) =>
        consentimento.AprovacoesPendentes > 0 || consentimento.AlcadaRecusada || consentimento.Decisoes is { Count: > 0 }
            ? consentimento.CriadoEm + Prazo
            : null;

    /// <summary>O estado do recurso naquele consentimento, antes da regra de cenário (RN97).</summary>
    public static string EstadoNoConsentimento(DadosDoConsentimento consentimento, string estadoGravado, DateTimeOffset agora) =>
        Situacao(consentimento, agora) switch
        {
            SituacaoDaAlcada.Recusada or SituacaoDaAlcada.Vencida => EstadosDeRecurso.Indisponivel,
            SituacaoDaAlcada.Pendente when estadoGravado != EstadosDeRecurso.Indisponivel => EstadosDeRecurso.Pendente,
            _ => estadoGravado,
        };

    /// <summary>Motivo do 409 de uma decisão; nulo se pode decidir (RN99).</summary>
    public static string? ErroDaDecisao(DadosDoConsentimento efetivo, DateTimeOffset agora)
    {
        if (efetivo.Estado == Ciclo.Rejeitado)
        {
            return "consentimento rejeitado";
        }

        return Situacao(efetivo, agora) switch
        {
            SituacaoDaAlcada.Pendente => null,
            SituacaoDaAlcada.Recusada => "alçada recusada",
            SituacaoDaAlcada.Vencida => "prazo da alçada encerrado",
            _ => "sem aprovação pendente",
        };
    }

    public static DadosDoConsentimento Aprovar(DadosDoConsentimento consentimento, string autor, DateTimeOffset agora) =>
        consentimento with
        {
            AprovacoesPendentes = consentimento.AprovacoesPendentes - 1,
            Decisoes = [.. consentimento.Decisoes ?? [], new DadosDaDecisao(autor, Aprovada, agora)],
        };

    public static DadosDoConsentimento Recusar(DadosDoConsentimento consentimento, string autor, DateTimeOffset agora) =>
        consentimento with
        {
            AlcadaRecusada = true,
            Decisoes = [.. consentimento.Decisoes ?? [], new DadosDaDecisao(autor, Recusada, agora)],
        };
}
