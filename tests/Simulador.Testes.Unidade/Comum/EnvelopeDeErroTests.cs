using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Simulador.Comum.Erros;

namespace Simulador.Testes.Unidade.Comum;

public class EnvelopeDeErroTests
{
    [Fact]
    public void Serializa_no_formato_oficial_com_a_hora_do_relogio()
    {
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        var resposta = EnvelopeDeErro.Criar("NAO_IMPLEMENTADO", "Título", "Detalhe", relogio);
        var json = JsonSerializer.Serialize(resposta, EnvelopeDeErro.OpcoesJson);

        using var doc = JsonDocument.Parse(json);
        var erro = doc.RootElement.GetProperty("errors")[0];
        Assert.Equal("NAO_IMPLEMENTADO", erro.GetProperty("code").GetString());
        Assert.Equal("Título", erro.GetProperty("title").GetString());
        Assert.Equal("Detalhe", erro.GetProperty("detail").GetString());
        Assert.Equal("2026-11-05T12:00:00Z", doc.RootElement.GetProperty("meta").GetProperty("requestDateTime").GetString());
    }

    [Fact]
    public void Converte_hora_local_para_utc()
    {
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 11, 5, 9, 0, 0, TimeSpan.FromHours(-3)));

        var resposta = EnvelopeDeErro.Criar("X", "T", "D", relogio);

        Assert.Equal("2026-11-05T12:00:00Z", resposta.Meta.RequestDateTime);
    }
}
