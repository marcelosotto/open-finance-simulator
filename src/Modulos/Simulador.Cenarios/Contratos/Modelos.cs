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
    string? Estado = null,
    string? Defeito = null,
    string? Campo = null,
    int? Quantidade = null);

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

/// <summary>A regra que venceria em cada grupo, na hora do banco (UI06).</summary>
public sealed record VencedorasDaChamada(DateTimeOffset Instante, RegraDeCenario? Comportamento, RegraDeCenario? Estado, RegraDeCenario? Defeito);

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

/// <summary>O que fazer com a chamada: esperar e, se houver, trocar a resposta ou aplicar um defeito (RN111).</summary>
public sealed record DecisaoDeComportamento(TimeSpan Espera, int? Status = null, string? Transporte = null, DefeitoDaChamada? Defeito = null);

/// <summary>Defeito de dado sorteado para a resposta normal (RN110, RN117).</summary>
public sealed record DefeitoDaChamada(string Nome, string? Campo = null, int? Quantidade = null);

/// <summary>Anomalia de protocolo de um banco (RN113); ausente = desligada (RN114).</summary>
public sealed record AnomaliaDaInstituicao(string Instituicao, string Nome, bool Ligada, DateTimeOffset AtualizadaEm);

/// <summary>Estado de uma anomalia num banco, para a administração.</summary>
public sealed record EstadoDaAnomalia(string Nome, bool Ligada);

/// <summary>Item de um catálogo de defeitos ou anomalias.</summary>
public sealed record ItemDoCatalogo(string Nome, string Descricao);

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

    Task<IReadOnlyList<AnomaliaDaInstituicao>> ListarAnomalias(CancellationToken cancelamento);

    Task SalvarAnomalia(AnomaliaDaInstituicao anomalia, CancellationToken cancelamento);

    /// <summary>Apaga as anomalias do banco, quando o banco é apagado (RN113).</summary>
    Task ApagarAnomalias(string instituicao, CancellationToken cancelamento);
}

/// <summary>O que o módulo precisa do ambiente; o host implementa (ADR-007).</summary>
public interface IAmbienteDosCenarios
{
    IReadOnlySet<int> StatusDeclarados { get; }

    bool OperacaoExiste(string operationId);

    /// <summary>Status declarados pela operação, como na chamada; nulo se a operação não existe.</summary>
    IReadOnlySet<int>? StatusDeclaradosDe(string operationId);

    /// <summary>A hora do banco (RN73); nulo se o banco não existe.</summary>
    Task<DateTimeOffset?> HoraDoBanco(string instituicao, CancellationToken cancelamento);

    Task<bool> InstituicaoExiste(string instituicao, CancellationToken cancelamento);

    /// <summary>Próxima posição da sequência; zero sem contadores (RN30, RN54).</summary>
    Task<long> ProximaPosicao(string chave, CancellationToken cancelamento);

    Task RegistrarHistorico(string alvo, string? autor, string acao, string? antes, string? depois, CancellationToken cancelamento);
}

/// <summary>Avaliação de regras e perfil na chamada.</summary>
public interface ICenarios
{
    Task<DecisaoDeComportamento> Decidir(ChamadaAvaliada chamada, PerfilDaChamada perfil, DateTimeOffset instante, IReadOnlySet<int> declarados, CancellationToken cancelamento);

    Task<string?> EstadoSobreposto(ChamadaAvaliada chamada, DateTimeOffset instante, CancellationToken cancelamento);

    /// <summary>Anomalias ligadas no banco, pelo cache de até 5 s (RN113, RNF12).</summary>
    Task<IReadOnlySet<string>> AnomaliasLigadas(string instituicao, CancellationToken cancelamento);

    /// <summary>Esquece as anomalias do banco apagado: o banco recriado com o mesmo nome nasce sem elas (RN113).</summary>
    Task ApagarAnomalias(string instituicao, CancellationToken cancelamento);

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
