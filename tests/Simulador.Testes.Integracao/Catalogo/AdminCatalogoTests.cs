using System.Net;
using System.Text.Json;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Catalogo;

public sealed class AdminCatalogoTests(FabricaCompartilhada compartilhada) : IClassFixture<FabricaCompartilhada>
{
    [Fact]
    public async Task Lista_as_operacoes_do_catalogo()
    {
        using var resposta = await compartilhada.Fabrica.ClienteAdministracao()
            .GetAsync(new Uri("/admin/catalogo", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("7bdb9c1a6c4c265d7aabcdbc662f94c61718135e", corpo.RootElement.GetProperty("commit").GetString());
        var operacoes = corpo.RootElement.GetProperty("operacoes").EnumerateArray().ToList();
        Assert.Equal(75, operacoes.Count);

        var saldo = operacoes.Single(o => o.GetProperty("operationId").GetString() == "accountsGetAccountsAccountIdBalances");
        Assert.Equal("accounts", saldo.GetProperty("contrato").GetString());
        Assert.Equal("accounts", saldo.GetProperty("api").GetString());
        Assert.Equal("v2", saldo.GetProperty("versao").GetString());
        Assert.Equal("GET", saldo.GetProperty("metodo").GetString());
        Assert.Equal("/accounts/{accountId}/balances", saldo.GetProperty("caminho").GetString());
        Assert.Equal("/{banco}/open-banking/accounts/v2/accounts/{accountId}/balances", saldo.GetProperty("caminhoPublicado").GetString());
        Assert.False(saldo.GetProperty("implementada").GetBoolean());
    }

    [Fact]
    public async Task Catalogo_nao_existe_na_porta_open_finance()
    {
        using var resposta = await compartilhada.Fabrica.ClienteOpenFinance()
            .GetAsync(new Uri("/admin/catalogo", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Pronto_com_specs_carregadas()
    {
        using var resposta = await compartilhada.Fabrica.ClienteAdministracao()
            .GetAsync(new Uri("/saude/pronto", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Nao_pronto_com_pasta_de_specs_vazia()
    {
        var vazia = Directory.CreateTempSubdirectory().FullName;
        using var fabrica = new FabricaDoSimulador().Com("Simulador:PastaDasSpecs", vazia);

        using var pronto = await fabrica.ClienteAdministracao().GetAsync(new Uri("/saude/pronto", UriKind.Relative), TestContext.Current.CancellationToken);
        using var operacao = await fabrica.ClienteOpenFinance()
            .GetAsync(new Uri("/banco-teste/open-banking/accounts/v2/accounts", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, pronto.StatusCode);
        Assert.Contains("catálogo não encontrado", await pronto.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.NotFound, operacao.StatusCode);
    }

    [Fact]
    public async Task Nao_pronto_com_hash_divergente()
    {
        var copia = Directory.CreateTempSubdirectory().FullName;
        var origem = Infra.Repositorio.PastaDasSpecs();
        foreach (var arquivo in Directory.EnumerateFiles(origem, "*", SearchOption.AllDirectories))
        {
            var destino = Path.Combine(copia, Path.GetRelativePath(origem, arquivo));
            Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
            File.Copy(arquivo, destino);
        }

        await File.AppendAllTextAsync(Path.Combine(copia, "contracts/expansion/snapshots/accounts-2.5.1.yaml"), "\n", TestContext.Current.CancellationToken);
        using var fabrica = new FabricaDoSimulador().Com("Simulador:PastaDasSpecs", copia);

        using var pronto = await fabrica.ClienteAdministracao().GetAsync(new Uri("/saude/pronto", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, pronto.StatusCode);
        Assert.Contains("accounts-2.5.1.yaml", await pronto.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }
}
