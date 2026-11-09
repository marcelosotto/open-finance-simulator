namespace Simulador.Massa.Dominio;

/// <summary>
/// Divide os itens entre os bancos em um ciclo embaralhado pela semente, para achar o n-ésimo
/// item de um banco sem varrer o mundo (RN52, RN56, research R04).
/// </summary>
internal sealed class CicloDeBancos
{
    private const int Tamanho = 10_000;

    private readonly string[] _ciclo;
    private readonly Dictionary<string, int[]> _posicoes;

    public CicloDeBancos(long semente, IReadOnlyDictionary<string, decimal> bancos)
    {
        _ciclo = [.. bancos.OrderBy(b => b.Key, StringComparer.Ordinal)
            .SelectMany(b => Enumerable.Repeat(b.Key, (int)(b.Value * 100)))];

        var sorteio = Sorteio.Para(semente, "ciclo");
        for (var i = _ciclo.Length - 1; i > 0; i--)
        {
            var outro = (int)sorteio.Entre(0, i);
            (_ciclo[i], _ciclo[outro]) = (_ciclo[outro], _ciclo[i]);
        }

        _posicoes = Enumerable.Range(0, _ciclo.Length)
            .GroupBy(i => _ciclo[i], StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);
    }

    public string BancoDe(long indice) => _ciclo[indice % Tamanho];

    /// <summary>Índice geral do n-ésimo item do banco, ou nulo se o banco não recebe itens.</summary>
    public long? IndiceNoBanco(string banco, long n)
    {
        if (!_posicoes.TryGetValue(banco, out var posicoes))
        {
            return null;
        }

        return (n / posicoes.Length * Tamanho) + posicoes[n % posicoes.Length];
    }
}
