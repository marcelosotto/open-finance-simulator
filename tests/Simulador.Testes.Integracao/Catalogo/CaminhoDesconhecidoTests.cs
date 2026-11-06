using System.Net;
using System.Text.Json;

namespace Simulador.Testes.Integracao.Catalogo;

public sealed class CaminhoDesconhecidoTests(FabricaCompartilhada compartilhada) : IClassFixture<FabricaCompartilhada>
{
    [Theory]
    [InlineData("GET", "/banco-teste/open-banking/nao-existe/v1/xpto")]
    [InlineData("GET", "/xpto")]
    [InlineData("POST", "/banco-teste/open-banking/accounts/v2/accounts")]
    [InlineData("DELETE", "/banco-teste/open-banking/resources/v3/resources")]
    [InlineData("GET", "/Banco_Invalido/open-banking/accounts/v2/accounts")]
    public async Task Responde_404_no_envelope_oficial(string metodo, string caminho)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), new Uri(caminho, UriKind.Relative));

        using var resposta = await compartilhada.Fabrica.ClienteOpenFinance().SendAsync(requisicao, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("NAO_ENCONTRADO", corpo.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Banco_com_mais_de_64_caracteres_responde_404()
    {
        var banco = new string('a', 65);

        using var resposta = await compartilhada.Fabrica.ClienteOpenFinance()
            .GetAsync(new Uri($"/{banco}/open-banking/accounts/v2/accounts", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
