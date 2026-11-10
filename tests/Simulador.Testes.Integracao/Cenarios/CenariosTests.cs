using System.Net;
using System.Net.Http.Json;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Persistencia;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Cenarios;

/// <summary>Repositório em MongoDB real (ADR-009).</summary>
public sealed class RepositorioDeCenariosMongoTests : IDisposable
{
    private readonly RepositorioMongo _repositorio = new(MongoDeTeste.Conexao, MongoDeTeste.NovaBase());
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public void Dispose() => _repositorio.Dispose();

    [Fact]
    public async Task Grava_le_e_apaga_regras_e_pacotes()
    {
        var agora = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        var regra = new RegraDeCenario("r-1", "nome", "banco", null, "cli", null, new EfeitoDaRegra("status", Status: 500), 50m, agora, agora.AddHours(1), 7, true, "bf", agora, agora);
        await _repositorio.SalvarRegra(regra, _ct);
        await _repositorio.SalvarPacote(new PacoteDeCenario("bf", true, agora), _ct);

        Assert.Equal(regra, await _repositorio.ObterRegra("r-1", _ct));
        Assert.Single(await _repositorio.ListarRegras(_ct));
        Assert.True((await _repositorio.ObterPacote("bf", _ct))!.Ligado);
        Assert.Single(await _repositorio.ListarPacotes(_ct));

        await _repositorio.SalvarRegra(regra with { Ligada = false }, _ct);
        Assert.False((await _repositorio.ObterRegra("r-1", _ct))!.Ligada);

        await _repositorio.ApagarRegra("r-1", _ct);
        await _repositorio.ApagarPacote("bf", _ct);
        Assert.Null(await _repositorio.ObterRegra("r-1", _ct));
        Assert.Null(await _repositorio.ObterPacote("bf", _ct));
        Assert.Empty(await _repositorio.ListarRegras(_ct));
    }
}

/// <summary>API de administração de cenários e relógio (contracts/admin-cenarios.md).</summary>
public sealed class AdminDeCenariosTests : IDisposable
{
    private readonly FabricaDoSimulador _fabrica = new FabricaDoSimulador().ComComportamento();
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public AdminDeCenariosTests() => _admin = _fabrica.ClienteAdministracao();

