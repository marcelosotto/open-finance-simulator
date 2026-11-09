using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Simulador.Catalogo.Contratos;
using Simulador.Marcacao.Contratos;
using Simulador.Testes.Aceitacao.Infra;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Passos de <c>docs/01-requisitos/gherkin/marcacao.feature</c> (UC23, RN60 a RN66, RN70).</summary>
[Binding]
public sealed class MarcacaoPassos(ContextoDeMarcacao contexto)
{
    private const string CaminhoDeTransacoes = "/banco-teste/open-banking/accounts/v2/accounts/conta-1/transactions";

    private static readonly Lazy<Regex> PadraoDoId = new(() => new Regex(
        ValidadorDeContrato.SchemaDaResposta(ManipuladorDeTransacoesDeTeste.Operacao)["properties"]!["data"]!["items"]!["properties"]!["transactionId"]!["pattern"]!.GetValue<string>(),
        RegexOptions.None,
        TimeSpan.FromSeconds(1)));

    private static readonly Lazy<Regex> PadraoDoConsentId = new(() =>
    {
        var documento = ServicosDeSpecs.LerDocumentoJson(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs(), "consents");
        var schema = ServicosDeSpecs.ResolverReferencias(documento["components"]!["schemas"]!["ResponseConsent"], documento)!;
        return new Regex(schema["properties"]!["data"]!["properties"]!["consentId"]!["pattern"]!.GetValue<string>(), RegexOptions.None, TimeSpan.FromSeconds(1));
    });

    [Given("a marca de ambiente {string}")]
    public void DadoAMarca(string marca) => contexto.Marca = marca;

    [Given("a marca de ambiente vazia")]
    public void DadoAMarcaVazia() => contexto.Marca = string.Empty;

    [Given("um recurso {string}")]
    public void DadoUmRecurso(string natureza)
    {
        contexto.Dados.Naturezas.Clear();
        contexto.Dados.Naturezas.Add(Natureza(natureza));
    }

    [Given("uma lista com dado persistido e dado virtual")]
    public void DadoUmaListaMista()
    {
        contexto.Dados.Naturezas.Clear();
        contexto.Dados.Naturezas.AddRange([Marcacao.Contratos.Natureza.Persistido, Marcacao.Contratos.Natureza.Virtual]);
    }

    [When("Ana consulta o recurso")]
    [When("Ana consulta a lista")]
    [When("Ana consulta uma transação")]
    public async Task QuandoAnaConsulta()
    {
        contexto.Resposta = await contexto.Fabrica.ClienteOpenFinance().GetAsync(new Uri(CaminhoDeTransacoes, UriKind.Relative));
        contexto.Corpo = await contexto.Resposta.Content.ReadAsStringAsync();
        Assert.True(contexto.Resposta.IsSuccessStatusCode, contexto.Corpo);
        using var json = JsonDocument.Parse(contexto.Corpo);
        var item = json.RootElement.GetProperty("data")[0];
        contexto.Identificador = item.GetProperty("transactionId").GetString();
        contexto.Texto = item.GetProperty("transactionName").GetString();
    }

    [When("Ana consulta uma transação persistida")]
    public async Task QuandoAnaConsultaUmaTransacaoPersistida()
    {
        DadoUmRecurso("persistido");
        await QuandoAnaConsulta();
    }

    [When("é criado um recurso {string}")]
    public void QuandoECriadoUmRecurso(string natureza) => contexto.Identificador = contexto.Marcador.NovoIdentificador(Natureza(natureza));

    [When("é criado um consentimento {string}")]
    public void QuandoECriadoUmConsentimento(string natureza) => contexto.ConsentId = contexto.Marcador.NovoConsentId(Natureza(natureza));

    [When("Ana valida a resposta contra a spec oficial")]
    public Task QuandoAnaValida() => QuandoAnaConsulta();

    [Then("o cabeçalho {string} deve ser {string}")]
    public void EntaoOCabecalhoDeveSer(string cabecalho, string valor) =>
        Assert.Equal(valor, Assert.Single(contexto.Resposta!.Headers.GetValues(cabecalho)));

    [Then("o cabeçalho {string} não deve ser enviado")]
    public void EntaoOCabecalhoNaoDeveSerEnviado(string cabecalho) => Assert.False(contexto.Resposta!.Headers.Contains(cabecalho));

    [Then("o ID deve começar com {string}")]
    public void EntaoOIdDeveComecarCom(string prefixo) => Assert.StartsWith(prefixo, contexto.Identificador, StringComparison.Ordinal);

