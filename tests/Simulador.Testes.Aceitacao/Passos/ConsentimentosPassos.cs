using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de consentimento-api, recursos e seguranca (spec 004).</summary>
[Binding]
[Scope(Feature = "API de Consentimentos")]
[Scope(Feature = "API de Recursos")]
[Scope(Feature = "Autenticar a receptora")]
public sealed class ConsentimentosPassos : IDisposable
{
    private const string Estavel = "banco-estavel";
    private const string Recursos = "RESOURCES_READ";
    private static readonly string[] Contas = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ", Recursos];
    private static readonly string[] Cartoes = ["CREDIT_CARDS_ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_LIMITS_READ", Recursos];
    private static readonly string[] Credito =
    [
        "LOANS_READ", "LOANS_WARRANTIES_READ", "LOANS_SCHEDULED_INSTALMENTS_READ", "LOANS_PAYMENTS_READ",
        "FINANCINGS_READ", "FINANCINGS_WARRANTIES_READ", "FINANCINGS_SCHEDULED_INSTALMENTS_READ", "FINANCINGS_PAYMENTS_READ",
        "UNARRANGED_ACCOUNTS_OVERDRAFT_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_WARRANTIES_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_SCHEDULED_INSTALMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_PAYMENTS_READ",
        "INVOICE_FINANCINGS_READ", "INVOICE_FINANCINGS_WARRANTIES_READ", "INVOICE_FINANCINGS_SCHEDULED_INSTALMENTS_READ", "INVOICE_FINANCINGS_PAYMENTS_READ",
        Recursos,
    ];

    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private readonly AdminDaMassa _massa;

    private string _banco = Estavel;
    private string _usuario = string.Empty;
    private string _senha = string.Empty;
    private string? _tokenDaReceptora;
    private string _documento = string.Empty;
    private string _consentId = string.Empty;
    private string _outroConsentId = string.Empty;
    private string _tokenDoConsentimento = string.Empty;
    private string? _tokenDoOutro;
    private string _resourceId = string.Empty;
    private string _validadeAntes = string.Empty;
    private HttpResponseMessage? _resposta;
    private string _corpo = string.Empty;
    private string _credencialDeTeste = string.Empty;
    private readonly Dictionary<string, string> _estados = new(StringComparer.Ordinal);

    public ConsentimentosPassos()
    {
        _fabrica = new FabricaDoSimulador().ComMongo();
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
        _massa = new AdminDaMassa(_admin);
    }

    private JsonElement Json => JsonDocument.Parse(_corpo).RootElement;

    private async Task Guardar(HttpResponseMessage resposta)
    {
        _resposta?.Dispose();
        _resposta = resposta;
        _corpo = await resposta.Content.ReadAsStringAsync();
    }

    private async Task<string> TokenDaReceptora()
    {
        if (_usuario.Length == 0)
        {
            var receptora = await AtalhosDeConsentimento.CriarReceptora(_admin);
            (_usuario, _senha) = (receptora.Usuario, receptora.Senha);
        }

        return _tokenDaReceptora ??= await Pedir("consents");
    }

