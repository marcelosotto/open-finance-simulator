using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Simulador.Catalogo.Contratos;

namespace Simulador.Catalogo.Leitura;

/// <summary>
/// Lê uma spec OpenAPI travada. O YAML é interpretado pelo Microsoft.OpenApi e convertido para
/// JSON; operações, parâmetros e schemas saem desse JSON, com os <c>$ref</c> resolvidos.
/// </summary>
internal static class LeitorDeSpecs
{
    private static readonly string[] Metodos = ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

    public static JsonObject LerDocumento(string arquivo)
    {
        var configuracao = new OpenApiReaderSettings();
        configuracao.AddYamlReader();
        var resultado = OpenApiDocument.Parse(File.ReadAllText(arquivo), "yaml", configuracao);
        var documento = resultado.Document
            ?? throw new InvalidOperationException($"Spec ilegível: {arquivo}");
        var json = documento.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0).GetAwaiter().GetResult();
        return JsonNode.Parse(json)!.AsObject();
    }

    public static IEnumerable<OperacaoDoCatalogo> LerOperacoes(string contrato, JsonObject documento)
    {
        var versao = documento["info"]?["version"]?.GetValue<string>() ?? string.Empty;
        var url = documento["servers"]?[0]?["url"]?.GetValue<string>()
            ?? throw new InvalidOperationException($"Spec {contrato} sem servers[0].url.");
        var caminhoBase = new Uri(url).AbsolutePath.TrimEnd('/');

        foreach (var (caminho, item) in documento["paths"]?.AsObject() ?? [])
        {
            var itemResolvido = Resolver(item, documento)!.AsObject();
            var parametrosDoCaminho = itemResolvido["parameters"]?.AsArray() ?? [];
            foreach (var metodo in Metodos)
            {
                if (itemResolvido[metodo] is not JsonObject operacao)
                {
                    continue;
                }

                var parametros = parametrosDoCaminho.Concat(operacao["parameters"]?.AsArray() ?? [])
                    .Select(p => Resolver(p, documento)!)
                    .Select(p => new ParametroDaOperacao(
                        p["name"]!.GetValue<string>(),
                        p["in"]!.GetValue<string>(),
                        p["required"]?.GetValue<bool>() ?? false,
                        p["schema"]?.ToJsonString() ?? "{}"))
                    .ToList();

                yield return new OperacaoDoCatalogo(
                    contrato,
                    versao,
                    caminhoBase,
                    metodo.ToUpperInvariant(),
                    caminho,
                    operacao["operationId"]?.GetValue<string>() ?? $"{contrato}:{metodo}:{caminho}",
                    parametros,
                    [.. parametros.Where(p => p is { Local: "header", Obrigatorio: true }).Select(p => p.Nome)],
                    LerRespostas(operacao["responses"]?.AsObject()));
            }
        }
    }

    private static Dictionary<string, string> LerRespostas(JsonObject? respostas)
    {
        var resultado = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (codigo, resposta) in respostas ?? [])
        {
            var conteudo = resposta?["content"]?.AsObject();
            var midia = conteudo?["application/json"] ?? conteudo?.FirstOrDefault().Value;
            resultado[codigo] = midia?["schema"]?.ToJsonString() ?? "{}";
        }

        return resultado;
    }

    /// <summary>Copia o nó trocando cada <c>$ref</c> local pelo alvo. Ciclos ficam como <c>$ref</c>.</summary>
    internal static JsonNode? Resolver(JsonNode? no, JsonObject documento, HashSet<string>? pilha = null)
    {
        pilha ??= new HashSet<string>(StringComparer.Ordinal);
        switch (no)
        {
            case JsonObject objeto when objeto["$ref"] is JsonValue valor && valor.GetValue<string>() is { } referencia
                && referencia.StartsWith("#/", StringComparison.Ordinal):
                if (!pilha.Add(referencia))
                {
                    return objeto.DeepClone();
                }

                var alvo = Navegar(documento, referencia);
                var resolvido = Resolver(alvo, documento, pilha);
                pilha.Remove(referencia);
                return resolvido;
            case JsonObject objeto:
                var copia = new JsonObject();
                foreach (var (chave, filho) in objeto)
                {
                    copia[chave] = Resolver(filho, documento, pilha);
                }

                return copia;
            case JsonArray lista:
                return new JsonArray([.. lista.Select(filho => Resolver(filho, documento, pilha))]);
            default:
                return no?.DeepClone();
        }
    }

    private static JsonNode? Navegar(JsonObject documento, string referencia)
    {
        JsonNode? atual = documento;
        foreach (var parte in referencia[2..].Split('/'))
        {
            atual = atual?[parte.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal)];
        }

        return atual ?? throw new InvalidOperationException($"Referência não encontrada: {referencia}");
    }
}
