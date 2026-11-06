namespace Simulador.Catalogo.Contratos;

/// <summary>Frequência da tabela oficial de endpoints.</summary>
public enum Frequencia
{
    Alta,
    MediaAlta,
    Media,
    Baixa,
}

/// <summary>Classe e limites de um endpoint, copiados da tabela oficial (FR-011).</summary>
/// <param name="Tpm">TPM fixo; nulo quando não há limite de tráfego ou quando é faixa QCA.</param>
/// <param name="TpmPorFaixaQca">TPM depende dos consentimentos ativos (RN21).</param>
/// <param name="LimiteMensal">Nulo quando não há limite operacional.</param>
public sealed record ClasseDoEndpoint(
    string Contrato,
    string Secao,
    string Metodo,
    string Caminho,
    Frequencia Frequencia,
    int SlaMs,
    int? Tpm,
    bool TpmPorFaixaQca,
    int? LimiteMensal);

/// <summary>Tabela de endpoints ausente ou fora do schema.</summary>
public sealed class TabelaInvalidaException : Exception
{
    public TabelaInvalidaException()
    {
    }

    public TabelaInvalidaException(string message)
        : base(message)
    {
    }

    public TabelaInvalidaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
