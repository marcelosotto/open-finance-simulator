namespace Simulador.Comum.Aleatorio;

/// <summary>Fonte de sorteio injetável. Com a mesma semente, a mesma sequência (princípio VI).</summary>
public interface IGeradorAleatorio
{
    /// <summary>Inteiro em [0, maximo).</summary>
    int ProximoInteiro(int maximo);

    /// <summary>Texto com letras minúsculas e dígitos.</summary>
    string ProximoTexto(int tamanho);
}

/// <summary>Gerador sobre <see cref="Random"/> com semente.</summary>
public sealed class GeradorAleatorioComSemente(int semente) : IGeradorAleatorio
{
    private const string Alfabeto = "abcdefghijklmnopqrstuvwxyz0123456789";

#pragma warning disable CA5394 // Sorteio simulado, não criptográfico.
    private readonly Random _random = new(semente);
    private readonly Lock _trava = new();

    public int ProximoInteiro(int maximo)
    {
        lock (_trava)
        {
            return _random.Next(maximo);
        }
    }

    public string ProximoTexto(int tamanho)
    {
        lock (_trava)
        {
            return string.Create(tamanho, _random, static (destino, random) =>
            {
                for (var i = 0; i < destino.Length; i++)
                {
                    destino[i] = Alfabeto[random.Next(Alfabeto.Length)];
                }
            });
        }
    }
#pragma warning restore CA5394
}
