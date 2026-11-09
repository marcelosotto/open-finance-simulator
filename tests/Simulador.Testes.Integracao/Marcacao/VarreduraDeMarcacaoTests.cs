using Simulador.Testes.Host;
using Simulador.Testes.Integracao.Catalogo;

namespace Simulador.Testes.Integracao.Marcacao;

/// <summary>FR-016 e FR-022: toda resposta das APIs Open Finance sai marcada.</summary>
public sealed class VarreduraDeMarcacaoTests
{
    [Theory]
    [InlineData("hml")]
    [InlineData("dev")]
    [InlineData("stage")]
    public async Task Com_marca_toda_resposta_traz_o_ambiente_e_nao_traz_natureza(string marca)
    {
        using var fabrica = new FabricaDoSimulador(marca);
        var cliente = fabrica.ClienteOpenFinance();
        var semMarca = new List<string>();

        foreach (var (_, metodo, caminho) in Operacoes())
        {
            using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), RotasGeradasTests.Preencher(caminho));
            using var resposta = await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);
            if (!resposta.Headers.TryGetValues("x-sim-environment", out var valores) || valores.Single() != marca
                || resposta.Headers.Contains("x-sim-data-nature"))
            {
                semMarca.Add($"{metodo} {caminho}");
            }
        }

        using var naoEncontrado = await cliente.GetAsync(new Uri("/xpto", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(marca, naoEncontrado.Headers.GetValues("x-sim-environment").Single());
        Assert.Empty(semMarca);
    }

    [Fact]
    public async Task Com_marca_vazia_nenhuma_resposta_traz_o_ambiente()
    {
        using var fabrica = new FabricaDoSimulador(string.Empty);
        var cliente = fabrica.ClienteOpenFinance();
        var comMarca = new List<string>();

        foreach (var (_, metodo, caminho) in Operacoes())
        {
            using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), RotasGeradasTests.Preencher(caminho));
            using var resposta = await cliente.SendAsync(requisicao, TestContext.Current.CancellationToken);
            if (resposta.Headers.Contains("x-sim-environment"))
            {
                comMarca.Add($"{metodo} {caminho}");
            }
        }

        Assert.Empty(comMarca);
    }

    [Fact]
    public async Task Administracao_nao_leva_cabecalhos_de_marcacao()
    {
        using var fabrica = new FabricaDoSimulador();

        using var resposta = await fabrica.ClienteAdministracao().GetAsync(new Uri("/saude/vivo", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.False(resposta.Headers.Contains("x-sim-environment"));
    }

    private static IEnumerable<(string, string, string)> Operacoes() =>
        RotasGeradasTests.TodasAsOperacoes().Select(d => (d.Data.Item1, d.Data.Item2, d.Data.Item3));
}
