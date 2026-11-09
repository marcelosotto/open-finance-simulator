using System.Globalization;

namespace Simulador.Massa.Dominio;

/// <summary>Base 36 usada em identificadores e na raiz do CNPJ.</summary>
internal static class Base36
{
    private const string Alfabeto = "0123456789abcdefghijklmnopqrstuvwxyz";

    public static string Codificar(long valor)
    {
        var texto = string.Empty;
        do
        {
            texto = Alfabeto[(int)(valor % 36)] + texto;
            valor /= 36;
        }
        while (valor > 0);

        return texto;
    }

    public static long? Decodificar(string texto)
    {
        var valor = 0L;
        foreach (var letra in texto)
        {
            var digito = Alfabeto.IndexOf(letra, StringComparison.Ordinal);
            if (digito < 0 || valor > (long.MaxValue - digito) / 36)
            {
                return null;
            }

            valor = (valor * 36) + digito;
        }

        return texto.Length == 0 ? null : valor;
    }
}

/// <summary>CPF e CNPJ válidos, sem repetição entre massa persistida e virtual (RN57, research R03).</summary>
internal static class Documentos
{
    public const long CapacidadeVirtual = 50_000_000;

    /// <summary>A última posição daria o CPF com todos os dígitos iguais.</summary>
    public const long CapacidadePersistida = 49_999_999;

    private const string Filial = "0001";
    private static readonly int[] PesosCnpj = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public static string Cpf(long posicao, bool virtualizado)
    {
        var corpo = "9" + ((virtualizado ? 0 : CapacidadeVirtual) + posicao).ToString("D8", CultureInfo.InvariantCulture);
        var primeiro = DigitoCpf(corpo);
        return corpo + primeiro + DigitoCpf(corpo + primeiro);
    }

    public static string Cnpj(long posicao, bool virtualizado)
    {
        var raiz = (virtualizado ? "V" : "P") + Base36.Codificar(posicao).ToUpperInvariant().PadLeft(7, '0') + Filial;
        var primeiro = DigitoCnpj(raiz);
        return raiz + primeiro + DigitoCnpj(raiz + primeiro);
    }

    /// <summary>Posição de um documento do mundo virtual; nulo se não for um.</summary>
    public static long? IndiceVirtual(string documento)
    {
        if (documento.Length == 11)
        {
            return long.TryParse(documento.AsSpan(1, 8), NumberStyles.None, CultureInfo.InvariantCulture, out var corpo)
                && corpo < CapacidadeVirtual && Cpf(corpo, virtualizado: true) == documento
                ? corpo
                : null;
        }

        if (documento.Length == 14 && documento[0] == 'V'
            && Base36.Decodificar(documento[1..8].ToLowerInvariant()) is { } posicao && Cnpj(posicao, virtualizado: true) == documento)
        {
            return posicao;
        }

        return null;
    }

    private static int DigitoCpf(string base10)
    {
        var soma = base10.Select((letra, i) => (letra - '0') * (base10.Length + 1 - i)).Sum();
        var digito = 11 - (soma % 11);
        return digito >= 10 ? 0 : digito;
    }

    /// <summary>Módulo 11 sobre o valor ASCII menos 48, como no CNPJ alfanumérico.</summary>
    private static int DigitoCnpj(string corpo)
    {
        var pesos = PesosCnpj.AsSpan(PesosCnpj.Length - corpo.Length);
        var soma = 0;
        for (var i = 0; i < corpo.Length; i++)
        {
            soma += (corpo[i] - '0') * pesos[i];
        }

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
