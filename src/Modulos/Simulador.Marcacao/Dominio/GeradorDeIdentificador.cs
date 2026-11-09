using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Contratos;

namespace Simulador.Marcacao.Dominio;

/// <summary>Gera IDs e <c>consentId</c> com a marca e a natureza (RN62).</summary>
internal sealed class GeradorDeIdentificador(MarcaDoAmbiente marca, IGeradorAleatorio aleatorio)
{
    public const int TamanhoAleatorio = 16;

    /// <summary>Tamanho máximo do ID no padrão oficial <c>^[a-zA-Z0-9][a-zA-Z0-9\-]{0,99}$</c>.</summary>
    public const int TamanhoMaximo = 100;

    public string NovoIdentificador(Natureza natureza)
    {
        var prefixo = marca.Vazia ? $"{Letra(natureza)}-" : $"{marca.Valor}-{Letra(natureza)}-";
        var tamanho = Math.Clamp(TamanhoMaximo - prefixo.Length, 1, TamanhoAleatorio);
        return prefixo + aleatorio.ProximoTexto(tamanho);
    }

    public string NovoConsentId(Natureza natureza) =>
        marca.Vazia
            ? $"urn:{Letra(natureza)}:{aleatorio.ProximoTexto(TamanhoAleatorio)}"
            : $"urn:{marca.Valor}:{Letra(natureza)}-{aleatorio.ProximoTexto(TamanhoAleatorio)}";

    public static char Letra(Natureza natureza) => natureza switch
    {
        Natureza.Persistido => 'p',
        Natureza.Virtual => 'v',
        _ => throw new ArgumentOutOfRangeException(nameof(natureza), natureza, "Natureza desconhecida."),
    };
}

/// <summary>Coloca a marca na frente do texto, dentro do tamanho oficial do campo (RN63).</summary>
internal static class MarcadorDeTexto
{
    public static string Marcar(MarcaDoAmbiente marca, string texto, int? tamanhoMaximo)
    {
        var marcado = marca.Textual + texto;
        return tamanhoMaximo is { } maximo && marcado.Length > maximo ? marcado[..maximo] : marcado;
    }
}

/// <summary>Natureza agregada de uma resposta (RN65).</summary>
internal static class AgregadorDeNatureza
{
    public static NaturezaDaResposta? Agregar(IEnumerable<Natureza> naturezas)
    {
        var distintas = naturezas.Distinct().ToList();
        return distintas.Count switch
        {
            0 => null,
            1 when distintas[0] == Natureza.Persistido => NaturezaDaResposta.Persistido,
            1 => NaturezaDaResposta.Virtual,
            _ => NaturezaDaResposta.Misto,
        };
    }

    public static string NomeNoCabecalho(NaturezaDaResposta natureza) => natureza switch
    {
        NaturezaDaResposta.Persistido => "persistido",
        NaturezaDaResposta.Virtual => "virtual",
        _ => "misto",
    };
}

/// <summary>Lê marca e natureza do próprio identificador, nunca da configuração atual (RN64).</summary>
internal static class LeitorDeMarca
{
    private const string PrefixoUrn = "urn:";

    public static MarcaLida? Ler(string identificador)
    {
        if (identificador.StartsWith(PrefixoUrn, StringComparison.Ordinal))
        {
            var partes = identificador[PrefixoUrn.Length..].Split(':');
            if (partes.Length != 2)
            {
                return null;
            }

            return partes[1].Contains('-', StringComparison.Ordinal)
                ? LerRecurso(partes[1]) is { Marca: "" } lida ? lida with { Marca = partes[0] } : null
                : Natureza(partes[0]) is { } natureza ? new MarcaLida(string.Empty, natureza) : null;
        }

        return LerRecurso(identificador);
    }

    private static MarcaLida? LerRecurso(string identificador)
    {
        var partes = identificador.Split('-');
        if (partes.Length < 2 || Natureza(partes[^2]) is not { } natureza)
        {
            return null;
        }

        return new MarcaLida(string.Join('-', partes[..^2]), natureza);
    }

    private static Natureza? Natureza(string letra) => letra switch
    {
        "p" => Contratos.Natureza.Persistido,
        "v" => Contratos.Natureza.Virtual,
        _ => null,
    };
}

/// <summary>Reúne geração de IDs, texto marcado e leitura da marca.</summary>
internal sealed class Marcador(MarcaDoAmbiente marca, IGeradorAleatorio aleatorio) : IMarcador
{
    private readonly GeradorDeIdentificador _gerador = new(marca, aleatorio);

    public MarcaDoAmbiente Marca => marca;

    public string NovoIdentificador(Natureza natureza) => _gerador.NovoIdentificador(natureza);

    public string NovoConsentId(Natureza natureza) => _gerador.NovoConsentId(natureza);

    public string MarcarTexto(string texto, int? tamanhoMaximo) => MarcadorDeTexto.Marcar(marca, texto, tamanhoMaximo);

    public MarcaLida? Ler(string identificador) => LeitorDeMarca.Ler(identificador);
}
