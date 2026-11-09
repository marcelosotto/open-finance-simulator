using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulador.Comum.Erros;

/// <summary>Erro no formato oficial <c>ResponseError</c> das specs de dados.</summary>
public sealed record ErroOficial(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("detail")] string Detail);

/// <summary>Bloco <c>meta</c> do erro oficial.</summary>
public sealed record MetaDeErro([property: JsonPropertyName("requestDateTime")] string RequestDateTime);

/// <summary>Corpo <c>ResponseError</c>.</summary>
public sealed record RespostaDeErro(
    [property: JsonPropertyName("errors")] IReadOnlyList<ErroOficial> Errors,
    [property: JsonPropertyName("meta")] MetaDeErro Meta);

/// <summary>Monta respostas de erro com a hora do relógio injetado.</summary>
public static class EnvelopeDeErro
{
    public static JsonSerializerOptions OpcoesJson { get; } = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static RespostaDeErro Criar(string code, string title, string detail, TimeProvider relogio)
    {
        ArgumentNullException.ThrowIfNull(relogio);
        var agora = relogio.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        return new RespostaDeErro([new ErroOficial(code, title, detail)], new MetaDeErro(agora));
    }
}
