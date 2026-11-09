using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Contratos;
using Simulador.Marcacao.Dominio;

namespace Simulador.Testes.Unidade.Marcacao;

public class IdentificadorComCorpoTests
{
    private static GeradorDeIdentificador Gerador(string marca) => new(new MarcaDoAmbiente(marca), new GeradorAleatorioComSemente(1));

    [Fact]
    public void Identificador_usa_o_corpo_informado()
    {
        Assert.Equal("hml-v-abc", Gerador("hml").Identificador(Natureza.Virtual, "abc"));
        Assert.Equal("p-abc", Gerador(string.Empty).Identificador(Natureza.Persistido, "abc"));
    }

    [Fact]
    public void ConsentId_usa_o_corpo_informado()
    {
        Assert.Equal("urn:hml:v-abc", Gerador("hml").ConsentId(Natureza.Virtual, "abc"));
        Assert.Equal("urn:p:abc", Gerador(string.Empty).ConsentId(Natureza.Persistido, "abc"));
    }
}
