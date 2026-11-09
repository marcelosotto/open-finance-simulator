using System.Text.Encodings.Web;
using System.Text.Json;

namespace Simulador.Cenarios.Contratos;

/// <summary>O que uma regra faz (UC04).</summary>
public sealed record EfeitoDaRegra(
    string Tipo,
    int? Status = null,
    int? LatenciaMs = null,
    int? EsperaMs = null,
    string? Resultado = null,
    string? Falha = null,
    string? Estado = null);

/// <summary>Regra de cenário (RN14).</summary>
public sealed record RegraDeCenario(
    string Id,
    string? Nome,
    string? Instituicao,
    string? Endpoint,
    string? Cliente,
    string? Recurso,
    EfeitoDaRegra Efeito,
    decimal FrequenciaPct,
    DateTimeOffset? Inicio,
    DateTimeOffset? Fim,
    long? Semente,
    bool Ligada,
    string? Pacote,
    DateTimeOffset CriadaEm,
    DateTimeOffset AtualizadaEm);

/// <summary>Pacote de cenário (RN19).</summary>
public sealed record PacoteDeCenario(string Nome, bool Ligado, DateTimeOffset CriadoEm);

/// <summary>O alcance de uma chamada real.</summary>
public sealed record ChamadaAvaliada(string Instituicao, string Endpoint, string? Cliente, string? Recurso);

/// <summary>Valores do perfil que valem nesta chamada (RN10, RN13).</summary>
public sealed record PerfilDaChamada(
    int TipicoMs,
    int VariacaoMs,
    decimal CaudaLentaPct,
    decimal TaxaDeErroPct,
    IReadOnlyDictionary<int, decimal> Pesos,
    int? JanelaDuracaoMin = null,
    int? JanelaIntervaloMin = null);

/// <summary>O que fazer com a chamada: esperar e, se houver, trocar a resposta.</summary>
public sealed record DecisaoDeComportamento(TimeSpan Espera, int? Status = null, string? Transporte = null);

/// <summary>Grava regras e pacotes.</summary>
public interface IRepositorioDeCenarios
{
    Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento);

    Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento);

    Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento);

    Task ApagarRegra(string id, CancellationToken cancelamento);

    Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento);

    Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento);

    Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento);

    Task ApagarPacote(string nome, CancellationToken cancelamento);
}

/// <summary>O que o módulo precisa do ambiente; o host implementa (ADR-007).</summary>
public interface IAmbienteDosCenarios
{
    IReadOnlySet<int> StatusDeclarados { get; }

    bool OperacaoExiste(string operationId);

    /// <summary>Próxima posição da sequência; zero sem contadores (RN30, RN54).</summary>
    Task<long> ProximaPosicao(string chave, CancellationToken cancelamento);

    Task RegistrarHistorico(string alvo, string? autor, string acao, string? antes, string? depois, CancellationToken cancelamento);
}

/// <summary>Avaliação de regras e perfil na chamada.</summary>
public interface ICenarios
{
    Task<DecisaoDeComportamento> Decidir(ChamadaAvaliada chamada, PerfilDaChamada perfil, DateTimeOffset instante, IReadOnlySet<int> declarados, CancellationToken cancelamento);

    Task<string?> EstadoSobreposto(ChamadaAvaliada chamada, DateTimeOffset instante, CancellationToken cancelamento);

    /// <summary>Número em [0, 1).</summary>
    double Sorteio();
}

/// <summary>Formato JSON da administração e da gravação.</summary>
public static class JsonDosCenarios
{
    public static JsonSerializerOptions Opcoes { get; } = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
