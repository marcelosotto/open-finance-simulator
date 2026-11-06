namespace Simulador.Ferramentas;

/// <summary>Opções no formato <c>--nome valor</c>.</summary>
internal sealed class Opcoes
{
    private readonly Dictionary<string, string> _valores = new(StringComparer.Ordinal);

    public Opcoes(IEnumerable<string> argumentos)
    {
        string? chave = null;
        foreach (var argumento in argumentos)
        {
            if (argumento.StartsWith("--", StringComparison.Ordinal))
            {
                chave = argumento[2..];
                _valores[chave] = string.Empty;
            }
            else if (chave is not null)
            {
                _valores[chave] = argumento;
                chave = null;
            }
        }
    }

    public string? Valor(string nome) => _valores.TryGetValue(nome, out var valor) && valor.Length > 0 ? valor : null;

    public string Valor(string nome, string padrao) => Valor(nome) ?? padrao;
}
