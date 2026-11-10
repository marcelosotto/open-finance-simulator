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

    public static string? Ler(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var bruto = contexto.Request.Headers[Cabecalho].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(bruto))
        {
            return null;
        }

        // Sequência inválida (como "100%") fica como veio.
        var autor = Uri.UnescapeDataString(bruto).Trim();
        return autor.Length == 0 ? null : autor;
    }
}
