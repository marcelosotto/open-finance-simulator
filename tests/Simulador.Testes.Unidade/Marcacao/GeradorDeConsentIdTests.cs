using System.Text.RegularExpressions;
using Simulador.Catalogo.Contratos;
using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Contratos;
using Simulador.Marcacao.Dominio;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Marcacao;

public class GeradorDeConsentIdTests
{
    private static readonly Lazy<Regex> PadraoOficial = new(() =>
    {
        var documento = ServicosDeSpecs.LerDocumentoJson(
            TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs(), "consents");
        var schema = ServicosDeSpecs.ResolverReferencias(documento["components"]!["schemas"]!["ResponseConsent"], documento)!;
        var padrao = schema["properties"]!["data"]!["properties"]!["consentId"]!["pattern"]!.GetValue<string>();
        return new Regex(padrao, RegexOptions.None, TimeSpan.FromSeconds(1));
    });

    [Theory]
    [InlineData("hml", Natureza.Persistido, "urn:hml:p-")]
    [InlineData("hml", Natureza.Virtual, "urn:hml:v-")]
    [InlineData("dev", Natureza.Virtual, "urn:dev:v-")]
    [InlineData("", Natureza.Persistido, "urn:p:")]
    [InlineData("", Natureza.Virtual, "urn:v:")]
    public void ConsentId_traz_marca_e_natureza_no_padrao_oficial(string marca, Natureza natureza, string prefixo)
    {
        var gerador = new GeradorDeIdentificador(new MarcaDoAmbiente(marca), new GeradorAleatorioComSemente(5));

        var consentId = gerador.NovoConsentId(natureza);

        Assert.StartsWith(prefixo, consentId, StringComparison.Ordinal);
        Assert.Matches(PadraoOficial.Value, consentId);
    }
}
