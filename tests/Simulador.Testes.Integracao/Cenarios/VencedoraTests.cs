using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Cenarios;

/// <summary>Qual regra vence numa chamada, pela administração (spec 010, UI06, Q9).</summary>
public sealed class VencedoraTests : IDisposable
{
    private const string Banco = "banco-estavel";
    private const string Endpoint = "accountsGetAccounts";
    private readonly FabricaDoSimulador _fabrica = new FabricaDoSimulador().ComComportamento().ComContadoresEmMemoria();
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public VencedoraTests() => _admin = _fabrica.ClienteAdministracao();

    public void Dispose() => _fabrica.Dispose();

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    private async Task<string> Criar(object regra)
    {
        using var resposta = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), regra, _ct);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct));
        return json.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<JsonElement> Vencedora(string consulta)
    {
        using var resposta = await _admin.GetAsync(U("/admin/cenarios/regras/vencedora?" + consulta), _ct);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct)).RootElement.Clone();
    }

    [Fact]
    public async Task A_regra_de_endpoint_vence_a_de_banco_e_estado_e_defeito_valem_a_parte()
    {
        await Criar(new { instituicao = Banco, efeito = new { tipo = "latencia", latenciaMs = 3000 } });
        var deEndpoint = await Criar(new { instituicao = Banco, endpoint = Endpoint, efeito = new { tipo = "status", status = 500 }, frequenciaPct = 10, semente = 7 });
        var deEstado = await Criar(new { instituicao = Banco, recurso = "conta-1", efeito = new { tipo = "estado", estado = "UNAVAILABLE" } });
        var deDefeito = await Criar(new { instituicao = Banco, efeito = new { tipo = "defeito", defeito = "total-records-incoerente" } });

        var sem = await Vencedora($"instituicao={Banco}&endpoint={Endpoint}");
        Assert.Equal(deEndpoint, sem.GetProperty("comportamento").GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.Null, sem.GetProperty("estado").ValueKind);
        Assert.Equal(deDefeito, sem.GetProperty("defeito").GetProperty("id").GetString());

        var comRecurso = await Vencedora($"instituicao={Banco}&endpoint={Endpoint}&recurso=conta-1&cliente=");
        Assert.Equal(deEstado, comRecurso.GetProperty("estado").GetProperty("id").GetString());

        // Nada é sorteado: a sequência da regra com semente não avança.
        Assert.DoesNotContain(((ContadoresEmMemoria)_fabrica.Contadores).Chaves, c => c.StartsWith("seq:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Status_nao_declarado_pelo_endpoint_nao_vence_e_desligada_nao_conta()
    {
        var latencia = await Criar(new { instituicao = Banco, efeito = new { tipo = "latencia", latenciaMs = 10 } });
        var desligada = await Criar(new { instituicao = Banco, endpoint = Endpoint, efeito = new { tipo = "latencia", latenciaMs = 20 } });
        Assert.Equal(HttpStatusCode.OK, (await _admin.PostAsync(U($"/admin/cenarios/regras/{desligada}/desligar"), null, _ct)).StatusCode);

        var vencedora = await Vencedora($"instituicao={Banco}&endpoint={Endpoint}");
        Assert.Equal(latencia, vencedora.GetProperty("comportamento").GetProperty("id").GetString());
    }

    [Fact]
    public async Task O_instante_e_a_hora_do_banco()
    {
        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsJsonAsync(U($"/admin/instituicoes/{Banco}/relogio"), new { fixadoEm = "2030-01-01T00:00:00Z" }, _ct)).StatusCode);
        await Criar(new { instituicao = Banco, efeito = new { tipo = "latencia", latenciaMs = 10 }, fim = "2029-01-01T00:00:00Z" });

        var vencedora = await Vencedora($"instituicao={Banco}&endpoint={Endpoint}");
        Assert.Equal(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero), vencedora.GetProperty("instante").GetDateTimeOffset());
        Assert.Equal(JsonValueKind.Null, vencedora.GetProperty("comportamento").ValueKind);
    }

    [Theory]
    [InlineData("endpoint=accountsGetAccounts", HttpStatusCode.UnprocessableEntity)]
    [InlineData("instituicao=banco-estavel", HttpStatusCode.UnprocessableEntity)]
    [InlineData("instituicao=banco-estavel&endpoint=naoExiste", HttpStatusCode.UnprocessableEntity)]
    [InlineData("instituicao=nao-existe&endpoint=accountsGetAccounts", HttpStatusCode.NotFound)]
    public async Task Recusa_pedido_incompleto_endpoint_desconhecido_e_banco_inexistente(string consulta, HttpStatusCode esperado)
    {
        using var resposta = await _admin.GetAsync(U("/admin/cenarios/regras/vencedora?" + consulta), _ct);
        Assert.Equal(esperado, resposta.StatusCode);
        Assert.Contains("erros", await resposta.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
    }
}
