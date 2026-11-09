using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Simulador.Instituicoes.Contratos;
using Simulador.Instituicoes.Dominio;
using Simulador.Testes.Aceitacao.Infra;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de <c>docs/01-requisitos/gherkin/instituicoes.feature</c> (UC01 a UC03).</summary>
[Binding]
public sealed class InstituicoesPassos(ContextoDeInstituicoes contexto)
{
    private const string Base = "/admin/instituicoes";
    private const string BancoDeComparacao = "Banco Verde";
    private static readonly string[] CamposDaInstituicao = ["publico", "produtos", "marca", "organisationId"];

    private static string EnderecoDe(string nome) => Endereco.Gerar(nome)!;

    private HttpClient Admin() => contexto.Fabrica.ClienteAdministracao();

    private async Task<JsonElement> Get(string caminho)
    {
        using var cliente = Admin();
        using var resposta = await cliente.GetAsync(new Uri(caminho, UriKind.Relative));
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.True(resposta.IsSuccessStatusCode, corpo);
        return JsonDocument.Parse(corpo).RootElement;
    }

    private async Task Enviar(HttpMethod metodo, string caminho, object? corpo = null)
    {
        using var cliente = Admin();
        using var requisicao = new HttpRequestMessage(metodo, new Uri(caminho, UriKind.Relative));
        requisicao.Headers.Add("x-sim-autor", "Diego");
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        contexto.Resposta?.Dispose();
        contexto.Resposta = await cliente.SendAsync(requisicao);
        contexto.Corpo = await contexto.Resposta.Content.ReadAsStringAsync();
    }

    private async Task Garantir(string nome, string? perfil = null)
    {
        using var cliente = Admin();
        using var existente = await cliente.GetAsync(new Uri($"{Base}/{EnderecoDe(nome)}", UriKind.Relative));
        if (existente.StatusCode == HttpStatusCode.NotFound)
        {
            await Enviar(HttpMethod.Post, Base, new { nome, perfil });
            Assert.Equal(HttpStatusCode.Created, contexto.Resposta!.StatusCode);
        }

        contexto.BancoAtual = nome;
    }

    private async Task<Instituicao> EmVigor(string nome) =>
        (await contexto.Fabrica.Services.GetRequiredService<IConsultaDeInstituicoes>().Obter(EnderecoDe(nome), CancellationToken.None))!;

    [Given("um ambiente recém-criado")]
    public void DadoUmAmbienteRecemCriado() => Admin().Dispose();

    [Given("um ambiente {string}")]
    public void DadoUmAmbiente(string marca) => contexto.Marca = marca;

    [Then("devem existir os bancos {string}, {string}, {string}, {string} e {string}")]
    public async Task EntaoDevemExistirOsBancos(string a, string b, string c, string d, string e)
    {
        var nomes = (await Get(Base)).EnumerateArray().Select(i => i.GetProperty("nome").GetString()).ToList();
        Assert.All(new[] { a, b, c, d, e }, nome => Assert.Contains(nome, nomes));
    }

    [Then("devem existir os bancos de nome real do ambiente")]
    public async Task EntaoDevemExistirOsBancosDeNomeReal()
    {
        var nomes = (await Get(Base)).EnumerateArray().Select(i => i.GetProperty("nome").GetString()).ToList();
        string[] reais = ["Itaú", "Nubank", "Bradesco", "Banco do Brasil", "Caixa", "Santander", "Inter", "C6 Bank", "BTG Pactual", "Sicoob", "Sicredi"];
        Assert.All(reais, real => Assert.Contains($"HML-{real}", nomes));
    }

    [When("Diego cria um banco inspirado no {string}")]
    public void QuandoCriaInspirado(string nome)
    {
        // O ambiente já nasce com os bancos reais; usa-se o que a semeadura criou (RN09).
        Admin().Dispose();
        contexto.BancoAtual = nome;
    }

    [Then("o nome do banco deve ser {string}")]
    public async Task EntaoONomeDeveSer(string esperado) => Assert.Equal(esperado, (await EmVigor(contexto.BancoAtual!)).Nome);

    [When("Diego cria o banco {string}")]
    public Task QuandoCriaOBanco(string nome) => Garantir(nome);

    [Then("o banco deve existir com o perfil {string}")]
    public async Task EntaoDeveExistirComOPerfil(string perfil) => Assert.Equal(perfil, (await EmVigor(contexto.BancoAtual!)).Perfil);

    [Then("deve ter um organisationId e um endereço próprios")]
    public async Task EntaoDeveTerOrganisationIdEEndereco()
    {
        var banco = await EmVigor(contexto.BancoAtual!);
        var outro = await EmVigor("Banco Estável");
        Assert.False(string.IsNullOrEmpty(banco.OrganisationId));
        Assert.NotEqual(outro.OrganisationId, banco.OrganisationId);
        Assert.NotEqual(outro.Endereco, banco.Endereco);
    }

    [Given("que o {string} usa o perfil {string}")]
    public async Task DadoQueUsaOPerfil(string nome, string perfil)
    {
        await Garantir(BancoDeComparacao, perfil);
        await Garantir(nome, perfil);
    }

    [When("Diego muda a latência do {string}")]
    public Task QuandoMudaALatencia(string nome) => Enviar(HttpMethod.Patch, $"{Base}/{EnderecoDe(nome)}", new
    {
        comportamento = new { latenciaPorClasse = new { alta = new { tipicoMs = 9_999 } } },
    });

