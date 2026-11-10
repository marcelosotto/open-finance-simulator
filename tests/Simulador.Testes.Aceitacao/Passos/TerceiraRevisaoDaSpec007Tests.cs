using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Achado B da 3ª revisão da spec 007: ajuste de valor feito depois da emissão, com relógio do banco alterado (RN37).</summary>
public sealed class TerceiraRevisaoDaSpec007Tests : IDisposable
{
    private readonly ContextoDeComportamento _c = new();

    public void Dispose() => _c.Dispose();

    private DateTimeOffset Agora => _c.Fabrica.Relogio.GetUtcNow();

    /// <summary>Banco fixado: o ajuste feito depois da página 1 é carimbado com o mesmo instante do banco, mas não aparece na página 2.</summary>
    [Fact]
    public async Task Banco_fixado_nao_mostra_ajuste_novo_na_pagina_seguinte()
    {
        await Preparar();
        await _c.MudarRelogio(new { fixadoEm = Agora });
        var proxima = await PrimeiraPagina();

        _c.Fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        await Ajustar();

        await AssertSemAjusteNaPagina(proxima);
    }

    /// <summary>Banco retrocedido depois da emissão: o ajuste fica carimbado antes da emissão no banco, mas não aparece na página 2.</summary>
    [Fact]
    public async Task Banco_retrocedido_nao_mostra_ajuste_novo_na_pagina_seguinte()
    {
        await Preparar();
        var proxima = await PrimeiraPagina();

        _c.Fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        await _c.MudarRelogio(new { fixadoEm = Agora.AddMinutes(-30) });
        await Ajustar();

        await AssertSemAjusteNaPagina(proxima);
    }

    private async Task Preparar()
    {
        await _c.Preparar("Banco Azul");
        await _c.Configurar(new { limites = new { limiteOperacionalLigado = false } });
    }

    private async Task<Uri> PrimeiraPagina()
    {
        await _c.Chamar(new Uri(_c.Em("/accounts/{accountId}/transactions") + "?page-size=1", UriKind.Relative));
        _c.Status(200);
        using var corpo = JsonDocument.Parse(_c.Corpo);
        Assert.Single(corpo.RootElement.GetProperty("data").EnumerateArray());
        Assert.True(corpo.RootElement.GetProperty("links").TryGetProperty("next", out _), _c.Corpo);
        return new Uri(new Uri(corpo.RootElement.GetProperty("links").GetProperty("next").GetString()!).PathAndQuery, UriKind.Relative);
    }

    /// <summary>Ajusta todas as transações do dia, para que a da página 2 seja uma delas.</summary>
    private async Task Ajustar()
    {
        var dia = Agora.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var lidas = JsonDocument.Parse(await _c.Admin.GetStringAsync(new Uri($"/admin/massa/recursos/{_c.Conta}/transacoes?de={dia}", UriKind.Relative), TestContext.Current.CancellationToken));
        var posicoes = lidas.RootElement.GetProperty("transacoes").EnumerateArray().Select(t => t.GetProperty("posicao").GetInt32()).ToList();
        Assert.Equal(2, posicoes.Count);
        foreach (var (posicao, valor) in posicoes.Zip([111.11m, 222.22m]))
        {
            using var resposta = await _c.Admin.PostAsJsonAsync(new Uri("/admin/massa/ajustes", UriKind.Relative), new { tipo = "transacao", recursoId = _c.Conta, dia, posicao, valor }, TestContext.Current.CancellationToken);
            Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
    }

    private async Task AssertSemAjusteNaPagina(Uri proxima)
    {
        await _c.Chamar(proxima);
        _c.Status(200);
        Assert.DoesNotContain("111.11", _c.Corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("222.22", _c.Corpo, StringComparison.Ordinal);
    }
}
