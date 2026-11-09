using System.Globalization;

namespace Simulador.Limites.Dominio;

/// <summary>Chaves e janelas dos contadores (RN22, RN25, RN29).</summary>
internal static class ChavesDeContador
{
    public static TimeSpan ValidadeDaCapacidade { get; } = TimeSpan.FromSeconds(5);

    public static TimeSpan ValidadeDoTrafego { get; } = TimeSpan.FromSeconds(120);

    /// <summary>Maior que o mês mais longo, mesmo com relógio simulado (RN73).</summary>
    public static TimeSpan ValidadeDaCota { get; } = TimeSpan.FromDays(70);

    public static string Capacidade(string banco, DateTimeOffset agora) =>
        string.Create(CultureInfo.InvariantCulture, $"lim:cap:{banco}:{agora.ToUnixTimeSeconds()}");

    public static string Trafego(string banco, string operacao, string receptora, DateTimeOffset agora) =>
        string.Create(CultureInfo.InvariantCulture, $"lim:tpm:{banco}:{operacao}:{receptora}:{agora.ToUnixTimeSeconds() / 60}");

    /// <summary>O mês é o calendário em UTC (RN25).</summary>
    public static string Cota(string banco, string operacao, string objeto, string cliente, string receptora, DateTimeOffset agora) =>
        string.Create(CultureInfo.InvariantCulture, $"lim:cota:{banco}:{agora.UtcDateTime:yyyyMM}:{operacao}:{objeto}:{cliente}:{receptora}");
}

/// <summary>Objeto mais granular da cota (RN25).</summary>
public static class ObjetoDaCota
{
    /// <summary>Nome do último parâmetro do caminho; nulo em rota sem parâmetro.</summary>
    public static string? Parametro(string caminho)
    {
        ArgumentNullException.ThrowIfNull(caminho);
        var fim = caminho.LastIndexOf('}');
        return fim < 0 ? null : caminho[(caminho.LastIndexOf('{') + 1)..fim];
    }
}
