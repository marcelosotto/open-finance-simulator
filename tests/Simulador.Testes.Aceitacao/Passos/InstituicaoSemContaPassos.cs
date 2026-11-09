using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Cenário de RN08 em <c>instituicoes.feature</c>, ligado na spec 004.</summary>
[Binding]
[Scope(Feature = "Cadastrar instituições simuladas")]
public sealed class InstituicaoSemContaPassos : IDisposable
{
    private const string Banco = "banco-azul";
    private static readonly string[] SoCartoes = ["cartoes"];
    private static readonly string[] Permissoes =
    [
        "ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ", "CREDIT_CARDS_ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_LIMITS_READ", "RESOURCES_READ",
    ];

    private readonly FabricaDoSimulador _fabrica = new FabricaDoSimulador().ComMongo();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private string _corpo = string.Empty;

    public InstituicaoSemContaPassos()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
    }

    [Given(@"^que o ""Banco Azul"" não oferece contas$")]
    public async Task SemContas()
    {
        using var resposta = await _admin.PostAsJsonAsync(new Uri("/admin/instituicoes", UriKind.Relative), new { nome = "Banco Azul", produtos = SoCartoes });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [When(@"^Ana cria um consentimento de contas e cartões nesse banco$")]
    public async Task Cria()
    {
        var massa = await new AdminDaMassa(_admin).Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco } });
        var token = await AtalhosDeConsentimento.Token(_admin, _api, Banco, "consents");
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), token, new
        {
            data = new { loggedUser = new { document = new { identification = massa.Cliente.Documento, rel = "CPF" } }, permissions = Permissoes },
        });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        _corpo = await resposta.Content.ReadAsStringAsync();
    }

    [Then(@"^as permissões de contas devem sair do consentimento$")]
    public void SemPermissoesDeContas()
    {
        using var corpo = JsonDocument.Parse(_corpo);
        var permissoes = corpo.RootElement.GetProperty("data").GetProperty("permissions").EnumerateArray().Select(p => p.GetString()!).ToList();
        Assert.DoesNotContain(permissoes, p => p.StartsWith("ACCOUNTS_", StringComparison.Ordinal));
        Assert.Contains("CREDIT_CARDS_ACCOUNTS_READ", permissoes);
    }

    public void Dispose()
    {
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }
}