    public void Dispose() => _fabrica.Dispose();

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    [Fact]
    public async Task Regra_tem_ciclo_completo_e_vai_para_o_historico()
    {
        using var criada = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), new { instituicao = "banco-estavel", efeito = new { tipo = "status", status = 500 } }, _ct);
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        var regra = (await criada.Content.ReadFromJsonAsync<RegraDeCenario>(JsonDosCenarios.Opcoes, _ct))!;

        using var lida = await _admin.GetAsync(U($"/admin/cenarios/regras/{regra.Id}"), _ct);
        Assert.Equal(HttpStatusCode.OK, lida.StatusCode);
        using var lista = await _admin.GetAsync(U("/admin/cenarios/regras"), _ct);
        Assert.Contains(regra.Id, await lista.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);

        using var alterada = await _admin.PutAsJsonAsync(U($"/admin/cenarios/regras/{regra.Id}"), new { instituicao = "banco-estavel", efeito = new { tipo = "status", status = 504 } }, _ct);
        Assert.Equal(HttpStatusCode.OK, alterada.StatusCode);
        using var desligada = await _admin.PostAsync(U($"/admin/cenarios/regras/{regra.Id}/desligar"), null, _ct);
        Assert.False((await desligada.Content.ReadFromJsonAsync<RegraDeCenario>(JsonDosCenarios.Opcoes, _ct))!.Ligada);
        using var ligada = await _admin.PostAsync(U($"/admin/cenarios/regras/{regra.Id}/ligar"), null, _ct);
        Assert.True((await ligada.Content.ReadFromJsonAsync<RegraDeCenario>(JsonDosCenarios.Opcoes, _ct))!.Ligada);

        using var apagada = await _admin.DeleteAsync(U($"/admin/cenarios/regras/{regra.Id}"), _ct);
        Assert.Equal(HttpStatusCode.NoContent, apagada.StatusCode);
        var historico = await _admin.GetStringAsync(U("/admin/instituicoes/banco-estavel/historico"), _ct);
        Assert.Contains("regra-apagada", historico, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recusa_regra_invalida_e_responde_404_e_409()
    {
        using var semFiltro = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), new { efeito = new { tipo = "status", status = 500 } }, _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semFiltro.StatusCode);
        using var naoDeclarado = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), new { cliente = "1", efeito = new { tipo = "status", status = 418 } }, _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, naoDeclarado.StatusCode);
        using var semPacote = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), new { cliente = "1", efeito = new { tipo = "status", status = 500 }, pacote = "x" }, _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, semPacote.StatusCode);
        using var corpoRuim = await _admin.PostAsync(U("/admin/cenarios/regras"), new StringContent("{", System.Text.Encoding.UTF8, "application/json"), _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corpoRuim.StatusCode);
        using var alterarRuim = await _admin.PutAsync(U("/admin/cenarios/regras/r-x"), new StringContent("{", System.Text.Encoding.UTF8, "application/json"), _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, alterarRuim.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync(U("/admin/cenarios/regras/r-x"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PutAsJsonAsync(U("/admin/cenarios/regras/r-x"), new { cliente = "1", efeito = new { tipo = "timeout" } }, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.DeleteAsync(U("/admin/cenarios/regras/r-x"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PostAsync(U("/admin/cenarios/regras/r-x/ligar"), null, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PostAsync(U("/admin/cenarios/pacotes/x/ligar"), null, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.DeleteAsync(U("/admin/cenarios/pacotes/x"), _ct)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsync(U("/admin/cenarios/pacotes/bf"), null, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsync(U("/admin/cenarios/pacotes/bf"), null, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), new { cliente = "1", efeito = new { tipo = "timeout" }, pacote = "bf" }, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _admin.DeleteAsync(U("/admin/cenarios/pacotes/bf"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _admin.PostAsync(U("/admin/cenarios/pacotes/bf/desligar"), null, _ct)).StatusCode);
        Assert.Contains("bf", await _admin.GetStringAsync(U("/admin/cenarios/pacotes"), _ct), StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsync(U("/admin/cenarios/pacotes/vazio"), null, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _admin.DeleteAsync(U("/admin/cenarios/pacotes/vazio"), _ct)).StatusCode);
    }

    [Fact]
    public async Task Relogio_aceita_fixar_ou_acelerar_e_recusa_o_resto()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _admin.PutAsJsonAsync(U("/admin/instituicoes/banco-estavel/relogio"), new { }, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _admin.PutAsJsonAsync(U("/admin/instituicoes/banco-estavel/relogio"), new { fator = 0 }, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _admin.PutAsJsonAsync(U("/admin/instituicoes/banco-estavel/relogio"), new { fator = 100_001 }, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _admin.PutAsync(U("/admin/instituicoes/banco-estavel/relogio"), new StringContent("{", System.Text.Encoding.UTF8, "application/json"), _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PutAsJsonAsync(U("/admin/instituicoes/nao-existe/relogio"), new { fator = 2 }, _ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.DeleteAsync(U("/admin/instituicoes/nao-existe/relogio"), _ct)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await _admin.PutAsJsonAsync(U("/admin/instituicoes/banco-estavel/relogio"), new { fixadoEm = "2030-01-01T00:00:00Z" }, _ct)).StatusCode);
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        using var acelerado = await _admin.PutAsJsonAsync(U("/admin/instituicoes/banco-estavel/relogio"), new { fator = 60 }, _ct);
        Assert.Contains("2030-01-01T00:00:00", await acelerado.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await _admin.DeleteAsync(U("/admin/instituicoes/banco-estavel/relogio"), _ct)).StatusCode);
        Assert.Contains("relogio-alterado", await _admin.GetStringAsync(U("/admin/instituicoes/banco-estavel/historico"), _ct), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, 3600, 0)]
    [InlineData(60, 60, 3600)]
    [InlineData(2, -30, -60)]
    public void Relogio_simulado_conta_do_ponto_de_partida(int fator, int segundosReais, int segundosSimulados)
    {
        var baseReal = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        var baseSimulada = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var relogio = new Simulador.Instituicoes.Contratos.RelogioSimulado(baseSimulada, baseReal, fator);
        Assert.Equal(baseSimulada.AddSeconds(segundosSimulados), relogio.Agora(baseReal.AddSeconds(segundosReais)));
    }

    [Fact]
    public void Relogio_acelerado_para_no_maior_instante()
    {
        var baseReal = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(DateTimeOffset.MaxValue.UtcTicks, new Simulador.Instituicoes.Contratos.RelogioSimulado(baseReal, baseReal, 100_000m).Agora(baseReal.AddYears(7000)).UtcTicks);
    }
}
