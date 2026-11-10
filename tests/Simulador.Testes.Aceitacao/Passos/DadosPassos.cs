using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de dados-e-transacoes e paginacao (spec 005).</summary>
[Binding]
[Scope(Feature = "Consultar dados cadastrais e transacionais")]
[Scope(Feature = "Paginar como o Open Finance")]
public sealed class DadosPassos : IDisposable
{
    private const string Banco = "banco-teste";
    private const string Perfil = "fixo";
    private const string InicioDoMes = "2026-10";
    private static readonly string[] ProdutosDoPerfil = ["contas", "cartoes", "fundos"];

    private readonly FabricaDoSimulador _fabrica = new FabricaDoSimulador().ComContadoresEmMemoria();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private readonly AdminDaMassa _massa;

    private string _usuario = string.Empty;
    private string _senha = string.Empty;
    private string _consentId = string.Empty;
    private string _token = string.Empty;
    private MassaGerada? _cliente;
    private Uri? _lista;
    private string _proximo = string.Empty;
    private string _chaveEnviada = string.Empty;
    private string _provisorio = string.Empty;
    private decimal _somaEfetivadas;
    private long _cotaAntes;
    private HttpResponseMessage? _resposta;
    private string _corpo = string.Empty;
    private readonly List<string> _lidos = [];

