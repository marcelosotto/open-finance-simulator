using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Massa;

/// <summary>Clientes de um banco e resumo da massa, para a tela escolher cliente sem digitar (spec 010, UI18, UI19).</summary>
public abstract class ClientesDoBancoTestsBase : IDisposable
{
    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    protected ClientesDoBancoTestsBase(FabricaDoSimulador fabrica)
    {
        _fabrica = fabrica;
        _admin = fabrica.ClienteAdministracao();
    }

    public void Dispose()
    {
        _fabrica.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    private async Task<string> CriarCliente(string tipo, params string[] bancos)
    {
        using var resposta = await _admin.PostAsJsonAsync(U("/admin/massa/clientes"), new { tipo, bancos }, _ct);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct));
        return corpo.RootElement.GetProperty("cliente").GetProperty("documento").GetString()!;
    }

    private async Task<JsonElement> Listar(string consulta)
    {
        using var resposta = await _admin.GetAsync(U("/admin/massa/clientes" + consulta), _ct);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct)).RootElement.Clone();
    }

    private static string[] Documentos(JsonElement pagina) =>
        [.. pagina.GetProperty("clientes").EnumerateArray().Select(c => c.GetProperty("documento").GetString()!)];

    [Fact]
    public async Task Lista_so_os_clientes_com_produto_no_banco_em_ordem_e_pagina_com_temMais()
    {
        var pf = await CriarCliente("PF", "banco-estavel");
        var pj = await CriarCliente("PJ", "banco-estavel", "banco-lento");
        var outro = await CriarCliente("PF", "banco-lento");

        var estavel = await Listar("?banco=banco-estavel");
        string[] esperados = [.. new[] { pf, pj }.Order(StringComparer.Ordinal)];
        Assert.Equal(esperados, Documentos(estavel));
        Assert.False(estavel.GetProperty("temMais").GetBoolean());
        var primeiro = estavel.GetProperty("clientes")[0];
        Assert.True(primeiro.TryGetProperty("nome", out _));
        Assert.True(primeiro.TryGetProperty("tipo", out _));

        var pagina1 = await Listar("?banco=banco-estavel&inicio=0&quantidade=1");
        Assert.Equal([esperados[0]], Documentos(pagina1));
        Assert.True(pagina1.GetProperty("temMais").GetBoolean());
        var pagina2 = await Listar("?banco=banco-estavel&inicio=1&quantidade=1");
        Assert.Equal([esperados[1]], Documentos(pagina2));
        Assert.False(pagina2.GetProperty("temMais").GetBoolean());

        Assert.Equal(3, Documentos(await Listar(string.Empty)).Length);
        Assert.Contains(outro, Documentos(await Listar("?banco=banco-lento")));
        Assert.Empty(Documentos(await Listar("?banco=banco-timeout")));
    }

    [Theory]
    [InlineData("?quantidade=0", HttpStatusCode.UnprocessableEntity)]
    [InlineData("?quantidade=1001", HttpStatusCode.UnprocessableEntity)]
    [InlineData("?inicio=-1", HttpStatusCode.UnprocessableEntity)]
    [InlineData("?banco=nao-existe", HttpStatusCode.NotFound)]
    public async Task Recusa_pagina_fora_da_faixa_e_banco_inexistente(string consulta, HttpStatusCode esperado)
    {
        using var resposta = await _admin.GetAsync(U("/admin/massa/clientes" + consulta), _ct);
        Assert.Equal(esperado, resposta.StatusCode);
        Assert.Contains("erros", await resposta.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resumo_conta_o_que_esta_persistido()
    {
        using var vazio = JsonDocument.Parse(await _admin.GetStringAsync(U("/admin/massa/resumo"), _ct));
        Assert.Equal(0, vazio.RootElement.GetProperty("clientes").GetInt64());

        await CriarCliente("PF", "banco-estavel");

        using var resumo = JsonDocument.Parse(await _admin.GetStringAsync(U("/admin/massa/resumo"), _ct));
        Assert.Equal(1, resumo.RootElement.GetProperty("clientes").GetInt64());
        Assert.True(resumo.RootElement.GetProperty("recursos").GetInt64() > 0);
        Assert.Equal(0, resumo.RootElement.GetProperty("consentimentos").GetInt64());
        Assert.Equal(0, resumo.RootElement.GetProperty("lotes").GetInt64());
    }
}

/// <summary>Com a massa em memória.</summary>
public sealed class ClientesDoBancoTests() : ClientesDoBancoTestsBase(new FabricaDoSimulador());

/// <summary>Com o MongoDB real (agregação por cliente).</summary>
public sealed class ClientesDoBancoNoMongoTests() : ClientesDoBancoTestsBase(new FabricaDoSimulador().ComMongo());
