using System.Text.Json;
using Simulador.Catalogo.Contratos;

namespace Simulador.Catalogo.Dominio;

/// <summary>
/// Lê e valida <c>config/tabela-de-endpoints.json</c> (contracts/tabela-de-endpoints.schema.json).
/// </summary>
internal sealed class TabelaDeEndpoints
{
    private static readonly int[] SlasValidos = [1500, 2000, 4000];

    private TabelaDeEndpoints(IReadOnlyList<ClasseDoEndpoint> classes, IReadOnlyList<(string, string, string)> excluidas)
    {
        Classes = classes;
        Excluidas = excluidas;
    }

    public IReadOnlyList<ClasseDoEndpoint> Classes { get; }

    /// <summary>Operações registradas em docs/ como fora da tabela: não são publicadas (FR-015).</summary>
    public IReadOnlyList<(string Contrato, string Metodo, string Caminho)> Excluidas { get; }

    public static TabelaDeEndpoints Ler(string arquivo) =>
        File.Exists(arquivo)
            ? Interpretar(File.ReadAllText(arquivo))
            : throw new TabelaInvalidaException($"Arquivo da tabela de endpoints não encontrado: {arquivo}");

    public static TabelaDeEndpoints Interpretar(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            var raiz = documento.RootElement;
            Exigir(raiz.GetProperty("timeoutSegundos").GetInt32() == 15, "timeoutSegundos deve ser 15.");
            Exigir(raiz.GetProperty("tps").GetInt32() == 300, "tps deve ser 300.");

            var classes = raiz.GetProperty("endpoints").EnumerateArray().Select(LerClasse).ToList();
            var excluidas = raiz.TryGetProperty("excluidas", out var lista)
                ? lista.EnumerateArray().Select(e => (Texto(e, "contrato"), Texto(e, "metodo"), Texto(e, "caminho"))).ToList()
                : [];
            return new TabelaDeEndpoints(classes, excluidas);
        }
        catch (Exception erro) when (erro is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new TabelaInvalidaException($"Tabela de endpoints inválida: {erro.Message}", erro);
        }
    }

    private static ClasseDoEndpoint LerClasse(JsonElement item)
    {
        var contrato = Texto(item, "contrato");
        var metodo = Texto(item, "metodo");
        var caminho = Texto(item, "caminho");
        var onde = $"{contrato} {metodo} {caminho}";

        var frequencia = Texto(item, "frequencia") switch
        {
            "alta" => Frequencia.Alta,
            "media-alta" => Frequencia.MediaAlta,
            "media" => Frequencia.Media,
            "baixa" => Frequencia.Baixa,
            var outra => throw new TabelaInvalidaException($"{onde}: frequencia inválida '{outra}'."),
        };

        var sla = item.GetProperty("slaMs").GetInt32();
        Exigir(SlasValidos.Contains(sla), $"{onde}: slaMs inválido {sla}.");

        var tpm = item.GetProperty("tpm");
        var faixaQca = tpm.ValueKind == JsonValueKind.String;
        Exigir(!faixaQca || tpm.GetString() == "faixa-qca", $"{onde}: tpm inválido.");
        int? tpmFixo = tpm.ValueKind == JsonValueKind.Number ? tpm.GetInt32() : null;
        Exigir(tpmFixo is null or >= 1, $"{onde}: tpm deve ser maior que zero.");

        var limite = item.GetProperty("limiteMensal");
        Exigir(limite.ValueKind is JsonValueKind.Number or JsonValueKind.Null, $"{onde}: limiteMensal inválido.");
        int? limiteMensal = limite.ValueKind == JsonValueKind.Number ? limite.GetInt32() : null;
        Exigir(limiteMensal is null or >= 1, $"{onde}: limiteMensal deve ser maior que zero.");

        return new ClasseDoEndpoint(contrato, Texto(item, "secao"), metodo, caminho, frequencia, sla, tpmFixo, faixaQca, limiteMensal);
    }

    private static string Texto(JsonElement item, string campo) => item.GetProperty(campo).GetString()!;

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new TabelaInvalidaException(mensagem);
        }
    }
}
