using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Simulador.Catalogo.Contratos;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Catalogo;

public sealed partial class RotasGeradasTests(FabricaCompartilhada compartilhada) : IClassFixture<FabricaCompartilhada>
{
    public static TheoryData<string, string, string> TodasAsOperacoes()
    {
        var dados = new TheoryData<string, string, string>();
        var trava = TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json"));
        foreach (var operacao in ServicosDeSpecs.LerOperacoes(trava, Repositorio.PastaDasSpecs()))
        {
            dados.Add(operacao.OperationId, operacao.Metodo, operacao.CaminhoPublicado);
        }

        return dados;
    }

    /// <summary>Preenche <c>{banco}</c> e os parâmetros de caminho.</summary>
    public static Uri Preencher(string caminhoPublicado, string banco = "banco-teste") =>
        new(ParametroDeCaminho().Replace(caminhoPublicado.Replace("{banco}", banco, StringComparison.Ordinal), "teste-1"), UriKind.Relative);

    [Theory]
    [MemberData(nameof(TodasAsOperacoes))]
    public async Task Toda_operacao_do_catalogo_responde_501(string operationId, string metodo, string caminhoPublicado)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), Preencher(caminhoPublicado));

        using var resposta = await compartilhada.Fabrica.ClienteOpenFinance().SendAsync(requisicao, TestContext.Current.CancellationToken);

        Assert.True(resposta.StatusCode == HttpStatusCode.NotImplemented, $"{operationId}: {resposta.StatusCode}");
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("NAO_IMPLEMENTADO", corpo.RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Equal("2026-11-05T12:00:00Z", corpo.RootElement.GetProperty("meta").GetProperty("requestDateTime").GetString());
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex ParametroDeCaminho();
}

/// <summary>Um host compartilhado pelos testes de uma classe.</summary>
public sealed class FabricaCompartilhada : IDisposable
{
    public FabricaDoSimulador Fabrica { get; } = new();

    public void Dispose() => Fabrica.Dispose();
}
