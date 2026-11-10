using System.Globalization;

namespace Simulador.Comum.Aleatorio;

/// <summary>
/// Semente dos geradores, lida de <c>Simulador:Semente</c> do mesmo jeito em todo o processo. Sem valor, ou com
/// valor que não é inteiro, vale uma semente sorteada uma vez por processo; assim, os geradores que derivam dela
/// continuam independentes entre si, com ou sem semente configurada.
/// </summary>
public static class SementeDoAmbiente
{
    public const string Chave = "Simulador:Semente";

    private static readonly int DoProcesso = Environment.TickCount;

    /// <summary>A semente configurada, ou a do processo quando falta ou é malformada.</summary>
    public static int De(string? valor) =>
        int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var semente) ? semente : DoProcesso;

    /// <summary>Semente de um gerador derivado: a do ambiente com um deslocamento próprio, nunca igual à base.</summary>
    public static int Derivada(string? valor, int deslocamento)
    {
        ArgumentOutOfRangeException.ThrowIfZero(deslocamento);
        return De(valor) ^ deslocamento;
    }
}
