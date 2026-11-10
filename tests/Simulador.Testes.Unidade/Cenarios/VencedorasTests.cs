using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;

namespace Simulador.Testes.Unidade.Cenarios;

/// <summary>As três vencedoras de uma chamada, sem sorteio (spec 010, UI06, RN15).</summary>
public sealed class VencedorasTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly HashSet<int> Declarados = [500];
    private static readonly ChamadaAvaliada Chamada = new("banco", "saldos", "cli", "conta");

    private static RegraDeCenario Regra(string id, EfeitoDaRegra efeito, string? endpoint = null, string? recurso = null, DateTimeOffset? fim = null) =>
        new(id, null, "banco", endpoint, null, recurso, efeito, 1m, null, fim, null, true, null, Agora, Agora);

    [Fact]
    public void Cada_grupo_tem_a_sua_vencedora()
    {
        RegraDeCenario[] regras =
        [
            Regra("lat", new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: 10)),
            Regra("sta", new EfeitoDaRegra(Efeitos.Status, Status: 500), endpoint: "saldos"),
            Regra("est", new EfeitoDaRegra(Efeitos.Estado, Estado: "UNAVAILABLE"), recurso: "conta"),
            Regra("def", new EfeitoDaRegra(Efeitos.Defeito, Defeito: "enum-invalido")),
        ];

        var vencedoras = SelecaoDeRegras.Vencedoras(regras, Chamada, Agora, Declarados);

        Assert.Equal(Agora, vencedoras.Instante);
        Assert.Equal("sta", vencedoras.Comportamento?.Id);
        Assert.Equal("est", vencedoras.Estado?.Id);
        Assert.Equal("def", vencedoras.Defeito?.Id);
    }

    [Fact]
    public void Status_nao_declarado_e_regra_vencida_nao_vencem()
    {
        RegraDeCenario[] regras =
        [
            Regra("sta", new EfeitoDaRegra(Efeitos.Status, Status: 429), endpoint: "saldos"),
            Regra("velha", new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: 10), recurso: "conta", fim: Agora),
        ];

        var vencedoras = SelecaoDeRegras.Vencedoras(regras, Chamada, Agora, Declarados);

        Assert.Null(vencedoras.Comportamento);
        Assert.Null(vencedoras.Estado);
        Assert.Null(vencedoras.Defeito);
    }
}
