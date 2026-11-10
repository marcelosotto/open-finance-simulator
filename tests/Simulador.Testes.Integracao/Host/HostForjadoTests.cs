using System.Net;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Host;

/// <summary>
/// As portas se separam pelo socket, nunca pelo cabeçalho Host (revisão da 010, FR-025 da 002): um Host forjado não
/// leva a administração para a porta pública, nem as APIs Open Finance para a de administração.
/// </summary>
public sealed class HostForjadoTests : IDisposable
{
    private readonly string _pasta = Directory.CreateTempSubdirectory("backoffice-").FullName;
    private readonly FabricaDoSimulador _fabrica;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public HostForjadoTests()
    {
        File.WriteAllText(Path.Combine(_pasta, "index.html"), "<!doctype html><title>Back-office</title>");
        File.WriteAllText(Path.Combine(_pasta, "app.0123456789abcdef.js"), "console.log(1)");
        _fabrica = new FabricaDoSimulador().ComReceptoraPadrao().Com("Simulador:PastaDoBackOffice", _pasta);
    }

    public void Dispose()
    {
        _fabrica.Dispose();
        Directory.Delete(_pasta, recursive: true);
    }

    private async Task<HttpResponseMessage> Pedir(int portaDoSocket, string hostForjado, HttpMethod metodo, string caminho)
    {
        using var pedido = new HttpRequestMessage(metodo, new Uri(caminho, UriKind.Relative));
        pedido.Headers.Host = hostForjado;
        return await _fabrica.Cliente(portaDoSocket).SendAsync(pedido, _ct);
    }

    [Theory]
    [InlineData("/admin/receptoras")]
    [InlineData("/admin/instituicoes")]
    [InlineData("/admin/ambiente")]
    [InlineData("/")]
    [InlineData("/bancos")]
    [InlineData("/app.0123456789abcdef.js")]
    public async Task Host_da_administracao_na_porta_publica_responde_404(string caminho)
    {
        using var resposta = await Pedir(FabricaDoSimulador.PortaOpenFinance, "x:8081", HttpMethod.Get, caminho);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.DoesNotContain("senha", await resposta.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Host_publico_na_porta_de_administracao_nao_alcanca_as_APIs_Open_Finance()
    {
        using var token = await Pedir(FabricaDoSimulador.PortaAdministracao, "x:8080", HttpMethod.Post, "/banco-estavel/token");
        using var contas = await Pedir(FabricaDoSimulador.PortaAdministracao, "x:8080", HttpMethod.Get, "/banco-estavel/open-banking/accounts/v2/accounts");

        Assert.Equal(HttpStatusCode.NotFound, token.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, contas.StatusCode);
        Assert.Equal("text/html", contas.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Cada_porta_continua_respondendo_com_qualquer_Host()
    {
        using var admin = await Pedir(FabricaDoSimulador.PortaAdministracao, "outro-nome:9999", HttpMethod.Get, "/admin/receptoras");
        using var token = await Pedir(FabricaDoSimulador.PortaOpenFinance, "outro-nome:9999", HttpMethod.Post, "/banco-estavel/token");

        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        // Sem corpo, o token responde a recusa OAuth: a rota foi alcançada pela porta pública com qualquer Host.
        Assert.Equal(HttpStatusCode.BadRequest, token.StatusCode);
        Assert.Contains("invalid_request", await token.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("DELETE", "/admin/ambiente")]
    [InlineData("PUT", "/admin/receptoras")]
    public async Task Metodo_errado_na_administracao_responde_404_como_rota_inexistente(string metodo, string caminho)
    {
        // As rotas Open Finance têm um fallback para qualquer método; ele é da outra porta e sai na escolha da rota,
        // então o método errado na 8081 responde 404, e não 405 (contrato da 010, "Portas").
        using var resposta = await Pedir(FabricaDoSimulador.PortaAdministracao, "localhost:8081", new HttpMethod(metodo), caminho);
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.NotEqual("text/html", resposta.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    public async Task Metodo_que_nao_e_GET_em_caminho_de_pagina_responde_404(string metodo)
    {
        using var resposta = await Pedir(FabricaDoSimulador.PortaAdministracao, "localhost:8081", new HttpMethod(metodo), "/bancos");
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Indice_sem_cache_e_arquivo_com_hash_imutavel()
    {
        using var indice = await Pedir(FabricaDoSimulador.PortaAdministracao, "localhost:8081", HttpMethod.Get, "/bancos");
        using var arquivo = await Pedir(FabricaDoSimulador.PortaAdministracao, "localhost:8081", HttpMethod.Get, "/app.0123456789abcdef.js");

        Assert.Equal("no-cache", indice.Headers.CacheControl?.ToString());
        Assert.Equal("public, max-age=31536000, immutable", arquivo.Headers.CacheControl?.ToString());
    }
}
