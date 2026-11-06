using System.Text.Json;
using Simulador.Catalogo.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Tabela;

/// <summary>FR-015: toda operação do catálogo tem linha na tabela ou está excluída em docs/, e vice-versa.</summary>
public class CatalogoTabelaTests
{
    private static readonly IReadOnlyList<OperacaoDoCatalogo> Operacoes = ServicosDeSpecs.LerOperacoes(
        TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs());

    private static readonly JsonElement Arquivo =
        JsonDocument.Parse(File.ReadAllText(Repositorio.Caminho("config", "tabela-de-endpoints.json"))).RootElement;

    private static readonly TabelaOficial Oficial =
        LeitorDaTabelaOficial.Ler(Repositorio.Caminho("docs", "01-requisitos", "tabela-de-endpoints.md"));

    private static HashSet<(string, string, string)> Chaves(string propriedade) =>
        Arquivo.GetProperty(propriedade).EnumerateArray()
            .Select(e => (e.GetProperty("contrato").GetString()!, e.GetProperty("metodo").GetString()!, e.GetProperty("caminho").GetString()!))
            .ToHashSet();

    [Fact]
    public void Toda_operacao_tem_linha_ou_esta_excluida_em_docs()
    {
        var linhas = Chaves("endpoints");
        var excluidas = Oficial.Exclusoes.Select(e => (e.Contrato, e.Metodo, e.Caminho)).ToHashSet();

        var semLinha = Operacoes.Where(o => !linhas.Contains(o.Chave) && !excluidas.Contains(o.Chave))
            .Select(o => $"{o.Contrato} {o.Metodo} {o.Caminho}").ToList();

        Assert.True(semLinha.Count == 0, "Operações sem linha na tabela oficial (decidir em docs/): " + string.Join(", ", semLinha));
    }

    [Fact]
    public void Toda_linha_e_toda_exclusao_tem_operacao_no_catalogo()
    {
        var chaves = Operacoes.Select(o => o.Chave).ToHashSet();

        var semOperacao = Chaves("endpoints").Concat(Chaves("excluidas")).Where(c => !chaves.Contains(c))
            .Select(c => $"{c.Item1} {c.Item2} {c.Item3}").ToList();

        Assert.True(semOperacao.Count == 0, "Linhas sem operação no catálogo: " + string.Join(", ", semOperacao));
    }

    [Fact]
    public void Mesmo_caminho_em_apis_diferentes_tem_classe_propria()
    {
        var accounts = Arquivo.GetProperty("endpoints").EnumerateArray()
            .Where(e => e.GetProperty("caminho").GetString() == "/accounts/{accountId}" || e.GetProperty("caminho").GetString() == "/accounts/{creditCardAccountId}")
            .ToDictionary(e => e.GetProperty("contrato").GetString()!, e => e.GetProperty("frequencia").GetString());

        Assert.Equal("baixa", accounts["accounts"]);
        Assert.Equal("media-alta", accounts["credit-cards-accounts"]);
    }
}
