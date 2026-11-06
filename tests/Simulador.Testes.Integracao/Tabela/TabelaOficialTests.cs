using System.Text.Json;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Tabela;

/// <summary>FR-014: o arquivo de classes é igual à tabela oficial em docs/.</summary>
public class TabelaOficialTests
{
    private static readonly TabelaOficial Oficial =
        LeitorDaTabelaOficial.Ler(Repositorio.Caminho("docs", "01-requisitos", "tabela-de-endpoints.md"));

    private static readonly JsonElement Arquivo =
        JsonDocument.Parse(File.ReadAllText(Repositorio.Caminho("config", "tabela-de-endpoints.json"))).RootElement;

    [Fact]
    public void Valores_comuns_sao_os_oficiais()
    {
        Assert.Equal(15, Arquivo.GetProperty("timeoutSegundos").GetInt32());
        Assert.Equal(300, Arquivo.GetProperty("tps").GetInt32());
    }

    [Fact]
    public void Cada_linha_da_tabela_oficial_esta_igual_no_arquivo()
    {
        var doArquivo = LinhasDoArquivo().ToDictionary(l => (l.Secao, l.Metodo, l.Caminho));
        var divergencias = new List<string>();

        foreach (var oficial in Oficial.Linhas)
        {
            if (!doArquivo.TryGetValue((oficial.Secao, oficial.Metodo, oficial.Caminho), out var linha))
            {
                divergencias.Add($"{oficial.Secao} {oficial.Metodo} {oficial.Caminho}: falta no arquivo");
                continue;
            }

            Comparar(divergencias, oficial, "frequencia", oficial.Frequencia, linha.Frequencia);
            Comparar(divergencias, oficial, "slaMs", oficial.SlaMs, linha.SlaMs);
            Comparar(divergencias, oficial, "tpm", oficial.Tpm, linha.Tpm);
            Comparar(divergencias, oficial, "faixa-qca", oficial.FaixaQca, linha.FaixaQca);
            Comparar(divergencias, oficial, "limiteMensal", oficial.LimiteMensal, linha.LimiteMensal);
        }

        Assert.True(divergencias.Count == 0, string.Join(Environment.NewLine, divergencias));
    }

    [Fact]
    public void Arquivo_nao_tem_linha_fora_da_tabela_oficial()
    {
        var oficiais = Oficial.Linhas.Select(l => (l.Secao, l.Metodo, l.Caminho)).ToHashSet();

        var sobrando = LinhasDoArquivo().Where(l => !oficiais.Contains((l.Secao, l.Metodo, l.Caminho)))
            .Select(l => $"{l.Secao} {l.Metodo} {l.Caminho}").ToList();

        Assert.Empty(sobrando);
    }

    [Fact]
    public void Exclusoes_do_arquivo_sao_as_de_docs()
    {
        var doArquivo = Arquivo.GetProperty("excluidas").EnumerateArray()
            .Select(e => (e.GetProperty("contrato").GetString(), e.GetProperty("metodo").GetString(), e.GetProperty("caminho").GetString()))
            .ToHashSet();

        Assert.Equal(Oficial.Exclusoes.Select(e => ((string?)e.Contrato, (string?)e.Metodo, (string?)e.Caminho)).ToHashSet(), doArquivo);
    }

    internal static IEnumerable<LinhaDaTabela> LinhasDoArquivo() =>
        Arquivo.GetProperty("endpoints").EnumerateArray().Select(e => new LinhaDaTabela(
            e.GetProperty("secao").GetString()!,
            e.GetProperty("metodo").GetString()!,
            e.GetProperty("caminho").GetString()!,
            e.GetProperty("frequencia").GetString()!,
            e.GetProperty("slaMs").GetInt32(),
            e.GetProperty("tpm").ValueKind == JsonValueKind.Number ? e.GetProperty("tpm").GetInt32() : null,
            e.GetProperty("tpm").ValueKind == JsonValueKind.String,
            e.GetProperty("limiteMensal").ValueKind == JsonValueKind.Number ? e.GetProperty("limiteMensal").GetInt32() : null));

    private static void Comparar<T>(List<string> divergencias, LinhaDaTabela oficial, string campo, T esperado, T encontrado)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, encontrado))
        {
            divergencias.Add($"{oficial.Secao} {oficial.Metodo} {oficial.Caminho}: {campo} esperado {esperado}, encontrado {encontrado}");
        }
    }
}
