using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Instituicoes.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Instituicoes;

public sealed class AdminInstituicoesTests : IDisposable
{
    private const string Base = "/admin/instituicoes";
    private static readonly string[] SoContas = ["contas"];
    private readonly ClientesDeTeste _clientes = new();
    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _admin;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public AdminInstituicoesTests()
    {
        _fabrica = new FabricaDoSimulador().ComServicos(s => s.AddSingleton<IConsultaDeClientes>(_clientes));
        _admin = _fabrica.ClienteAdministracao();
    }

    public void Dispose()
    {
        _admin.Dispose();
        _fabrica.Dispose();
    }

    private Task<HttpResponseMessage> Criar(object corpo) => _admin.PostAsJsonAsync(new Uri(Base, UriKind.Relative), corpo, _ct);

    private async Task<HttpResponseMessage> Patch(string endereco, string json)
    {
        using var conteudo = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await _admin.PatchAsync(new Uri($"{Base}/{endereco}", UriKind.Relative), conteudo, _ct);
    }

    private async Task<JsonElement> Json(HttpResponseMessage resposta) =>
        JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct)).RootElement;

    private async Task<JsonElement> Get(string caminho)
    {
        using var resposta = await _admin.GetAsync(new Uri(caminho, UriKind.Relative), _ct);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return await Json(resposta);
    }

    private async Task<HttpStatusCode> ChamarOpenFinance(string endereco)
    {
        using var cliente = _fabrica.ClienteOpenFinance();
        using var resposta = await cliente.GetAsync(new Uri($"/{endereco}/open-banking/accounts/v2/accounts", UriKind.Relative), _ct);
        return resposta.StatusCode;
    }

    [Fact]
    public async Task Lista_os_bancos_prontos_e_os_perfis()
    {
        var lista = await Get(Base);
        var perfis = await Get("/admin/perfis-de-comportamento");

        Assert.Equal(17, lista.GetArrayLength());
        Assert.Equal(5, perfis.GetArrayLength());
        Assert.Contains(lista.EnumerateArray(), i => i.GetProperty("nome").GetString() == "HML-Banco do Brasil");
    }

    [Fact]
    public async Task Cria_so_com_o_nome_e_responde_no_endereco()
    {
        using var resposta = await Criar(new { nome = "Banco Azul" });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var corpo = await Json(resposta);
        Assert.Equal("banco-azul", corpo.GetProperty("endereco").GetString());
        Assert.Equal("estavel", corpo.GetProperty("perfil").GetString());
        Assert.Equal("ambos", corpo.GetProperty("publico").GetString());
        Assert.Equal("Banco Azul", corpo.GetProperty("marca").GetString());
        Assert.StartsWith("hml-p-", corpo.GetProperty("organisationId").GetString(), StringComparison.Ordinal);
        Assert.Equal(4, corpo.GetProperty("sla").GetArrayLength());
        Assert.Equal(0, corpo.GetProperty("avisos").GetArrayLength());
        Assert.Equal(HttpStatusCode.NotImplemented, await ChamarOpenFinance("banco-azul"));
    }

    [Fact]
    public async Task Aceita_campos_opcionais()
    {
        using var resposta = await Criar(new { nome = "Banco Pj", perfil = "lento", publico = "PJ", produtos = SoContas, marca = "Pj" });

        var corpo = await Json(resposta);
        Assert.Equal("lento", corpo.GetProperty("perfil").GetString());
        Assert.Equal("PJ", corpo.GetProperty("publico").GetString());
        Assert.Equal(1, corpo.GetProperty("produtos").GetArrayLength());
        Assert.Equal("Pj", corpo.GetProperty("marca").GetString());
    }

    [Fact]
    public async Task Inspirado_em_banco_real_leva_o_prefixo_no_nome_e_nao_no_endereco()
    {
        using var resposta = await Criar(new { nome = "Banco Real Novo", inspiradoEmBancoReal = true });

        var corpo = await Json(resposta);
        Assert.Equal("HML-Banco Real Novo", corpo.GetProperty("nome").GetString());
        Assert.Equal("banco-real-novo", corpo.GetProperty("endereco").GetString());
    }

    [Fact]
    public async Task Endereco_desconhecido_responde_404() =>
        Assert.Equal(HttpStatusCode.NotFound, await ChamarOpenFinance("nao-existe"));

    [Theory]
    [InlineData("""{"nome":"Banco Azul"}""")]
    [InlineData("""{"nome":"banco azul"}""")]
    [InlineData("""{"nome":"Banco  Azul!"}""")]
    public async Task Nome_repetido_responde_409(string segundo)
    {
        using var primeira = await Criar(new { nome = "Banco Azul" });
        using var conteudo = new StringContent(segundo, System.Text.Encoding.UTF8, "application/json");

        using var resposta = await _admin.PostAsync(new Uri(Base, UriKind.Relative), conteudo, _ct);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Inspirado_em_banco_ja_pronto_e_repetido()
    {
        using var resposta = await Criar(new { nome = "Itaú", inspiradoEmBancoReal = true });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Theory]
    [InlineData("""{"nome":"!!!"}""")]
    [InlineData("""{"nome":" "}""")]
    [InlineData("""{"nome":"X","perfil":"nao-existe"}""")]
    [InlineData("""{"nome":"X","publico":"Z"}""")]
    [InlineData("""{"perfil":"lento"}""")]
    [InlineData("""{"nome":"X","campoErrado":1}""")]
    [InlineData("""{ invalido""")]
    [InlineData("null")]
    public async Task Cadastro_invalido_responde_422(string json)
    {
        using var conteudo = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var resposta = await _admin.PostAsync(new Uri(Base, UriKind.Relative), conteudo, _ct);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Patch_altera_e_vai_para_o_historico_sem_mudar_o_perfil()
    {
        using var criacao = await Criar(new { nome = "Banco Azul", perfil = "lento" });
        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, new Uri($"{Base}/banco-azul", UriKind.Relative))
        {
            Content = new StringContent("""{"comportamento":{"taxaDeErroPct":5}}""", System.Text.Encoding.UTF8, "application/json"),
        };
        requisicao.Headers.Add("x-sim-autor", "Diego");

        using var resposta = await _admin.SendAsync(requisicao, _ct);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var historico = await Get($"{Base}/banco-azul/historico");
        Assert.Equal(2, historico.GetArrayLength());
        var ultima = historico[0];
        Assert.Equal("Diego", ultima.GetProperty("autor").GetString());
        Assert.Equal("alterada", ultima.GetProperty("acao").GetString());
        var mudanca = ultima.GetProperty("mudancas")[0];
        Assert.Equal("comportamento.taxaDeErroPct", mudanca.GetProperty("campo").GetString());
        Assert.Equal("0.5", mudanca.GetProperty("antes").GetString());
        Assert.Equal("5", mudanca.GetProperty("depois").GetString());
        Assert.Equal("operador", historico[1].GetProperty("autor").GetString());
        Assert.Equal(0.5m, (await Get("/admin/perfis-de-comportamento"))[1].GetProperty("comportamento").GetProperty("taxaDeErroPct").GetDecimal());
    }

    [Fact]
    public async Task Patch_sem_mudanca_nao_gera_historico()
    {
        using var resposta = await Patch("banco-estavel", """{"comportamento":{"taxaDeErroPct":0.2}}""");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(1, (await Get($"{Base}/banco-estavel/historico")).GetArrayLength());
    }

    [Fact]
    public async Task Valor_abaixo_do_oficial_e_aceito_com_aviso()
    {
        using var resposta = await Patch("banco-estavel", """{"limites":{"capacidadeTps":100}}""");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var aviso = (await Json(resposta)).GetProperty("avisos")[0];
        Assert.Equal("capacidadeTps", aviso.GetProperty("campo").GetString());
        Assert.Equal(300, aviso.GetProperty("oficial").GetInt32());
    }

    [Fact]
    public async Task Qca_por_receptora_entra_e_sai_por_merge()
    {
        using var entrou = await Patch("banco-estavel", """{"limites":{"qcaPorReceptora":{"org-1":3000000}}}""");
        var consulta = _fabrica.Services.GetRequiredService<IConsultaDeInstituicoes>();
        Assert.Equal(8_000, (await consulta.Obter("banco-estavel", _ct))!.Limites.TpmDaAlta("org-1"));

        using var saiu = await Patch("banco-estavel", """{"limites":{"qcaPorReceptora":{"org-1":null}}}""");

        Assert.Equal(2_500, (await consulta.Obter("banco-estavel", _ct))!.Limites.TpmDaAlta("org-1"));
    }

    [Theory]
    [InlineData("""{"comportamento":{"taxaDeErroPct":101}}""")]
    [InlineData("""{"comportamento":{"pesosPorStatus":{"418":1}}}""")]
    [InlineData("""{"comportamento":{"pesosPorStatus":{"429":1}}}""")]
    [InlineData("""{"comportamento":{"pesosPorStatus":{"503":1}}}""")]
    [InlineData("""{"comportamento":null}""")]
    [InlineData("""{"comportamento":{"taxaDeErroPct":"x"}}""")]
    [InlineData("""{"limites":{"campoErrado":1}}""")]
    [InlineData("""{"endereco":"outro"}""")]
    [InlineData("""{"situacao":"inativa"}""")]
    [InlineData("""{ invalido""")]
    [InlineData("null")]
    public async Task Patch_invalido_responde_422(string json)
    {
        using var resposta = await Patch("banco-estavel", json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
    }

    [Fact]
    public async Task Renomear_nao_muda_o_endereco_e_nao_pode_repetir()
    {
        using var ok = await Patch("banco-estavel", """{"nome":"Banco Novo"}""");
        using var repetido = await Patch("banco-lento", """{"nome":"banco novo"}""");
        using var enderecoDeOutro = await Patch("banco-lento", """{"nome":"banco estavel"}""");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("banco-estavel", (await Json(ok)).GetProperty("endereco").GetString());
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, enderecoDeOutro.StatusCode);
    }

    [Fact]
    public async Task Sem_clientes_apaga_e_mantem_historico()
    {
        using var criacao = await Criar(new { nome = "Banco Azul" });

        using var resposta = await _admin.DeleteAsync(new Uri($"{Base}/banco-azul", UriKind.Relative), _ct);

        Assert.Equal("apagada", (await Json(resposta)).GetProperty("resultado").GetString());
        using var depois = await _admin.GetAsync(new Uri($"{Base}/banco-azul", UriKind.Relative), _ct);
        Assert.Equal(HttpStatusCode.NotFound, depois.StatusCode);
        Assert.Equal("apagada", (await Get($"{Base}/banco-azul/historico"))[0].GetProperty("acao").GetString());
    }

    [Fact]
    public async Task Com_clientes_inativa_responde_503_e_reativa()
    {
        using var criacao = await Criar(new { nome = "Banco Azul" });
        _clientes.Enderecos.Add("banco-azul");

        using var resposta = await _admin.DeleteAsync(new Uri($"{Base}/banco-azul", UriKind.Relative), _ct);
        using var denovo = await _admin.DeleteAsync(new Uri($"{Base}/banco-azul", UriKind.Relative), _ct);

        Assert.Equal("inativada", (await Json(resposta)).GetProperty("resultado").GetString());
        Assert.Equal("inativada", (await Json(denovo)).GetProperty("resultado").GetString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, await ChamarOpenFinance("banco-azul"));
        Assert.Equal(2, (await Get($"{Base}/banco-azul/historico")).GetArrayLength());

        using var reativada = await _admin.PostAsync(new Uri($"{Base}/banco-azul/reativar", UriKind.Relative), null, _ct);

        Assert.Equal(HttpStatusCode.OK, reativada.StatusCode);
        Assert.Equal(HttpStatusCode.NotImplemented, await ChamarOpenFinance("banco-azul"));
        Assert.Equal("reativada", (await Get($"{Base}/banco-azul/historico"))[0].GetProperty("acao").GetString());
    }

    [Fact]
    public async Task Banco_pronto_so_e_inativado()
    {
        using var resposta = await _admin.DeleteAsync(new Uri($"{Base}/itau", UriKind.Relative), _ct);

        Assert.Equal("inativada", (await Json(resposta)).GetProperty("resultado").GetString());
    }

    [Fact]
    public async Task Inativa_responde_503_no_envelope_sem_explicar()
    {
        using var inativa = await _admin.DeleteAsync(new Uri($"{Base}/itau", UriKind.Relative), _ct);
        using var cliente = _fabrica.ClienteOpenFinance();

        using var resposta = await cliente.GetAsync(new Uri("/itau/open-banking/accounts/v2/accounts", UriKind.Relative), _ct);

        var corpo = await resposta.Content.ReadAsStringAsync(_ct);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Contains("SERVICO_INDISPONIVEL", corpo, StringComparison.Ordinal);
        Assert.DoesNotContain("inativ", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("GET", "/nao-existe")]
    [InlineData("DELETE", "/nao-existe")]
    [InlineData("POST", "/nao-existe/reativar")]
    [InlineData("GET", "/nao-existe/historico")]
    public async Task Instituicao_desconhecida_responde_404(string metodo, string sufixo)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), new Uri(Base + sufixo, UriKind.Relative));

        using var resposta = await _admin.SendAsync(requisicao, _ct);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Patch_em_desconhecida_responde_404()
    {
        using var resposta = await Patch("nao-existe", """{"nome":"X"}""");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
