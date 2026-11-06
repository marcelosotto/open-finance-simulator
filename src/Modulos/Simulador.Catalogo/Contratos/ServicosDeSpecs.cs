using System.Text.Json.Nodes;
using Simulador.Catalogo.Dominio;
using Simulador.Catalogo.Leitura;

namespace Simulador.Catalogo.Contratos;

/// <summary>Operações sobre as specs travadas usadas pelo host, pela ferramenta e pelos testes.</summary>
public static class ServicosDeSpecs
{
    /// <summary>Confere os arquivos de <paramref name="pasta"/> contra a trava e o catálogo do commit.</summary>
    public static IReadOnlyList<FalhaDeConferencia> Conferir(TravaDasSpecs trava, string pasta)
    {
        ArgumentNullException.ThrowIfNull(trava);
        return ConferenciaDeHash.Conferir(
            trava,
            relativo =>
            {
                var caminho = Path.Combine(pasta, relativo);
                return File.Exists(caminho) ? File.ReadAllBytes(caminho) : null;
            },
            () => Directory.Exists(pasta)
                ? Directory.EnumerateFiles(pasta, "*", SearchOption.AllDirectories)
                    .Select(arquivo => Path.GetRelativePath(pasta, arquivo).Replace('\\', '/'))
                : []);
    }

    /// <summary>Lê as operações de todos os contratos travados.</summary>
    public static IReadOnlyList<OperacaoDoCatalogo> LerOperacoes(TravaDasSpecs trava, string pasta)
    {
        ArgumentNullException.ThrowIfNull(trava);
        return [.. trava.Contratos.SelectMany(contrato =>
            LeitorDeSpecs.LerOperacoes(contrato.Id, LeitorDeSpecs.LerDocumento(Path.Combine(pasta, contrato.CaminhoLocal))))];
    }

    /// <summary>A spec de um contrato travado, convertida para JSON (OpenAPI 3.0).</summary>
    public static JsonObject LerDocumentoJson(TravaDasSpecs trava, string pasta, string contrato)
    {
        ArgumentNullException.ThrowIfNull(trava);
        var travado = trava.Contratos.SingleOrDefault(c => c.Id == contrato)
            ?? throw new ArgumentException($"Contrato não travado: {contrato}", nameof(contrato));
        return LeitorDeSpecs.LerDocumento(Path.Combine(pasta, travado.CaminhoLocal));
    }

    /// <summary>Operações novas, alteradas e removidas entre dois commits.</summary>
    public static DiferencaEntreVersoes Comparar(
        string commitAnterior,
        IReadOnlyList<OperacaoDoCatalogo> anteriores,
        string commitNovo,
        IReadOnlyList<OperacaoDoCatalogo> novas) =>
        ComparadorDeVersoes.Comparar(commitAnterior, anteriores, commitNovo, novas);

    /// <summary>Copia um nó do documento trocando os <c>$ref</c> locais pelos alvos.</summary>
    public static JsonNode? ResolverReferencias(JsonNode? no, JsonObject documento) => LeitorDeSpecs.Resolver(no, documento);
}
