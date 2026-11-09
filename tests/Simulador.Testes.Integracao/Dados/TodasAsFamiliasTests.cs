using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Catalogo.Contratos;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Dados;

/// <summary>RNF01, FR-001 e FR-004: os 69 endpoints de dados validam contra o contrato travado.</summary>
public sealed class TodasAsFamiliasTests : IDisposable
{
    private const string Banco = "banco-teste";
    private const string Perfil = "completo";

    private static readonly string[] SoPix = ["PIX"];

    private static readonly string[] Todos =
    [
        "contas", "cartoes", "emprestimos", "financiamentos", "adiantamentos", "direitos-creditorios",
        "investimentos", "renda-fixa-credito", "renda-variavel", "tesouro-direto", "fundos", "cambio",
    ];

    private readonly EspiaoDeDados _espiao = new();
    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private readonly AdminDaMassa _massa;
    private string _usuario = string.Empty;
    private string _senha = string.Empty;
    private string _consentId = string.Empty;

    public TodasAsFamiliasTests()
    {
        _fabrica = new FabricaDoSimulador().ComEspiaoDeDados(_espiao);
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
        _massa = new AdminDaMassa(_admin);
    }

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    private static IReadOnlyList<OperacaoDoCatalogo> Operacoes() =>
        [.. ServicosDeSpecs.LerOperacoes(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs())
            .Where(o => o.Contrato is not ("consents" or "resources"))];

    private async Task<(MassaGerada Massa, string Token)> Preparar(string tipo, params string[] produtosExtras)
    {
        if (_usuario.Length == 0)
        {
            (_usuario, _senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
            await _massa.Enviar<PerfilDeDados>(HttpMethod.Put, $"/perfis/{Perfil}", new { transacoesMin = 3, transacoesMax = 3, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = SoPix, produtos = Todos });
        }

        var massa = await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo, bancos = new[] { Banco }, perfil = Perfil });
        foreach (var produto in produtosExtras)
        {
            await AdicionarProduto(massa.Cliente.Documento, produto);
        }