    [Then("o ID deve seguir o padrão oficial")]
    public void EntaoOIdDeveSeguirOPadrao() => Assert.Matches(PadraoDoId.Value, contexto.Identificador);

    [Then("o consentId deve começar com {string}")]
    public void EntaoOConsentIdDeveComecarCom(string prefixo) => Assert.StartsWith(prefixo, contexto.ConsentId, StringComparison.Ordinal);

    [Then("o consentId deve seguir o padrão oficial")]
    public void EntaoOConsentIdDeveSeguirOPadrao() => Assert.Matches(PadraoDoConsentId.Value, contexto.ConsentId);

    [Then("transactionName deve começar com {string}")]
    public void EntaoTransactionNameDeveComecarCom(string prefixo) => Assert.StartsWith(prefixo, contexto.Texto, StringComparison.Ordinal);

    [Then("o campo deve respeitar o tamanho oficial")]
    public void EntaoOCampoDeveRespeitarOTamanhoOficial()
    {
        var oficial = ValidadorDeContrato.SchemaDaResposta(ManipuladorDeTransacoesDeTeste.Operacao)
            ["properties"]!["data"]!["items"]!["properties"]!["transactionName"]!["maxLength"]!.GetValue<int>();
        Assert.Equal(oficial, contexto.Texto!.Length);
    }

    [Then("transactionName não deve ter marca de ambiente")]
    public void EntaoTransactionNameNaoDeveTerMarca() => Assert.StartsWith("PIX recebido", contexto.Texto, StringComparison.Ordinal);

    [Then("a resposta deve ser válida")]
    public void EntaoARespostaDeveSerValida() =>
        Assert.Empty(ValidadorDeContrato.Validar(ManipuladorDeTransacoesDeTeste.Operacao, contexto.Corpo!));

    [Then("a marca deve aparecer no cabeçalho, no identificador e no texto")]
    public void EntaoAMarcaApareceNosTresLugares()
    {
        Assert.Equal(contexto.Marca, Assert.Single(contexto.Resposta!.Headers.GetValues("x-sim-environment")));
        Assert.StartsWith($"{contexto.Marca}-", contexto.Identificador, StringComparison.Ordinal);
        Assert.StartsWith($"[{contexto.Marca.ToUpperInvariant()}] ", contexto.Texto, StringComparison.Ordinal);
    }

    [Given("um cliente criado com a marca {string}")]
    public void DadoUmClienteCriadoComAMarca(string marca)
    {
        contexto.Marca = marca;
        contexto.Identificador = contexto.Marcador.NovoIdentificador(Marcacao.Contratos.Natureza.Persistido);
        contexto.Texto = contexto.Marcador.MarcarTexto("Mariana Alves", 200);
    }

    [When("a massa é copiada para um ambiente com a marca {string}")]
    public void QuandoAMassaECopiada(string marca)
    {
        var destino = contexto.NovaFabrica(marca);
        var marcaLida = destino.Services.GetRequiredService<IMarcador>().Ler(contexto.Identificador!);
        Assert.Equal(marca, destino.Services.GetRequiredService<IMarcador>().Marca.Valor);
        contexto.Marca = marcaLida!.Marca;
    }

    [Then("o cliente deve continuar marcado como {string}")]
    public void EntaoOClienteDeveContinuarMarcadoComo(string marca)
    {
        Assert.Equal(marca, contexto.Marca);
        Assert.StartsWith($"[{marca.ToUpperInvariant()}] ", contexto.Texto, StringComparison.Ordinal);
    }

    [When("Diego tenta desligar a marcação de um banco")]
    public async Task QuandoDiegoTentaDesligar()
    {
        contexto.Resposta = await contexto.Fabrica.ClienteAdministracao().PostAsync(
            new Uri("/admin/bancos/banco-teste/marcacao/desligar", UriKind.Relative), content: null);
    }

    [Then("a opção não deve existir")]
    public void EntaoAOpcaoNaoDeveExistir()
    {
        Assert.Equal(System.Net.HttpStatusCode.NotFound, contexto.Resposta!.StatusCode);
        var rotas = contexto.Fabrica.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText ?? string.Empty);
        Assert.DoesNotContain(rotas, r => r.Contains("marca", StringComparison.OrdinalIgnoreCase));
    }

    private static Natureza Natureza(string nome) => nome switch
    {
        "persistido" => Marcacao.Contratos.Natureza.Persistido,
        "virtual" => Marcacao.Contratos.Natureza.Virtual,
        _ => throw new ArgumentOutOfRangeException(nameof(nome), nome, "Natureza desconhecida."),
    };
}