    public DadosPassos()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
        _massa = new AdminDaMassa(_admin);
    }

    private JsonElement Json => JsonDocument.Parse(_corpo).RootElement;

    private JsonElement.ArrayEnumerator Dados => Json.GetProperty("data").EnumerateArray();

    private DateOnly Hoje => DateOnly.FromDateTime(_fabrica.Relogio.GetUtcNow().UtcDateTime);

    private static string Data(DateOnly dia) => dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private DateOnly Dia(string texto) => texto == "hoje" ? Hoje : Hoje.AddDays(int.Parse(texto[1..], CultureInfo.InvariantCulture));

    private async Task Preparar(int porDia, string[] tipos, string[] permissoes, int volumeMaximo = -1)
    {
        (_usuario, _senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var maximo = volumeMaximo < 0 ? porDia : volumeMaximo;
        await _massa.Enviar<PerfilDeDados>(HttpMethod.Put, $"/perfis/{Perfil}", new { transacoesMin = porDia, transacoesMax = maximo, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = tipos, produtos = ProdutosDoPerfil });
        _cliente = await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco }, perfil = Perfil });
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), await Token("consents"), new
        {
            data = new { loggedUser = new { document = new { identification = _cliente.Cliente.Documento, rel = "CPF" } }, permissions = permissoes },
        });
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        _consentId = corpo.RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
        await RenovarToken();
    }

    private static string[] TodasAsPermissoes() => [.. Permissoes.Para("PF", null)];

    private async Task RenovarToken() => _token = await Token($"consent:{_consentId}");

    private async Task<string> Token(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, _usuario, _senha, escopo);
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    private string Id(string tipo) => _cliente!.Recursos.First(r => r.Tipo == tipo).ResourceId;

    private string Conta => Id("ACCOUNT");

    private static Uri Caminho(string contrato, string caminho, string id, string consulta = "") =>
        new(AtalhosDeConsentimento.Caminho(Banco, contrato, caminho).ToString()
            .Replace("{accountId}", id, StringComparison.Ordinal)
            .Replace("{creditCardAccountId}", id, StringComparison.Ordinal)
            .Replace("{investmentId}", id, StringComparison.Ordinal)
            .Replace("{billId}", $"{id}-b202610", StringComparison.Ordinal) + consulta, UriKind.Relative);

    private Uri Transacoes(string consulta = "") => Caminho("accounts", "/accounts/{accountId}/transactions", Conta, consulta);

    private Uri Fatura(string consulta = "") => Caminho("credit-cards-accounts", "/accounts/{creditCardAccountId}/bills/{billId}/transactions", Id("CREDIT_CARD_ACCOUNT"), consulta);

    private async Task Chamar(Uri caminho, string? correlacao = AtalhosDeConsentimento.Correlacao)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
        if (correlacao is not null)
        {
            requisicao.Headers.Add("x-fapi-interaction-id", correlacao);
        }

        requisicao.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        _resposta?.Dispose();
        _resposta = await _api.SendAsync(requisicao);
        _corpo = await _resposta.Content.ReadAsStringAsync();
    }

    private async Task Seguir(string url) => await Chamar(new Uri(new Uri(url).PathAndQuery, UriKind.Relative));

    private string Link(string nome) => Json.GetProperty("links").GetProperty(nome).GetString()!;

    private static string Chave(string url) => Uri.UnescapeDataString(url.Split("pagination-key=")[1].Split('&')[0]);

    private void GuardarIds() => _lidos.AddRange(Dados.Select(d => d.GetProperty("transactionId").GetString()!));

    private async Task ListaDeTresPaginas()
    {
        await Preparar(2, ["PIX"], TodasAsPermissoes());
        _lista = Fatura();
        await Chamar(_lista);
        Assert.Equal(62, Json.GetProperty("meta").GetProperty("totalRecords").GetInt32());
    }

    // dados-e-transacoes.feature
    [Given(@"^que Mariana tem um consentimento autorizado no ""Banco Estável""$")]
    public Task Contexto() => Preparar(6, ["PIX", "BOLETO"], TodasAsPermissoes());

    [When(@"^Ana consulta contas sem x-fapi-interaction-id$")]
    public Task SemCorrelacao() => Chamar(Caminho("accounts", "/accounts", string.Empty), null);

    [When(@"^Ana consulta contas com x-fapi-interaction-id válido$")]
    public Task ComCorrelacao() => Chamar(Caminho("accounts", "/accounts", string.Empty));

    [Then(@"^a resposta deve ter status (\d+)$")]
    public void Status(int esperado) => Assert.True((int)_resposta!.StatusCode == esperado, $"{_resposta.StatusCode}: {_corpo}");

    [Then(@"^a resposta deve trazer um x-fapi-interaction-id gerado$")]
    public void CorrelacaoGerada() => Assert.Matches("^[0-9a-f-]{36}$", _resposta!.Headers.GetValues("x-fapi-interaction-id").Single());

    [Then(@"^a resposta deve trazer o mesmo valor$")]
    public void MesmaCorrelacao() => Assert.Equal(AtalhosDeConsentimento.Correlacao, _resposta!.Headers.GetValues("x-fapi-interaction-id").Single());

    [When(@"^Ana consulta o saldo$")]
    public Task ConsultaSaldo() => Chamar(Caminho("accounts", "/accounts/{accountId}/balances", Conta));

    [Then(@"^a resposta deve validar contra a spec da versão fixada$")]
    public void ValidaContrato() => Assert.Empty(ValidadorDeContrato.Validar("accountsGetAccountsAccountIdBalances", _corpo));

    [When(@"^Ana consulta ""(transactions|transactions-current)"" de (\S+) até (\S+)$")]
    public Task ConsultaJanela(string endpoint, string de, string ate) =>
        Chamar(Caminho("accounts", $"/accounts/{{accountId}}/{endpoint}", Conta, $"?fromBookingDate={Data(Dia(de))}&toBookingDate={Data(Dia(ate))}&page-size=1000"));

    [When(@"^Ana consulta o cartão com data final em D\+1$")]
    public Task CartaoFuturo() => Chamar(Caminho("credit-cards-accounts", "/accounts/{creditCardAccountId}/transactions", Id("CREDIT_CARD_ACCOUNT"), $"?fromTransactionDate={Data(Hoje)}&toTransactionDate={Data(Hoje.AddDays(1))}"));

    [Then(@"^nenhuma transação deve passar de D\+30$")]
    public void AteD30()
    {
        Assert.NotEmpty(Dados);
        Assert.All(Dados, d => Assert.True(DateOnly.FromDateTime(d.GetProperty("transactionDateTime").GetDateTime()) <= Hoje.AddDays(30)));
    }

    [Then(@"^todas devem estar em ""(.*)""$")]
    public void TodasEm(string estado) => Assert.All(Dados, d => Assert.Equal(estado, d.GetProperty("completedAuthorisedPaymentType").GetString()));

    [Given(@"^que existem lançamentos futuros na conta$")]
    public async Task ExistemFuturos()
    {
        await Chamar(Transacoes($"?fromBookingDate={Data(Hoje.AddDays(1))}&toBookingDate={Data(Hoje.AddDays(30))}"));
        Assert.NotEmpty(Dados);
    }

    [When(@"^Ana consulta transações só com a data inicial$")]
    public Task SoInicial() => Chamar(Transacoes($"?fromBookingDate={Data(Hoje)}"));

    [When(@"^Ana consulta transações sem datas$")]
    [When(@"^Ana consulta transações$")]
    public Task SemDatas() => Chamar(Transacoes());

    [Then(@"^só devem vir transações de hoje$")]
    public void SoHoje()
    {
        Assert.NotEmpty(Dados);
        Assert.All(Dados, d => Assert.Equal(Hoje, DateOnly.FromDateTime(d.GetProperty("transactionDateTime").GetDateTime())));
    }

    [Then(@"^devem vir da mais recente para a mais antiga$")]
    public void Ordem()
    {
        var datas = Dados.Select(d => d.GetProperty("transactionDateTime").GetDateTime()).ToList();
        Assert.True(datas.Count > 1);
        Assert.Equal(datas.OrderDescending(), datas);
    }

    [Given(@"^uma transação em processamento$")]
    public async Task EmProcessamento()
    {
        await Chamar(Transacoes());
        var processando = Dados.First(d => d.GetProperty("completedAuthorisedPaymentType").GetString() == EstadosDeTransacao.Processando);
        _provisorio = processando.GetProperty("transactionId").GetString()!;
    }

    [When(@"^ela é efetivada$")]
    public async Task EFetivada()
    {
        var ontem = Hoje;
        _fabrica.Relogio.Advance(TimeSpan.FromDays(1));
        await RenovarToken();
        await Chamar(Transacoes($"?fromBookingDate={Data(ontem)}&toBookingDate={Data(ontem)}"));
    }

    [Then(@"^só o identificador definitivo deve aparecer$")]
    public void SoDefinitivo()
    {
        var ids = Dados.Select(d => d.GetProperty("transactionId").GetString()).ToList();
        Assert.DoesNotContain(_provisorio, ids);
        Assert.Contains(_provisorio.Replace("-p2", "-t2", StringComparison.Ordinal), ids);
    }

    [When(@"^Ana consulta uma transação PIX$")]
    public async Task UmaPix()
    {
        await Chamar(Transacoes());
        Assert.Contains(Dados, d => d.GetProperty("type").GetString() == "PIX");
    }

    [Then(@"^os dados da contraparte devem vir preenchidos$")]
    public void Contraparte() => Assert.All(Dados.Where(d => d.GetProperty("type").GetString() == "PIX"), d =>
    {
        Assert.False(string.IsNullOrEmpty(d.GetProperty("partieCnpjCpf").GetString()));
        Assert.False(string.IsNullOrEmpty(d.GetProperty("partieNumber").GetString()));
    });

    [When(@"^Ana soma as transações efetivadas ao saldo inicial$")]
    public async Task SomaEfetivadas()
    {
        var recurso = _cliente!.Recursos.First(r => r.Tipo == "ACCOUNT");
        var dados = await _massa.Ler<JsonElement>($"/recursos/{recurso.ResourceId}/transacoes?de={Data(recurso.InicioDoHistorico)}&ate={Data(Hoje.AddDays(30))}");
        var efetivadas = dados.GetProperty("transacoes").EnumerateArray().Where(t => t.GetProperty("estado").GetString() == EstadosDeTransacao.Efetivada);
        _somaEfetivadas = recurso.SaldoInicial + efetivadas.Sum(t => t.GetProperty("valor").GetDecimal() * (t.GetProperty("direcao").GetString() == "CREDITO" ? 1 : -1));
    }

    [Then(@"^o resultado deve ser igual ao saldo informado$")]
    public async Task IgualAoSaldo()
    {
        await ConsultaSaldo();
        var saldo = decimal.Parse(Json.GetProperty("data").GetProperty("availableAmount").GetProperty("amount").GetString()!, CultureInfo.InvariantCulture);
        Assert.Equal(_somaEfetivadas, saldo);
    }

    [Given(@"^um consentimento só com permissões cadastrais$")]
    public Task SoCadastrais() =>
        Preparar(1, ["PIX"], [.. Permissoes.Para("PF", null).Where(p => p.StartsWith("CUSTOMERS_", StringComparison.Ordinal) || p == "RESOURCES_READ")]);

    [When(@"^Ana consulta recursos$")]
    public Task ConsultaRecursos() => Chamar(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources"));

    [Then(@"^a resposta deve ter status 200 com lista vazia$")]
    public void ListaVazia()
    {
        Status(200);
        Assert.Empty(Dados);
    }

    // paginacao.feature
    [When(@"^Ana pede transações sem page e page-size$")]
    public async Task SemPaginacao()
    {
        await Preparar(30, ["PIX"], TodasAsPermissoes());
        await Chamar(Transacoes());
    }

    [Then(@"^a resposta deve ter até (\d+) registros$")]
    public void AteRegistros(int maximo)
    {
        Assert.NotEmpty(Dados);
        Assert.True(Dados.Count() <= maximo);
    }

    [Given(@"^uma lista de (fundos|contas) com 47 registros$")]
    public async Task Lista47(string familia)
    {
        await Preparar(47, ["PIX"], TodasAsPermissoes());
        _lista = familia == "contas" ? Transacoes() : Caminho("funds", "/investments/{investmentId}/transactions", Id("FUND"));
    }

    [When(@"^Ana pede page-size (\d+)$")]
    public async Task PedePageSize(int tamanho)
    {
        _lista ??= await ListaParaLimites();
        await Chamar(new Uri($"{_lista}?page-size={tamanho}", UriKind.Relative));
        _proximo = Json.TryGetProperty("links", out var links) && links.TryGetProperty("next", out var next) ? next.GetString()! : string.Empty;
    }

    private async Task<Uri> ListaParaLimites()
    {
        await Preparar(60, ["PIX"], TodasAsPermissoes());
        return Fatura();
    }

    [Then(@"^a primeira página deve ter (\d+) registros$")]
    public void Primeira(int esperado) => Assert.Equal(esperado, Dados.Count());

    [Then(@"^a segunda deve ter (\d+) registros$")]
    public async Task Segunda(int esperado)
    {
        await Seguir(_proximo);
        Assert.Equal(esperado, Dados.Count());
    }

    [Then(@"^o link self deve mostrar page-size (\d+)$")]
    public void SelfMostra(int tamanho) => Assert.Contains($"page-size={tamanho}", Link("self"), StringComparison.Ordinal);

    [Given(@"^uma lista com 3 páginas$")]
    [Given(@"^uma consulta com 3 páginas$")]
    public Task TresPaginas() => ListaDeTresPaginas();

    [When(@"^Ana pede a página (\d+)$")]
    public Task PedePagina(int pagina) => Chamar(Fatura($"?page={pagina}"));

    [Then(@"^a resposta deve ter status (\d+) com ""(.*)""$")]
    public void StatusComCodigo(int esperado, string codigo)
    {
        Status(esperado);
        Assert.Contains(codigo, _corpo, StringComparison.Ordinal);
    }

    [When(@"^Ana pede a primeira página$")]
    public Task PrimeiraPagina() => Chamar(Fatura());

    [When(@"^Ana pede a última página$")]
    public Task UltimaPagina() => Chamar(Fatura("?page=3"));

    [Then(@"^links deve ter (.*)$")]
    public void LinksTem(string nomes)
    {
        foreach (var nome in Nomes(nomes))
        {
            Assert.True(Json.GetProperty("links").TryGetProperty(nome, out _), nome);
        }
    }

    [Then(@"^links não deve ter (.*)$")]
    public void LinksNaoTem(string nomes)
    {
        foreach (var nome in Nomes(nomes))
        {
            Assert.False(Json.GetProperty("links").TryGetProperty(nome, out _), nome);
        }
    }

    private static string[] Nomes(string texto) => texto.Replace(" e ", ",", StringComparison.Ordinal).Replace(" nem ", ",", StringComparison.Ordinal).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    [Given(@"^que o ""Banco Restritivo"" tem máximo de 800 registros$")]
    public async Task Maximo800()
    {
        _lista = await ListaParaLimites();
        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, new Uri($"/admin/instituicoes/{Banco}", UriKind.Relative)) { Content = JsonContent.Create(new { limites = new { pageSizeMaximo = 800 } }) };
        using var resposta = await _admin.SendAsync(requisicao);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    [When(@"^Ana pede a página 2 com page-size 1000$")]
    public Task Pagina2De1000() => Chamar(new Uri($"{_lista}?page=2&page-size=1000", UriKind.Relative));

    [Then(@"^a resposta deve trazer os itens 801 a 1600$")]
    public void Itens801()
    {
        Assert.Equal(800, Dados.Count());
        Assert.Contains($"-t{InicioDoMes.Replace("-", string.Empty, StringComparison.Ordinal)}18x20", Dados.First().GetProperty("transactionId").GetString(), StringComparison.Ordinal);
        SelfMostra(800);
    }

    [Given(@"^que a única conta do consentimento está indisponível$")]
    public async Task ContaIndisponivel()
    {
        await Preparar(1, ["PIX"], TodasAsPermissoes());
        using var resposta = await _admin.PutAsJsonAsync(new Uri($"/admin/recursos/{Conta}/estado", UriKind.Relative), new { estado = "TEMPORARILY_UNAVAILABLE" });
        resposta.EnsureSuccessStatusCode();
    }

    [When(@"^Ana lista contas$")]
    public Task ListaContas() => Chamar(Caminho("accounts", "/accounts", string.Empty));

    [Then(@"^meta\.(totalRecords|totalPages) deve ser 0$")]
    public void MetaZero(string campo) => Assert.Equal(0, Json.GetProperty("meta").GetProperty(campo).GetInt32());

    [Given(@"^que a conta não tem transações no período$")]
    public Task SemTransacoes() => Preparar(0, ["PIX"], TodasAsPermissoes());

    [Then(@"^meta não deve ter totais$")]
    public void SemTotais()
    {
        Assert.Empty(Dados);
        Assert.False(Json.GetProperty("meta").TryGetProperty("totalRecords", out _));
    }

    [Given(@"^uma chave ""(expirada|inválida)""$")]
    public async Task ChaveRuim(string estado)
    {
        await ListaDeTresPaginas();
        _proximo = Link("next");
        var chave = Chave(_proximo);
        if (estado == "expirada")
        {
            _chaveEnviada = chave;
            _fabrica.Relogio.Advance(TimeSpan.FromMinutes(61));
            await RenovarToken();
        }
        else
        {
            _chaveEnviada = chave + "x";
            _proximo = _proximo.Replace(Uri.EscapeDataString(chave), Uri.EscapeDataString(_chaveEnviada), StringComparison.Ordinal);
        }
    }

    // RN113 (spec 009)
    [Given(@"^que o ""Banco Restritivo"" tem a anomalia ""(.*)""$")]
    public async Task ComAnomalia(string anomalia)
    {
        using var resposta = await _admin.PostAsync(new Uri($"/admin/cenarios/anomalias/{Banco}/{anomalia}/ligar", UriKind.Relative), null);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    [Given(@"^uma chave expirada$")]
    public Task ChaveExpirada() => ChaveRuim("expirada");

    [When(@"^Ana pede a página seguinte$")]
    public async Task PaginaSeguinte()
    {
        _cotaAntes = Cota;
        await Seguir(_proximo);
    }

    [Given(@"^uma chave emitida há 59 minutos$")]
    public async Task ChaveDe59Minutos()
    {
        await ListaDeTresPaginas();
        _proximo = Link("next");
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(59));
        await RenovarToken();
    }

    [Then(@"^a cota não deve mudar$")]
    public void CotaNaoMudou()
    {
        Status(200);
        Assert.Equal(_cotaAntes, Cota);
    }

    [Then(@"^a cota deve subir 1$")]
    public void CotaSubiu()
    {
        Status(200);
        Assert.Equal(_cotaAntes + 1, Cota);
    }

    private long Cota => ((ContadoresEmMemoria)_fabrica.Contadores).TotalDaCota;

    [Then(@"^os links devem trazer uma nova chave$")]
    public void NovaChave() => Assert.NotEqual(_chaveEnviada, Chave(Link("self")));

    [When(@"^Ana lê as 3 páginas com a mesma chave$")]
    public async Task LeTres()
    {
        GuardarIds();
        await Seguir(Link("next"));
        GuardarIds();
        await Seguir(Link("next"));
        GuardarIds();
    }

    [Then(@"^nenhum registro deve se repetir ou faltar$")]
    public void SemRepetir()
    {
        Assert.Equal(62, _lidos.Count);
        Assert.Equal(62, _lidos.Distinct(StringComparer.Ordinal).Count());
    }

    public void Dispose()
    {
        _resposta?.Dispose();
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
