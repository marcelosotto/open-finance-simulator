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
    [InlineData("%E9", "%E9")]
    [InlineData("Bruno%0D%0A%5BINFO%5D%20falso", "Bruno [INFO] falso")]
    [InlineData("Ana%00Moura", "Ana Moura")]
    [InlineData("%0D%0A", "operador")]
    [InlineData("%00", "operador")]
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

/// <summary>Limpeza do autor sem subir o host (revisão da 010).</summary>
public sealed class LimpezaDoAutorTests
{
    [Fact]
    public void Limita_o_tamanho()
    {
        var longo = new string('a', 500);
        Assert.Equal(Simulador.Comum.Http.AutorDoPedido.TamanhoMaximo, Simulador.Comum.Http.AutorDoPedido.Limpar(longo)!.Length);
    }

    [Fact]
    public void Tabulacao_e_espacos_repetidos_viram_um_espaco_e_as_pontas_somem()
    {
        Assert.Equal("Ana Moura", Simulador.Comum.Http.AutorDoPedido.Limpar("  Ana\t\t Moura \u0007"));
    }
}
