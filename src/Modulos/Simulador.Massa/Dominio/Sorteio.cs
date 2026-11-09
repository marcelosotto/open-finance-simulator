using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Simulador.Massa.Dominio;

/// <summary>SplitMix64: a mesma semente dá a mesma sequência em qualquer versão do .NET (RN54).</summary>
internal sealed class Sorteio(long semente)
{
    private ulong _estado = unchecked((ulong)semente);

    /// <summary>Semente derivada de uma semente e de rótulos, independente da ordem de geração.</summary>
    public static long Derivar(long semente, params ReadOnlySpan<object> rotulos)
    {
        var texto = new StringBuilder(semente.ToString(CultureInfo.InvariantCulture));
        foreach (var rotulo in rotulos)
        {
            texto.Append('|').Append(Convert.ToString(rotulo, CultureInfo.InvariantCulture));
        }

        return BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(texto.ToString())), 0);
    }

    public static Sorteio Para(long semente, params ReadOnlySpan<object> rotulos) => new(Derivar(semente, rotulos));

    public ulong Proximo()
    {
        unchecked
        {
            var z = _estado += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    /// <summary>Inteiro em [minimo, maximo].</summary>
    public long Entre(long minimo, long maximo) => minimo + (long)(Proximo() % (ulong)(maximo - minimo + 1));

    /// <summary>Verdadeiro com a porcentagem dada, com 2 casas de precisão.</summary>
    public bool Chance(decimal porcentagem) => Entre(0, 9_999) < porcentagem * 100;
}
