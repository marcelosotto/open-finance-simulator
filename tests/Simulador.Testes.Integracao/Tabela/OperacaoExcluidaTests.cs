using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Tabela;

/// <summary>FR-015: operação registrada como excluída não é publicada nem listada.</summary>
public class OperacaoExcluidaTests
{
    [Fact]
    public async Task Operacao_excluida_responde_404_e_some_do_catalogo()
    {
        var tabela = JsonNode.Parse(await File.ReadAllTextAsync(Repositorio.Caminho("config", "tabela-de-endpoints.json"), TestContext.Current.CancellationToken))!;
        var endpoints = tabela["endpoints"]!.AsArray();
        var reservas = endpoints.Single(e => e!["contrato"]!.GetValue<string>() == "accounts"
            && e["caminho"]!.GetValue<string>() == "/accounts/{accountId}/reserved-balances")!;
        endpoints.Remove(reservas);
        tabela["excluidas"] = new JsonArray(new JsonObject
        {
            ["contrato"] = "accounts",
            ["metodo"] = "GET",
            ["caminho"] = "/accounts/{accountId}/reserved-balances",
            ["motivo"] = "teste",
        });
        var arquivo = Path.Combine(Directory.CreateTempSubdirectory().FullName, "tabela.json");
        await File.WriteAllTextAsync(arquivo, tabela.ToJsonString(), TestContext.Current.CancellationToken);
        using var fabrica = new FabricaDoSimulador().Com("Simulador:ArquivoDaTabela", arquivo);

        using var operacao = await fabrica.ClienteOpenFinance().GetAsync(
            new Uri("/banco-teste/open-banking/accounts/v2/accounts/a1/reserved-balances", UriKind.Relative), TestContext.Current.CancellationToken);
        using var catalogo = await fabrica.ClienteAdministracao().GetAsync(new Uri("/admin/catalogo", UriKind.Relative), TestContext.Current.CancellationToken);
        using var pronto = await fabrica.ClienteAdministracao().GetAsync(new Uri("/saude/pronto", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, operacao.StatusCode);
        Assert.Equal(HttpStatusCode.OK, pronto.StatusCode);
        using var corpo = JsonDocument.Parse(await catalogo.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var ids = corpo.RootElement.GetProperty("operacoes").EnumerateArray().Select(o => o.GetProperty("operationId").GetString()).ToList();
        Assert.Equal(74, ids.Count);
        Assert.DoesNotContain("accountsGetAccountsAccountIdReservedBalances", ids);
    }

    [Fact]
    public async Task Operacao_sem_linha_e_sem_exclusao_deixa_o_catalogo_nao_pronto()
    {
        var tabela = JsonNode.Parse(await File.ReadAllTextAsync(Repositorio.Caminho("config", "tabela-de-endpoints.json"), TestContext.Current.CancellationToken))!;
        var endpoints = tabela["endpoints"]!.AsArray();
        endpoints.Remove(endpoints.Single(e => e!["contrato"]!.GetValue<string>() == "exchanges"
            && e["caminho"]!.GetValue<string>() == "/operations/{operationId}/events"));
        var arquivo = Path.Combine(Directory.CreateTempSubdirectory().FullName, "tabela.json");
        await File.WriteAllTextAsync(arquivo, tabela.ToJsonString(), TestContext.Current.CancellationToken);
        using var fabrica = new FabricaDoSimulador().Com("Simulador:ArquivoDaTabela", arquivo);

        using var pronto = await fabrica.ClienteAdministracao().GetAsync(new Uri("/saude/pronto", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, pronto.StatusCode);
        Assert.Contains("/operations/{operationId}/events", await pronto.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Admin_traz_a_classe_de_cada_operacao()
    {
        using var fabrica = new FabricaDoSimulador();

        using var catalogo = await fabrica.ClienteAdministracao().GetAsync(new Uri("/admin/catalogo", UriKind.Relative), TestContext.Current.CancellationToken);

        using var corpo = JsonDocument.Parse(await catalogo.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var operacoes = corpo.RootElement.GetProperty("operacoes").EnumerateArray().ToList();
        var saldo = operacoes.Single(o => o.GetProperty("operationId").GetString() == "accountsGetAccountsAccountIdBalances").GetProperty("classe");
        Assert.Equal("alta", saldo.GetProperty("frequencia").GetString());
        Assert.Equal(1500, saldo.GetProperty("slaMs").GetInt32());
        Assert.Equal("faixa-qca", saldo.GetProperty("tpm").GetString());
        Assert.Equal(420, saldo.GetProperty("limiteMensal").GetInt32());

        var consentimento = operacoes.Single(o => o.GetProperty("operationId").GetString() == "consentsPostConsents").GetProperty("classe");
        Assert.Equal(JsonValueKind.Null, consentimento.GetProperty("tpm").ValueKind);
        Assert.Equal(JsonValueKind.Null, consentimento.GetProperty("limiteMensal").ValueKind);

        var cadastro = operacoes.Single(o => o.GetProperty("operationId").GetString() == "customersGetPersonalIdentifications").GetProperty("classe");
        Assert.Equal(1000, cadastro.GetProperty("tpm").GetInt32());
    }
}
