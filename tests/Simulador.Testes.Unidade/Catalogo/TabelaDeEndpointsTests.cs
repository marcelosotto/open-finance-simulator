using Simulador.Catalogo.Contratos;
using Simulador.Catalogo.Dominio;

namespace Simulador.Testes.Unidade.Catalogo;

public class TabelaDeEndpointsTests
{
    private static string Json(
        string frequencia = "\"alta\"",
        string sla = "1500",
        string tpm = "\"faixa-qca\"",
        string limite = "420",
        string timeout = "15",
        string tps = "300",
        string excluidas = "[]") =>
        "{\"fonte\":\"F02\",\"timeoutSegundos\":" + timeout + ",\"tps\":" + tps + ",\"endpoints\":[{\"contrato\":\"accounts\",\"secao\":\"Contas\","
        + "\"metodo\":\"GET\",\"caminho\":\"/accounts/{accountId}/balances\",\"frequencia\":" + frequencia + ",\"slaMs\":" + sla
        + ",\"tpm\":" + tpm + ",\"limiteMensal\":" + limite + "}],\"excluidas\":" + excluidas + "}";

    [Fact]
    public void Le_classe_com_faixa_qca()
    {
        var tabela = TabelaDeEndpoints.Interpretar(Json());

        var classe = Assert.Single(tabela.Classes);
        Assert.Equal(new ClasseDoEndpoint("accounts", "Contas", "GET", "/accounts/{accountId}/balances", Frequencia.Alta, 1500, null, true, 420), classe);
        Assert.Empty(tabela.Excluidas);
    }

    [Theory]
    [InlineData("\"alta\"", Frequencia.Alta)]
    [InlineData("\"media-alta\"", Frequencia.MediaAlta)]
    [InlineData("\"media\"", Frequencia.Media)]
    [InlineData("\"baixa\"", Frequencia.Baixa)]
    public void Le_as_quatro_frequencias(string valor, Frequencia esperada) =>
        Assert.Equal(esperada, Assert.Single(TabelaDeEndpoints.Interpretar(Json(frequencia: valor)).Classes).Frequencia);

    [Fact]
    public void Le_tpm_fixo_e_limites_nulos()
    {
        var classe = Assert.Single(TabelaDeEndpoints.Interpretar(Json(tpm: "1000", limite: "null")).Classes);
        Assert.Equal(1000, classe.Tpm);
        Assert.False(classe.TpmPorFaixaQca);
        Assert.Null(classe.LimiteMensal);

        var semTrafego = Assert.Single(TabelaDeEndpoints.Interpretar(Json(tpm: "null")).Classes);
        Assert.Null(semTrafego.Tpm);
        Assert.False(semTrafego.TpmPorFaixaQca);
    }

    [Fact]
    public void Sem_exclusoes_a_lista_e_vazia()
    {
        var json = Json().Replace(",\"excluidas\":[]", string.Empty, StringComparison.Ordinal);

        Assert.Empty(TabelaDeEndpoints.Interpretar(json).Excluidas);
    }

    [Fact]
    public void Le_a_tabela_do_repositorio() =>
        Assert.Equal(75, TabelaDeEndpoints.Ler(Simulador.Testes.Infra.Repositorio.Caminho("config", "tabela-de-endpoints.json")).Classes.Count);

    [Fact]
    public void Le_exclusoes()
    {
        var tabela = TabelaDeEndpoints.Interpretar(Json(excluidas: "[{\"contrato\":\"accounts\",\"metodo\":\"GET\",\"caminho\":\"/x\",\"motivo\":\"m\"}]"));

        Assert.Equal(("accounts", "GET", "/x"), Assert.Single(tabela.Excluidas));
    }

    [Theory]
    [InlineData("\"rapida\"", "1500", "1", "1", "frequencia")]
    [InlineData("\"alta\"", "1000", "1", "1", "slaMs")]
    [InlineData("\"alta\"", "1500", "0", "1", "tpm")]
    [InlineData("\"alta\"", "1500", "\"outra\"", "1", "tpm")]
    [InlineData("\"alta\"", "1500", "1", "0", "limiteMensal")]
    [InlineData("\"alta\"", "1500", "1", "\"x\"", "limiteMensal")]
    public void Recusa_valor_fora_do_schema(string frequencia, string sla, string tpm, string limite, string campo)
    {
        var erro = Assert.Throws<TabelaInvalidaException>(() => TabelaDeEndpoints.Interpretar(Json(frequencia, sla, tpm, limite)));

        Assert.Contains(campo, erro.Message, StringComparison.Ordinal);
        Assert.Contains("/accounts/{accountId}/balances", erro.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("16", "300", "timeoutSegundos")]
    [InlineData("15", "200", "tps")]
    public void Recusa_valores_comuns_diferentes_dos_oficiais(string timeout, string tps, string campo)
    {
        var erro = Assert.Throws<TabelaInvalidaException>(() => TabelaDeEndpoints.Interpretar(Json(timeout: timeout, tps: tps)));

        Assert.Contains(campo, erro.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ nao e json")]
    [InlineData("{\"timeoutSegundos\":15,\"tps\":300}")]
    [InlineData("[]")]
    public void Recusa_json_invalido(string json) =>
        Assert.Throws<TabelaInvalidaException>(() => TabelaDeEndpoints.Interpretar(json));

    [Fact]
    public void Recusa_arquivo_ausente()
    {
        var erro = Assert.Throws<TabelaInvalidaException>(() => TabelaDeEndpoints.Ler(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json")));

        Assert.Contains("não encontrado", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Excecao_tem_construtores_padrao()
    {
        Assert.NotNull(new TabelaInvalidaException().Message);
        Assert.Equal("x", new TabelaInvalidaException("x", new InvalidOperationException()).Message);
        Assert.NotNull(new TravaInvalidaException().Message);
    }
}