    private async Task<string> Pedir(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, _banco, _usuario, _senha, escopo);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<string> CriarCliente(string tipo = "PF", string? banco = null)
    {
        var massa = await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo, bancos = new[] { banco ?? _banco } });
        return massa.Cliente.Documento;
    }

    private async Task Criar(object dados) =>
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(_banco, "consents", "/consents"), await TokenDaReceptora(), new { data = dados }));

    private async Task<string> CriarConsentimento(string[] permissoes, DateTimeOffset? expira = null)
    {
        if (_documento.Length == 0)
        {
            _documento = await CriarCliente();
        }

        await Criar(new
        {
            loggedUser = new { document = new { identification = _documento, rel = "CPF" } },
            permissions = permissoes,
            expirationDateTime = expira?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture),
        });
        Assert.Equal(HttpStatusCode.Created, _resposta!.StatusCode);
        return Json.GetProperty("data").GetProperty("consentId").GetString()!;
    }

    private async Task Autorizado(string[] permissoes, DateTimeOffset? expira = null)
    {
        _consentId = await CriarConsentimento(permissoes, expira);
        _tokenDoConsentimento = await Pedir($"consent:{_consentId}");
    }

    private Uri CaminhoDoConsentimento(string complemento = "") =>
        AtalhosDeConsentimento.Caminho(_banco, "consents", "/consents/{consentId}" + complemento, _consentId);

    private async Task Revogar(string consentId)
    {
        var caminho = AtalhosDeConsentimento.Caminho(_banco, "consents", "/consents/{consentId}", consentId);
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, caminho, await TokenDaReceptora()));
    }

    private async Task ConsultarApi(string nome)
    {
        var (contrato, caminho) = nome switch
        {
            "recursos" => ("resources", "/resources"),
            "contas" => ("accounts", "/accounts"),
            "cartões" => ("credit-cards-accounts", "/accounts"),
            _ => ("accounts", "/accounts/{accountId}/balances"),
        };
        var endereco = AtalhosDeConsentimento.Caminho(_banco, contrato, caminho).ToString().Replace("{accountId}", "conta-1", StringComparison.Ordinal);
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(endereco, UriKind.Relative), _tokenDoConsentimento.Length == 0 ? null : _tokenDoConsentimento));
    }

    private async Task AjustarBanco(object alteracao)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, new Uri($"/admin/instituicoes/{_banco}", UriKind.Relative)) { Content = JsonContent.Create(alteracao) };
        using var resposta = await _admin.SendAsync(requisicao);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    private async Task<string> AdicionarProduto(string documento, string produto, string? estado = null)
    {
        using var resposta = await _admin.PostAsJsonAsync(new Uri($"/admin/clientes/{documento}/produtos", UriKind.Relative), new { banco = _banco, produto, estado });
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("resourceId").GetString()!;
    }

    private async Task<HttpStatusCode> MudarEstado(string resourceId, string estado)
    {
        using var resposta = await _admin.PutAsJsonAsync(new Uri($"/admin/recursos/{resourceId}/estado", UriKind.Relative), new { estado });
        return resposta.StatusCode;
    }

    private async Task ConsultarRecursos() => await ConsultarApi("recursos");

    private JsonElement.ArrayEnumerator ItensDeRecursos() => Json.GetProperty("data").EnumerateArray();

    // seguranca.feature
    [When(@"^Ana consulta contas sem token$")]
    public Task SemToken() => ConsultarApi("contas");

    [Then(@"^a resposta deve ter status (\d+)$")]
    public void Status(int status) => Assert.True((int)_resposta!.StatusCode == status, $"{_resposta.StatusCode}: {_corpo}");

    [Then(@"^a resposta deve ter status (\d+) sem corpo$")]
    public void StatusSemCorpo(int status)
    {
        Status(status);
        Assert.Empty(_corpo);
    }

    [Then(@"^a resposta deve ter status (\d+) com ""(.*)""$")]
    public void StatusComCodigo(int status, string codigo)
    {
        Status(status);
        Assert.Equal(codigo, Json.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Given(@"^um consentimento só com contas$")]
    public Task SoContas() => Autorizado(Contas);

    [When(@"^Ana consulta cartões$")]
    public Task ConsultaCartoes() => ConsultarApi("cartões");

    [Given(@"^um consentimento revogado$")]
    public async Task UmRevogado()
    {
        await Autorizado(Contas);
        await Revogar(_consentId);
    }

    [When(@"^a receptora pede um token para ele$")]
    public async Task PedeToken()
    {
        await Guardar(await AtalhosDeConsentimento.PedirToken(_api, _banco, _usuario, _senha, $"consent:{_consentId}"));
        _recusaEsperada = HttpStatusCode.BadRequest;
    }

    [When(@"^a receptora pede um token com senha errada$")]
    public async Task SenhaErrada()
    {
        await TokenDaReceptora();
        await Guardar(await AtalhosDeConsentimento.PedirToken(_api, _banco, _usuario, "errada", "consents"));
        _recusaEsperada = HttpStatusCode.Unauthorized;
    }

    [Then(@"^o pedido deve ser recusado$")]
    public void Recusado() => Assert.Equal(_recusaEsperada, _resposta!.StatusCode);

    [When(@"^Ana chama uma API de administração sem usuário$")]
    public async Task AdminSemUsuario() => await Guardar(await _admin.GetAsync(new Uri("/admin/receptoras", UriKind.Relative)));

    [Then(@"^a chamada deve ser atendida$")]
    public void Atendida() => Assert.Equal(HttpStatusCode.OK, _resposta!.StatusCode);

    [When(@"^Ana chama uma API de administração pelo endereço das APIs Open Finance$")]
    public async Task AdminPeloOpenFinance() => await Guardar(await _api.GetAsync(new Uri("/admin/receptoras", UriKind.Relative)));

    [Given(@"^uma credencial emitida pelo simulador$")]
    public async Task CredencialEmitida()
    {
        using var resposta = await _admin.PostAsJsonAsync(new Uri("/admin/receptoras", UriKind.Relative), new { nome = "Coletor" });
        _credencialDeTeste = await resposta.Content.ReadAsStringAsync();
    }

    [Then(@"^ela deve estar identificada como credencial de teste$")]
    public void DeTeste()
    {
        using var corpo = JsonDocument.Parse(_credencialDeTeste);
        Assert.True(corpo.RootElement.GetProperty("credencialDeTeste").GetBoolean());
        Assert.StartsWith("teste-", corpo.RootElement.GetProperty("usuario").GetString(), StringComparison.Ordinal);
    }

    // consentimento-api.feature
    [Given(@"^que Mariana tem conta e cartão no ""Banco Estável""$")]
    public async Task MarianaTemProdutos() => _documento = await CriarCliente();

    [When(@"^Ana cria um consentimento de contas para Mariana nesse banco$")]
    public Task CriaDeContas() => Autorizado(Contas);

    [Then(@"^o consentimento deve estar em ""(.*)""$")]
    public async Task EstadoDoConsentimento(string estado)
    {
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, CaminhoDoConsentimento(), await TokenDaReceptora()));
        Assert.Equal(estado, Json.GetProperty("data").GetProperty("status").GetString());
    }

    [Then(@"^só a conta deve ser compartilhada$")]
    public async Task SoAConta()
    {
        await ConsultarRecursos();
        Assert.All(ItensDeRecursos(), r => Assert.Equal("ACCOUNT", r.GetProperty("type").GetString()));
        Assert.NotEmpty(ItensDeRecursos());
    }

    [When(@"^Ana cria um consentimento com um CPF que não existe no simulador$")]
    public Task CpfInexistente() => Criar(new { loggedUser = new { document = new { identification = "12345678909", rel = "CPF" } }, permissions = Contas });

    [Given(@"^um consentimento em ""REJECTED""$")]
    public async Task EmRejected()
    {
        await Autorizado(Contas, _fabrica.Relogio.GetUtcNow().AddDays(30));
        await Revogar(_consentId);
    }

    private async Task Renovar(DateTimeOffset? nova)
    {
        var dados = new { loggedUser = new { document = new { identification = _documento, rel = "CPF" } }, expirationDateTime = nova?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture) };
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, CaminhoDoConsentimento("/extends"), _tokenDoConsentimento, new { data = dados }, AtalhosDeConsentimento.CabecalhosDeRenovacao));
    }

    [When(@"^Ana tenta renovar o consentimento$")]
    public Task TentaRenovar() => Renovar(_fabrica.Relogio.GetUtcNow().AddDays(60));

    [Given(@"^um banco que ""(.*)""$")]
    public async Task UmBancoQue(string oferta)
    {
        _banco = "banco-oferta";
        var produtos = oferta switch
        {
            "não oferece contas" => new[] { "cartoes", "emprestimos" },
            "não oferece crédito" => new[] { "contas", "cartoes" },
            _ => new[] { "contas", "cartoes", "emprestimos" },
        };
        using var resposta = await _admin.PostAsJsonAsync(new Uri("/admin/instituicoes", UriKind.Relative), new { nome = "Banco Oferta", produtos });
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    [When(@"^Ana cria um consentimento com ""(.*)""$")]
    public async Task CriaComPedido(string pedido)
    {
        _documento = await CriarCliente();
        await TokenDaReceptora();
        var permissoes = pedido switch
        {
            "grupo incompleto de contas" => new[] { "ACCOUNTS_READ", Recursos },
            "só contas" => Contas,
            "contas e cartões" => [.. Contas, .. Cartoes.Where(p => p != Recursos)],
            _ => Credito,
        };
        await Criar(new { loggedUser = new { document = new { identification = _documento, rel = "CPF" } }, permissions = permissoes });
    }

    [Then(@"^o resultado deve ser ""(.*)""$")]
    public void Resultado(string resultado)
    {
        var partes = resultado.Split(' ', 2);
        Status(int.Parse(partes[0], System.Globalization.CultureInfo.InvariantCulture));
        if (partes[0] == "422")
        {
            Assert.Equal(partes[1], Json.GetProperty("errors")[0].GetProperty("code").GetString());
            return;
        }

        var permissoes = Json.GetProperty("data").GetProperty("permissions").EnumerateArray().Select(p => p.GetString()!).ToList();
        if (partes[1] == "sem as permissões de contas")
        {
            Assert.DoesNotContain(permissoes, p => p.StartsWith("ACCOUNTS_", StringComparison.Ordinal));
            Assert.Contains("CREDIT_CARDS_ACCOUNTS_READ", permissoes);
        }
        else
        {
            Assert.Contains("LOANS_READ", permissoes);
        }
    }

    [When(@"^Ana cria um consentimento ""(.*)""$")]
    public async Task CriaPfPj(string pedido)
    {
        const string CnpjFicticio = "00000000000191";
        object? empresa = pedido == "com permissões PJ sem dados PJ" ? null : new { document = new { identification = CnpjFicticio, rel = "CNPJ" } };
        string[] permissoes = pedido switch
        {
            "com dados PJ e permissões PF" => ["CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", Recursos],
            "com permissões PF e PJ" => ["CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", "CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ", Recursos],
            _ => ["CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ", Recursos],
        };
        await Criar(new { loggedUser = new { document = new { identification = "90000000000", rel = "CPF" } }, businessEntity = empresa, permissions = permissoes });
    }

    [When(@"^Ana cria um consentimento sem expirationDateTime$")]
    public Task SemExpiracao() => Autorizado(Contas);

    [Then(@"^o consentimento não deve ter data final$")]
    public async Task SemDataFinal()
    {
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, CaminhoDoConsentimento(), await TokenDaReceptora()));
        Assert.False(Json.GetProperty("data").TryGetProperty("expirationDateTime", out _));
    }

    [Given(@"^um consentimento autorizado$")]
    public Task UmAutorizado() => Autorizado(Contas, _fabrica.Relogio.GetUtcNow().AddDays(30));

    [When(@"^Ana chama DELETE no consentimento$")]
    public Task ChamaDelete() => Revogar(_consentId);

    [Then(@"^as APIs de dados devem responder 401$")]
    public async Task ApisRespondem401()
    {
        await ConsultarApi("contas");
        Status(401);
    }

    [Given(@"^um consentimento autorizado de Mariana$")]
    public Task AutorizadoDeMariana() => Autorizado(Contas);

    [When(@"^Bruno revoga no painel$")]
    public async Task BrunoRevoga() =>
        await Guardar(await _admin.PostAsync(new Uri($"/admin/consentimentos/{_consentId}/revogar", UriKind.Relative), null));

    [Then(@"^a rejeição deve registrar quem revogou$")]
    public async Task RegistraQuem()
    {
        await EstadoDoConsentimento("REJECTED");
        var rejeicao = Json.GetProperty("data").GetProperty("rejection");
        Assert.Equal("USER", rejeicao.GetProperty("rejectedBy").GetString());
        Assert.Equal("CUSTOMER_MANUALLY_REVOKED", rejeicao.GetProperty("reason").GetProperty("code").GetString());
    }

    [Given(@"^que Mariana tem dois consentimentos no ""Banco Estável""$")]
    public async Task DoisConsentimentos()
    {
        await Autorizado(Contas);
        _outroConsentId = await CriarConsentimento(Contas);
        _tokenDoOutro = await Pedir($"consent:{_outroConsentId}");
    }

    [When(@"^Ana revoga um deles$")]
    public Task RevogaUm() => Revogar(_consentId);

    [Then(@"^o outro deve continuar válido$")]
    public async Task OutroValido()
    {
        _tokenDoConsentimento = _tokenDoOutro!;
        await ConsultarRecursos();
        Status(200);
    }

    [Given(@"^um consentimento ""(.*)""$")]
    public async Task ConsentimentoEm(string estado)
    {
        switch (estado)
        {
            case "inexistente":
                await Autorizado(Contas);
                await _massa.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "instituicao", id = _banco, confirmar = true });
                break;
            case "revogado":
                await Autorizado(Contas);
                await Revogar(_consentId);
                break;
            default:
                await Autorizado(Contas, _fabrica.Relogio.GetUtcNow().AddMinutes(10));
                _fabrica.Relogio.Advance(TimeSpan.FromMinutes(11));
                break;
        }
    }

    [When(@"^Ana consulta ""(.*)""$")]
    public Task AnaConsulta(string api) => ConsultarApi(api);

    [Given(@"^um consentimento autorizado que vence em 30 dias$")]
    public async Task VenceEm30Dias()
    {
        await Autorizado(Contas, _fabrica.Relogio.GetUtcNow().AddDays(30));
        _validadeAntes = Json.GetProperty("data").GetProperty("expirationDateTime").GetString()!;
    }

    [When(@"^Ana renova por mais 12 meses$")]
    public Task RenovaDozeMeses() => Renovar(_fabrica.Relogio.GetUtcNow().AddMonths(12).AddDays(30));

    [Then(@"^a validade deve mudar$")]
    public async Task ValidadeMudou()
    {
        Status(201);
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, CaminhoDoConsentimento(), await TokenDaReceptora()));
        Assert.NotEqual(_validadeAntes, Json.GetProperty("data").GetProperty("expirationDateTime").GetString());
    }

    [Then(@"^a extensão deve aparecer em ""/extensions""$")]
    public async Task ExtensaoAparece()
    {
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, CaminhoDoConsentimento("/extensions"), await TokenDaReceptora()));
        Assert.Equal(1, Json.GetProperty("data").GetArrayLength());
    }

    [When(@"^Ana renova para uma data menor que a validade atual$")]
    public Task RenovaMenor() => Renovar(_fabrica.Relogio.GetUtcNow().AddDays(1));

    // recursos.feature
    [Given(@"^que Rafael tem um consentimento autorizado com duas contas$")]
    public async Task RafaelDuasContas()
    {
        _documento = await CriarCliente();
        await TokenDaReceptora();
        _resourceId = await AdicionarProduto(_documento, "contas");
        await Autorizado(Contas);
    }

    [Given(@"^que uma conta está em ""(.*)""$")]
    public async Task UmaContaEm(string estado) => Assert.Equal(HttpStatusCode.OK, await MudarEstado(_resourceId, estado));

    [When(@"^Ana consulta recursos$")]
    public Task AnaConsultaRecursos() => ConsultarRecursos();

    [Then(@"^as duas contas devem aparecer$")]
    public void DuasContas() => Assert.Equal(2, ItensDeRecursos().Count(r => r.GetProperty("type").GetString() == "ACCOUNT"));

    [Given(@"^que o banco leva 2 minutos para preparar os recursos$")]
    public Task LevaDoisMinutos() => AjustarBanco(new { limites = new { preparacaoDeRecursosSeg = 120 } });

    [When(@"^Ana consulta recursos depois de 1 minuto$")]
    public Task DepoisDeUmMinuto()
    {
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        return ConsultarRecursos();
    }

    [Given(@"^que o banco ficou preparando por mais de 5 minutos$")]
    public async Task PreparandoMaisDeCinco()
    {
        await AjustarBanco(new { limites = new { preparacaoDeRecursosSeg = 600 } });
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)));
    }

    [Given(@"^um recurso em ""(.*)""$")]
    public async Task UmRecursoEm(string estado) => _resourceId = await AdicionarProduto(_documento, "contas", estado);

    [Given(@"^um fundo em ""(.*)""$")]
    public async Task UmFundoEm(string estado) => _resourceId = await AdicionarProduto(_documento, "fundos", estado);

    private HttpStatusCode _mudanca;
    private HttpStatusCode _recusaEsperada;

    [When(@"^Diego muda o estado para ""(.*)""$")]
    public async Task DiegoMuda(string estado) => _mudanca = await MudarEstado(_resourceId, estado);

    [Then(@"^a mudança deve ser ""(.*)""$")]
    public void Mudanca(string resultado) => Assert.Equal(resultado == "aceita" ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity, _mudanca);

    [Then(@"^a mudança deve ser aceita$")]
    public void MudancaAceita() => Assert.Equal(HttpStatusCode.OK, _mudanca);

    [Given(@"^um consentimento com operações de crédito$")]
    public Task ComCredito() => Autorizado(Credito);

    [When(@"^Diego adiciona um empréstimo ao cliente$")]
    public async Task AdicionaEmprestimo() => _resourceId = await AdicionarProduto(_documento, "emprestimos");

    [Then(@"^o empréstimo deve aparecer em Recursos$")]
    public async Task EmprestimoAparece()
    {
        await ConsultarRecursos();
        Assert.Contains(ItensDeRecursos(), r => r.GetProperty("resourceId").GetString() == _resourceId);
    }

    [Given(@"^que a conta está em ""(.*)""$")]
    public async Task AContaEm(string estado)
    {
        if (estado == "PENDING_AUTHORISATION")
        {
            _resourceId = await AdicionarProduto(_documento, "contas", estado);
            await Autorizado(Contas);
            return;
        }

        Assert.Equal(HttpStatusCode.OK, await MudarEstado(_resourceId, estado));
    }

    [When(@"^Ana lista contas$")]
    public Task ListaContas() => ConsultarApi("contas");

    [Then(@"^a conta ""(aparece|não aparece)""$")]
    public void ContaNaLista(string lista)
    {
        var presente = ItensDeRecursos().Any(r => r.GetProperty("accountId").GetString() == _resourceId);
        Assert.Equal(lista == "aparece", presente);
    }

    private async Task ConsultarConta(string resourceId, string token)
    {
        var endereco = AtalhosDeConsentimento.Caminho(_banco, "accounts", "/accounts/{accountId}").ToString().Replace("{accountId}", resourceId, StringComparison.Ordinal);
        await Guardar(await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(endereco, UriKind.Relative), token));
    }

    [When(@"^Ana consulta a conta$")]
    public Task ConsultaConta() => ConsultarConta(_resourceId, _tokenDoConsentimento);

    [When(@"^Ana consulta uma conta que não é de Rafael$")]
    public async Task ContaDeOutro()
    {
        var outro = await CriarCliente();
        await ConsultarConta(await AdicionarProduto(outro, "contas"), _tokenDoConsentimento);
    }

    [Given(@"^que Mariana tem a conta A em um consentimento e a conta B em outro$")]
    public async Task ContasEmConsentimentos()
    {
        _documento = await CriarCliente();
        await TokenDaReceptora();
        await AdicionarProduto(_documento, "contas");
        await Autorizado(Contas);
        _resourceId = await AdicionarProduto(_documento, "contas");
    }

    [When(@"^Ana consulta a conta B com o primeiro consentimento$")]
    public Task ContaB() => ConsultarConta(_resourceId, _tokenDoConsentimento);

    [Given(@"^um consentimento com uma conta ""(.*)"" e outra ""(.*)""$")]
    public async Task ContasComEstados(string primeiro, string segundo)
    {
        _documento = await CriarCliente();
        _estados[await AdicionarProduto(_documento, "contas", primeiro)] = primeiro;
        _estados[await AdicionarProduto(_documento, "contas", segundo)] = segundo;
        await Autorizado(Contas);
    }

    [Then(@"^cada conta deve aparecer com o próprio estado$")]
    public void ProprioEstado()
    {
        foreach (var (id, estado) in _estados)
        {
            Assert.Contains(ItensDeRecursos(), r => r.GetProperty("resourceId").GetString() == id && r.GetProperty("status").GetString() == estado);
        }
    }

    public void Dispose()
    {
        _resposta?.Dispose();
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
