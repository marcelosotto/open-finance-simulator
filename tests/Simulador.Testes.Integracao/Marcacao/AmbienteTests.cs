using System.Net;
using System.Text.Json;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Marcacao;

/// <summary><c>GET /admin/ambiente</c>: a marca que a tela mostra (spec 010, Q2, RN72).</summary>
public sealed class AmbienteTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("hml", "hml")]
    [InlineData("stage-2", "stage-2")]
    [InlineData("", "")]
    public async Task Devolve_a_marca_da_configuracao(string configurada, string esperada)
    {
        using var fabrica = new FabricaDoSimulador(configurada);
        using var resposta = await fabrica.ClienteAdministracao().GetAsync(new Uri("/admin/ambiente", UriKind.Relative), _ct);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct));
        Assert.Equal(esperada, json.RootElement.GetProperty("marca").GetString());
        Assert.Equal(FabricaDoSimulador.PortaOpenFinance, json.RootElement.GetProperty("portaOpenFinance").GetInt32());
    }

    [Fact]
    public async Task Nao_existe_na_porta_open_finance()
    {
        using var fabrica = new FabricaDoSimulador().SemPortao();
        using var resposta = await fabrica.ClienteOpenFinance().GetAsync(new Uri("/admin/ambiente", UriKind.Relative), _ct);
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
