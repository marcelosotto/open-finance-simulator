using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Simulador.Dados.Aplicacao;

/// <summary>Valores da massa de um item; a chave é <c>campo</c> ou <c>pai.campo</c>.</summary>
internal sealed record Preenchimento(IReadOnlyDictionary<string, string> Valores, string BaseDosIds, uint Semente);

/// <summary>
/// Monta um item a partir do schema travado (princípio VII, Q5): emite os campos obrigatórios e
/// os que têm valor da massa. Nada é sorteado: o mesmo item dá sempre o mesmo resultado.
/// </summary>
internal static class GeradorPorSchema
{
    private const int MaximoDeProfundidade = 12;
    private const int MaximoDoId = 100;
    private static readonly ConcurrentDictionary<string, Regex> Padroes = new(StringComparer.Ordinal);

    public static JsonNode? Gerar(JsonNode? schema, Preenchimento item) => No(schema as JsonObject, string.Empty, null, item);

    private static string TipoDe(JsonObject schema) =>
        schema["type"]?.GetValue<string>() ?? (schema["properties"] is not null ? "object" : "string");

    private static JsonNode? No(JsonObject? schema, string nome, string? pai, Preenchimento item)
    {
        if (schema is null)
        {
            return null;
        }

        return TipoDe(schema) switch
        {
            "object" => Objeto(schema, nome, item),
            "array" => Lista(schema, nome, pai, item),
            "integer" or "number" => Numero(schema, nome, item),
            "boolean" => JsonValue.Create(schema["example"]?.GetValue<bool>() ?? false),
            _ => JsonValue.Create(Texto(schema, nome, pai, item)),
        };
    }

    private static JsonObject Objeto(JsonObject schema, string nome, Preenchimento item)
    {
        var obrigatorios = (schema["required"]?.AsArray() ?? []).Select(n => n!.GetValue<string>()).ToHashSet(StringComparer.Ordinal);
        var saida = new JsonObject();
        foreach (var (campo, filho) in schema["properties"]?.AsObject() ?? [])
        {
            if (obrigatorios.Contains(campo) || TemValor(filho as JsonObject, campo, nome, item, 0))
            {
                saida[campo] = No(filho as JsonObject, campo, nome, item);
            }
        }

        return saida;
    }

    private static bool TemValor(JsonObject? schema, string nome, string pai, Preenchimento item, int profundidade)
    {
        if (item.Valores.ContainsKey(nome) || item.Valores.ContainsKey($"{pai}.{nome}"))
        {
            return true;
        }

        return profundidade < MaximoDeProfundidade
            && (schema?["properties"]?.AsObject() ?? []).Any(p => TemValor(p.Value as JsonObject, p.Key, nome, item, profundidade + 1));
    }

    private static JsonArray Lista(JsonObject schema, string nome, string? pai, Preenchimento item)
    {
        var quantidade = Math.Max(schema["minItems"]?.GetValue<int>() ?? 1, 1);
        var saida = new JsonArray();
        for (var i = 0; i < quantidade; i++)
        {
            saida.Add(No(schema["items"] as JsonObject, nome, pai, item));
        }

        return saida;
    }

    private static JsonValue Numero(JsonObject schema, string nome, Preenchimento item)
    {
        var texto = item.Valores.GetValueOrDefault(nome) ?? (schema["example"] ?? schema["minimum"])?.ToString() ?? "1";
        var valor = decimal.Parse(texto, NumberStyles.Float, CultureInfo.InvariantCulture);
        return TipoDe(schema) == "integer" ? JsonValue.Create((long)valor) : JsonValue.Create(valor);
    }

    private static string Texto(JsonObject schema, string nome, string? pai, Preenchimento item)
    {
        foreach (var candidato in Candidatos(nome, pai, item))
        {
            if (Cabe(schema, candidato))
            {
                return candidato;
            }
        }

        if (schema["enum"]?.AsArray() is { Count: > 0 } valores)
        {
            return valores[0]!.GetValue<string>();
        }

        if (schema["example"]?.ToString() is { } exemplo && Cabe(schema, exemplo))
        {
            return exemplo;
        }

        return PorFormato(schema);
    }

    private static IEnumerable<string> Candidatos(string nome, string? pai, Preenchimento item)
    {
        if (item.Valores.TryGetValue($"{pai}.{nome}", out var especifico))
        {
            yield return especifico;
        }

        if (item.Valores.TryGetValue(nome, out var valor))
        {
            yield return valor;
        }

        if (nome.Contains("cnpj", StringComparison.OrdinalIgnoreCase) || nome.Contains("cpf", StringComparison.OrdinalIgnoreCase))
        {
            yield return Documentos.Cpf(item.Semente);
            yield return Documentos.Cnpj(item.Semente);
        }

        if (nome.EndsWith("Id", StringComparison.Ordinal))
        {
            var espaco = Math.Max(MaximoDoId - nome.Length - 1, 0);
            var prefixo = item.BaseDosIds.Length > espaco ? item.BaseDosIds[..espaco] : item.BaseDosIds;
            yield return $"{prefixo}-{nome}";
        }
    }

    private static string PorFormato(JsonObject schema)
    {
        var formato = schema["format"]?.GetValue<string>();
        return formato switch
        {
            "date" => "2026-01-15",
            "date-time" => "2026-01-15T12:00:00Z",
            _ => new string('A', Math.Max(schema["minLength"]?.GetValue<int>() ?? 1, 1)),
        };
    }

    private static bool Cabe(JsonObject schema, string texto)
    {
        if (schema["maxLength"]?.GetValue<int>() is { } maximo && texto.Length > maximo)
        {
            return false;
        }

        if (schema["enum"]?.AsArray() is { } valores && valores.All(v => v!.GetValue<string>() != texto))
        {
            return false;
        }

        return schema["pattern"]?.GetValue<string>() is not { } padrao
            || Padroes.GetOrAdd(padrao, p => new Regex(p, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))).IsMatch(texto);
    }
}

/// <summary>CPF e CNPJ com dígitos válidos; CPF sempre começa com 9 (RN57).</summary>
internal static class Documentos
{
    public static uint Semente(string texto)
    {
        var hash = 2166136261u;
        foreach (var letra in texto)
        {
            hash = unchecked((hash ^ letra) * 16777619u);
        }

        return hash;
    }

    public static string Cpf(uint semente)
    {
        var corpo = string.Create(CultureInfo.InvariantCulture, $"9{semente % 100_000_000:D8}");
        var primeiro = Digito(corpo, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var segundo = Digito(corpo + primeiro, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
        return $"{corpo}{primeiro}{segundo}";
    }

    public static string Cnpj(uint semente)
    {
        var corpo = string.Create(CultureInfo.InvariantCulture, $"{semente % 100_000_000:D8}0001");
        var primeiro = Digito(corpo, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var segundo = Digito(corpo + primeiro, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return $"{corpo}{primeiro}{segundo}";
    }

    private static int Digito(string numero, int[] pesos)
    {
        var resto = numero.Select((c, i) => (c - '0') * pesos[i]).Sum() % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
