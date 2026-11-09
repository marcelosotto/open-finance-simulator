using System.Text.Json;
using System.Text.Json.Nodes;
using Simulador.Catalogo.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Catalogo;

public class LeitorDeSpecsTests
{
    private static readonly TravaDasSpecs Trava = TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json"));
    private static readonly Lazy<IReadOnlyList<OperacaoDoCatalogo>> Operacoes =
        new(() => ServicosDeSpecs.LerOperacoes(Trava, Repositorio.PastaDasSpecs()));

    [Fact]
    public void Extrai_api_e_versao_do_servidor()
    {
        var operacao = Operacoes.Value.Single(o => o.OperationId == "accountsGetAccountsAccountIdBalances");

        Assert.Equal("accounts", operacao.Contrato);
        Assert.Equal("2.5.1", operacao.VersaoDoContrato);
        Assert.Equal("accounts", operacao.Api);
        Assert.Equal("v2", operacao.Versao);
        Assert.Equal("GET", operacao.Metodo);
        Assert.Equal("/accounts/{accountId}/balances", operacao.Caminho);
        Assert.Equal("/{banco}/open-banking/accounts/v2/accounts/{accountId}/balances", operacao.CaminhoPublicado);
    }

    [Fact]
    public void Extrai_parametros_cabecalhos_e_schemas()
    {
        var operacao = Operacoes.Value.Single(o => o.OperationId == "accountsGetAccountsAccountIdTransactions");

        var accountId = Assert.Single(operacao.Parametros, p => p.Nome == "accountId");
        Assert.Equal("path", accountId.Local);
        Assert.True(accountId.Obrigatorio);
        Assert.Contains("Authorization", operacao.CabecalhosExigidos);
        Assert.Contains("x-fapi-interaction-id", operacao.Parametros.Select(p => p.Nome));
        Assert.Contains("200", operacao.Respostas.Keys);

        var schema = JsonNode.Parse(operacao.Respostas["200"])!;
        var transactionName = schema["properties"]!["data"]!["items"]!["properties"]!["transactionName"]!;
        Assert.Equal(200, transactionName["maxLength"]!.GetValue<int>());
        Assert.DoesNotContain("$ref", operacao.Respostas["200"], StringComparison.Ordinal);
    }

    [Fact]
    public void Numero_de_operacoes_bate_com_o_catalogo_do_commit()
    {
        using var catalogo = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repositorio.PastaDasSpecs(), Trava.Catalogo)));
        var esperado = catalogo.RootElement.GetProperty("contracts").EnumerateArray()
            .Where(c => Trava.Contratos.Any(t => t.Id == c.GetProperty("id").GetString()))
            .ToDictionary(c => c.GetProperty("id").GetString()!, c => c.GetProperty("operations").GetArrayLength());

        var lido = Operacoes.Value.GroupBy(o => o.Contrato).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(15, lido.Count);
        Assert.Equal(esperado.OrderBy(p => p.Key), lido.OrderBy(p => p.Key));
    }

    [Fact]
    public void Documento_json_do_contrato_tem_os_componentes()
    {
        var documento = ServicosDeSpecs.LerDocumentoJson(Trava, Repositorio.PastaDasSpecs(), "consents");

        Assert.NotNull(documento["components"]?["schemas"]);
        Assert.Equal("3.3.1", documento["info"]!["version"]!.GetValue<string>());
    }

    [Fact]
    public void Contrato_desconhecido_gera_erro()
    {
        Assert.Throws<ArgumentException>(() => ServicosDeSpecs.LerDocumentoJson(Trava, Repositorio.PastaDasSpecs(), "nao-existe"));
    }
}
