using Simulador.Marcacao.Contratos;
using Simulador.Marcacao.Dominio;

namespace Simulador.Testes.Unidade.Marcacao;

public class MarcadorDeTextoTests
{
    [Theory]
    [InlineData("hml", "PIX recebido", "[HML] PIX recebido")]
    [InlineData("dev", "Mariana Alves", "[DEV] Mariana Alves")]
    [InlineData("", "PIX recebido", "PIX recebido")]
    public void Coloca_a_marca_na_frente(string marca, string texto, string esperado) =>
        Assert.Equal(esperado, MarcadorDeTexto.Marcar(new MarcaDoAmbiente(marca), texto, 200));

    [Fact]
    public void Corta_o_fim_do_texto_original_e_mantem_a_marca()
    {
        var marcado = MarcadorDeTexto.Marcar(new MarcaDoAmbiente("hml"), "PIX recebido de Lima Santos", 16);

        Assert.Equal("[HML] PIX recebi", marcado);
        Assert.Equal(16, marcado.Length);
    }

    [Fact]
    public void Sem_tamanho_maximo_nao_corta() =>
        Assert.Equal("[HML] texto longo", MarcadorDeTexto.Marcar(new MarcaDoAmbiente("hml"), "texto longo", null));

    [Fact]
    public void Marca_vazia_respeita_o_tamanho() =>
        Assert.Equal("PIX", MarcadorDeTexto.Marcar(new MarcaDoAmbiente(string.Empty), "PIX recebido", 3));

    [Fact]
    public void Tamanho_menor_que_a_marca_devolve_so_a_marca_cortada() =>
        Assert.Equal("[HM", MarcadorDeTexto.Marcar(new MarcaDoAmbiente("hml"), "PIX", 3));
}
