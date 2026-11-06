using Simulador.Marcacao.Contratos;

namespace Simulador.Testes.Unidade.Marcacao;

public class MarcaDoAmbienteTests
{
    [Theory]
    [InlineData("hml", "[HML] ")]
    [InlineData("dev", "[DEV] ")]
    [InlineData("stage", "[STAGE] ")]
    [InlineData(" hml ", "[HML] ")]
    public void Marca_preenchida_gera_texto_em_maiusculas(string valor, string textual)
    {
        var marca = new MarcaDoAmbiente(valor);

        Assert.False(marca.Vazia);
        Assert.Equal(textual, marca.Textual);
        Assert.Equal(valor.Trim(), marca.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Marca_vazia_nao_gera_texto(string? valor)
    {
        var marca = new MarcaDoAmbiente(valor);

        Assert.True(marca.Vazia);
        Assert.Equal(string.Empty, marca.Textual);
        Assert.Equal(string.Empty, marca.Valor);
    }

    [Fact]
    public void Nenhum_valor_e_recusado()
    {
        var marca = new MarcaDoAmbiente("qualquer coisa: com espaço");

        Assert.Equal("[QUALQUER COISA: COM ESPAÇO] ", marca.Textual);
    }
}
