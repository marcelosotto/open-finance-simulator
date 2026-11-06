using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Text;
using Simulador.Catalogo.Contratos;
using Simulador.Ferramentas.Comandos;

namespace Simulador.Testes.Unidade.Ferramentas;

public class BaixarSpecsTests
{
    private static readonly TravaDasSpecs Trava = new(
        "dono/specs",
        new string('a', 40),
        "contracts/expansion/catalog.json",
        [new ContratoTravado("accounts", "contracts/expansion/snapshots/accounts.yaml", new string('0', 64))]);

    [Fact]
    public async Task Commit_inexistente_gera_erro()
    {
        using var handler = new HandlerFixo(HttpStatusCode.NotFound, []);

        var erro = await BaixarSpecs.Baixar(Trava, Directory.CreateTempSubdirectory().FullName, null, handler);

        Assert.Contains("não encontrado", erro, StringComparison.Ordinal);
        Assert.Equal(new Uri($"https://codeload.github.com/dono/specs/tar.gz/{Trava.Commit}"), handler.Url);
    }

    [Fact]
    public async Task Erro_http_gera_erro()
    {
        using var handler = new HandlerFixo(HttpStatusCode.InternalServerError, []);

        var erro = await BaixarSpecs.Baixar(Trava, Directory.CreateTempSubdirectory().FullName, "token", handler);

        Assert.Contains("HTTP 500", erro, StringComparison.Ordinal);
        Assert.Equal("Bearer token", handler.Autorizacao);
    }

    [Fact]
    public async Task Extrai_so_o_catalogo_e_os_contratos_travados()
    {
        var pacote = Pacote(new Dictionary<string, string>
        {
            ["specs-aaa/contracts/expansion/catalog.json"] = "{}",
            ["specs-aaa/contracts/expansion/snapshots/accounts.yaml"] = "openapi: 3.0.0",
            ["specs-aaa/contracts/expansion/snapshots/outro.yaml"] = "x",
        });
        using var handler = new HandlerFixo(HttpStatusCode.OK, pacote);
        var pasta = Path.Combine(Directory.CreateTempSubdirectory().FullName, "destino");
        Directory.CreateDirectory(pasta);
        await File.WriteAllTextAsync(Path.Combine(pasta, "velho.txt"), "x", TestContext.Current.CancellationToken);

        var erro = await BaixarSpecs.Baixar(Trava, pasta, null, handler);

        Assert.Null(erro);
        Assert.Null(handler.Autorizacao);
        Assert.True(File.Exists(Path.Combine(pasta, "contracts/expansion/catalog.json")));
        Assert.Equal("openapi: 3.0.0", await File.ReadAllTextAsync(Path.Combine(pasta, "contracts/expansion/snapshots/accounts.yaml"), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(pasta, "contracts/expansion/snapshots/outro.yaml")));
        Assert.False(File.Exists(Path.Combine(pasta, "velho.txt")));
    }

    [Fact]
    public void Pasta_padrao_fica_ao_lado_da_trava()
    {
        var pasta = BaixarSpecs.PastaPadrao(Path.Combine("raiz", "specs.lock.json"), "abc");

        Assert.Equal(Path.Combine(Path.GetFullPath("raiz"), ".specs", "abc"), pasta);
    }

    private static byte[] Pacote(Dictionary<string, string> arquivos)
    {
        using var memoria = new MemoryStream();
        using (var gzip = new GZipStream(memoria, CompressionLevel.Fastest, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            tar.WriteEntry(new PaxTarEntry(TarEntryType.Directory, "specs-aaa/"));
            foreach (var (nome, conteudo) in arquivos)
            {
                tar.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, nome) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(conteudo)) });
            }
        }

        return memoria.ToArray();
    }

    private sealed class HandlerFixo(HttpStatusCode status, byte[] corpo) : HttpMessageHandler
    {
        public Uri? Url { get; private set; }

        public string? Autorizacao { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri;
            Autorizacao = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(corpo) });
        }
    }
}
