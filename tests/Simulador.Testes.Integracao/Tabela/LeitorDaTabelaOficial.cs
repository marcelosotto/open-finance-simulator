using System.Globalization;

namespace Simulador.Testes.Integracao.Tabela;

/// <summary>Uma linha da tabela oficial, já convertida.</summary>
internal sealed record LinhaDaTabela(
    string Secao,
    string Metodo,
    string Caminho,
    string Frequencia,
    int SlaMs,
    int? Tpm,
    bool FaixaQca,
    int? LimiteMensal);

/// <summary>Operação registrada em "Operações fora da tabela".</summary>
internal sealed record ExclusaoDaTabela(string Contrato, string Metodo, string Caminho, string Motivo);

internal sealed record TabelaOficial(IReadOnlyList<LinhaDaTabela> Linhas, IReadOnlyList<ExclusaoDaTabela> Exclusoes);

/// <summary>
/// Lê <c>docs/01-requisitos/tabela-de-endpoints.md</c>, a fonte da verdade (princípio I). Código só de teste.
/// </summary>
internal static class LeitorDaTabelaOficial
{
    private const string SecaoDeExclusoes = "Operações fora da tabela";

    public static TabelaOficial Ler(string arquivo) => Interpretar(File.ReadAllText(arquivo));

    public static TabelaOficial Interpretar(string markdown)
    {
        var linhas = new List<LinhaDaTabela>();
        var exclusoes = new List<ExclusaoDaTabela>();
        var secao = string.Empty;

        foreach (var bruta in markdown.Split('\n'))
        {
            var linha = bruta.Trim();
            if (linha.StartsWith('#'))
            {
                secao = linha.TrimStart('#').Trim();
                continue;
            }

            if (!linha.StartsWith('|') || linha.StartsWith("|---", StringComparison.Ordinal))
            {
                continue;
            }

            var celulas = linha.Trim('|').Split('|').Select(c => c.Trim().Trim('`')).ToArray();
            if (secao == SecaoDeExclusoes)
            {
                if (celulas[0] != "Contrato")
                {
                    exclusoes.Add(new ExclusaoDaTabela(celulas[0], celulas[1], celulas[2], celulas[3]));
                }

                continue;
            }

            if (celulas[0] is "API" or "Endpoint")
            {
                continue;
            }

            var comApi = celulas.Length == 4;
            var secaoDaLinha = comApi ? celulas[0] : secao;
            var endpoint = comApi ? celulas[1] : celulas[0];
            var frequencia = comApi ? celulas[2] : celulas[1];
            var sla = comApi ? celulas[3] : celulas[2];
            var tpm = comApi ? null : celulas[3];
            var limite = comApi ? null : celulas[4];
            var espaco = endpoint.IndexOf(' ', StringComparison.Ordinal);

            linhas.Add(new LinhaDaTabela(
                secaoDaLinha,
                endpoint[..espaco],
                endpoint[(espaco + 1)..],
                Frequencia(frequencia),
                Numero(sla)!.Value,
                tpm == "Faixa QCA" ? null : Numero(tpm),
                tpm == "Faixa QCA",
                Numero(limite)));
        }

        return new TabelaOficial(linhas, exclusoes);
    }

    private static string Frequencia(string valor) => valor switch
    {
        "Alta" => "alta",
        "Média-alta" => "media-alta",
        "Média" => "media",
        "Baixa" => "baixa",
        _ => throw new FormatException($"Frequência desconhecida: {valor}"),
    };

    private static int? Numero(string? valor) =>
        valor is null ? null : int.Parse(valor.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);
}
