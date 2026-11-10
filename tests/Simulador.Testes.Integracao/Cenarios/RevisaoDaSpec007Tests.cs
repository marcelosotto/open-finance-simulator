using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Catalogo.Contratos;
using Simulador.Host;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Cenarios;

/// <summary>Achados da revisão independente da spec 007, no host.</summary>
public sealed class RevisaoDaSpec007Tests : IDisposable
{
    private readonly FabricaDoSimulador _fabrica = new();

    public RevisaoDaSpec007Tests() => _fabrica.ClienteAdministracao().Dispose();

    public void Dispose() => _fabrica.Dispose();

    /// <summary>Achado 4 (RN73): a chamada tem um instante único também em hora real.</summary>
    [Fact]
    public async Task Filtro_grava_o_instante_mesmo_sem_relogio_alterado()
    {
        var acesso = _fabrica.Services.GetRequiredService<IHttpContextAccessor>();
        var contexto = new DefaultHttpContext { RequestServices = _fabrica.Services };
        contexto.Request.RouteValues["banco"] = "banco-estavel";
        acesso.HttpContext = contexto;
        var filtro = _fabrica.Services.GetRequiredService<FiltroDaChamada>();

        await filtro.InvokeAsync(new DefaultEndpointFilterInvocationContext(contexto), _ => ValueTask.FromResult<object?>(null));

        Assert.Equal(_fabrica.Relogio.GetUtcNow(), contexto.Items[RelogioDaChamada.ItemDoInstante]);
        Assert.False(contexto.Response.Headers.ContainsKey("x-sim-clock"));
    }

    /// <summary>Achado 11: em nota de negociação, o alcance por recurso usa o recurso dono da nota.</summary>
    [Fact]
    public void Recurso_da_nota_de_negociacao_e_o_recurso_dono()
    {
        var operacao = new OperacaoDoCatalogo("variable-incomes", "1.3.0", "/open-banking/variable-incomes/v1", "GET", "/broker-notes/{brokerNoteId}", "notas", [], [], new Dictionary<string, string>());
        var contexto = new DefaultHttpContext();
        contexto.Request.RouteValues["brokerNoteId"] = Simulador.Dados.Dominio.IdsDerivados.DaNota("hml-p-r1");

        Assert.Equal("hml-p-r1", ComportamentoDaChamada.RecursoDaChamada(contexto, operacao));
    }
}
