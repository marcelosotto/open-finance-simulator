using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de consentimento-no-painel (spec 008).</summary>
[Binding]
[Scope(Feature = "Criar consentimento no painel")]
public sealed class PainelPassos : IDisposable
{
    private const string Estavel = "banco-estavel";
    private static readonly string[] Contas = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ"];
    private static readonly string[] Cartoes = ["CREDIT_CARDS_ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_LIMITS_READ"];
    private static readonly string[] Credito =
    [
        "LOANS_READ", "LOANS_WARRANTIES_READ", "LOANS_SCHEDULED_INSTALMENTS_READ", "LOANS_PAYMENTS_READ",
        "FINANCINGS_READ", "FINANCINGS_WARRANTIES_READ", "FINANCINGS_SCHEDULED_INSTALMENTS_READ", "FINANCINGS_PAYMENTS_READ",
        "UNARRANGED_ACCOUNTS_OVERDRAFT_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_WARRANTIES_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_SCHEDULED_INSTALMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_PAYMENTS_READ",
        "INVOICE_FINANCINGS_READ", "INVOICE_FINANCINGS_WARRANTIES_READ", "INVOICE_FINANCINGS_SCHEDULED_INSTALMENTS_READ", "INVOICE_FINANCINGS_PAYMENTS_READ",
    ];

    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _admin;
    private readonly AdminDaMassa _massa;

    private MassaGerada? _cliente;
    private string _receptora = string.Empty;
    private string[] _permissoes = Contas;
    private int? _prazo;
    private string[]? _contas;
    private string _consentId = string.Empty;
    private HttpStatusCode _status;
    private string _corpo = string.Empty;

    public PainelPassos()
    {
        _fabrica = new FabricaDoSimulador().ComMongo();
        _admin = _fabrica.ClienteAdministracao();
        _massa = new AdminDaMassa(_admin);
    }

    private JsonElement Json => JsonDocument.Parse(_corpo).RootElement;

