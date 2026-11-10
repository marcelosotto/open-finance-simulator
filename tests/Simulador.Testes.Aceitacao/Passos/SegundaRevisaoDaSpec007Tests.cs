using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Achados da 2ª revisão da spec 007 na lista paginada de recursos (RN37), provados pela API.</summary>
public sealed class SegundaRevisaoDaSpec007Tests : IDisposable
{
    private readonly ContextoDeComportamento _c = new();

    public void Dispose() => _c.Dispose();

    private DateTimeOffset Agora => _c.Fabrica.Relogio.GetUtcNow();

    private Uri PrimeiraPagina => new(_c.Em("/accounts") + "?page-size=1", UriKind.Relative);

    /// <summary>Achado 2: com o relógio fixado, empréstimo criado depois da emissão não entra nas páginas seguintes.</summary>
    [Fact]
    public async Task Relogio_fixado_nao_poe_recurso_novo_na_pagina_seguinte()
    {
        await Preparar(contasExtras: 0);
        await _c.MudarRelogio(new { fixadoEm = Agora });
        await _c.AdicionarProduto("emprestimos");
        await _c.AdicionarProduto("emprestimos");
        var emprestimos = new Uri(AtalhosDeConsentimento.Caminho(_c.Banco, "loans", "/contracts").ToString(), UriKind.Relative);
        var (proxima, total) = await Pagina(new Uri(emprestimos + "?page-size=1", UriKind.Relative));
        Assert.Equal(2, total);

        var novo = await _c.AdicionarProduto("emprestimos");
        var consentimento = await _c.Fabrica.Services.GetRequiredService<IConsultaDeMassa>().ObterConsentimento(_c.ConsentId, TestContext.Current.CancellationToken);
        Assert.Contains(novo, consentimento!.Recursos);
        await _c.Chamar(emprestimos);
        Assert.Contains(novo, _c.Corpo, StringComparison.Ordinal);
        var (depois, totalDepois) = await Pagina(proxima!);

        Assert.Equal(2, totalDepois);
        Assert.Null(depois);
    }

    /// <summary>Achado 2: fixar o relógio adiante depois da emissão não some com as contas que já estavam na lista.</summary>
    [Fact]
    public async Task Relogio_fixado_depois_da_chave_nao_some_com_contas()
    {
        await Preparar(contasExtras: 1);
        var (proxima, _) = await Pagina(PrimeiraPagina);
        var primeira = Contas()[0];

        await _c.MudarRelogio(new { fixadoEm = Agora.AddMinutes(30) });
        var (_, total) = await Pagina(proxima!);

        Assert.Equal(2, total);
        Assert.NotEqual(primeira, Assert.Single(Contas()));
    }

    /// <summary>Achado 5: regra de estado criada depois da emissão não tira conta das páginas seguintes.</summary>
    [Fact]
    public async Task Regra_de_estado_criada_depois_da_emissao_nao_vale_na_pagina_seguinte()
    {
        await Preparar(contasExtras: 1);
        var (proxima, _) = await Pagina(PrimeiraPagina);
        var primeira = Contas()[0];

        await _c.CriarRegra(new { cliente = _c.Documento, efeito = new { tipo = "estado", estado = "TEMPORARILY_UNAVAILABLE" } });
        var (_, total) = await Pagina(proxima!);

        Assert.Equal(2, total);
        Assert.NotEqual(primeira, Assert.Single(Contas()));
    }

    /// <summary>Achado 5: regra de estado apagada depois da emissão continua valendo nas páginas seguintes.</summary>
    [Fact]
    public async Task Regra_de_estado_apagada_depois_da_emissao_continua_valendo()
    {
        await Preparar(contasExtras: 2);
        using var criada = await _c.Admin.PostAsJsonAsync(new Uri("/admin/cenarios/regras", UriKind.Relative), new { recurso = _c.Conta, efeito = new { tipo = "estado", estado = "TEMPORARILY_UNAVAILABLE" } }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        using var regra = JsonDocument.Parse(await criada.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var (proxima, total) = await Pagina(PrimeiraPagina);
        Assert.Equal(2, total);

        using var apagada = await _c.Admin.DeleteAsync(new Uri($"/admin/cenarios/regras/{regra.RootElement.GetProperty("id").GetString()}", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.True(apagada.IsSuccessStatusCode);
        var (_, totalDepois) = await Pagina(proxima!);

        Assert.Equal(2, totalDepois);
        Assert.NotEqual(_c.Conta, Assert.Single(Contas()));
    }

    private async Task Preparar(int contasExtras)
    {
        await _c.Preparar("Banco Azul", contasExtras: contasExtras);
        await _c.Configurar(new { limites = new { limiteOperacionalLigado = false } });
    }

    private async Task<(Uri? Proxima, int Total)> Pagina(Uri caminho)
    {
        await _c.Chamar(caminho);
        _c.Status(200);
        using var corpo = JsonDocument.Parse(_c.Corpo);
        var raiz = corpo.RootElement;
        var proxima = raiz.GetProperty("links").TryGetProperty("next", out var link) ? new Uri(new Uri(link.GetString()!).PathAndQuery, UriKind.Relative) : null;
        return (proxima, raiz.GetProperty("meta").GetProperty("totalRecords").GetInt32());
    }

    private List<string> Contas()
    {
        using var corpo = JsonDocument.Parse(_c.Corpo);
        return [.. corpo.RootElement.GetProperty("data").EnumerateArray().Select(c => c.GetProperty("accountId").GetString()!)];
    }
}
