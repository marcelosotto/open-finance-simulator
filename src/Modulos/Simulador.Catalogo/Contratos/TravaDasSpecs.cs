using Simulador.Catalogo.Dominio;

namespace Simulador.Catalogo.Contratos;

/// <summary>Contrato travado no <c>specs.lock.json</c> (research R01).</summary>
public sealed record ContratoTravado(string Id, string CaminhoLocal, string Sha256);

/// <summary>Conteúdo do <c>specs.lock.json</c>: repositório, commit e contratos travados.</summary>
public sealed record TravaDasSpecs(
    string Repositorio,
    string Commit,
    string Catalogo,
    IReadOnlyList<ContratoTravado> Contratos)
{
    public const string CatalogoPadrao = "contracts/expansion/catalog.json";

    /// <summary>Lê e valida o arquivo de trava.</summary>
    /// <exception cref="TravaInvalidaException">Arquivo ausente ou inválido.</exception>
    public static TravaDasSpecs Ler(string caminho)
    {
        if (!File.Exists(caminho))
        {
            throw new TravaInvalidaException($"Arquivo de trava não encontrado: {caminho}");
        }

        return Interpretar(File.ReadAllText(caminho));
    }

    /// <summary>Valida o JSON da trava.</summary>
    /// <exception cref="TravaInvalidaException">JSON malformado ou fora do schema.</exception>
    public static TravaDasSpecs Interpretar(string json) => LeitorDeTrava.Interpretar(json);
}

/// <summary>Trava ausente, malformada ou fora do schema.</summary>
public sealed class TravaInvalidaException : Exception
{
    public TravaInvalidaException()
    {
    }

    public TravaInvalidaException(string message)
        : base(message)
    {
    }

    public TravaInvalidaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Uma falha de conferência das specs: arquivo e motivo.</summary>
public sealed record FalhaDeConferencia(string Arquivo, string Motivo)
{
    public override string ToString() => $"{Arquivo}: {Motivo}";
}
