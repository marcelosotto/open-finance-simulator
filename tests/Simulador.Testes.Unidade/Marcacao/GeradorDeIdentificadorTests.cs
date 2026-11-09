using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Contratos;
using Simulador.Marcacao.Dominio;

namespace Simulador.Testes.Unidade.Marcacao;

public class GeradorDeIdentificadorTests
{
    private const string PadraoOficial = @"^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$";

    private static GeradorDeIdentificador Gerador(string? marca, int semente = 1) =>
        new(new MarcaDoAmbiente(marca), new GeradorAleatorioComSemente(semente));

    [Theory]
    [InlineData("hml", Natureza.Persistido, "hml-p-")]
    [InlineData("hml", Natureza.Virtual, "hml-v-")]
    [InlineData("dev", Natureza.Persistido, "dev-p-")]
    [InlineData("stage", Natureza.Virtual, "stage-v-")]
    [InlineData("", Natureza.Persistido, "p-")]
    [InlineData("", Natureza.Virtual, "v-")]
    public void Prefixo_traz_marca_e_natureza(string marca, Natureza natureza, string prefixo)
    {
        var id = Gerador(marca).NovoIdentificador(natureza);

        Assert.StartsWith(prefixo, id, StringComparison.Ordinal);
        Assert.Matches(PadraoOficial, id);
        Assert.Equal(prefixo.Length + GeradorDeIdentificador.TamanhoAleatorio, id.Length);
    }

    [Fact]
    public void Marca_longa_mantem_o_id_em_100_caracteres_sem_cortar_a_marca()
    {
        var marca = new string('m', 95);

        var id = Gerador(marca).NovoIdentificador(Natureza.Persistido);

        Assert.Equal(100, id.Length);
        Assert.StartsWith(marca + "-p-", id, StringComparison.Ordinal);
        Assert.Matches(PadraoOficial, id);
    }

    [Fact]
    public void Marca_maior_que_o_limite_ainda_tem_um_caractere_aleatorio()
    {
        var marca = new string('m', 120);

        var id = Gerador(marca).NovoIdentificador(Natureza.Virtual);

        Assert.Equal(marca.Length + 4, id.Length);
    }

    [Fact]
    public void Mesma_semente_gera_os_mesmos_ids()
    {
        Assert.Equal(Gerador("hml", 9).NovoIdentificador(Natureza.Virtual), Gerador("hml", 9).NovoIdentificador(Natureza.Virtual));
        Assert.NotEqual(Gerador("hml", 9).NovoIdentificador(Natureza.Virtual), Gerador("hml", 10).NovoIdentificador(Natureza.Virtual));
    }

    [Fact]
    public void Natureza_desconhecida_e_recusada() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Gerador("hml").NovoIdentificador((Natureza)9));
}
