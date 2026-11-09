using System.Globalization;
using System.Net;
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

    private readonly FabricaDoSimulador _fabrica = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private readonly AdminDaMassa _massa;
    private string _usuario = string.Empty;
    private string _senha = string.Empty;

    public TodasAsFamiliasTests()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
        _massa = new AdminDaMassa(_admin);
    }

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    private static IReadOnlyList<OperacaoDoCatalogo> Operacoes() =>
        [.. ServicosDeSpecs.LerOperacoes(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs())
            .Where(o => o.Contrato is not ("consents" or "resources"))];

    private async Task<(MassaGerada Massa, string Token)> Preparar(string tipo)
    {
        if (_usuario.Length == 0)
        {
            (_usuario, _senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
            await _massa.Enviar<PerfilDeDados>(HttpMethod.Put, $"/perfis/{Perfil}", new { transacoesMin = 3, transacoesMax = 3, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = SoPix, produtos = Todos });
        }

        var massa = await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo, bancos = new[] { Banco }, perfil = Perfil });
        var token = await Token("consents");
        var cpf = tipo == "PF" ? massa.Cliente.Documento : (await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco } })).Cliente.Documento;
        object dados = tipo == "PF"
            ? new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, permissions = Permissoes.Para(tipo, null) }
            : new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, businessEntity = new { document = new { identification = massa.Cliente.Documento, rel = "CNPJ" } }, permissions = Permissoes.Para(tipo, null) };
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), token, new { data = dados });
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync(Cancelamento));
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento));
        var consentId = corpo.RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
        return (massa, await Token($"consent:{consentId}"));
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

    [Fact]
    public async Task Fatura_de_outro_recurso_ou_do_mes_aberto_e_proibida()
    {
        var (massa, token) = await Preparar("PF");
        var operacao = Operacoes().First(o => o.Caminho.Contains("{billId}", StringComparison.Ordinal));
        var endereco = Endereco(operacao, massa).ToString();

        foreach (var trocada in new[] { endereco.Replace("-b202610", "-b202611", StringComparison.Ordinal), endereco.Replace("-b202610", "-bxx", StringComparison.Ordinal) })
        {
            using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(trocada, UriKind.Relative), token);
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
            Assert.Contains("RESOURCE_FORBIDDEN", await resposta.Content.ReadAsStringAsync(Cancelamento), StringComparison.Ordinal);
        }
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

    public void Dispose()
    {
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
