using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Reqnroll;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de defeitos-e-anomalias.feature (spec 009).</summary>
[Binding]
[Scope(Feature = "Injetar defeitos e anomalias")]
public sealed partial class DefeitosPassos(ContextoDeComportamento c)
{
    private const string TransacoesId = "accountsGetAccountsAccountIdTransactions";
    private const string Correlacao = "x-fapi-interaction-id";
    private const int Consultas = 1_000;
    private readonly List<(int Status, string Corpo)> _respostas = [];
    private readonly List<List<int>> _rodadas = [];
    private readonly List<int> _totais = [];
    private string _regra = string.Empty;
    private object? _corpoDaRegra;
    private bool _preparado;
    private string _defeito = string.Empty;

    private JsonElement Json => JsonDocument.Parse(c.Corpo).RootElement;

    private List<JsonElement> Itens => [.. Json.GetProperty("data").EnumerateArray()];

    private DateOnly Hoje => DateOnly.FromDateTime(c.Fabrica.Relogio.GetUtcNow().UtcDateTime);

    private Uri Transacoes(string consulta = "") => new(c.Em("/accounts/{accountId}/transactions") + consulta, UriKind.Relative);

    /// <summary>Transações da fatura de outubro do cartão: o meta do contrato tem totalRecords, ao contrário das transações de conta (RN117).</summary>
    private Uri DaFatura(string consulta = "") =>
        new(AtalhosDeConsentimento.Caminho(c.Banco, "credit-cards-accounts", "/accounts/{creditCardAccountId}/bills/{billId}/transactions").ToString()
            .Replace("{creditCardAccountId}", c.Extras[0], StringComparison.Ordinal)
            .Replace("{billId}", $"{c.Extras[0]}-b202610", StringComparison.Ordinal) + consulta, UriKind.Relative);

    private async Task Garantir(string banco = "Banco Estável")
    {
        if (!_preparado)
        {
            await c.Preparar(banco, produtosExtras: ["cartoes"]);
            _preparado = true;
        }
    }

    private async Task SemLimites() =>
        await c.Configurar(new { limites = new { capacidadeTps = 100_000_000, limiteOperacionalLigado = false, tpm = new { alta = 100_000_000, media = 100_000_000, baixa = 100_000_000 } } });

    private async Task CriarDefeito(string defeito, decimal frequencia = 100m, long? semente = null, int? quantidade = null)
    {
        _corpoDaRegra = new
        {
            instituicao = c.Banco,
            efeito = new { tipo = "defeito", defeito, quantidade },
            frequenciaPct = frequencia,
            semente,
        };
        using var resposta = await c.Admin.PostAsJsonAsync(new Uri("/admin/cenarios/regras", UriKind.Relative), _corpoDaRegra);
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        _regra = corpo.RootElement.GetProperty("id").GetString()!;
    }

