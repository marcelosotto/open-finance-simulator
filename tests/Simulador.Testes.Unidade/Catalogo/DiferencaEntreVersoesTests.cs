using Simulador.Catalogo.Contratos;
using Simulador.Catalogo.Dominio;

namespace Simulador.Testes.Unidade.Catalogo;

public class DiferencaEntreVersoesTests
{
    private const string Base = "7bdb9c1a6c4c265d7aabcdbc662f94c61718135e";
    private const string Novo = "1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b";

    private static OperacaoDoCatalogo Operacao(
        string caminho = "/accounts",
        string versao = "2.5.1",
        string schema = "{\"type\":\"object\"}",
        string? parametro = "page",
        string[]? cabecalhos = null,
        string codigo = "200") =>
        new(
            "accounts",
            versao,
            "/open-banking/accounts/v2",
            "GET",
            caminho,
            "op" + caminho,
            parametro is null ? [] : [new ParametroDaOperacao(parametro, "query", false, "{}")],
            cabecalhos ?? ["Authorization"],
            new Dictionary<string, string> { [codigo] = schema });

    [Fact]
    public void Conjuntos_iguais_nao_tem_diferenca()
    {
        var diferenca = ComparadorDeVersoes.Comparar(Base, [Operacao()], Novo, [Operacao()]);

        Assert.Empty(diferenca.Novas);
        Assert.Empty(diferenca.Removidas);
        Assert.Empty(diferenca.Alteradas);
        Assert.False(diferenca.TemMudancas);
    }

    [Fact]
    public void Detecta_novas_e_removidas()
    {
        var diferenca = ComparadorDeVersoes.Comparar(Base, [Operacao("/a")], Novo, [Operacao("/b")]);

        Assert.Equal("/b", Assert.Single(diferenca.Novas).Caminho);
        Assert.Equal("/a", Assert.Single(diferenca.Removidas).Caminho);
        Assert.True(diferenca.TemMudancas);
    }

    [Fact]
    public void Detecta_versao_do_contrato()
    {
        var alterada = Assert.Single(ComparadorDeVersoes.Comparar(Base, [Operacao()], Novo, [Operacao(versao: "2.6.0")]).Alteradas);

        Assert.Contains("versão do contrato: 2.5.1 → 2.6.0", alterada.Mudancas);
    }

    [Fact]
    public void Detecta_parametros()
    {
        var alterada = Assert.Single(ComparadorDeVersoes.Comparar(Base, [Operacao()], Novo, [Operacao(parametro: "page-size")]).Alteradas);

        Assert.Contains("parâmetros mudaram", alterada.Mudancas);
    }

    [Fact]
    public void Detecta_cabecalhos_exigidos()
    {
        var alterada = Assert.Single(ComparadorDeVersoes.Comparar(
            Base, [Operacao()], Novo, [Operacao(cabecalhos: ["Authorization", "x-fapi-interaction-id"])]).Alteradas);

        Assert.Contains("cabeçalhos exigidos mudaram", alterada.Mudancas);
    }

    [Fact]
    public void Detecta_schema_de_resposta()
    {
        var alterada = Assert.Single(ComparadorDeVersoes.Comparar(
            Base, [Operacao()], Novo, [Operacao(schema: "{\"type\":\"array\"}")]).Alteradas);

        Assert.Contains("schema da resposta 200 mudou", alterada.Mudancas);
    }

    [Fact]
    public void Detecta_resposta_nova_e_removida()
    {
        var alterada = Assert.Single(ComparadorDeVersoes.Comparar(Base, [Operacao()], Novo, [Operacao(codigo: "201")]).Alteradas);

        Assert.Contains("resposta 201 nova", alterada.Mudancas);
        Assert.Contains("resposta 200 removida", alterada.Mudancas);
    }

    [Fact]
    public void Markdown_traz_commits_curtos_e_secoes_com_contagem()
    {
        var diferenca = ComparadorDeVersoes.Comparar(
            Base,
            [Operacao("/a"), Operacao("/c")],
            Novo,
            [Operacao("/b"), Operacao("/c", schema: "{\"type\":\"array\"}")]);

        var markdown = diferenca.ParaMarkdown();

        Assert.Contains("## Specs: 7bdb9c1 → 1a2b3c4", markdown, StringComparison.Ordinal);
        Assert.Contains("### Novas (1)", markdown, StringComparison.Ordinal);
        Assert.Contains("- GET /open-banking/accounts/v2/b (accounts 2.5.1)", markdown, StringComparison.Ordinal);
        Assert.Contains("### Alteradas (1)", markdown, StringComparison.Ordinal);
        Assert.Contains("- GET /open-banking/accounts/v2/c: schema da resposta 200 mudou", markdown, StringComparison.Ordinal);
        Assert.Contains("### Removidas (1)", markdown, StringComparison.Ordinal);
        Assert.Contains("- GET /open-banking/accounts/v2/a (accounts 2.5.1)", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Markdown_sem_mudancas_avisa()
    {
        var markdown = ComparadorDeVersoes.Comparar(Base, [Operacao()], Base, [Operacao()]).ParaMarkdown();

        Assert.Contains("Nenhuma operação nova, alterada ou removida.", markdown, StringComparison.Ordinal);
    }
}
