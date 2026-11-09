using Simulador.Limites.Dominio;

namespace Simulador.Testes.Unidade.Limites;

/// <summary>Janelas, chaves e objeto da cota (RN22, RN25, RN29).</summary>
public class DominioDosLimitesTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 30, 23, 59, 59, 999, TimeSpan.Zero);

    [Fact]
    public void Segundo_cheio_muda_a_chave_da_capacidade()
    {
        Assert.Equal(ChavesDeContador.Capacidade("b", Agora), ChavesDeContador.Capacidade("b", Agora.AddMilliseconds(-999)));
        Assert.NotEqual(ChavesDeContador.Capacidade("b", Agora), ChavesDeContador.Capacidade("b", Agora.AddMilliseconds(1)));
    }

    [Fact]
    public void Minuto_cheio_muda_a_chave_do_trafego()
    {
        Assert.Equal(ChavesDeContador.Trafego("b", "op", "r", Agora), ChavesDeContador.Trafego("b", "op", "r", Agora.AddSeconds(-59)));
        Assert.NotEqual(ChavesDeContador.Trafego("b", "op", "r", Agora), ChavesDeContador.Trafego("b", "op", "r", Agora.AddMilliseconds(1)));
    }

    [Fact]
    public void Mes_da_cota_vira_em_utc()
    {
        var virada = ChavesDeContador.Cota("b", "op", "o", "c", "r", Agora.AddMilliseconds(1));
        Assert.Contains(":202612:", virada, StringComparison.Ordinal);
        Assert.Contains(":202611:", ChavesDeContador.Cota("b", "op", "o", "c", "r", Agora), StringComparison.Ordinal);
        Assert.Contains(":202612:", ChavesDeContador.Cota("b", "op", "o", "c", "r", new DateTimeOffset(2026, 11, 30, 22, 0, 0, TimeSpan.FromHours(-3))), StringComparison.Ordinal);
    }

    [Fact]
    public void Validades_cobrem_as_janelas()
    {
        Assert.True(ChavesDeContador.ValidadeDaCapacidade > TimeSpan.FromSeconds(1));
        Assert.True(ChavesDeContador.ValidadeDoTrafego > TimeSpan.FromMinutes(1));
        Assert.True(ChavesDeContador.ValidadeDaCota > TimeSpan.FromDays(31));
    }

    [Theory]
    [InlineData("/accounts", null)]
    [InlineData("/accounts/{accountId}/balances", "accountId")]
    [InlineData("/accounts/{creditCardAccountId}/bills/{billId}/transactions", "billId")]
    public void Objeto_e_o_ultimo_parametro(string caminho, string? esperado) =>
        Assert.Equal(esperado, ObjetoDaCota.Parametro(caminho));
}
