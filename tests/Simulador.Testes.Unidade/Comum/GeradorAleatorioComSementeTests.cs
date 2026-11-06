using Simulador.Comum.Aleatorio;

namespace Simulador.Testes.Unidade.Comum;

public class GeradorAleatorioComSementeTests
{
    [Fact]
    public void Mesma_semente_gera_a_mesma_sequencia()
    {
        var a = new GeradorAleatorioComSemente(42);
        var b = new GeradorAleatorioComSemente(42);

        Assert.Equal(a.ProximoTexto(20), b.ProximoTexto(20));
        Assert.Equal(a.ProximoInteiro(1000), b.ProximoInteiro(1000));
    }

    [Fact]
    public void Sementes_diferentes_geram_sequencias_diferentes()
    {
        var a = new GeradorAleatorioComSemente(1);
        var b = new GeradorAleatorioComSemente(2);

        Assert.NotEqual(a.ProximoTexto(20), b.ProximoTexto(20));
    }

    [Fact]
    public void Texto_so_tem_letras_minusculas_e_digitos()
    {
        var texto = new GeradorAleatorioComSemente(7).ProximoTexto(500);

        Assert.Equal(500, texto.Length);
        Assert.Matches("^[a-z0-9]+$", texto);
    }

    [Fact]
    public void Inteiro_fica_abaixo_do_maximo()
    {
        var gerador = new GeradorAleatorioComSemente(3);

        for (var i = 0; i < 100; i++)
        {
            Assert.InRange(gerador.ProximoInteiro(5), 0, 4);
        }
    }
}
