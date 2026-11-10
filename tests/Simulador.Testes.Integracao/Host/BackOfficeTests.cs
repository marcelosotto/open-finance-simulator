using System.Net;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Host;

/// <summary>O build do front servido só na porta de administração (spec 010, Q3).</summary>
public sealed class BackOfficeTests : IDisposable
{
    private readonly string _pasta = Directory.CreateTempSubdirectory("backoffice-").FullName;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public BackOfficeTests()
    {
        File.WriteAllText(Path.Combine(_pasta, "index.html"), "<!doctype html><title>Back-office</title>");
        File.WriteAllText(Path.Combine(_pasta, "app.123.js"), "console.log(1)");
    }

    public void Dispose() => Directory.Delete(_pasta, recursive: true);

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    private static FabricaDoSimulador Fabrica(string? pasta) => new FabricaDoSimulador().SemPortao().Com("Simulador:PastaDoBackOffice", pasta);

    [Theory]
    [InlineData("/")]
    [InlineData("/bancos")]
    [InlineData("/consentimentos/urn:hml:p-c0x0")]
    public async Task Rotas_da_tela_devolvem_o_indice(string caminho)
    {
        using var fabrica = Fabrica(_pasta);
        using var resposta = await fabrica.ClienteAdministracao().GetAsync(U(caminho), _ct);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("text/html", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Back-office", await resposta.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Arquivo_do_build_e_servido_e_arquivo_ausente_e_404()
    {
        using var fabrica = Fabrica(_pasta);
        var admin = fabrica.ClienteAdministracao();

        using var arquivo = await admin.GetAsync(U("/app.123.js"), _ct);
        Assert.Equal(HttpStatusCode.OK, arquivo.StatusCode);
        Assert.Equal("console.log(1)", await arquivo.Content.ReadAsStringAsync(_ct));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(U("/nao-existe.js"), _ct)).StatusCode);
    }

    [Fact]
    public async Task Administracao_e_saude_nao_mudam()
    {
        using var fabrica = Fabrica(_pasta);
        var admin = fabrica.ClienteAdministracao();

        using var inexistente = await admin.GetAsync(U("/admin/nao-existe"), _ct);
        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.NotEqual("text/html", inexistente.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(U("/saude/nao-existe"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync(U("/admin/ambiente"), _ct)).StatusCode);
    }

    [Theory]
    [InlineData("DELETE", "/admin/ambiente")]
    [InlineData("GET", "/admin/consentimentos")]
    [InlineData("GET", "/admin/massa/limpezas")]
    public async Task Metodo_errado_na_administracao_responde_como_sem_a_tela(string metodo, string caminho)
    {
        using var com = Fabrica(_pasta);
        using var sem = Fabrica(null);
        using var pedidoCom = new HttpRequestMessage(new HttpMethod(metodo), U(caminho));
        using var pedidoSem = new HttpRequestMessage(new HttpMethod(metodo), U(caminho));

        using var respostaCom = await com.ClienteAdministracao().SendAsync(pedidoCom, _ct);
        using var respostaSem = await sem.ClienteAdministracao().SendAsync(pedidoSem, _ct);

        Assert.Equal(respostaSem.StatusCode, respostaCom.StatusCode);
        Assert.NotEqual("text/html", respostaCom.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Porta_open_finance_nao_serve_a_tela()
    {
        using var fabrica = Fabrica(_pasta);
        var api = fabrica.ClienteOpenFinance();

        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync(U("/"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync(U("/app.123.js"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetAsync(U("/bancos"), _ct)).StatusCode);
    }

    [Fact]
    public async Task Sem_a_pasta_nada_e_servido()
    {
        using var fabrica = Fabrica(Path.Combine(_pasta, "nao-existe"));
        Assert.Equal(HttpStatusCode.NotFound, (await fabrica.ClienteAdministracao().GetAsync(U("/"), _ct)).StatusCode);
    }
}
