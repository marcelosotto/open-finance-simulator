using System.Text.Json.Nodes;
using Simulador.Dados.Aplicacao;
using Simulador.Massa.Contratos;

namespace Simulador.Testes.Unidade.Dados;

/// <summary>Correções da revisão da spec 005.</summary>
public class RevisaoDosDadosTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Id_longo_mantem_o_prefixo_oficial_e_o_tamanho()
    {
        var schema = JsonNode.Parse("""{"type":"object","required":["personalId"],"properties":{"personalId":{"type":"string","maxLength":100}}}""");
        var item = new Preenchimento(new Dictionary<string, string>(StringComparer.Ordinal), "hml-p-" + new string('a', 200), 1);

        var id = GeradorPorSchema.Gerar(schema, item)!["personalId"]!.GetValue<string>();

        Assert.StartsWith("hml-p-", id, StringComparison.Ordinal);
        Assert.EndsWith("-personalId", id, StringComparison.Ordinal);
        Assert.True(id.Length <= 100);
    }

    [Fact]
    public void Numero_usa_o_valor_da_massa()
    {
        var schema = JsonNode.Parse("""{"type":"object","properties":{"payeeMCC":{"type":"number","example":5137}}}""");
        var item = new Preenchimento(new Dictionary<string, string>(StringComparer.Ordinal) { ["payeeMCC"] = "4321" }, "b", 1);

        Assert.Equal("4321", GeradorPorSchema.Gerar(schema, item)!["payeeMCC"]!.ToString());
    }

    [Fact]
    public void Ajuste_vale_o_que_tinha_no_instante()
    {
        var ajuste = new Ajuste("a", "transacao", false, Valor: 3m, AtualizadoEm: T0.AddHours(2), ValoresAnteriores: [new ValorAnterior(1m, T0, T0.AddHours(1)), new ValorAnterior(2m, T0.AddHours(1), T0.AddHours(2))]);

        Assert.Null(NoInstante.Valor(ajuste, T0.AddHours(-1)));
        Assert.Equal(1m, NoInstante.Valor(ajuste, T0.AddMinutes(30)));
        Assert.Equal(2m, NoInstante.Valor(ajuste, T0.AddMinutes(90)));
        Assert.Equal(3m, NoInstante.Valor(ajuste, T0.AddHours(2)));
        Assert.Null(NoInstante.Valor(ajuste with { ValoresAnteriores = null }, T0));
        Assert.Equal(3m, NoInstante.Valor(ajuste with { AtualizadoEm = null }, T0));
    }

    [Fact]
    public void Estado_vale_o_que_tinha_no_instante()
    {
        EstadoAnterior[] anteriores = [new("A", T0.AddHours(1)), new("B", T0.AddHours(2))];

        Assert.Equal("A", NoInstante.Estado("C", anteriores, T0));
        Assert.Equal("B", NoInstante.Estado("C", anteriores, T0.AddMinutes(90)));
        Assert.Equal("C", NoInstante.Estado("C", anteriores, T0.AddHours(2)));
        Assert.Equal("C", NoInstante.Estado("C", null, T0));
    }
}
