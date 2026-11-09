using Simulador.Catalogo.Contratos;

namespace Simulador.Catalogo.Dominio;

/// <summary>Catálogo em memória: operações publicadas, classes e problemas de carga.</summary>
internal sealed class CatalogoCarregado : ICatalogo
{
    private readonly Dictionary<(string, string, string), ClasseDoEndpoint> _classes;

    public CatalogoCarregado(
        string commit,
        IReadOnlyList<OperacaoDoCatalogo> operacoes,
        IEnumerable<ClasseDoEndpoint> classes,
        IReadOnlyList<string> problemas)
    {
        Commit = commit;
        Operacoes = operacoes;
        Problemas = problemas;
        _classes = classes.ToDictionary(c => (c.Contrato, c.Metodo, c.Caminho));
    }

    public string Commit { get; }

    public bool Pronto => Problemas.Count == 0;

    public IReadOnlyList<string> Problemas { get; }

    public IReadOnlyList<OperacaoDoCatalogo> Operacoes { get; }

    public ClasseDoEndpoint? ClasseDe(OperacaoDoCatalogo operacao)
    {
        ArgumentNullException.ThrowIfNull(operacao);
        return _classes.GetValueOrDefault(operacao.Chave);
    }

    /// <summary>Catálogo vazio e não pronto.</summary>
    public static CatalogoCarregado Falho(string commit, IReadOnlyList<string> problemas) => new(commit, [], [], problemas);
}
