using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulador.Cenarios.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Cenarios;

/// <summary>
/// Revisão da 009, achados 1 e 6. Sem leitura do cache de cenários (MongoDB fora), o portão e a chave seguem o
/// caminho oficial, e depressa. O MongoDB fora leva cerca de 30 s para acusar; aqui a leitura demorada nunca termina.
/// </summary>
public sealed class AnomaliasSemLeituraTests : IDisposable
{
    private const string Banco = "banco-estavel";
    private static readonly string[] Tipos = ["PIX"];
    private static readonly string[] Produtos = ["contas"];
    private static readonly TimeSpan Folga = TimeSpan.FromSeconds(6);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private FabricaDoSimulador? _fabrica;

    public void Dispose() => _fabrica?.Dispose();

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    private FabricaDoSimulador Fabrica(Action<IServiceCollection> registrar, bool comComportamento = true)
    {
        _fabrica = new FabricaDoSimulador().ComServicos(registrar);
        return comComportamento ? _fabrica.ComComportamento() : _fabrica;
    }

    private FabricaDoSimulador SemCenarios(bool demora, bool comComportamento = true) => Fabrica(
        s =>
        {
            s.RemoveAll<IRepositorioDeCenarios>();
            s.AddSingleton<IRepositorioDeCenarios>(new RepositorioForaDoAr(demora));
        },
        comComportamento);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sem_token_segue_401_depressa_e_devolve_a_mesma_correlacao(bool demora)
    {
        using var api = SemCenarios(demora).ClienteOpenFinance();
        for (var i = 0; i < 2; i++)
        {
            var relogio = Stopwatch.StartNew();
            using var resposta = await AtalhosDeConsentimento.Chamar(api, HttpMethod.Get, AtalhosDeConsentimento.Caminho(Banco, "accounts", "/accounts"), null);
            Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
            Assert.Equal(AtalhosDeConsentimento.Correlacao, resposta.Headers.GetValues("x-fapi-interaction-id").Single());

            // A primeira espera no máximo a leitura curta; a segunda encontra a falha guardada e não espera.
            var limite = i == 0 ? Folga : TimeSpan.FromSeconds(1);
            Assert.True(relogio.Elapsed < limite, $"chamada {i + 1} levou {relogio.Elapsed}");
        }
    }

    /// <summary>O caminho de <c>chave-recusada-com-422</c>: sem leitura, a chave ruim segue RN36 e gera nova chave.</summary>
    [Fact]
    public async Task Chave_ruim_sem_leitura_das_anomalias_segue_a_regra_oficial_depressa()
    {
        var fabrica = SemCenarios(demora: true, comComportamento: false);
        using var admin = fabrica.ClienteAdministracao();
        using var api = fabrica.ClienteOpenFinance();
        var (usuario, senha, _) = await AtalhosDeConsentimento.CriarReceptora(admin);
        var massa = new AdminDaMassa(admin);
        await massa.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/fixo", new { transacoesMin = 2, transacoesMax = 2, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = Tipos, produtos = Produtos });
        var cliente = await massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco }, perfil = "fixo" });
        var conta = cliente.Recursos.First(r => r.Tipo == "ACCOUNT").ResourceId;

        async Task<string> Token(string escopo)
        {
            using var resposta = await AtalhosDeConsentimento.PedirToken(api, Banco, usuario, senha, escopo);
            resposta.EnsureSuccessStatusCode();
            using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct));
            return corpo.RootElement.GetProperty("access_token").GetString()!;
        }

        using var criado = await AtalhosDeConsentimento.Chamar(api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), await Token("consents"), new
        {
            data = new { loggedUser = new { document = new { identification = cliente.Cliente.Documento, rel = "CPF" } }, permissions = Permissoes.Para("PF", null) },
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        using var consentimento = JsonDocument.Parse(await criado.Content.ReadAsStringAsync(_ct));
        var token = await Token($"consent:{consentimento.RootElement.GetProperty("data").GetProperty("consentId").GetString()}");
        var transacoes = AtalhosDeConsentimento.Caminho(Banco, "accounts", "/accounts/{accountId}/transactions").ToString().Replace("{accountId}", conta, StringComparison.Ordinal);

        var relogio = Stopwatch.StartNew();
        using var resposta = await AtalhosDeConsentimento.Chamar(api, HttpMethod.Get, U(transacoes + "?pagination-key=ruim"), token);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(relogio.Elapsed < Folga, $"a chamada levou {relogio.Elapsed}");
    }

    /// <summary>Achado 1: se a limpeza das anomalias falha, o banco não é apagado e o pedido pode ser repetido.</summary>
    [Fact]
    public async Task Limpeza_que_falha_nao_apaga_o_banco()
    {
        using var admin = Fabrica(
            s =>
            {
                s.RemoveAll<ILimpezaDeAnomalias>();
                s.AddSingleton<ILimpezaDeAnomalias>(new LimpezaForaDoAr());
            },
            comComportamento: false).ClienteAdministracao();
        using (var criado = await admin.PostAsJsonAsync(U("/admin/instituicoes"), new { nome = "Banco Passageiro" }, _ct))
        {
            Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        }

        using (var apagado = await admin.DeleteAsync(U("/admin/instituicoes/banco-passageiro"), _ct))
        {
            Assert.Equal(HttpStatusCode.InternalServerError, apagado.StatusCode);
        }
        using var depois = await admin.GetAsync(U("/admin/instituicoes/banco-passageiro"), _ct);
        Assert.Equal(HttpStatusCode.OK, depois.StatusCode);
        var historico = await admin.GetStringAsync(U("/admin/instituicoes/banco-passageiro/historico"), _ct);
        Assert.DoesNotContain("\"apagada\"", historico, StringComparison.Ordinal);
    }

    private sealed class LimpezaForaDoAr : ILimpezaDeAnomalias
    {
        public Task Apagar(string endereco, CancellationToken cancelamento) => Task.FromException(new TimeoutException("MongoDB fora do ar"));
    }

    /// <summary>Falha na hora, ou nunca responde, como o MongoDB fora antes de o driver desistir.</summary>
    private sealed class RepositorioForaDoAr(bool demora) : IRepositorioDeCenarios
    {
        public Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento) => Falha<IReadOnlyList<RegraDeCenario>>();

        public Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento) => Falha<RegraDeCenario?>();

        public Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento) => Falha<bool>();

        public Task ApagarRegra(string id, CancellationToken cancelamento) => Falha<bool>();

        public Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento) => Falha<IReadOnlyList<PacoteDeCenario>>();

        public Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento) => Falha<PacoteDeCenario?>();

        public Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento) => Falha<bool>();

        public Task ApagarPacote(string nome, CancellationToken cancelamento) => Falha<bool>();

        public Task<IReadOnlyList<AnomaliaDaInstituicao>> ListarAnomalias(CancellationToken cancelamento) => Falha<IReadOnlyList<AnomaliaDaInstituicao>>();

        public Task SalvarAnomalia(AnomaliaDaInstituicao anomalia, CancellationToken cancelamento) => Falha<bool>();

        public Task ApagarAnomalias(string instituicao, CancellationToken cancelamento) => Falha<bool>();

        private Task<T> Falha<T>() => demora ? new TaskCompletionSource<T>().Task : Task.FromException<T>(new TimeoutException("MongoDB fora do ar"));
    }
}
