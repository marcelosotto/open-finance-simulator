using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reqnroll;
using Simulador.Consentimentos.Contratos;
using Simulador.Dados.Contratos;
using Simulador.Host;
using Simulador.Instituicoes.Contratos;
using Simulador.Limites.Dominio;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de limites.feature (spec 006).</summary>
[Binding]
[Scope(Feature = "Aplicar os limites oficiais")]
public sealed class LimitesPassos : IDisposable
{
    private const string Banco = "banco-teste";
    private const string Perfil = "fixo";
    private const string Contas = "accountsGetAccounts";
    private const string Conta = "accountsGetAccountsAccountId";
    private const string Saldos = "accountsGetAccountsAccountIdBalances";
    private const string MesDaFatura = "-b202610";
    private const string ChaveDoToken = "chave-compartilhada-dos-testes-de-limites";
    private static readonly string[] Tipos = ["PIX"];
    private static readonly string[] Produtos = ["contas", "cartoes"];

    private readonly ContadoresEmMemoria _contadores = new();
    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private readonly AdminDaMassa _massa;
    private readonly Falha _falha = new();

    private FabricaDoSimulador? _outra;
    private HttpClient? _apiDaOutra;
    private string _usuario = string.Empty;
    private string _senha = string.Empty;
    private string _organizacao = string.Empty;
    private string _consentId = string.Empty;
    private string _token = string.Empty;
    private MassaGerada? _cliente;
    private string _contaExtra = string.Empty;
    private string _operacao = Conta;
    private HttpResponseMessage? _resposta;
    private string _corpo = string.Empty;
    private readonly List<HttpStatusCode> _status = [];

    public LimitesPassos()
    {
        _fabrica = new FabricaDoSimulador()
            .ComContadoresEmMemoria(_contadores)
            .Com("Simulador:Token:Chave", ChaveDoToken)
            .ComServicos(s =>
            {
                s.RemoveAll<IAmbienteDosDados>();
                s.AddSingleton<IAmbienteDosDados>(p => new AmbienteComFalha(ActivatorUtilities.CreateInstance<AmbienteDosDados>(p), _falha));
                s.AddSingleton<IStartupFilter, FiltroDeErroInterno>();
            });
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
        _massa = new AdminDaMassa(_admin);
    }

    private JsonElement Json => JsonDocument.Parse(_corpo).RootElement;

    private DateTimeOffset Agora => _fabrica.Relogio.GetUtcNow();

    private string ContaDoCliente => _cliente!.Recursos.First(r => r.Tipo == "ACCOUNT").ResourceId;

    private string Cartao => _cliente!.Recursos.First(r => r.Tipo == "CREDIT_CARD_ACCOUNT").ResourceId;

    private string Fatura(string mes = MesDaFatura) => Cartao + mes;

