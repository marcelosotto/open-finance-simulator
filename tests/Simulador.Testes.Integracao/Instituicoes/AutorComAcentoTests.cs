using System.Net;
using System.Net.Http.Json;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Instituicoes;

/// <summary>O autor de x-sim-autor chega em percent-encoding UTF-8 e é gravado com acento (revisão da 010, RN71).</summary>
public sealed class AutorComAcentoTests : IDisposable
{
    private readonly FabricaDoSimulador _fabrica = new();
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public AutorComAcentoTests() => _admin = _fabrica.ClienteAdministracao();

    public void Dispose() => _fabrica.Dispose();

    [Theory]
    [InlineData("Jo%C3%A3o", "João")]
    [InlineData("Concei%C3%A7%C3%A3o", "Conceição")]
    [InlineData("Diego", "Diego")]
    [InlineData("Ana%20Moura", "Ana Moura")]
    [InlineData("100%", "100%")]
    public async Task Grava_o_autor_decodificado_e_ascii_puro_continua_igual(string enviado, string gravado)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post, new Uri("/admin/instituicoes", UriKind.Relative))
        {
            Content = JsonContent.Create(new { nome = "Banco do Autor" }),
        };
        pedido.Headers.Add("x-sim-autor", enviado);
        using var resposta = await _admin.SendAsync(pedido, _ct);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var historico = await _admin.GetStringAsync(new Uri("/admin/instituicoes/banco-do-autor/historico", UriKind.Relative), _ct);
        Assert.Contains($"\"autor\":\"{gravado}\"", historico, StringComparison.Ordinal);
    }
}