    [Then("o perfil {string} deve continuar igual")]
    public async Task EntaoOPerfilDeveContinuarIgual(string perfil)
    {
        Assert.Equal(HttpStatusCode.OK, contexto.Resposta!.StatusCode);
        var modelo = (await Get("/admin/perfis-de-comportamento")).EnumerateArray().Single(p => p.GetProperty("nome").GetString() == perfil);
        Assert.Equal(1_200, modelo.GetProperty("comportamento").GetProperty("latenciaPorClasse").GetProperty("alta").GetProperty("tipicoMs").GetInt32());
    }

    [Then("os outros bancos com esse perfil não devem mudar")]
    public async Task EntaoOsOutrosNaoDevemMudar()
    {
        Assert.Equal(1_200, (await EmVigor(BancoDeComparacao)).Comportamento.LatenciaPorClasse["alta"].TipicoMs);
        Assert.Equal(1_200, (await EmVigor("Banco Lento")).Comportamento.LatenciaPorClasse["alta"].TipicoMs);
        Assert.Equal(9_999, (await EmVigor(contexto.BancoAtual!)).Comportamento.LatenciaPorClasse["alta"].TipicoMs);
    }

    [When("Diego muda a taxa de erro do {string} para {int}%")]
    public async Task QuandoMudaATaxa(string nome, int taxa)
    {
        await Garantir(nome);
        await Enviar(HttpMethod.Patch, $"{Base}/{EnderecoDe(nome)}", new { comportamento = new { taxaDeErroPct = taxa } });
    }

    [Then("a próxima chamada deve usar {int}%")]
    public async Task EntaoAProximaChamadaDeveUsar(int taxa) =>
        Assert.Equal(taxa, (await EmVigor(contexto.BancoAtual!)).Comportamento.TaxaDeErroPct);

    [Then("o histórico deve mostrar Diego, o valor antigo e o novo")]
    public async Task EntaoOHistoricoDeveMostrar()
    {
        var ultima = (await Get($"{Base}/{EnderecoDe(contexto.BancoAtual!)}/historico"))[0];
        var mudanca = ultima.GetProperty("mudancas")[0];
        Assert.Equal("Diego", ultima.GetProperty("autor").GetString());
        Assert.Equal("0.2", mudanca.GetProperty("antes").GetString());
        Assert.Equal("5", mudanca.GetProperty("depois").GetString());
    }

    [Given("que o {string} tem clientes")]
    public async Task DadoQueTemClientes(string nome)
    {
        await Garantir(nome);
        contexto.Clientes.Enderecos.Add(EnderecoDe(nome));
    }

    [When("Diego tenta apagar o {string}")]
    public Task QuandoTentaApagar(string nome) => Enviar(HttpMethod.Delete, $"{Base}/{EnderecoDe(nome)}");

    [Then("o banco deve ser inativado")]
    public async Task EntaoDeveSerInativado() => Assert.Equal(Situacoes.Inativa, (await EmVigor(contexto.BancoAtual!)).Situacao);

    [Then("as chamadas a ele devem responder como fora do ar")]
    public async Task EntaoDevemResponderForaDoAr()
    {
        await ChamarOpenFinance(contexto.BancoAtual!);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, contexto.Resposta!.StatusCode);
    }

    private async Task ChamarOpenFinance(string nome)
    {
        using var cliente = contexto.Fabrica.ClienteOpenFinance();
        contexto.Resposta?.Dispose();
        contexto.Resposta = await cliente.GetAsync(new Uri($"/{EnderecoDe(nome)}/open-banking/accounts/v2/accounts", UriKind.Relative));
    }

    [Given("que Ana chama o endereço do {string}")]
    public Task DadoQueAnaChama(string nome)
    {
        contexto.BancoAtual = nome;
        return ChamarOpenFinance(nome);
    }

    [Then("a resposta deve vir com o comportamento do {string}")]
    public async Task EntaoARespostaDeveVirComOComportamento(string nome)
    {
        Assert.Equal(HttpStatusCode.NotImplemented, contexto.Resposta!.StatusCode);
        Assert.Equal("lento", (await EmVigor(nome)).Perfil);
    }

    [Then("nenhum cabeçalho próprio do simulador deve ser exigido")]
    public void EntaoNenhumCabecalhoDeveSerExigido() => Assert.NotEqual(HttpStatusCode.BadRequest, contexto.Resposta!.StatusCode);

    [When("Diego configura o status {int} para o endpoint de saldos")]
    public Task QuandoConfiguraOStatus(int status) => Enviar(HttpMethod.Patch, $"{Base}/banco-estavel", new
    {
        comportamento = new { pesosPorStatus = new Dictionary<string, decimal> { [status.ToString(System.Globalization.CultureInfo.InvariantCulture)] = 1 } },
    });

    [Then("a configuração deve ser recusada")]
    public void EntaoDeveSerRecusada() => Assert.Equal(HttpStatusCode.UnprocessableEntity, contexto.Resposta!.StatusCode);

    [When("Diego abre o {string}")]
    public async Task QuandoAbre(string nome)
    {
        await Garantir(nome);
        await Enviar(HttpMethod.Get, $"{Base}/{EnderecoDe(nome)}");
    }

    [Then("deve ver público, produtos, marca e organisationId")]
    public void EntaoDeveVerOsDados()
    {
        Assert.Equal(HttpStatusCode.OK, contexto.Resposta!.StatusCode);
        var corpo = JsonDocument.Parse(contexto.Corpo!, default).RootElement;
        Assert.All(CamposDaInstituicao, campo => Assert.True(corpo.TryGetProperty(campo, out _), campo));
    }
}