    private async Task Cliente(string tipo = "PF")
    {
        _cliente = await _massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo, bancos = new[] { Estavel } });
        if (_receptora.Length == 0)
        {
            (_, _, _receptora) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        }
    }

    private async Task Enviar(HttpMethod metodo, string caminho, object? corpo = null)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(caminho, UriKind.Relative));
        requisicao.Headers.Add("x-sim-autor", "bruno");
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        using var resposta = await _admin.SendAsync(requisicao);
        _status = resposta.StatusCode;
        _corpo = await resposta.Content.ReadAsStringAsync();
    }

    private async Task Confirmar(int? alcadas = null)
    {
        await Enviar(HttpMethod.Post, "/admin/consentimentos", new
        {
            cliente = _cliente!.Cliente.Documento,
            banco = Estavel,
            receptora = _receptora,
            permissoes = _permissoes,
            contas = _contas,
            prazoMeses = _prazo,
            aprovacoesPendentes = alcadas,
        });
        if (_status == HttpStatusCode.Created)
        {
            _consentId = Json.GetProperty("consentId").GetString()!;
        }
    }

    private async Task Consultar() => await Enviar(HttpMethod.Get, $"/admin/consentimentos/{_consentId}");

    private string[] Recursos(params string[] tipos) =>
        [.. _cliente!.Recursos.Where(r => r.Banco == Estavel && tipos.Contains(r.Tipo)).Select(r => r.ResourceId).Order(StringComparer.Ordinal)];

    private async Task<string[]> EstadosDosRecursos()
    {
        await Consultar();
        Assert.Equal(HttpStatusCode.OK, _status);
        return [.. Json.GetProperty("recursos").EnumerateArray().Select(r => r.GetProperty("estado").GetString()!)];
    }

    private async Task Aurora(int alcadas)
    {
        await Cliente("PJ");
        await Confirmar(alcadas);
        Assert.Equal(HttpStatusCode.Created, _status);
    }

    [When(@"^Bruno escolhe Mariana, o ""Banco Estável"" e a receptora ""Coletor""$")]
    public Task EscolheMariana() => Cliente();

    [When(@"^escolhe contas e cartões, com prazo de 12 meses$")]
    public void EscolheContasECartoes() => (_permissoes, _prazo) = ([.. Contas, .. Cartoes], 12);

    [When(@"^confirma$")]
    [When(@"^tenta confirmar$")]
    public Task ConfirmaOPedido() => Confirmar();

    [Then(@"^o consentimento deve nascer em ""(.*)""$")]
    public void NasceEm(string estado)
    {
        Assert.Equal(HttpStatusCode.Created, _status);
        Assert.Equal(estado, Json.GetProperty("estado").GetString());
        var criado = Json.GetProperty("criadoEm").GetDateTimeOffset();
        Assert.Equal(criado.AddMonths(12), Json.GetProperty("expiraEm").GetDateTimeOffset());
        Assert.Equal(_receptora, Json.GetProperty("receptora").GetString());
    }

    [Then(@"^os recursos selecionados devem estar disponíveis$")]
    public void SelecionadosDisponiveis()
    {
        var recursos = Json.GetProperty("recursos").EnumerateArray().ToList();
        Assert.Equal(Recursos("ACCOUNT", "CREDIT_CARD_ACCOUNT"), recursos.Select(r => r.GetProperty("resourceId").GetString()!).Order(StringComparer.Ordinal));
        Assert.All(recursos, r => Assert.Equal("AVAILABLE", r.GetProperty("estado").GetString()));
    }

    [When(@"^Bruno cria um consentimento sem validade$")]
    public async Task CriaSemValidade()
    {
        await Cliente();
        await Confirmar();
    }

    [Then(@"^o consentimento não deve ter data final$")]
    public void SemDataFinal()
    {
        Assert.Equal(HttpStatusCode.Created, _status);
        Assert.False(Json.TryGetProperty("expiraEm", out _));
    }

    [Given(@"^que Bruno desmarcou todas as contas$")]
    public async Task DesmarcouTodas()
    {
        await Cliente();
        _contas = [];
    }

    [Then(@"^o consentimento não deve ser criado$")]
    public void NaoCriado()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, _status);
        Assert.Empty(_consentId);
    }

    [When(@"^Bruno escolhe operações de crédito$")]
    public async Task EscolheCredito()
    {
        await Cliente();
        _permissoes = Credito;
        await Confirmar();
    }

    [Then(@"^todas as operações do cliente devem ser compartilhadas$")]
    public void TodasAsOperacoes()
    {
        Assert.Equal(HttpStatusCode.Created, _status);
        var esperados = Recursos("LOAN", "FINANCING", "UNARRANGED_ACCOUNT_OVERDRAFT", "INVOICE_FINANCING");
        Assert.NotEmpty(esperados);
        Assert.Equal(esperados, Json.GetProperty("recursos").EnumerateArray().Select(r => r.GetProperty("resourceId").GetString()!).Order(StringComparer.Ordinal));
    }

    [When(@"^Bruno escolhe um grupo incompleto de contas$")]
    public async Task GrupoIncompleto()
    {
        await Cliente();
        _permissoes = ["ACCOUNTS_READ"];
        await Confirmar();
        Assert.Contains("COMBINACAO_PERMISSOES_INCORRETA", _corpo, StringComparison.Ordinal);
    }

    [Given(@"^o cliente PF Mariana$")]
    public Task ClientePf() => Cliente();

    [When(@"^Bruno abre as permissões$")]
    public Task AbreAsPermissoes() => Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?cliente={_cliente!.Cliente.Documento}&banco={Estavel}");

    [Then(@"^só devem aparecer permissões PF$")]
    public void SoPf()
    {
        Assert.Equal(HttpStatusCode.OK, _status);
        var permissoes = Json.GetProperty("grupos").EnumerateArray().SelectMany(g => g.GetProperty("permissoes").EnumerateArray()).Select(p => p.GetString()!).ToList();
        Assert.Contains("CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", permissoes);
        Assert.DoesNotContain(permissoes, p => p.StartsWith("CUSTOMERS_BUSINESS_", StringComparison.Ordinal));
    }

    [Given(@"^um consentimento da Indústria Aurora com duas aprovações pendentes$")]
    public async Task AuroraComDuas()
    {
        await Aurora(2);
        Assert.All(await EstadosDosRecursos(), e => Assert.Equal("PENDING_AUTHORISATION", e));
    }

    [Given(@"^um consentimento da Indústria Aurora com aprovação pendente$")]
    public Task AuroraComUma() => Aurora(1);

    [When(@"^Bruno aprova duas vezes no painel$")]
    public async Task AprovaDuasVezes()
    {
        await Enviar(HttpMethod.Post, $"/admin/consentimentos/{_consentId}/aprovar");
        Assert.Equal(HttpStatusCode.OK, _status);
        Assert.All(await EstadosDosRecursos(), e => Assert.Equal("PENDING_AUTHORISATION", e));
        await Enviar(HttpMethod.Post, $"/admin/consentimentos/{_consentId}/aprovar");
        Assert.Equal(HttpStatusCode.OK, _status);
    }

    [When(@"^Bruno recusa no painel$")]
    public async Task Recusa()
    {
        await Enviar(HttpMethod.Post, $"/admin/consentimentos/{_consentId}/recusar");
        Assert.Equal(HttpStatusCode.OK, _status);
    }

    [Then(@"^os recursos devem (?:ir para|estar em|nascer em) ""(.*)""$")]
    public async Task RecursosEm(string estado)
    {
        var estados = await EstadosDosRecursos();
        Assert.NotEmpty(estados);
        Assert.All(estados, e => Assert.Equal(estado, e));
        Assert.Equal("AUTHORISED", Json.GetProperty("estado").GetString());
    }

    [Given(@"^um consentimento com aprovação pendente há 16 dias$")]
    public async Task PendenteHa16Dias()
    {
        await Aurora(1);
        _fabrica.Relogio.Advance(TimeSpan.FromDays(16));
    }

    [When(@"^Bruno cria um consentimento do Comércio Sol sem alçadas$")]
    public async Task ComercioSol()
    {
        await Cliente("PJ");
        await Confirmar();
        Assert.Equal(HttpStatusCode.Created, _status);
    }

    public void Dispose()
    {
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
