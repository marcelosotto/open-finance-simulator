using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Simulador.Catalogo.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Host;

/// <summary>
/// Valida respostas contra o schema da spec travada (FR-023, RNF01). O schema OpenAPI 3.0 é
/// adaptado para JSON Schema: <c>nullable</c> vira tipo nulo, e palavras próprias do OpenAPI saem.
/// </summary>
public static class ValidadorDeContrato
{
    private static readonly string[] PalavrasDoOpenApi = ["nullable", "example", "discriminator", "xml", "externalDocs", "deprecated"];

    private static readonly Lazy<IReadOnlyList<OperacaoDoCatalogo>> Operacoes = new(() =>
        ServicosDeSpecs.LerOperacoes(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs()));

    /// <summary>Schema da resposta da operação, já resolvido.</summary>
    public static JsonNode SchemaDaResposta(string operationId, string codigo = "200") =>
        JsonNode.Parse(Operacoes.Value.Single(o => o.OperationId == operationId).Respostas[codigo])!;

    /// <summary>Erros de validação; lista vazia quando a resposta é válida.</summary>
    public static IReadOnlyList<string> Validar(string operationId, string corpo, string codigo = "200") =>
        Validar(SchemaDaResposta(operationId, codigo), corpo);

    public static IReadOnlyList<string> Validar(JsonNode schemaOpenApi, string corpo)
    {
        var schema = JsonSchema.FromText(Adaptar(schemaOpenApi)!.ToJsonString());
        using var instancia = JsonDocument.Parse(corpo);
        var resultado = schema.Evaluate(instancia.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        return resultado.IsValid
            ? []
            : [.. (resultado.Details ?? []).Where(d => d.Errors is not null)
                .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"))];
    }

    /// <summary>Converte um schema OpenAPI 3.0 em JSON Schema.</summary>
    public static JsonNode? Adaptar(JsonNode? no)
    {
        switch (no)
        {
            case JsonObject objeto:
                var copia = new JsonObject();
                foreach (var (chave, filho) in objeto)
                {
                    if (!PalavrasDoOpenApi.Contains(chave) && !chave.StartsWith("x-", StringComparison.Ordinal))
                    {
                        copia[chave] = Adaptar(filho);
                    }
                }

                if (objeto["nullable"]?.GetValue<bool>() == true && copia["type"] is JsonValue tipo)
                {
                    copia["type"] = new JsonArray(tipo.GetValue<string>(), "null");
                    if (copia["enum"] is JsonArray valores)
                    {
                        valores.Add(null);
                    }
                }

                return copia;
            case JsonArray lista:
                return new JsonArray([.. lista.Select(Adaptar)]);
            default:
                return no?.DeepClone();
        }
    }
}
