using System.Text.Json;
using System.Text.RegularExpressions;
using Simulador.Catalogo.Contratos;

namespace Simulador.Catalogo.Dominio;

/// <summary>Valida o <c>specs.lock.json</c> conforme <c>contracts/specs-lock.schema.json</c>.</summary>
internal static partial class LeitorDeTrava
{
    public static TravaDasSpecs Interpretar(string json)
    {
        JsonElement raiz;
        try
        {
            using var documento = JsonDocument.Parse(json);
            raiz = documento.RootElement.Clone();
        }
        catch (JsonException erro)
        {
            throw new TravaInvalidaException($"JSON inválido na trava: {erro.Message}", erro);
        }

        var repositorio = Texto(raiz, "repositorio");
        var commit = Texto(raiz, "commit");
        var catalogo = raiz.TryGetProperty("catalogo", out var c) && c.ValueKind == JsonValueKind.String
            ? c.GetString()!
            : TravaDasSpecs.CatalogoPadrao;

        if (!PadraoRepositorio().IsMatch(repositorio))
        {
            throw new TravaInvalidaException($"repositorio inválido: '{repositorio}'. Use dono/nome.");
        }

        if (!PadraoCommit().IsMatch(commit))
        {
            throw new TravaInvalidaException($"commit inválido: '{commit}'. Use o SHA de 40 caracteres minúsculos.");
        }

        if (!raiz.TryGetProperty("contratos", out var lista) || lista.ValueKind != JsonValueKind.Array || lista.GetArrayLength() == 0)
        {
            throw new TravaInvalidaException("contratos: a trava precisa de pelo menos um contrato.");
        }

        var contratos = new List<ContratoTravado>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in lista.EnumerateArray())
        {
            var contrato = new ContratoTravado(Texto(item, "id"), Texto(item, "caminhoLocal"), Texto(item, "sha256"));
            if (contrato.Id.Length == 0 || contrato.CaminhoLocal.Length == 0)
            {
                throw new TravaInvalidaException("contrato sem id ou caminhoLocal.");
            }

            if (!PadraoSha256().IsMatch(contrato.Sha256))
            {
                throw new TravaInvalidaException($"sha256 inválido no contrato {contrato.Id}.");
            }

            if (!ids.Add(contrato.Id))
            {
                throw new TravaInvalidaException($"contrato repetido: {contrato.Id}.");
            }

            contratos.Add(contrato);
        }

        return new TravaDasSpecs(repositorio, commit, catalogo, contratos);
    }

    private static string Texto(JsonElement objeto, string campo) =>
        objeto.ValueKind == JsonValueKind.Object && objeto.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()!
            : throw new TravaInvalidaException($"{campo}: campo obrigatório ausente.");

    [GeneratedRegex("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")]
    private static partial Regex PadraoRepositorio();

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex PadraoCommit();

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex PadraoSha256();
}
