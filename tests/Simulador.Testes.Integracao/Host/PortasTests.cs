using System.Net;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Host;

public sealed class PortasTests : IDisposable
{
    private readonly FabricaDoSimulador _fabrica = new();

    [Fact]
    public async Task Saude_responde_na_porta_de_administracao()
    {
        using var resposta = await _fabrica.ClienteAdministracao().GetAsync(new Uri("/saude/vivo", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Saude_nao_existe_na_porta_open_finance()
    {
        using var resposta = await _fabrica.ClienteOpenFinance().GetAsync(new Uri("/saude/vivo", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    public void Dispose() => _fabrica.Dispose();
}