    private async Task LigarAnomalia(string anomalia)
    {
        using var resposta = await c.Admin.PostAsync(new Uri($"/admin/cenarios/anomalias/{c.Banco}/{anomalia}/ligar", UriKind.Relative), null);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    // RN111
    [When(@"^Ana faz 1\.000 consultas no ""(.*)""$")]
    public async Task MilConsultas(string banco)
    {
        await c.Preparar(banco, semErros: false);
        await SemLimites();
        for (var i = 0; i < Consultas; i++)
        {
            if (i % 100 == 0)
            {
                await c.RenovarToken();
            }

            await c.Chamar(Transacoes(), renovar: false);
            _respostas.Add(((int)c.Resposta!.StatusCode, c.Corpo));
        }
    }

    [Then(@"^todas devem validar contra a spec$")]
    public void TodasValidam()
    {
        Assert.Equal(Consultas, _respostas.Count);
        foreach (var grupo in _respostas.GroupBy(r => (r.Status, Forma: Regex.Replace(r.Corpo, "\"requestDateTime\":\"[^\"]*\"", string.Empty))))
        {
            var (status, corpo) = grupo.First();
            Assert.True(ValidadorDeContrato.Validar(TransacoesId, corpo, status.ToString(CultureInfo.InvariantCulture)).Count == 0, $"{status}: {corpo}");
        }
    }

    // RN110
    [Given(@"^o defeito ""(.*)"" com taxa de 100%$")]
    public async Task DefeitoComTaxaCheia(string defeito)
    {
        _defeito = defeito;
        await Garantir();
        await CriarDefeito(defeito);
    }

    [When(@"^Ana consulta transações$")]
    public Task ConsultaTransacoes() => c.Chamar(_defeito == "total-records-incoerente" ? DaFatura() : Transacoes());

    [Then(@"^a resposta deve trazer ""(.*)""$")]
    public void RespostaTraz(string efeito)
    {
        c.Status(200);
        var itens = Itens;
        Assert.True(itens.Count >= 2, c.Corpo);
        var primeiro = itens[0];
        switch (efeito)
        {
            case "sem transactionAmount":
                Assert.False(primeiro.TryGetProperty("transactionAmount", out _));
                Assert.True(itens[1].TryGetProperty("transactionAmount", out _));
                break;
            case "data fora do formato":
                Assert.Matches(@"^\d{2}/\d{2}/\d{4}$", primeiro.GetProperty("transactionDateTime").GetString()!);
                break;
            case "type fora da lista":
                Assert.Equal("VALOR_FORA_DA_LISTA", primeiro.GetProperty("type").GetString());
                break;
            case "transactionId igual entre itens":
                Assert.Equal(primeiro.GetProperty("transactionId").GetString(), itens[1].GetProperty("transactionId").GetString());
                break;
            case "totalRecords diferente da soma":
                Assert.NotEqual(itens.Count, Json.GetProperty("meta").GetProperty("totalRecords").GetInt32());
                break;
            default:
                Assert.Equal("transação fora do período", efeito);
                Assert.True(DateOnly.FromDateTime(primeiro.GetProperty("transactionDateTime").GetDateTime()) < Hoje.AddYears(-1), c.Corpo);
                break;
        }
    }

    // RN111, RN54
    [Given(@"^um defeito com taxa de 10% e semente fixa$")]
    public async Task DefeitoComSemente()
    {
        await Garantir();
        await SemLimites();
        await CriarDefeito("enum-invalido", 10m, 42);
    }

    [When(@"^Bruno roda 1\.000 consultas duas vezes$")]
    public async Task DuasRodadas()
    {
        for (var rodada = 0; rodada < 2; rodada++)
        {
            if (rodada > 0)
            {
                // Regravar a regra recomeça a sequência da semente (RN54).
                c.Fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
                using var alterada = await c.Admin.PutAsJsonAsync(new Uri($"/admin/cenarios/regras/{_regra}", UriKind.Relative), _corpoDaRegra);
                Assert.True(alterada.IsSuccessStatusCode, await alterada.Content.ReadAsStringAsync());
            }

            var afetadas = new List<int>();
            for (var i = 0; i < Consultas; i++)
            {
                if (i % 100 == 0)
                {
                    await c.RenovarToken();
                }

                await c.Chamar(Transacoes(), renovar: false);
                c.Status(200);
                if (Itens[0].GetProperty("type").GetString() == "VALOR_FORA_DA_LISTA")
                {
                    afetadas.Add(i);
                }
            }

            _rodadas.Add(afetadas);
        }
    }

    [Then(@"^as duas rodadas devem afetar as mesmas chamadas$")]
    public void MesmasChamadas()
    {
        Assert.InRange(_rodadas[0].Count, 50, 150);
        Assert.Equal(_rodadas[0], _rodadas[1]);
    }

    // RN113
    [Given(@"^a anomalia ""(.*)"" ligada no ""(.*)""$")]
    public async Task AnomaliaLigada(string anomalia, string banco)
    {
        await Garantir(banco);
        await LigarAnomalia(anomalia);
    }

    [When(@"^acontece ""(.*)""$")]
    public async Task Acontece(string situacao)
    {
        switch (situacao)
        {
            case "consentimento revogado":
                await c.RenovarToken();
                using (var revogacao = await AtalhosDeConsentimento.Chamar(c.Api, HttpMethod.Delete, AtalhosDeConsentimento.Caminho(c.Banco, "consents", "/consents/{consentId}", c.ConsentId), await c.NovoToken("consents")))
                {
                    Assert.Equal(HttpStatusCode.NoContent, revogacao.StatusCode);
                }

                await c.Chamar(c.Saldos, renovar: false);
                break;
            case "chave expirada":
                await c.Chamar(Transacoes());
                var self = Json.GetProperty("links").GetProperty("self").GetString()!;
                Assert.Contains("pagination-key=", self, StringComparison.Ordinal);
                c.Fabrica.Relogio.Advance(TimeSpan.FromMinutes(61));
                await c.Chamar(new Uri(new Uri(self).PathAndQuery, UriKind.Relative));
                break;
            default:
                Assert.Equal("chamada válida", situacao);
                await c.Chamar(c.Saldos);
                break;
        }
    }

    [Then(@"^a resposta deve ser ""(.*)""$")]
    public void RespostaDeveSer(string resultado)
    {
        if (resultado.StartsWith("status ", StringComparison.Ordinal))
        {
            c.Status(int.Parse(resultado["status ".Length..], CultureInfo.InvariantCulture));
            return;
        }

        Assert.Equal("outro id na volta", resultado);
        c.Status(200);
        var devolvido = c.Resposta!.Headers.GetValues(Correlacao).Single();
        Assert.NotEqual(AtalhosDeConsentimento.Correlacao, devolvido);
        Assert.True(Guid.TryParse(devolvido, out _), devolvido);
    }

    // RN114
    [Given(@"^o ""(.*)"" com a anomalia ""(.*)"" ligada$")]
    public async Task BancoComAnomalia(string banco, string anomalia)
    {
        using var criado = await c.Admin.PostAsJsonAsync(new Uri("/admin/instituicoes", UriKind.Relative), new { nome = banco });
        Assert.True(criado.IsSuccessStatusCode, await criado.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await criado.Content.ReadAsStringAsync());
        using var ligada = await c.Admin.PostAsync(new Uri($"/admin/cenarios/anomalias/{corpo.RootElement.GetProperty("endereco").GetString()}/{anomalia}/ligar", UriKind.Relative), null);
        Assert.True(ligada.IsSuccessStatusCode, await ligada.Content.ReadAsStringAsync());
    }

    [When(@"^Diego abre a lista de bancos$")]
    public async Task AbreLista()
    {
        using var resposta = await c.Admin.GetAsync(new Uri("/admin/instituicoes", UriKind.Relative));
        resposta.EnsureSuccessStatusCode();
        _respostas.Add(((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync()));
    }

    [Then(@"^o banco deve mostrar um aviso de anomalias ligadas$")]
    public void MostraAviso()
    {
        using var lista = JsonDocument.Parse(_respostas[^1].Corpo);
        var bancos = lista.RootElement.EnumerateArray().ToDictionary(b => b.GetProperty("endereco").GetString()!, b => b.GetProperty("anomaliasLigadas").EnumerateArray().Select(a => a.GetString()!).ToList());
        Assert.Equal(["revogado-com-403"], bancos["banco-azul"]);
        Assert.Empty(bancos["banco-estavel"]);
    }

    // RN115
    [Given(@"^uma regra que adiciona registros entre páginas$")]
    public async Task RegraQueAdiciona()
    {
        await Garantir();
        await CriarDefeito("dado-muda-na-paginacao", quantidade: 2);
    }

    [When(@"^Ana lê todas as páginas$")]
    public async Task LeTodas()
    {
        await c.Chamar(DaFatura("?page-size=25"));
        while (true)
        {
            c.Status(200);
            _totais.Add(Json.GetProperty("meta").GetProperty("totalRecords").GetInt32());
            if (_totais.Count > 1)
            {
                // O dado muda, mas a resposta continua válida no schema (RN115).
                Assert.Contains(Itens, i => i.GetProperty("transactionId").GetString()!.EndsWith("-N1", StringComparison.Ordinal));
                Assert.Empty(ValidadorDeContrato.Validar("creditCardsGetAccountsCreditCardAccountIdBillsBillIdTransactions", c.Corpo));
            }

            if (!Json.GetProperty("links").TryGetProperty("next", out var proxima))
            {
                break;
            }

            await c.Chamar(new Uri(new Uri(proxima.GetString()!).PathAndQuery, UriKind.Relative), renovar: false);
        }
    }

    [Then(@"^totalRecords deve mudar entre as páginas$")]
    public void TotalMuda()
    {
        Assert.True(_totais.Count >= 2, string.Join(',', _totais));
        Assert.All(_totais.Skip(1), t => Assert.Equal(_totais[0] + 2, t));
    }
}
