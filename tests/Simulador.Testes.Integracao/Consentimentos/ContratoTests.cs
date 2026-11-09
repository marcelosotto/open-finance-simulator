using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Consentimentos;

/// <summary>RNF01, Q5, Q6 e Q9: as respostas validam contra os contratos oficiais.</summary>
public sealed class ContratoTests : IDisposable
{
    private const string Banco = "banco-teste";
    private static readonly string[] Contas = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ", "RESOURCES_READ"];
    private readonly FabricaDoSimulador _fabrica = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;

    public ContratoTests()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
    }

    private static string Data(DateTimeOffset valor) => valor.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private async Task<string> Cliente(string tipo = "PF")
    {
        var massa = await new AdminDaMassa(_admin).Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo, bancos = new[] { Banco } });
        return massa.Cliente.Documento;
    }

    private static Uri Caminho(string caminho, string? id = null) => AtalhosDeConsentimento.Caminho(Banco, "consents", caminho, id);

    private static async Task<string> Validar(HttpResponseMessage resposta, string operacao, int status)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True((int)resposta.StatusCode == status, $"{resposta.StatusCode}: {corpo}");
        Assert.Empty(ValidadorDeContrato.Validar(operacao, corpo, status.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return corpo;
    }

    [Fact]
    public async Task Ciclo_completo_valida_contra_o_contrato()
    {
        var (usuario, senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var cpf = await Cliente();
        var tokenReceptora = await Token(usuario, senha, "consents");

        using var criada = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), tokenReceptora, new
        {
            data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = Contas, expirationDateTime = Data(_fabrica.Relogio.GetUtcNow().AddDays(30)) },
        });
        var corpo = await Validar(criada, "consentsPostConsents", 201);
        Assert.Equal("persistido", criada.Headers.GetValues("x-sim-data-nature").Single());
        Assert.Equal(AtalhosDeConsentimento.Correlacao, criada.Headers.GetValues("x-fapi-interaction-id").Single());
        var id = JsonDocument.Parse(corpo).RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
        var tokenConsentimento = await Token(usuario, senha, $"consent:{id}");

        using var recursos = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"), tokenConsentimento);
        await Validar(recursos, "resourcesGetResources", 200);

        using var renovada = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents/{consentId}/extends", id), tokenConsentimento, new
        {
            data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, expirationDateTime = Data(_fabrica.Relogio.GetUtcNow().AddDays(60)) },
        });
        await Validar(renovada, "consentsPostConsentsConsentIdExtends", 201);

        using var extensoes = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Caminho("/consents/{consentId}/extensions", id), tokenReceptora);
        await Validar(extensoes, "consentsGetConsentsConsentIdExtensions", 200);

        using var revogada = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), tokenReceptora);
        Assert.Equal(HttpStatusCode.NoContent, revogada.StatusCode);

        using var consulta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Caminho("/consents/{consentId}", id), tokenReceptora);
        var lida = await Validar(consulta, "consentsGetConsentsConsentId", 200);
        var rejeicao = JsonDocument.Parse(lida).RootElement.GetProperty("data").GetProperty("rejection");
        Assert.Equal("TPP", rejeicao.GetProperty("rejectedBy").GetString());

        using var deNovo = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), tokenReceptora);
        await Validar(deNovo, "consentsDeleteConsentsConsentId", 422);
    }

    [Fact]
    public async Task Sem_correlacao_responde_400_e_gera_o_valor()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"));
        using var resposta = await _api.SendAsync(requisicao, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.True(Guid.TryParse(resposta.Headers.GetValues("x-fapi-interaction-id").Single(), out _));
    }

    [Fact]
    public async Task Outra_receptora_nao_usa_consentimento_criado_por_api()
    {
        var (usuario, senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var (outra, senhaDaOutra, _) = await AtalhosDeConsentimento.CriarReceptora(_admin, "Outra");
        var id = await Criar(usuario, senha, await Cliente());

        using var token = await AtalhosDeConsentimento.PedirToken(_api, Banco, outra, senhaDaOutra, $"consent:{id}");
        Assert.Equal(HttpStatusCode.BadRequest, token.StatusCode);
        using var consulta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Caminho("/consents/{consentId}", id), await Token(outra, senhaDaOutra, "consents"));
        Assert.Equal(HttpStatusCode.Forbidden, consulta.StatusCode);
    }

    [Fact]
    public async Task Pedido_pj_usa_cnpj_e_exige_cpf_simulado()
    {
        var (usuario, senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var cpf = await Cliente();
        var cnpj = await Cliente("PJ");
        var token = await Token(usuario, senha, "consents");

        async Task<HttpStatusCode> Pedir(string logado)
        {
            using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), token, new
            {
                data = new { loggedUser = new { document = new { identification = logado, rel = "CPF" } }, businessEntity = new { document = new { identification = cnpj, rel = "CNPJ" } }, permissions = Contas },
            });
            return resposta.StatusCode;
        }

        Assert.Equal(HttpStatusCode.Created, await Pedir(cpf));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, await Pedir("12345678909"));
    }

    [Fact]
    public async Task Token_vence_em_900_segundos()
    {
        var (usuario, senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var token = await Token(usuario, senha, "consents");
        _fabrica.Relogio.Advance(TimeSpan.FromSeconds(900));
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Caminho("/consents/{consentId}", "urn:x:y"), token);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Delete_e_extends_em_virtual_gravam_ajuste()
    {
        var massa = new AdminDaMassa(_admin);
        await massa.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total = 10, semente = 1, distribuicao = new { bancos = new Dictionary<string, decimal> { [Banco] = 100 }, perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100 } }, confirmar = true });
        var item = (await massa.Ler<IReadOnlyList<MassaGerada>>("/mundo-virtual/itens?inicio=0&quantidade=1"))[0];
        var id = item.Consentimentos[0].ConsentId;
        var (usuario, senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var tokenConsentimento = await Token(usuario, senha, $"consent:{id}");
        var tokenReceptora = await Token(usuario, senha, "consents");

        using var extensao = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents/{consentId}/extends", id), tokenConsentimento, new
        {
            data = new { loggedUser = new { document = new { identification = item.Cliente.Documento, rel = "CPF" } } },
        });
        Assert.Equal(HttpStatusCode.Created, extensao.StatusCode);
        using var extensoes = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Caminho("/consents/{consentId}/extensions", id), tokenReceptora);
        Assert.Equal(1, JsonDocument.Parse(await extensoes.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("data").GetArrayLength());

        using var revogada = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), tokenReceptora);
        Assert.Equal(HttpStatusCode.NoContent, revogada.StatusCode);
        Assert.Equal("REJECTED", (await massa.Ler<Consentimento>($"/consentimentos/{Uri.EscapeDataString(id)}")).Estado);

        using var produto = await _admin.PostAsJsonAsync(new Uri($"/admin/clientes/{item.Cliente.Documento}/produtos", UriKind.Relative), new { banco = Banco, produto = "contas" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, produto.StatusCode);
    }

    private async Task<string> Token(string usuario, string senha, string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, usuario, senha, escopo);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<string> Criar(string usuario, string senha, string cpf)
    {
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), await Token(usuario, senha, "consents"), new
        {
            data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = Contas },
        });
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
    }

    public void Dispose()
    {
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
