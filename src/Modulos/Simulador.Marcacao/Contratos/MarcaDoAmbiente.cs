namespace Simulador.Marcacao.Contratos;

/// <summary>
/// Marca do ambiente: valor livre da configuração da implantação, só concatenado (RN60).
/// Vazia em produção: nada é concatenado (RN70).
/// </summary>
public sealed record MarcaDoAmbiente
{
    public MarcaDoAmbiente(string? valor) => Valor = valor?.Trim() ?? string.Empty;

    public string Valor { get; }

    public bool Vazia => Valor.Length == 0;

    /// <summary>Prefixo do texto marcado: <c>[HML] </c>; vazio quando a marca é vazia (RN63).</summary>
    public string Textual => Vazia ? string.Empty : $"[{Valor.ToUpperInvariant()}] ";
}

/// <summary>Natureza do dado (RN62).</summary>
public enum Natureza
{
    Persistido,
    Virtual,
}

/// <summary>Natureza agregada de uma resposta (RN65).</summary>
public enum NaturezaDaResposta
{
    Persistido,
    Virtual,
    Misto,
}

/// <summary>Marca e natureza lidas de um identificador já gravado (RN64).</summary>
public sealed record MarcaLida(string Marca, Natureza Natureza);

/// <summary>Gera identificadores e textos marcados e lê a marca de um identificador.</summary>
public interface IMarcador
{
    MarcaDoAmbiente Marca { get; }

    /// <summary><c>{marca}-{p|v}-{aleatório}</c>, ou <c>{p|v}-{aleatório}</c> com a marca vazia.</summary>
    string NovoIdentificador(Natureza natureza);

    /// <summary><c>urn:{marca}:{p|v}-{aleatório}</c>, ou <c>urn:{p|v}:{aleatório}</c> com a marca vazia.</summary>
    string NovoConsentId(Natureza natureza);

    /// <summary>Coloca <see cref="MarcaDoAmbiente.Textual"/> na frente, dentro do tamanho máximo.</summary>
    string MarcarTexto(string texto, int? tamanhoMaximo);

    /// <summary>Marca e natureza gravadas no identificador; nulo se não houver.</summary>
    MarcaLida? Ler(string identificador);
}

/// <summary>Registra, durante a chamada, a natureza de cada item da resposta.</summary>
public interface IRegistroDeNatureza
{
    void Registrar(Natureza natureza);
}
