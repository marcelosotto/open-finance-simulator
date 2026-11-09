using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Simulador.Consentimentos.Contratos;

/// <summary>Quem chama as APIs, com credenciais de teste (RN120, RN123).</summary>
public sealed record Receptora(string OrganisationId, string Nome, string Usuario, string Senha, DateTimeOffset CriadaEm);

/// <summary>Grava as receptoras.</summary>
public interface IRepositorioDeReceptoras
{
    Task Preparar(CancellationToken cancelamento);

    /// <summary>Falso quando o <c>organisationId</c> já existe.</summary>
    Task<bool> Inserir(Receptora receptora, CancellationToken cancelamento);

    Task<Receptora?> Obter(string organisationId, CancellationToken cancelamento);

    Task<Receptora?> ObterPorUsuario(string usuario, CancellationToken cancelamento);

    Task<IReadOnlyList<Receptora>> Listar(CancellationToken cancelamento);
}

/// <summary>O que o portão precisa saber da operação chamada.</summary>
/// <param name="Contrato">Id do contrato (ex.: <c>accounts</c>).</param>
/// <param name="Escopos">Escopos OAuth declarados na spec.</param>
/// <param name="Permissoes">Permissões exigidas pela spec; vazia usa a família do contrato.</param>
public sealed record OperacaoProtegida(string Contrato, IReadOnlyList<string> Escopos, IReadOnlyList<string> Permissoes);

/// <summary>Portão de cabeçalhos e segurança (RN38, RN39, RN88, RN122).</summary>
public interface IPortaoDeSeguranca
{
    Task<IResult?> Avaliar(HttpContext contexto, OperacaoProtegida operacao);
}

/// <summary>Dados de uma instituição usados aqui.</summary>
public sealed record DadosDoBanco(bool Ativo, IReadOnlyList<string> Produtos, int PreparacaoSeg);

/// <summary>Renovação de um consentimento.</summary>
public sealed record DadosDaExtensao(
    DateTimeOffset? NovaValidade,
    DateTimeOffset? ValidadeAnterior,
    string Documento,
    string Rel,
    DateTimeOffset SolicitadaEm,
    string? Ip,
    string? UserAgent);

/// <summary>Consentimento como este módulo o enxerga.</summary>
public sealed record DadosDoConsentimento(
    string ConsentId,
    string Cliente,
    string Banco,
    string Estado,
    IReadOnlyList<string> Permissoes,
    IReadOnlyList<string> Recursos,
    DateTimeOffset CriadoEm,
    string Marca,
    string Natureza,
    string? LoteId,
    DateTimeOffset? ExpiraEm,
    string? Receptora,
    string? RejeitadoPor,
    string? Razao,
    DateTimeOffset? AtualizadoEm,
    string? UsuarioLogado,
    int AprovacoesPendentes,
    IReadOnlyList<DadosDaExtensao> Extensoes,
    int Versao = 0);

/// <summary>Recurso como este módulo o enxerga.</summary>
public sealed record DadosDoRecurso(string ResourceId, string Cliente, string Banco, string Tipo, string Estado, string Natureza, int Versao = 0);

/// <summary>Cliente simulado com o que tem.</summary>
public sealed record DadosDoCliente(
    string Documento,
    string Tipo,
    string Natureza,
    IReadOnlyList<DadosDoRecurso> Recursos,
    IReadOnlyList<DadosDoConsentimento> Consentimentos);

/// <summary>O que o módulo precisa dos outros; o host implementa (ADR-007).</summary>
public interface IAmbienteDosConsentimentos
{
    Task<DadosDoBanco?> Banco(string banco, CancellationToken cancelamento);

    Task<DadosDoCliente?> Cliente(string documento, CancellationToken cancelamento);

    Task<DadosDoConsentimento?> Consentimento(string consentId, CancellationToken cancelamento);

    Task<DadosDoRecurso?> Recurso(string resourceId, CancellationToken cancelamento);

    Task<string?> NovoConsentId(CancellationToken cancelamento);

    Task Inserir(DadosDoConsentimento consentimento, CancellationToken cancelamento);

    /// <summary>Falso se outra chamada gravou desde a leitura (RN82).</summary>
    Task<bool> Atualizar(DadosDoConsentimento consentimento, CancellationToken cancelamento);

    /// <summary>Falso se outra chamada gravou desde a leitura (RN108).</summary>
    Task<bool> MudarEstado(DadosDoRecurso recurso, string estado, CancellationToken cancelamento);

    Task<DadosDoRecurso?> AdicionarProduto(string documento, string banco, string produto, string estado, CancellationToken cancelamento);

    string Marca { get; }

    string NovoOrganisationId();

    /// <summary>Declara a natureza de um item da resposta (RN61, RN65).</summary>
    void RegistrarNatureza(HttpContext contexto, string natureza);

    Task RegistrarHistorico(string? autor, string acao, string campo, string? antes, string? depois, CancellationToken cancelamento);
}

/// <summary>Formato JSON da administração e da gravação.</summary>
public static class JsonDosConsentimentos
{
    public static JsonSerializerOptions Opcoes { get; } = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