        var token = await Token("consents");
        var cpf = tipo == "PF" ? massa.Cliente.Documento : (await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco } })).Cliente.Documento;
        object dados = tipo == "PF"
            ? new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = Permissoes.Para(tipo, null) }
            : new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, businessEntity = new { document = new { identification = massa.Cliente.Documento, rel = "CNPJ" } }, permissions = Permissoes.Para(tipo, null) };
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), token, new { data = dados });
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync(Cancelamento));
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento));
        _consentId = corpo.RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
        return (massa, await Token($"consent:{_consentId}"));
    }

    private async Task<string> Token(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, _usuario, _senha, escopo);
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento));
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    private static string TipoDo(string contrato) => contrato switch
    {
        "accounts" => "ACCOUNT",
        "credit-cards-accounts" => "CREDIT_CARD_ACCOUNT",
        "loans" => "LOAN",
        "financings" => "FINANCING",
        "unarranged-accounts-overdraft" => "UNARRANGED_ACCOUNT_OVERDRAFT",
        "invoice-financings" => "INVOICE_FINANCING",
        "bank-fixed-incomes" => "BANK_FIXED_INCOME",
        "credit-fixed-incomes" => "CREDIT_FIXED_INCOME",
        "variable-incomes" => "VARIABLE_INCOME",
        "treasure-titles" => "TREASURE_TITLE",
        "funds" => "FUND",
        _ => "EXCHANGE",
    };

    private static Uri Endereco(OperacaoDoCatalogo operacao, MassaGerada massa)
    {
        var caminho = operacao.CaminhoPublicado.Replace("{banco}", Banco, StringComparison.Ordinal);
        if (operacao.Contrato == "customers")
        {
            return new Uri(caminho, UriKind.Relative);
        }

        var id = massa.Recursos.First(r => r.Tipo == TipoDo(operacao.Contrato)).ResourceId;
        var fatura = string.Create(CultureInfo.InvariantCulture, $"{id}-b202610");
        return new Uri(
            caminho.Replace("{accountId}", id, StringComparison.Ordinal)
                .Replace("{creditCardAccountId}", id, StringComparison.Ordinal)
                .Replace("{contractId}", id, StringComparison.Ordinal)
                .Replace("{investmentId}", id, StringComparison.Ordinal)
                .Replace("{operationId}", id, StringComparison.Ordinal)
                .Replace("{billId}", fatura, StringComparison.Ordinal)
                .Replace("{brokerNoteId}", id + "-n1", StringComparison.Ordinal),
            UriKind.Relative);
    }

    [Fact]
    public async Task Toda_operacao_de_dados_responde_valido()
    {
        var (pf, tokenPf) = await Preparar("PF");
        var (pj, tokenPj) = await Preparar("PJ");
        var operacoes = Operacoes();
        Assert.Equal(69, operacoes.Count);

        var falhas = new List<string>();
        foreach (var operacao in operacoes)
        {
            var empresarial = operacao.Caminho.StartsWith("/business", StringComparison.Ordinal);
            var massa = empresarial ? pj : pf;
            using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Endereco(operacao, massa), empresarial ? tokenPj : tokenPf);
            var corpo = await resposta.Content.ReadAsStringAsync(Cancelamento);
            if (resposta.StatusCode != HttpStatusCode.OK)
            {
                falhas.Add($"{operacao.OperationId}: {resposta.StatusCode} {corpo}");
                continue;
            }

            falhas.AddRange(ValidadorDeContrato.Validar(operacao.OperationId, corpo).Select(e => $"{operacao.OperationId}: {e}"));
        }

        Assert.True(falhas.Count == 0, string.Join('\n', falhas.Take(60)));
    }

    [Fact]
    public async Task A_mesma_consulta_da_o_mesmo_resultado()
    {
        var (massa, token) = await Preparar("PF");
        var operacao = Operacoes().First(o => o.OperationId.Contains("Transactions", StringComparison.Ordinal) && o.Contrato == "accounts");

        using var primeira = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Endereco(operacao, massa), token);
        using var segunda = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, Endereco(operacao, massa), token);

        Assert.Equal(await primeira.Content.ReadAsStringAsync(Cancelamento), await segunda.Content.ReadAsStringAsync(Cancelamento));
    }

    [Theory]
    [InlineData("?page=0", "PARAMETRO_INVALIDO")]
    [InlineData("?page-size=abc", "PARAMETRO_INVALIDO")]
    [InlineData("?fromBookingDate=2026-11-05", "PARAMETRO_NAO_INFORMADO")]
    [InlineData("?fromBookingDate=2020-01-01&toBookingDate=2020-01-02", "PARAMETRO_INVALIDO")]
    [InlineData("?page=9", "PAGE_NOT_FOUND")]
    public async Task Parametro_ruim_responde_422(string consulta, string codigo)
    {
        var (massa, token) = await Preparar("PF");
        var operacao = Operacoes().First(o => o.Contrato == "accounts" && o.Caminho.EndsWith("/transactions", StringComparison.Ordinal));

        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(Endereco(operacao, massa) + consulta, UriKind.Relative), token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, resposta.StatusCode);
        Assert.Contains(codigo, await resposta.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("-b202611")] // mês aberto
    [InlineData("-bxx")] // malformada
    [InlineData("-b202510")] // antes do histórico do cliente: não existe
    [InlineData("-b202509")] // vencida há mais de 12 meses
    public async Task Fatura_inexistente_ou_fora_da_janela_e_proibida(string sufixo)
    {
        var (massa, token) = await Preparar("PF");
        var operacao = Operacoes().First(o => o.Caminho.Contains("{billId}", StringComparison.Ordinal));
        var endereco = Endereco(operacao, massa).ToString().Replace("-b202610", sufixo, StringComparison.Ordinal);

        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(endereco, UriKind.Relative), token);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Contains("RESOURCE_FORBIDDEN", await resposta.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fatura_de_outro_recurso_e_proibida()
    {
        var (massa, token) = await Preparar("PF");
        var operacao = Operacoes().First(o => o.Caminho.Contains("{billId}", StringComparison.Ordinal));
        var cartao = massa.Recursos.First(r => r.Tipo == "CREDIT_CARD_ACCOUNT").ResourceId;
        var endereco = Endereco(operacao, massa).ToString().Replace($"{cartao}-b", "outro-b", StringComparison.Ordinal);

        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(endereco, UriKind.Relative), token);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Contains("RESOURCE_FORBIDDEN", await resposta.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
    }

    private async Task AdicionarProduto(string documento, string produto)
    {
        using var resposta = await _admin.PostAsJsonAsync(new Uri($"/admin/clientes/{documento}/produtos", UriKind.Relative), new { banco = Banco, produto }, Cancelamento);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync(Cancelamento));
    }

    private async Task<JsonElement> Ler(Uri endereco, string token)
    {
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, endereco, token);
        var texto = await resposta.Content.ReadAsStringAsync(Cancelamento);
        Assert.True(resposta.StatusCode == HttpStatusCode.OK, texto);
        return JsonDocument.Parse(texto).RootElement;
    }

    private static Uri Seguinte(JsonElement pagina) =>
        new(new Uri(pagina.GetProperty("links").GetProperty("next").GetString()!, UriKind.Absolute).PathAndQuery, UriKind.Relative);

    private static OperacaoDoCatalogo Operacao(string operationId) => Operacoes().Single(o => o.OperationId == operationId);

    private static Uri ComConsulta(Uri endereco, string consulta) => new(endereco + consulta, UriKind.Relative);

    private const string JanelaDoMes = "?fromBookingDate=2026-10-01&toBookingDate=2026-10-31&page-size=1000";

    [Fact]
    public async Task Filtro_de_credito_ou_debito_reduz_a_lista_e_entra_na_chave()
    {
        var (massa, token) = await Preparar("PF");
        var endereco = Endereco(Operacao("accountsGetAccountsAccountIdTransactions"), massa);
        var todas = (await Ler(ComConsulta(endereco, JanelaDoMes), token)).GetProperty("data").GetArrayLength();

        var debitos = await Ler(ComConsulta(endereco, JanelaDoMes + "&creditDebitIndicator=DEBITO"), token);
        var creditos = await Ler(ComConsulta(endereco, JanelaDoMes + "&creditDebitIndicator=CREDITO"), token);

        Assert.All(debitos.GetProperty("data").EnumerateArray(), t => Assert.Equal("DEBITO", t.GetProperty("creditDebitType").GetString()));
        Assert.All(creditos.GetProperty("data").EnumerateArray(), t => Assert.Equal("CREDITO", t.GetProperty("creditDebitType").GetString()));
        Assert.True(debitos.GetProperty("data").GetArrayLength() > 0 && creditos.GetProperty("data").GetArrayLength() > 0);
        Assert.Equal(todas, debitos.GetProperty("data").GetArrayLength() + creditos.GetProperty("data").GetArrayLength());

        var paginada = await Ler(ComConsulta(endereco, "?fromBookingDate=2026-10-01&toBookingDate=2026-10-31&page-size=1&creditDebitIndicator=DEBITO"), token);
        var proxima = paginada.GetProperty("links").GetProperty("next").GetString()!;
        var trocada = new Uri(new Uri(proxima.Replace("creditDebitIndicator=DEBITO", "creditDebitIndicator=CREDITO", StringComparison.Ordinal), UriKind.Absolute).PathAndQuery, UriKind.Relative);
        var chaveEnviada = proxima.Split("pagination-key=")[1].Split('&')[0];

        var recusada = await Ler(trocada, token);

        Assert.DoesNotContain(chaveEnviada, recusada.GetProperty("links").GetProperty("self").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Filtro_de_tipo_de_conta_compara_com_a_resposta()
    {
        var (massa, token) = await Preparar("PF");
        var endereco = Endereco(Operacao("accountsGetAccounts"), massa);
        var tipo = (await Ler(endereco, token)).GetProperty("data")[0].GetProperty("type").GetString()!;
        var outro = tipo == "CONTA_POUPANCA" ? "CONTA_DEPOSITO_A_VISTA" : "CONTA_POUPANCA";

        Assert.NotEqual(0, (await Ler(ComConsulta(endereco, $"?accountType={tipo}"), token)).GetProperty("data").GetArrayLength());
        Assert.Equal(0, (await Ler(ComConsulta(endereco, $"?accountType={outro}"), token)).GetProperty("data").GetArrayLength());
    }

    [Fact]
    public async Task Filtros_do_cartao_comparam_com_a_resposta()
    {
        var (massa, token) = await Preparar("PF");
        var endereco = Endereco(Operacao("creditCardsGetAccountsCreditCardAccountIdBillsBillIdTransactions"), massa);
        var dados = (await Ler(endereco, token)).GetProperty("data");
        var primeira = dados[0];
        var tipo = primeira.GetProperty("transactionType").GetString()!;
        var outro = tipo == "CASHBACK" ? "TARIFA" : "CASHBACK";
        var mcc = primeira.GetProperty("payeeMCC").GetRawText();

        Assert.Equal(dados.GetArrayLength(), (await Ler(ComConsulta(endereco, $"?transactionType={tipo}"), token)).GetProperty("data").GetArrayLength());
        Assert.Equal(0, (await Ler(ComConsulta(endereco, $"?transactionType={outro}"), token)).GetProperty("data").GetArrayLength());
        var filtradas = (await Ler(ComConsulta(endereco, $"?payeeMCC={mcc}"), token)).GetProperty("data");
        Assert.True(filtradas.GetArrayLength() is > 0);
        Assert.True(filtradas.GetArrayLength() < dados.GetArrayLength());
        Assert.All(filtradas.EnumerateArray(), t => Assert.Equal(mcc, t.GetProperty("payeeMCC").GetRawText()));
    }

    [Fact]
    public async Task Bills_mostra_a_fatura_fechada_que_ainda_nao_venceu()
    {
        _fabrica.Relogio.SetUtcNow(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero));
        var (massa, token) = await Preparar("PF");

        var faturas = (await Ler(Endereco(Operacao("creditCardsGetAccountsCreditCardAccountIdBills"), massa), token)).GetProperty("data");

        Assert.Contains(faturas.EnumerateArray(), f => f.GetProperty("dueDate").GetString() == "2026-12-10");
    }

    [Fact]
    public async Task Bills_calcula_so_o_periodo_pedido()
    {
        var (massa, token) = await Preparar("PF");
        _espiao.Intervalos.Clear();

        await Ler(ComConsulta(Endereco(Operacao("creditCardsGetAccountsCreditCardAccountIdBills"), massa), "?fromDueDate=2026-10-01&toDueDate=2026-10-31"), token);

        var (de, ate) = Assert.Single(_espiao.Intervalos);
        Assert.Equal(new DateOnly(2026, 9, 1), de);
        Assert.Equal(new DateOnly(2026, 10, 31), ate);
    }

    [Fact]
    public async Task Ajuste_regravado_depois_da_chave_mantem_o_valor_da_emissao()
    {
        var (massa, token) = await Preparar("PF");
        var conta = massa.Recursos.First(r => r.Tipo == "ACCOUNT");
        var dia = _fabrica.Relogio.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await _massa.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = conta.ResourceId, dia, posicao = 1, valor = 111.11m });
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        var primeira = await Ler(ComConsulta(Endereco(Operacao("accountsGetAccountsAccountIdTransactions"), massa), "?page-size=1"), token);
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        await _massa.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = conta.ResourceId, dia, posicao = 1, valor = 222.22m });

        var segunda = (await Ler(Seguinte(primeira), token)).GetRawText();

        Assert.Contains("111.11", segunda, StringComparison.Ordinal);
        Assert.DoesNotContain("222.22", segunda, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lista_de_recursos_fica_no_instante_da_chave()
    {
        var (massa, token) = await Preparar("PF", "contas");
        var endereco = ComConsulta(Endereco(Operacao("accountsGetAccounts"), massa), "?page-size=1");
        var primeira = await Ler(endereco, token);
        Assert.Equal(2, primeira.GetProperty("meta").GetProperty("totalRecords").GetInt32());
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        var conta = massa.Recursos.First(r => r.Tipo == "ACCOUNT").ResourceId;
        using var mudou = await _admin.PutAsJsonAsync(new Uri($"/admin/recursos/{conta}/estado", UriKind.Relative), new { estado = "TEMPORARILY_UNAVAILABLE" }, Cancelamento);
        mudou.EnsureSuccessStatusCode();

        var segunda = await Ler(Seguinte(primeira), token);

        Assert.Equal(2, segunda.GetProperty("meta").GetProperty("totalRecords").GetInt32());
    }

    [Fact]
    public async Task Recurso_novo_nao_entra_na_lista_da_chave()
    {
        var (massa, token) = await Preparar("PF", "emprestimos");
        var primeira = await Ler(ComConsulta(Endereco(Operacao("loansGetContracts"), massa), "?page-size=1"), token);
        Assert.Equal(2, primeira.GetProperty("meta").GetProperty("totalRecords").GetInt32());
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));
        await AdicionarProduto(massa.Cliente.Documento, "emprestimos");

        var segunda = await Ler(Seguinte(primeira), token);

        Assert.Equal(2, segunda.GetProperty("meta").GetProperty("totalRecords").GetInt32());
    }

    [Fact]
    public async Task Ids_de_dados_cadastrais_trazem_o_prefixo_oficial()
    {
        var (massa, token) = await Preparar("PF");

        var dados = await Ler(Endereco(Operacao("customersGetPersonalIdentifications"), massa), token);

        Assert.StartsWith("hml-p-", dados.GetProperty("data")[0].GetProperty("personalId").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Saldo_so_e_calculado_na_rota_de_saldos()
    {
        var (massa, token) = await Preparar("PF");
        _espiao.Saldos = 0;

        await Ler(Endereco(Operacao("accountsGetAccountsAccountId"), massa), token);
        Assert.Equal(0, _espiao.Saldos);

        await Ler(Endereco(Operacao("accountsGetAccountsAccountIdBalances"), massa), token);
        Assert.Equal(1, _espiao.Saldos);
    }

    [Fact]
    public async Task Transacao_de_hoje_nao_tem_hora_futura()
    {
        var (massa, token) = await Preparar("PF");
        var agora = _fabrica.Relogio.GetUtcNow();

        var dados = (await Ler(Endereco(Operacao("accountsGetAccountsAccountIdTransactions"), massa), token)).GetProperty("data");

        Assert.NotEqual(0, dados.GetArrayLength());
        Assert.All(dados.EnumerateArray(), t =>
        {
            var momento = t.GetProperty("transactionDateTime").GetDateTimeOffset();
            Assert.True(momento <= agora, $"{momento:O}");
            Assert.Equal(agora.UtcDateTime.Date, momento.UtcDateTime.Date);
        });
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page-size=abc")]
    [InlineData("?page-size=0")]
    public async Task Recursos_e_extensoes_recusam_paginacao_invalida(string consulta)
    {
        var (_, token) = await Preparar("PF");
        using var recursos = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(AtalhosDeConsentimento.Caminho(Banco, "resources", "/resources") + consulta, UriKind.Relative), token);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, recursos.StatusCode);
        Assert.Contains("PARAMETRO_INVALIDO", await recursos.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
        using var extensoes = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents/{consentId}/extensions", _consentId) + consulta, UriKind.Relative), await Token("consents"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, extensoes.StatusCode);
        Assert.Contains("PARAMETRO_INVALIDO", await extensoes.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ajuste_depois_da_chave_nao_aparece_nas_paginas_seguintes()
    {
        var (massa, token) = await Preparar("PF");
        var conta = massa.Recursos.First(r => r.Tipo == "ACCOUNT");
        var operacao = Operacoes().First(o => o.Contrato == "accounts" && o.Caminho.EndsWith("/transactions", StringComparison.Ordinal));
        var endereco = new Uri(Endereco(operacao, massa) + "?page-size=1", UriKind.Relative);
        using var primeira = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, endereco, token);
        using var corpo = JsonDocument.Parse(await primeira.Content.ReadAsStringAsync(Cancelamento));
        var proxima = new Uri(corpo.RootElement.GetProperty("links").GetProperty("next").GetString()!, UriKind.Absolute);
        var dia = _fabrica.Relogio.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(1));

        await _massa.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = conta.ResourceId, dia, posicao = 1, valor = 123.45m });

        using var segunda = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(proxima.PathAndQuery, UriKind.Relative), token);
        Assert.DoesNotContain("123.45", await segunda.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chave_de_recurso_virtual_atravessa_a_meia_noite()
    {
        _fabrica.Relogio.SetUtcNow(new DateTimeOffset(2026, 11, 5, 23, 50, 0, TimeSpan.Zero));
        (_usuario, _senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        await _massa.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total = 10, semente = 1, distribuicao = new { bancos = new Dictionary<string, decimal> { [Banco] = 100 }, perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100 } }, confirmar = true });
        var item = (await _massa.Ler<IReadOnlyList<MassaGerada>>("/mundo-virtual/itens?inicio=0&quantidade=10")).First(i => i.Recursos.Any(r => r.Tipo == "ACCOUNT"));
        var escopo = $"consent:{item.Consentimentos[0].ConsentId}";
        var primeira = await Ler(Endereco(Operacao("accountsGetAccounts"), item), await Token(escopo));
        var total = primeira.GetProperty("meta").GetProperty("totalRecords").GetInt32();
        Assert.True(total > 0);
        var comChave = new Uri(new Uri(primeira.GetProperty("links").GetProperty("self").GetString()!, UriKind.Absolute).PathAndQuery, UriKind.Relative);
        Assert.Contains("pagination-key=", comChave.ToString(), StringComparison.Ordinal);
        _fabrica.Relogio.Advance(TimeSpan.FromMinutes(20));

        var depois = await Ler(comChave, await Token(escopo));

        Assert.Equal(total, depois.GetProperty("meta").GetProperty("totalRecords").GetInt32());
    }

    [Fact]
    public async Task Hora_da_transacao_nao_muda_entre_consultas_do_dia()
    {
        var (massa, _) = await Preparar("PF");
        var endereco = Endereco(Operacao("accountsGetAccountsAccountIdTransactions"), massa);
        var token = await Token($"consent:{_consentId}");
        var antes = await Ler(endereco, token);
        _fabrica.Relogio.Advance(TimeSpan.FromHours(3));

        var depois = await Ler(endereco, await Token($"consent:{_consentId}"));

        Assert.Equal(antes.GetProperty("data").GetRawText(), depois.GetProperty("data").GetRawText());
    }

    [Fact]
    public async Task Na_primeira_hora_do_dia_a_hora_nao_passa_do_instante()
    {
        _fabrica.Relogio.SetUtcNow(new DateTimeOffset(2026, 11, 6, 0, 10, 0, TimeSpan.Zero));
        var (massa, token) = await Preparar("PF");
        var agora = _fabrica.Relogio.GetUtcNow();

        var dados = (await Ler(Endereco(Operacao("accountsGetAccountsAccountIdTransactions"), massa), token)).GetProperty("data");

        Assert.NotEqual(0, dados.GetArrayLength());
        Assert.All(dados.EnumerateArray(), t => Assert.True(t.GetProperty("transactionDateTime").GetDateTimeOffset() <= agora));
    }

    public void Dispose()
    {
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
