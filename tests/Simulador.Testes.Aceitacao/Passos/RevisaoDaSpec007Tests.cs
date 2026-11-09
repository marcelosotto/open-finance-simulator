using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Achados da revisão independente da spec 007, provados pela API.</summary>
public sealed class RevisaoDaSpec007Tests : IDisposable
{
    private readonly ContextoDeComportamento _c = new();

    public void Dispose() => _c.Dispose();

    private DateTimeOffset Agora => _c.Fabrica.Relogio.GetUtcNow();

    private static Uri ComConsulta(Uri caminho, string consulta) => new(caminho + consulta, UriKind.Relative);

    /// <summary>Achado 1 (RN37, RN108): na página seguinte, a regra de estado vale pelo instante de emissão da chave.</summary>
    [Fact]
    public async Task Regra_de_estado_na_pagina_seguinte_usa_o_instante_da_chave()
    {
        await _c.Preparar("Banco Azul", contasExtras: 1);
        await _c.Configurar(new { limites = new { limiteOperacionalLigado = false } });
        var contas = _c.Em("/accounts");
        await _c.Chamar(ComConsulta(contas, "?page-size=1"));
        _c.Status(200);
        using var primeira = JsonDocument.Parse(_c.Corpo);
        var segundaConta = await OutraConta(primeira.RootElement.GetProperty("data")[0].GetProperty("accountId").GetString()!);
        var proxima = primeira.RootElement.GetProperty("links").GetProperty("next").GetString()!;
        await _c.CriarRegra(new { recurso = segundaConta, efeito = new { tipo = "estado", estado = "TEMPORARILY_UNAVAILABLE" }, inicio = Agora.AddMinutes(1) });
        _c.Fabrica.Relogio.Advance(TimeSpan.FromMinutes(2));

        await _c.Chamar(new Uri(new Uri(proxima).PathAndQuery, UriKind.Relative));

        _c.Status(200);
        using var segunda = JsonDocument.Parse(_c.Corpo);
        Assert.Equal(segundaConta, segunda.RootElement.GetProperty("data")[0].GetProperty("accountId").GetString());
    }

    private async Task<string> OutraConta(string primeira)
    {
        var consulta = _c.Fabrica.Services.GetRequiredService<IConsultaDeMassa>();
        var consentimento = await consulta.ObterConsentimento(_c.ConsentId, TestContext.Current.CancellationToken);
        return consentimento!.Recursos.First(r => r != primeira);
    }

    /// <summary>Achado 3 (Q17): relógio antes da criação conta como zero decorrido; sem preparação, a lista sai.</summary>
    [Fact]
    public async Task Recursos_com_relogio_antes_da_criacao_nao_ficam_em_202()
    {
        await _c.Preparar("Banco Azul");
        await _c.MudarRelogio(new { fixadoEm = "2020-01-01T00:00:00Z" });
        await _c.Chamar(new Uri(AtalhosDeConsentimento.Caminho(_c.Banco, "resources", "/resources").ToString(), UriKind.Relative));
        _c.Status(200);
    }

    /// <summary>Achado 5 (RN37, RN73): com o relógio no passado, a conta do acesso direto também aparece na lista.</summary>
    [Fact]
    public async Task Relogio_no_passado_nao_some_com_a_conta_da_lista()
    {
        await _c.Preparar("Banco Azul");
        await _c.MudarRelogio(new { fixadoEm = "2020-01-01T00:00:00Z" });
        await _c.Chamar(_c.Em("/accounts/{accountId}"));
        _c.Status(200);
        await _c.Chamar(_c.Em("/accounts"));
        _c.Status(200);
        Assert.Contains(_c.Conta, _c.Corpo, StringComparison.Ordinal);
    }

    /// <summary>Achado 6 (RN17): resposta sem corpo não recebe JSON inválido.</summary>
    [Fact]
    public async Task Json_invalido_nao_age_em_resposta_sem_corpo()
    {
        await _c.Preparar("Banco Azul");
        await _c.CriarRegra(new { instituicao = _c.Banco, endpoint = "consentsDeleteConsentsConsentId", efeito = new { tipo = "transporte", falha = "json-invalido" } });
        using var resposta = await AtalhosDeConsentimento.Chamar(_c.Api, HttpMethod.Delete, AtalhosDeConsentimento.Caminho(_c.Banco, "consents", "/consents/{consentId}", _c.ConsentId), await _c.NovoToken("consents"));
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(await resposta.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Achado 7 (ADR-008): a cota já está contada quando a espera simulada começa.</summary>
    [Fact]
    public async Task Cota_e_contada_antes_da_espera()
    {
        await _c.Preparar("Banco Azul");
        await _c.CriarRegra(new { instituicao = _c.Banco, endpoint = ContextoDeComportamento.SaldosId, efeito = new { tipo = "latencia", latenciaMs = 100 } });
        var contadores = (ContadoresEmMemoria)_c.Fabrica.Contadores;
        long? naEspera = null;
        _c.Fabrica.Esperas.AoEsperar = () => naEspera = contadores.TotalDaCota;
        await _c.Chamar(_c.Saldos);
        _c.Status(200);
        Assert.Equal(1, naEspera);
    }

    /// <summary>Achado 8 (Q7): ligar produto a consentimento confere a validade pela hora do banco.</summary>
    [Fact]
    public async Task Produto_novo_confere_a_validade_pela_hora_do_banco()
    {
        await _c.Preparar("Banco Azul", expiraEm: Agora.AddHours(1));
        await _c.MudarRelogio(new { fixadoEm = Agora.AddHours(2) });
        var emprestimo = await _c.AdicionarProduto("emprestimos");
        var consentimento = await _c.Fabrica.Services.GetRequiredService<IConsultaDeMassa>().ObterConsentimento(_c.ConsentId, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(emprestimo, consentimento!.Recursos);
    }
}
