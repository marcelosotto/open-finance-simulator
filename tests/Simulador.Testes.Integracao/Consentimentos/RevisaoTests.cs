using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Consentimentos;

/// <summary>Corridas, validação do corpo, paginação e natureza da spec 004.</summary>
public sealed class RevisaoTests : IDisposable
{
    private const string Banco = "banco-teste";
    private static readonly string[] Contas = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ", "RESOURCES_READ"];
    private static readonly string[] BancoEstavel = ["banco-estavel"];
    private readonly FabricaDoSimulador _fabrica = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private string _usuario = string.Empty;
    private string _senha = string.Empty;

    public RevisaoTests()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
    }

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    private static Uri Caminho(string caminho, string? id = null) => AtalhosDeConsentimento.Caminho(Banco, "consents", caminho, id);

    private static string Data(DateTimeOffset valor) => valor.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    private DateTimeOffset Agora => _fabrica.Relogio.GetUtcNow();

    private async Task Receptora() => (_usuario, _senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);

    private async Task<string> Token(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, _usuario, _senha, escopo);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento)).RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<string> Cliente() =>
        (await new AdminDaMassa(_admin).Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco } })).Cliente.Documento;

    private async Task<(string Id, string Cpf)> Consentimento(int dias = 30)
    {
        var cpf = await Cliente();
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), await Token("consents"), new
        {
            data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = Contas, expirationDateTime = Data(Agora.AddDays(dias)) },
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento)).RootElement.GetProperty("data").GetProperty("consentId").GetString()!, cpf);
    }

    private Task<HttpResponseMessage> Renovar(string id, string cpf, string token, DateTimeOffset? nova, IReadOnlyDictionary<string, string>? cabecalhos = null) =>
        AtalhosDeConsentimento.Chamar(
            _api,
            HttpMethod.Post,
            Caminho("/consents/{consentId}/extends", id),
            token,
            new { data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, expirationDateTime = nova is { } d ? Data(d) : null } },
            cabecalhos ?? AtalhosDeConsentimento.CabecalhosDeRenovacao);

    private static async Task<JsonElement> Ler(HttpResponseMessage resposta)
    {
        using (resposta)
        {
            return JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento)).RootElement;
        }
    }

    private async Task<string> EstadoDe(string id) =>
        (await Ler(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Caminho("/consents/{consentId}", id), await Token("consents")))).GetProperty("data").GetProperty("status").GetString()!;

    private async Task<JsonElement> Extensoes(string id, string consulta = "")
    {
        var caminho = new Uri(Caminho("/consents/{consentId}/extensions", id) + consulta, UriKind.Relative);
        return await Ler(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, caminho, await Token("consents")));
    }

    private async Task<(string Id, MassaGerada Item)> MundoVirtual()
    {
        var massa = new AdminDaMassa(_admin);
        await massa.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total = 10, semente = 1, distribuicao = new { bancos = new Dictionary<string, decimal> { [Banco] = 100 }, perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100 } }, confirmar = true });
        var item = (await massa.Ler<IReadOnlyList<MassaGerada>>("/mundo-virtual/itens?inicio=0&quantidade=1"))[0];
        return (item.Consentimentos[0].ConsentId, item);
    }

    // Item 1
    [Fact]
    public async Task Renovacao_que_perde_para_revogacao_nao_ressuscita_o_consentimento()
    {
        await Receptora();
        var (id, cpf) = await Consentimento();
        var tokenDoConsentimento = await Token($"consent:{id}");
        var tokenDaReceptora = await Token("consents");
        _fabrica.Massa.AntesDaEscritaCondicional = async () =>
        {
            using var revogacao = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), tokenDaReceptora);
            Assert.Equal(HttpStatusCode.NoContent, revogacao.StatusCode);
        };

        using var renovacao = await Renovar(id, cpf, tokenDoConsentimento, Agora.AddDays(60));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, renovacao.StatusCode);
        var corpo = await renovacao.Content.ReadAsStringAsync(Cancelamento);
        Assert.Equal("ESTADO_CONSENTIMENTO_INVALIDO", JsonDocument.Parse(corpo).RootElement.GetProperty("errors")[0].GetProperty("code").GetString());
        Assert.Equal("REJECTED", await EstadoDe(id));
    }

    [Fact]
    public async Task Duas_renovacoes_simultaneas_guardam_as_duas_extensoes()
    {
        await Receptora();
        var (id, cpf) = await Consentimento();
        var token = await Token($"consent:{id}");
        _fabrica.Massa.AntesDaEscritaCondicional = async () =>
        {
            using var primeira = await Renovar(id, cpf, token, Agora.AddDays(60));
            Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        };

        using var segunda = await Renovar(id, cpf, token, Agora.AddDays(90));

        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(2, (await Extensoes(id)).GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Revogacao_que_perde_para_outra_revogacao_responde_422()
    {
        await Receptora();
        var (id, _) = await Consentimento();
        var token = await Token("consents");
        _fabrica.Massa.AntesDaEscritaCondicional = async () =>
        {
            using var primeira = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), token);
            Assert.Equal(HttpStatusCode.NoContent, primeira.StatusCode);
        };

        using var segunda = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
        Assert.Empty(ValidadorDeContrato.Validar("consentsDeleteConsentsConsentId", await segunda.Content.ReadAsStringAsync(Cancelamento), "422"));
    }

    [Fact]
    public async Task Produto_novo_nao_desfaz_revogacao_concorrente()
    {
        await Receptora();
        var cpf = await Cliente();
        var credito = new[]
        {
            "LOANS_READ", "LOANS_WARRANTIES_READ", "LOANS_SCHEDULED_INSTALMENTS_READ", "LOANS_PAYMENTS_READ",
            "FINANCINGS_READ", "FINANCINGS_WARRANTIES_READ", "FINANCINGS_SCHEDULED_INSTALMENTS_READ", "FINANCINGS_PAYMENTS_READ",
            "UNARRANGED_ACCOUNTS_OVERDRAFT_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_WARRANTIES_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_SCHEDULED_INSTALMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_PAYMENTS_READ",
            "INVOICE_FINANCINGS_READ", "INVOICE_FINANCINGS_WARRANTIES_READ", "INVOICE_FINANCINGS_SCHEDULED_INSTALMENTS_READ", "INVOICE_FINANCINGS_PAYMENTS_READ",
            "RESOURCES_READ",
        };
        var token = await Token("consents");
        using var criacao = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), token, new { data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = credito } });
        var id = (await Ler(criacao)).GetProperty("data").GetProperty("consentId").GetString()!;
        _fabrica.Massa.AntesDaEscritaCondicional = async () =>
        {
            using var revogacao = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), token);
            Assert.Equal(HttpStatusCode.NoContent, revogacao.StatusCode);
        };

        using var produto = await _admin.PostAsJsonAsync(new Uri($"/admin/clientes/{cpf}/produtos", UriKind.Relative), new { banco = Banco, produto = "emprestimos" }, Cancelamento);

        Assert.Equal(HttpStatusCode.Created, produto.StatusCode);
        Assert.Equal("REJECTED", await EstadoDe(id));
    }

    [Fact]
    public async Task Corrida_em_consentimento_virtual_tambem_respeita_a_revogacao()
    {
        await Receptora();
        var (id, item) = await MundoVirtual();
        var tokenDoConsentimento = await Token($"consent:{id}");
        var tokenDaReceptora = await Token("consents");
        _fabrica.Massa.AntesDaEscritaCondicional = async () =>
        {
            using var revogacao = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, Caminho("/consents/{consentId}", id), tokenDaReceptora);
            Assert.Equal(HttpStatusCode.NoContent, revogacao.StatusCode);
        };

        using var renovacao = await Renovar(id, item.Cliente.Documento, tokenDoConsentimento, null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, renovacao.StatusCode);
        Assert.Equal("REJECTED", await EstadoDe(id));
    }

    [Fact]
    public async Task Escrita_condicional_no_mongo()
    {
        using var fabrica = new FabricaDoSimulador().ComMongo();
        using var http = fabrica.ClienteAdministracao();
        var massa = await new AdminDaMassa(http).Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = BancoEstavel });
        var repositorio = fabrica.Services.GetRequiredService<IRepositorioDeMassa>();
        var consentimento = new Consentimento("urn:hml:p-corrida", massa.Cliente.Documento, "banco-estavel", "AUTHORISED", Contas, [], Agora, "hml", "persistido");
        await repositorio.SalvarConsentimento(consentimento, Cancelamento);

        Assert.True(await repositorio.SalvarConsentimentoSeVersao(consentimento with { Versao = 1 }, 0, Cancelamento));
        Assert.False(await repositorio.SalvarConsentimentoSeVersao(consentimento with { Versao = 1 }, 0, Cancelamento));
        Assert.True(await repositorio.SalvarConsentimentoSeVersao(consentimento with { Versao = 2 }, 1, Cancelamento));

        var ajuste = new Ajuste("consentimento:corrida", "consentimento", DoMundo: true, Estado: "REJECTED", Versao: 1);
        Assert.True(await repositorio.SalvarAjusteSeVersao(ajuste, 0, Cancelamento));
        Assert.False(await repositorio.SalvarAjusteSeVersao(ajuste, 0, Cancelamento));
        Assert.True(await repositorio.SalvarAjusteSeVersao(ajuste with { Versao = 2 }, 1, Cancelamento));
    }

    // Item 2
    public static TheoryData<string> CabecalhoAusente() => ["x-fapi-customer-ip-address", "x-customer-user-agent"];

    [Theory]
    [MemberData(nameof(CabecalhoAusente))]
    public async Task Renovacao_sem_cabecalho_exigido_responde_400(string ausente)
    {
        await Receptora();
        var (id, cpf) = await Consentimento();
        var token = await Token($"consent:{id}");
        var cabecalhos = AtalhosDeConsentimento.CabecalhosDeRenovacao.Where(c => c.Key != ausente).ToDictionary(c => c.Key, c => c.Value);

        using var resposta = await Renovar(id, cpf, token, Agora.AddDays(60), cabecalhos);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Extensions_devolve_ip_e_user_agent_da_renovacao()
    {
        await Receptora();
        var (id, cpf) = await Consentimento();
        using var renovada = await Renovar(id, cpf, await Token($"consent:{id}"), Agora.AddDays(60));
        Assert.Equal(HttpStatusCode.Created, renovada.StatusCode);

        var extensao = (await Extensoes(id)).GetProperty("data")[0];

        Assert.Equal(AtalhosDeConsentimento.IpDoCliente, extensao.GetProperty("xFapiCustomerIpAddress").GetString());
        Assert.Equal(AtalhosDeConsentimento.AgenteDoCliente, extensao.GetProperty("xCustomerUserAgent").GetString());
    }

    // Item 3
    public static TheoryData<string> CorposInvalidos() =>
    [
        """{"data":{"loggedUser":{"document":{"identification":"abc","rel":"CPF"}},"permissions":["ACCOUNTS_READ","ACCOUNTS_BALANCES_READ","RESOURCES_READ"]}}""",
        """{"data":{"loggedUser":{"document":{"identification":"90000000000"}},"permissions":["ACCOUNTS_READ","ACCOUNTS_BALANCES_READ","RESOURCES_READ"]}}""",
        """{"data":{"loggedUser":{"document":{"identification":"90000000000","rel":"cpf"}},"permissions":["ACCOUNTS_READ","ACCOUNTS_BALANCES_READ","RESOURCES_READ"]}}""",
        """{"data":{"loggedUser":{"document":{"identification":"90000000000","rel":"CPF"}},"permissions":["ACCOUNTS_READ","ACCOUNTS_BALANCES_READ","RESOURCES_READ"],"expirationDateTime":"2030-01-01T00:00:00-03:00"}}""",
        """{"data":{"loggedUser":{"document":{"identification":"90000000000","rel":"CPF"}},"permissions":["ACCOUNTS_READ","ACCOUNTS_BALANCES_READ","RESOURCES_READ"],"expirationDateTime":"2030-02-31T00:00:00Z"}}""",
        """{"data":{"loggedUser":{"document":{"identification":"90000000000","rel":"CPF"}},"businessEntity":{"document":{"identification":"123","rel":"CNPJ"}},"permissions":["ACCOUNTS_READ","ACCOUNTS_BALANCES_READ","RESOURCES_READ"]}}""",
    ];

    [Theory]
    [MemberData(nameof(CorposInvalidos))]
    public async Task Corpo_fora_do_contrato_responde_400(string corpo)
    {
        await Receptora();
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, Caminho("/consents")) { Content = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json") };
        requisicao.Headers.Add("x-fapi-interaction-id", AtalhosDeConsentimento.Correlacao);
        requisicao.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await Token("consents"));

        using var resposta = await _api.SendAsync(requisicao, Cancelamento);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    // Item 4
    [Fact]
    public async Task Page_size_abaixo_de_25_em_recursos_vira_25()
    {
        await Receptora();
        var (id, _) = await Consentimento();
        var caminho = new Uri(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources") + "?page-size=5", UriKind.Relative);

        var corpo = await Ler(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, caminho, await Token($"consent:{id}")));

        Assert.Contains("page-size=25", corpo.GetProperty("links").GetProperty("self").GetString(), StringComparison.Ordinal);
    }

    // Item 5
    [Fact]
    public async Task Extensions_paginadas_com_links_e_fora_da_faixa()
    {
        await Receptora();
        var (id, cpf) = await Consentimento();
        var token = await Token($"consent:{id}");
        for (var i = 0; i < 26; i++)
        {
            using var renovada = await Renovar(id, cpf, token, Agora.AddDays(60 + i));
            Assert.Equal(HttpStatusCode.Created, renovada.StatusCode);
        }

        var primeira = await Extensoes(id, "?page-size=5");
        Assert.Equal(25, primeira.GetProperty("data").GetArrayLength());
        Assert.Equal(26, primeira.GetProperty("meta").GetProperty("totalRecords").GetInt32());
        Assert.Equal(2, primeira.GetProperty("meta").GetProperty("totalPages").GetInt32());
        Assert.Contains("page=2", primeira.GetProperty("links").GetProperty("next").GetString(), StringComparison.Ordinal);
        Assert.Empty(ValidadorDeContrato.Validar("consentsGetConsentsConsentIdExtensions", primeira.GetRawText()));

        var segunda = await Extensoes(id, "?page=2");
        Assert.Equal(1, segunda.GetProperty("data").GetArrayLength());
        Assert.Contains("page=1", segunda.GetProperty("links").GetProperty("first").GetString(), StringComparison.Ordinal);

        Assert.Equal("PAGE_NOT_FOUND", (await Extensoes(id, "?page=3")).GetProperty("errors")[0].GetProperty("code").GetString());
    }

    // Item 6
    [Fact]
    public async Task Ajuste_de_administracao_preserva_as_extensoes_do_virtual()
    {
        await Receptora();
        var (id, item) = await MundoVirtual();
        using var renovada = await Renovar(id, item.Cliente.Documento, await Token($"consent:{id}"), null);
        Assert.Equal(HttpStatusCode.Created, renovada.StatusCode);

        await new AdminDaMassa(_admin).Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = id, estado = "REJECTED" });

        Assert.Equal(1, (await Extensoes(id)).GetProperty("data").GetArrayLength());
        Assert.Equal("REJECTED", await EstadoDe(id));
    }

    // Item 7
    [Fact]
    public async Task Natureza_reflete_o_corpo_de_recursos()
    {
        await Receptora();
        var (_, item) = await MundoVirtual();
        var token = await Token("consents");
        using var criada = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), token, new { data = new { loggedUser = new { document = new { identification = item.Cliente.Documento, rel = "CPF" } }, permissions = Contas } });
        var id = (await Ler(criada)).GetProperty("data").GetProperty("consentId").GetString()!;
        using var lista = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"), await Token($"consent:{id}"));
        Assert.Equal("virtual", lista.Headers.GetValues("x-sim-data-nature").Single());

        var cadastro = new[] { "CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", "RESOURCES_READ" };
        var cpf = await Cliente();
        using var soCadastro = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), token, new { data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = cadastro } });
        var idCadastro = (await Ler(soCadastro)).GetProperty("data").GetProperty("consentId").GetString()!;
        using var vazia = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"), await Token($"consent:{idCadastro}"));
        Assert.False(vazia.Headers.Contains("x-sim-data-nature"));
    }

    // Item 8
    [Fact]
    public async Task Correlacao_gerada_sai_do_gerador_com_semente()
    {
        async Task<string> Gerar()
        {
            using var fabrica = new FabricaDoSimulador();
            using var cliente = fabrica.ClienteOpenFinance();
            using var resposta = await cliente.GetAsync(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"), Cancelamento);
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
            return resposta.Headers.GetValues("x-fapi-interaction-id").Single();
        }

        Assert.Equal(await Gerar(), await Gerar());
    }

    // Item 9
    [Fact]
    public async Task Erros_422_validam_contra_o_contrato()
    {
        await Receptora();
        var (id, cpf) = await Consentimento();
        using var dataMenor = await Renovar(id, cpf, await Token($"consent:{id}"), Agora.AddDays(1));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, dataMenor.StatusCode);
        Assert.Empty(ValidadorDeContrato.Validar("consentsPostConsentsConsentIdExtends", await dataMenor.Content.ReadAsStringAsync(Cancelamento), "422"));

        var incompleto = new[] { "ACCOUNTS_READ", "RESOURCES_READ" };
        using var criacao = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, Caminho("/consents"), await Token("consents"), new { data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = incompleto } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, criacao.StatusCode);
        Assert.Empty(ValidadorDeContrato.Validar("consentsPostConsents", await criacao.Content.ReadAsStringAsync(Cancelamento), "422"));
    }

    // Item 10
    [Fact]
    public void Comparacao_de_segredo_e_em_tempo_constante()
    {
        var fonte = File.ReadAllText(Simulador.Testes.Infra.Repositorio.Caminho("src", "Modulos", "Simulador.Consentimentos", "Http", "Rotas.cs"));
        Assert.Contains("CodecDeToken.Iguais(receptora.Senha", fonte, StringComparison.Ordinal);
        Assert.DoesNotContain("receptora.Senha !=", fonte, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