    private async Task Preparar(int porDia = 2, bool contaExtra = false)
    {
        (_usuario, _senha, _organizacao) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        await _massa.Enviar<PerfilDeDados>(HttpMethod.Put, $"/perfis/{Perfil}", new { transacoesMin = porDia, transacoesMax = porDia, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = Tipos, produtos = Produtos });
        _cliente = await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco }, perfil = Perfil });
        if (contaExtra)
        {
            using var produto = await _admin.PostAsJsonAsync(new Uri($"/admin/clientes/{_cliente.Cliente.Documento}/produtos", UriKind.Relative), new { banco = Banco, produto = "contas" });
            Assert.True(produto.IsSuccessStatusCode, await produto.Content.ReadAsStringAsync());
            using var novo = JsonDocument.Parse(await produto.Content.ReadAsStringAsync());
            _contaExtra = novo.RootElement.GetProperty("resourceId").GetString()!;
        }

        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), await Token("consents"), new
        {
            data = new { loggedUser = new { document = new { identification = _cliente.Cliente.Documento, rel = "CPF" } }, permissions = Permissoes.Para("PF", null) },
        });
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        _consentId = corpo.RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
        await RenovarToken();
        _fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
    }

    private async Task RenovarToken() => _token = await Token($"consent:{_consentId}");

    private async Task<string> Token(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, _usuario, _senha, escopo);
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task Configurar(object limites)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, new Uri($"/admin/instituicoes/{Banco}", UriKind.Relative)) { Content = JsonContent.Create(new { limites }) };
        using var resposta = await _admin.SendAsync(requisicao);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
        _corpo = await resposta.Content.ReadAsStringAsync();
    }

    private static Uri Em(string contrato, string caminho, string id = "") =>
        new(AtalhosDeConsentimento.Caminho(Banco, contrato, caminho).ToString()
            .Replace("{accountId}", id, StringComparison.Ordinal)
            .Replace("{creditCardAccountId}", id, StringComparison.Ordinal), UriKind.Relative);

    private static Uri CaminhoDaOperacao(string operacao, string conta) => operacao switch
    {
        Contas => Em("accounts", "/accounts"),
        Saldos => Em("accounts", "/accounts/{accountId}/balances", conta),
        _ => Em("accounts", "/accounts/{accountId}", conta),
    };

    private Uri TransacoesDaFatura(string fatura, string consulta = "") =>
        new(Em("credit-cards-accounts", "/accounts/{creditCardAccountId}/bills/{billId}/transactions", Cartao).ToString().Replace("{billId}", fatura, StringComparison.Ordinal) + consulta, UriKind.Relative);

    private async Task Chamar(Uri caminho, HttpClient? cliente = null)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
        requisicao.Headers.Add("x-fapi-interaction-id", AtalhosDeConsentimento.Correlacao);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        _resposta?.Dispose();
        _resposta = await (cliente ?? _api).SendAsync(requisicao);
        _corpo = await _resposta.Content.ReadAsStringAsync();
        _status.Add(_resposta.StatusCode);
    }

    private Task ChamarOperacao(string operacao, string? conta = null, HttpClient? cliente = null)
    {
        _operacao = operacao;
        return Chamar(CaminhoDaOperacao(operacao, conta ?? ContaDoCliente), cliente);
    }

    private void DefinirTrafego(string operacao, long total) =>
        _contadores.Definir(ChavesDeContador.Trafego(Banco, operacao, _organizacao, Agora), total);

    private async Task ConferirTpm(int tpm)
    {
        DefinirTrafego(Saldos, tpm - 1);
        await ChamarOperacao(Saldos);
        Status(200);
        await ChamarOperacao(Saldos);
        Status(429);
    }

    private void ProximoSegundoSe(int indice, int aCada)
    {
        if (indice > 0 && indice % aCada == 0)
        {
            _fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
            _outra?.Relogio.Advance(TimeSpan.FromSeconds(1));
        }
    }

    [Then(@"^a resposta deve ter status (\d+)$")]
    [Then(@"^a última deve receber (\d+)$")]
    public void Status(int esperado)
    {
        Assert.True((int)_resposta!.StatusCode == esperado, $"{_resposta.StatusCode}: {_corpo}");
        if (esperado is 423 or 429 or 529)
        {
            Assert.Empty(ValidadorDeContrato.Validar(_operacao, _corpo, esperado.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
    }

    // Tráfego
    [Given(@"^que a receptora tem (\d+) consentimentos ativos no ""Banco Estável""$")]
    public async Task ReceptoraComQca(long qca)
    {
        await Preparar();
        await Configurar(new { qca });
    }

    [Then(@"^o TPM de saldos deve ser (\d+)$")]
    public Task TpmDeSaldos(int tpm) => ConferirTpm(tpm);

    [Given(@"^que o endpoint ""/accounts"" chegou ao limite no minuto$")]
    public async Task ContasNoLimite()
    {
        await Preparar();
        DefinirTrafego(Contas, 1000);
        await ChamarOperacao(Contas);
        Status(429);
    }

    [When(@"^Ana chama ""/accounts/\{accountId\}"" no mesmo minuto$")]
    public Task ChamaConta() => ChamarOperacao(Conta);

    [Then(@"^a chamada não deve receber 429$")]
    public void Nao429() => Assert.NotEqual(HttpStatusCode.TooManyRequests, _resposta!.StatusCode);

    [Given(@"^que o limite estourou às 10h25m59s$")]
    public async Task Estourou()
    {
        await Preparar();
        _fabrica.Relogio.SetUtcNow(new DateTimeOffset(2026, 11, 6, 10, 25, 59, TimeSpan.Zero));
        await RenovarToken();
        DefinirTrafego(Contas, 1000);
        await ChamarOperacao(Contas);
        Status(429);
    }

    [When(@"^Ana chama de novo às 10h26m00s$")]
    public async Task ChamaNaVirada()
    {
        _fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
        await ChamarOperacao(Contas);
    }

    [Then(@"^a chamada deve ser atendida$")]
    public void Atendida() => Status(200);

    [Given(@"^que o endpoint tem TPM 1000$")]
    public async Task Tpm1000()
    {
        await Preparar();
        await Configurar(new { limiteOperacionalLigado = false });
    }

    [When(@"^chegam 1001 chamadas no mesmo minuto$")]
    public async Task Chegam1001()
    {
        for (var i = 0; i < 1001; i++)
        {
            ProximoSegundoSe(i, 200);
            await ChamarOperacao(Conta);
        }

        Assert.Single(_status, HttpStatusCode.TooManyRequests);
        Assert.True(_resposta!.Headers.Contains("Retry-After"));
    }

    [When(@"^chegam 5000 chamadas a ""/resources"" no mesmo minuto$")]
    public async Task Chegam5000()
    {
        await Preparar();
        for (var i = 0; i < 5000; i++)
        {
            ProximoSegundoSe(i, 100);
            await Chamar(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"));
        }
    }

    [Then(@"^nenhuma deve receber 429$")]
    public void Nenhuma429() => Assert.DoesNotContain(HttpStatusCode.TooManyRequests, _status);

    // Limite operacional
    [Given(@"^que o limite operacional está ligado$")]
    public Task Ligado() => Preparar();

    [When(@"^chega a chamada (\d+) do mês ao endpoint ""(.*)"" do mesmo recurso e cliente$")]
    public async Task ChamadaDoMes(int numero, string endpoint)
    {
        var operacao = endpoint.EndsWith("/balances", StringComparison.Ordinal) ? Saldos : Conta;
        await ChamarOperacao(operacao);
        Status(200);
        _contadores.DefinirCota(ContaDoCliente, numero - 1);
        await ChamarOperacao(operacao);
    }

    [Given(@"^que uma chamada respondeu 500$")]
    public async Task Respondeu500()
    {
        await Preparar();
        _falha.Ligada = true;
        await ChamarOperacao(Saldos);
        Assert.Equal(HttpStatusCode.InternalServerError, _resposta!.StatusCode);
    }

    [Given(@"^que uma chamada respondeu 429$")]
    public async Task Respondeu429()
    {
        await Preparar();
        DefinirTrafego(Conta, 1000);
        await ChamarOperacao(Conta);
        Status(429);
    }

    [Then(@"^a cota do mês não deve mudar$")]
    public void CotaIgual() => Assert.Equal(0, _contadores.TotalDaCota);

    [Given(@"^uma consulta com 3 páginas$")]
    public Task TresPaginas() => Preparar();

    [When(@"^Ana lê as 3 páginas$")]
    public async Task LeTres()
    {
        await Chamar(TransacoesDaFatura(Fatura()));
        await Seguir(Proxima());
        await Seguir(Proxima());
        Assert.Equal(200, (int)_resposta!.StatusCode);
    }

    private string Proxima() => Json.GetProperty("links").GetProperty("next").GetString()!;

    private Task Seguir(string url) => Chamar(new Uri(new Uri(url).PathAndQuery, UriKind.Relative));

    [Then(@"^a cota deve subir 1$")]
    public void Subiu1() => Assert.Equal(1, _contadores.TotalDaCota);

    [Given(@"^que a conta ""A"" esgotou a cota de saldos$")]
    public async Task ContaAEsgotou()
    {
        await Preparar(contaExtra: true);
        await ChamarOperacao(Saldos);
        Status(200);
        _contadores.DefinirCota(ContaDoCliente, 420);
        await ChamarOperacao(Saldos);
        Status(423);
    }

    [When(@"^Ana consulta o saldo da conta ""B""$")]
    public Task SaldoB() => ChamarOperacao(Saldos, _contaExtra);

    [Given(@"^que a fatura ""A"" esgotou a cota de transações$")]
    public async Task FaturaAEsgotou()
    {
        await Preparar();
        await Chamar(TransacoesDaFatura(Fatura()));
        Assert.Equal(200, (int)_resposta!.StatusCode);
        _contadores.DefinirCota(Fatura(), 30);
    }

    [When(@"^Ana consulta as transações da fatura ""B""$")]
    public Task FaturaB() => Chamar(TransacoesDaFatura(Fatura("-b202609")));

    [Given(@"^uma consulta com 3 páginas que gastou a última unidade da cota$")]
    public async Task GastouAUltima()
    {
        await Preparar();
        await Chamar(TransacoesDaFatura(Fatura()));
        Assert.Equal(200, (int)_resposta!.StatusCode);
        _contadores.DefinirCota(Fatura(), 30);
    }

    [When(@"^Ana pede a página seguinte com a chave recebida$")]
    public Task ComChaveRecebida() => Seguir(Proxima());

    [When(@"^Ana pede a página seguinte com uma chave inválida$")]
    public async Task ComChaveInvalida()
    {
        _operacao = "creditCardsGetAccountsCreditCardAccountIdBillsBillIdTransactions";
        var chave = Proxima().Split("pagination-key=")[1].Split('&')[0];
        await Seguir(Proxima().Replace(chave, chave + "x", StringComparison.Ordinal));
    }

    // Capacidade
    [Given(@"^que o ""Banco Azul"" tem capacidade de (\d+) TPS$")]
    public async Task Capacidade(int tps)
    {
        await Preparar();
        await Configurar(new { capacidadeTps = tps });
        _fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
    }

    [When(@"^chegam 301 chamadas no mesmo segundo$")]
    public async Task Chegam301()
    {
        _operacao = "resourcesGetResources";
        for (var i = 0; i < 301; i++)
        {
            await Chamar(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"));
        }

        Assert.Single(_status, (HttpStatusCode)529);
    }

    [When(@"^chegam no mesmo segundo uma chamada de contas, uma de recursos e uma de consentimentos$")]
    public async Task TresRotas()
    {
        await ChamarOperacao(Conta);
        await Chamar(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"));
        _operacao = "consentsGetConsentsConsentId";
        _token = await Token("consents");
        await Chamar(AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents/{consentId}", _consentId));
    }

    // Instâncias
    [Given(@"^duas instâncias do simulador$")]
    public async Task DuasInstancias()
    {
        await Preparar();
        await Configurar(new { limiteOperacionalLigado = false });
        var instituicoes = _fabrica.Services.GetRequiredService<IRepositorioDeInstituicoes>();
        var massa = _fabrica.Services.GetRequiredService<IRepositorioDeMassa>();
        var receptoras = _fabrica.Services.GetRequiredService<IRepositorioDeReceptoras>();
        _outra = new FabricaDoSimulador()
            .ComContadoresEmMemoria(_contadores)
            .Com("Simulador:Token:Chave", ChaveDoToken)
            .ComServicos(s =>
            {
                s.RemoveAll<IRepositorioDeInstituicoes>();
                s.AddSingleton(instituicoes);
                s.RemoveAll<IRepositorioDeMassa>();
                s.AddSingleton(massa);
                s.RemoveAll<IRepositorioDeReceptoras>();
                s.AddSingleton(receptoras);
            });
        _outra.Relogio.SetUtcNow(Agora);
        _apiDaOutra = _outra.ClienteOpenFinance();
    }

    [When(@"^1001 chamadas se dividem entre elas no mesmo minuto$")]
    public async Task SeDividem()
    {
        for (var i = 0; i < 1001; i++)
        {
            ProximoSegundoSe(i, 200);
            await ChamarOperacao(Conta, cliente: i % 2 == 0 ? _api : _apiDaOutra);
        }
    }

    [Then(@"^o limite deve ser aplicado sobre o total$")]
    public void SobreOTotal()
    {
        Assert.Single(_status, HttpStatusCode.TooManyRequests);
        Status(429);
    }

    // Ordem
    [Given(@"^que o endpoint já passou do TPM$")]
    public async Task PassouDoTpm()
    {
        await Preparar();
        DefinirTrafego(Conta, 1000);
    }

    [When(@"^Ana chama com consentimento revogado$")]
    public async Task ComRevogado()
    {
        using var revogar = await _admin.PostAsync(new Uri($"/admin/consentimentos/{Uri.EscapeDataString(_consentId)}/revogar", UriKind.Relative), null);
        Assert.True(revogar.IsSuccessStatusCode, await revogar.Content.ReadAsStringAsync());
        await ChamarOperacao(Conta);
    }

    [When(@"^Ana chama com a conta de outro cliente$")]
    public Task ComContaAlheia() => ChamarOperacao(Conta, "conta-de-outro-cliente");

    // QCA e configuração
    [Given(@"^que o ""HML-Itaú"" tem QCA de 7\.000\.000$")]
    public async Task Qca7Milhoes()
    {
        await Preparar();
        await Configurar(new { qca = 7_000_000L });
    }

    [Given(@"^que a receptora ""Coletor"" tem QCA de 500\.000 nesse banco$")]
    public Task QcaDoColetor() => Configurar(new { qcaPorReceptora = new Dictionary<string, long> { [_organizacao] = 500_000L } });

    [When(@"^uma receptora sem valor próprio chama um endpoint de alta frequência$")]
    [When(@"^""Coletor"" chama um endpoint de alta frequência$")]
    public Task ChamaAlta() => ChamarOperacao(Saldos);

    [Then(@"^o TPM deve ser o da faixa de 7\.000\.000$")]
    public Task Faixa7Milhoes() => ConferirTpm(12_000);

    [Then(@"^o TPM deve ser 2\.500$")]
    public Task Tpm2500() => ConferirTpm(2_500);

    [Given(@"^o ""Banco Azul"" com os limites oficiais$")]
    public Task Oficiais() => Preparar();

    [When(@"^Diego muda o limite de baixa frequência para 5$")]
    public Task MudaBaixa() => Configurar(new { limiteMensal = new { baixa = 5 } });

    [Then(@"^a configuração deve ser salva$")]
    public void Salva() => Assert.Equal(5, Json.GetProperty("limites").GetProperty("limiteMensal").GetProperty("baixa").GetInt32());

    [Then(@"^a tela deve avisar que o valor está abaixo do oficial$")]
    public void Avisa() => Assert.Contains(Json.GetProperty("avisos").EnumerateArray(), a => a.GetProperty("campo").GetString() == "limiteMensal.baixa");

    [Then(@"^a 6ª chamada do mês deve responder 423$")]
    public async Task SextaChamada()
    {
        await ChamarOperacao(Conta);
        Status(200);
        _contadores.DefinirCota(ContaDoCliente, 5);
        await ChamarOperacao(Conta);
        Status(423);
    }

    public void Dispose()
    {
        _resposta?.Dispose();
        _api.Dispose();
        _admin.Dispose();
        _apiDaOutra?.Dispose();
        _outra?.Dispose();
        _fabrica.Dispose();
    }

    /// <summary>Liga a falha de dados dos cenários de erro.</summary>
    private sealed class Falha
    {
        public bool Ligada { get; set; }
    }

    /// <summary>Faz a leitura de saldo falhar, para o cenário de 500 (Q11).</summary>
    private sealed class AmbienteComFalha(IAmbienteDosDados interno, Falha falha) : IAmbienteDosDados
    {
        public ConsentimentoDaChamada? Consentimento(HttpContext contexto) => interno.Consentimento(contexto);

        public Task<RecursoDeDados?> Recurso(string resourceId, CancellationToken cancelamento) => interno.Recurso(resourceId, cancelamento);

        public Task<RecursoDeDados?> RecursoNoInstante(string resourceId, DateTimeOffset instante, CancellationToken cancelamento) => interno.RecursoNoInstante(resourceId, instante, cancelamento);

        public Task<ClienteDeDados?> Cliente(string documento, CancellationToken cancelamento) => interno.Cliente(documento, cancelamento);

        public Task<IReadOnlyList<TransacaoDeDados>> Transacoes(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, CancellationToken cancelamento) =>
            interno.Transacoes(resourceId, de, ate, instante, cancelamento);

        public Task<decimal> Saldo(string resourceId, CancellationToken cancelamento) =>
            falha.Ligada ? throw new InvalidOperationException("Falha provocada pelo teste.") : interno.Saldo(resourceId, cancelamento);

        public Task<LimitesDePaginacao> Limites(string banco, CancellationToken cancelamento) => interno.Limites(banco, cancelamento);

        public void RegistrarNatureza(HttpContext contexto, string natureza) => interno.RegistrarNatureza(contexto, natureza);

        public Task<ICotaDeDados?> Cota(HttpContext contexto, OperacaoDeDados operacao, ConsentimentoDaChamada consentimento, CancellationToken cancelamento) =>
            interno.Cota(contexto, operacao, consentimento, cancelamento);
    }

    /// <summary>O servidor de testes repassa exceções; aqui elas viram 500, como no Kestrel.</summary>
    private sealed class FiltroDeErroInterno : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (contexto, proximo) =>
            {
                try
                {
                    await proximo();
                }
                catch (InvalidOperationException)
                {
                    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });
            next(app);
        };
    }
}
