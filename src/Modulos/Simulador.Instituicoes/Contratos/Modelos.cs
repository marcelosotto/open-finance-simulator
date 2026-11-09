using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Simulador.Instituicoes.Dominio;

namespace Simulador.Instituicoes.Contratos;

/// <summary>Latência de uma classe de frequência (RN10).</summary>
/// <param name="TipicoMs">p50.</param>
/// <param name="VariacaoMs">p95 menos p50.</param>
/// <param name="CaudaLentaPct">Porcentagem de chamadas acima de 15 s.</param>
public sealed record Latencia(int TipicoMs, int VariacaoMs, decimal CaudaLentaPct);

/// <summary>Janela de indisponibilidade recorrente de um perfil.</summary>
public sealed record Janela(int DuracaoMin, int IntervaloMin);

/// <summary>Como a instituição responde (UC02).</summary>
public sealed record Comportamento(
    IReadOnlyDictionary<string, Latencia> LatenciaPorClasse,
    decimal TaxaDeErroPct,
    IReadOnlyDictionary<int, decimal> PesosPorStatus,
    decimal RecursoIndisponivelPct,
    Janela? Janela = null);

/// <summary>TPM por classe. Sem <paramref name="Alta"/>, vale a faixa de QCA (RN21).</summary>
public sealed record Tpm(int MediaAlta, int Media, int Baixa, int? Alta = null);

/// <summary>Cota mensal por classe (RN26).</summary>
public sealed record LimiteMensal(int Alta, int SaldosELimites, int MediaAlta, int Media, int Baixa);

/// <summary>Limites e paginação da instituição (UC03).</summary>
public sealed record Limites(
    long Qca,
    IReadOnlyDictionary<string, long> QcaPorReceptora,
    Tpm Tpm,
    bool LimiteOperacionalLigado,
    LimiteMensal LimiteMensal,
    int CapacidadeTps,
    int PageSizeMaximo,
    int ValidadeDaChaveMin,
    bool RetryAfterLigado,
    int RetryAfterSeg,
    int PreparacaoDeRecursosSeg)
{
    /// <summary>TPM da alta frequência para uma receptora (RN21).</summary>
    public int TpmDaAlta(string receptora) =>
        Tpm.Alta ?? ValoresOficiais.TpmDaAlta(QcaPorReceptora.TryGetValue(receptora, out var qca) ? qca : Qca);
}

/// <summary>Modelo de comportamento e limites (RN04).</summary>
public sealed record PerfilDeComportamento(string Nome, Comportamento Comportamento, Limites Limites);

/// <summary>Situações possíveis de uma instituição (RN06).</summary>
public static class Situacoes
{
    public const string Ativa = "ativa";
    public const string Inativa = "inativa";
}

/// <summary>Instituição simulada (RN01).</summary>
public sealed record Instituicao(
    string Endereco,
    string Nome,
    string OrganisationId,
    string Perfil,
    string Marca,
    string Publico,
    IReadOnlyList<string> Produtos,
    string Situacao,
    bool Pronta,
    Comportamento Comportamento,
    Limites Limites)
{
    /// <summary>Se a instituição oferece o produto (RN08).</summary>
    public bool Oferece(string produto) => Produtos.Contains(produto);
}

/// <summary>Um campo alterado.</summary>
public sealed record Mudanca(string Campo, string? Antes, string? Depois);

/// <summary>Linha do histórico de configuração (RN05, RN71).</summary>
public sealed record EntradaDeHistorico(
    string Instituicao,
    DateTimeOffset Data,
    string Autor,
    string Acao,
    IReadOnlyList<Mudanca> Mudancas);

/// <summary>Grava instituições e histórico.</summary>
public interface IRepositorioDeInstituicoes
{
    Task<Instituicao?> Obter(string endereco, CancellationToken cancelamento);

    Task<IReadOnlyList<Instituicao>> Listar(CancellationToken cancelamento);

    /// <summary>Falso quando o endereço já existe.</summary>
    Task<bool> Inserir(Instituicao instituicao, CancellationToken cancelamento);

    Task Substituir(Instituicao instituicao, CancellationToken cancelamento);

    Task Apagar(string endereco, CancellationToken cancelamento);

    Task AcrescentarHistorico(EntradaDeHistorico entrada, CancellationToken cancelamento);

    /// <summary>Do mais novo ao mais antigo.</summary>
    Task<IReadOnlyList<EntradaDeHistorico>> LerHistorico(string endereco, CancellationToken cancelamento);
}

/// <summary>Leitura da configuração em vigor, para as outras specs (RN05, RN08).</summary>
public interface IConsultaDeInstituicoes
{
    Task<Instituicao?> Obter(string endereco, CancellationToken cancelamento);
}

/// <summary>Diz se uma instituição tem clientes; a spec 003 implementa (RN06).</summary>
public interface IConsultaDeClientes
{
    Task<bool> TemClientes(string endereco, CancellationToken cancelamento);
}

/// <summary>O que o módulo precisa do ambiente; o host implementa (ADR-007).</summary>
public interface IAmbienteDasInstituicoes
{
    string Marca { get; }

    IReadOnlySet<int> StatusDeclarados { get; }

    string NovoOrganisationId();
}

/// <summary>Formato JSON da API de administração e da gravação.</summary>
public static class JsonDasInstituicoes
{
    public static JsonSerializerOptions Opcoes { get; } = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
}
