using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Contratos;
using Simulador.Marcacao.Dominio;

namespace Simulador.Testes.Unidade.Marcacao;

public class NaturezaTests
{
    [Fact]
    public void Todos_persistidos() =>
        Assert.Equal(NaturezaDaResposta.Persistido, AgregadorDeNatureza.Agregar([Natureza.Persistido, Natureza.Persistido]));

    [Fact]
    public void Todos_virtuais() =>
        Assert.Equal(NaturezaDaResposta.Virtual, AgregadorDeNatureza.Agregar([Natureza.Virtual]));

    [Fact]
    public void Ambos_e_misto() =>
        Assert.Equal(NaturezaDaResposta.Misto, AgregadorDeNatureza.Agregar([Natureza.Persistido, Natureza.Virtual]));

    [Fact]
    public void Nenhum_item_nao_tem_natureza() => Assert.Null(AgregadorDeNatureza.Agregar([]));

    [Theory]
    [InlineData(NaturezaDaResposta.Persistido, "persistido")]
    [InlineData(NaturezaDaResposta.Virtual, "virtual")]
    [InlineData(NaturezaDaResposta.Misto, "misto")]
    public void Nome_no_cabecalho(NaturezaDaResposta natureza, string nome) =>
        Assert.Equal(nome, AgregadorDeNatureza.NomeNoCabecalho(natureza));

    [Theory]
    [InlineData("dev-p-abc123", "dev", Natureza.Persistido)]
    [InlineData("hml-v-abc123", "hml", Natureza.Virtual)]
    [InlineData("meu-amb-v-abc", "meu-amb", Natureza.Virtual)]
    [InlineData("p-abc123", "", Natureza.Persistido)]
    [InlineData("urn:hml:v-abc123", "hml", Natureza.Virtual)]
    [InlineData("urn:dev:p-abc", "dev", Natureza.Persistido)]
    [InlineData("urn:v:abc123", "", Natureza.Virtual)]
    [InlineData("urn:p:abc", "", Natureza.Persistido)]
    public void Le_marca_e_natureza_do_proprio_id(string id, string marca, Natureza natureza) =>
        Assert.Equal(new MarcaLida(marca, natureza), LeitorDeMarca.Ler(id));

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("hml-x-abc")]
    [InlineData("urn:hml:x-abc")]
    [InlineData("urn:x:abc")]
    [InlineData("urn:a:b:c")]
    [InlineData("urn:")]
    [InlineData("urn:hml:dev-p-abc")]
    public void Id_sem_marca_reconhecida_devolve_nulo(string id) => Assert.Null(LeitorDeMarca.Ler(id));

    [Fact]
    public void Marcador_reune_gerador_texto_e_leitura()
    {
        var marcador = new Marcador(new MarcaDoAmbiente("hml"), new GeradorAleatorioComSemente(1));

        var id = marcador.NovoIdentificador(Natureza.Virtual);

        Assert.Equal("hml", marcador.Marca.Valor);
        Assert.StartsWith("urn:hml:p-", marcador.NovoConsentId(Natureza.Persistido), StringComparison.Ordinal);
        Assert.Equal("[HML] PIX", marcador.MarcarTexto("PIX", 200));
        Assert.Equal(new MarcaLida("hml", Natureza.Virtual), marcador.Ler(id));
    }
}
