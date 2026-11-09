using Simulador.Ferramentas;

namespace Simulador.Testes.Unidade.Ferramentas;

public class CoberturaComandoTests
{
    private static string Relatorio(params (string Classe, string Arquivo, int Linha, int Hits, string? Ramo)[] linhas)
    {
        var classes = linhas.GroupBy(l => (l.Classe, l.Arquivo)).Select(g =>
            $"""<class name="{g.Key.Classe}" filename="{g.Key.Arquivo}"><lines>{string.Concat(g.Select(l =>
                $"""<line number="{l.Linha}" hits="{l.Hits}" branch="{(l.Ramo is null ? "False" : "True")}"{(l.Ramo is null ? "" : $" condition-coverage=\"{l.Ramo}\"")} />"""))}</lines></class>""");
        return $"""<?xml version="1.0"?><coverage><packages><package name="p"><classes>{string.Concat(classes)}</classes></package></packages></coverage>""";
    }

    private static async Task<(int Codigo, string Saida)> Conferir(params string[] relatorios)
    {
        var pasta = Directory.CreateTempSubdirectory().FullName;
        for (var i = 0; i < relatorios.Length; i++)
        {
            var subpasta = Directory.CreateDirectory(Path.Combine(pasta, $"r{i}")).FullName;
            await File.WriteAllTextAsync(Path.Combine(subpasta, "coverage.cobertura.xml"), relatorios[i], TestContext.Current.CancellationToken);
        }

        using var saida = new StringWriter();
        var codigo = await Roteador.Executar(["cobertura", "conferir", "--relatorios", Path.Combine(pasta, "**", "coverage.cobertura.xml")], saida);
        return (codigo, saida.ToString());
    }

    [Fact]
    public async Task Passa_com_dominio_completo_e_total_acima_de_80()
    {
        var (codigo, saida) = await Conferir(Relatorio(
            ("Simulador.Catalogo.Dominio.A", "A.cs", 1, 1, "100% (2/2)"),
            ("Simulador.Catalogo.Rotas.B", "B.cs", 1, 1, null),
            ("Simulador.Catalogo.Rotas.B", "B.cs", 2, 1, null),
            ("Simulador.Catalogo.Rotas.B", "B.cs", 3, 1, null),
            ("Simulador.Catalogo.Rotas.B", "B.cs", 4, 0, null)));

        Assert.Equal(0, codigo);
        Assert.Contains("Domínio: 100", saida, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Falha_com_linha_de_dominio_sem_cobertura_e_nomeia_a_classe()
    {
        var (codigo, saida) = await Conferir(Relatorio(
            ("Simulador.Catalogo.Dominio.A", "A.cs", 1, 1, null),
            ("Simulador.Catalogo.Dominio.A", "A.cs", 7, 0, null)));

        Assert.Equal(1, codigo);
        Assert.Contains("Simulador.Catalogo.Dominio.A", saida, StringComparison.Ordinal);
        Assert.Contains("A.cs:7", saida, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Falha_com_ramo_de_dominio_incompleto()
    {
        var (codigo, saida) = await Conferir(Relatorio(("Simulador.Marcacao.Dominio.X", "X.cs", 3, 2, "50% (1/2)")));

        Assert.Equal(1, codigo);
        Assert.Contains("X.cs:3", saida, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Falha_com_total_abaixo_de_80()
    {
        var linhas = Enumerable.Range(1, 1000).Select(i => ("Simulador.Host.H", "H.cs", i, i <= 799 ? 1 : 0, (string?)null)).ToArray();

        var (codigo, saida) = await Conferir(Relatorio(linhas));

        Assert.Equal(1, codigo);
        Assert.Contains("79,9", saida.Replace('.', ','), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Soma_os_relatorios_pela_maior_cobertura_de_cada_linha()
    {
        var (codigo, _) = await Conferir(
            Relatorio(("Simulador.Catalogo.Dominio.A", "A.cs", 1, 0, "0% (0/2)")),
            Relatorio(("Simulador.Catalogo.Dominio.A", "A.cs", 1, 3, "100% (2/2)")));

        Assert.Equal(0, codigo);
    }

    [Fact]
    public async Task Sem_relatorios_falha()
    {
        var (codigo, saida) = await Conferir();

        Assert.Equal(1, codigo);
        Assert.Contains("Nenhum relatório", saida, StringComparison.Ordinal);
    }
}
