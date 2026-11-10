using System.Text;
using Microsoft.AspNetCore.Http;

namespace Simulador.Comum.Http;

/// <summary>
/// Lê o autor informado em <c>x-sim-autor</c> (RN05, RN71). O back-office manda o nome em percent-encoding UTF-8,
/// porque o cabeçalho HTTP só aceita ASCII e o Kestrel recusa "João" com 400. Nome em ASCII puro, sem
/// <c>%</c>, chega igual ao que foi enviado, como antes.
/// </summary>
public static class AutorDoPedido
{
    public const string Cabecalho = "x-sim-autor";

    /// <summary>Tamanho máximo gravado no histórico; o resto é cortado.</summary>
    public const int TamanhoMaximo = 100;

    public static string? Ler(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var bruto = contexto.Request.Headers[Cabecalho].FirstOrDefault();
        return string.IsNullOrWhiteSpace(bruto) ? null : Limpar(Uri.UnescapeDataString(bruto));
    }

    /// <summary>
    /// Troca caracteres de controle (quebra de linha, nulo, tabulação) por espaço, junta espaços e limita o tamanho.
    /// Assim um "%0D%0A" decodificado não forja linha no histórico nem nos logs. Sequência inválida (como "%E9")
    /// fica como veio. Vazio depois da limpeza vale como sem autor.
    /// </summary>
    public static string? Limpar(string autor)
    {
        ArgumentNullException.ThrowIfNull(autor);
        var limpo = new StringBuilder(Math.Min(autor.Length, TamanhoMaximo));
        var espaco = false;
        foreach (var caractere in autor)
        {
            if (char.IsControl(caractere) || char.IsWhiteSpace(caractere))
            {
                espaco = limpo.Length > 0;
                continue;
            }

            if (espaco)
            {
                limpo.Append(' ');
                espaco = false;
            }

            limpo.Append(caractere);
            if (limpo.Length >= TamanhoMaximo)
            {
                break;
            }
        }

        return limpo.Length == 0 ? null : limpo.ToString();
    }
}
