using System.Security.Cryptography;
using System.Text.Json;
using Simulador.Catalogo.Contratos;

namespace Simulador.Catalogo.Dominio;

/// <summary>
/// Confere os bytes das specs contra a trava e contra o <c>catalog.json</c> do commit (FR-002).
/// </summary>
internal static class ConferenciaDeHash
{
    /// <param name="trava">Trava lida do <c>specs.lock.json</c>.</param>
    /// <param name="lerArquivo">Lê um arquivo pelo caminho relativo; nulo se não existir.</param>
    /// <param name="listarArquivos">Caminhos relativos presentes na pasta das specs.</param>
    public static IReadOnlyList<FalhaDeConferencia> Conferir(
        TravaDasSpecs trava,
        Func<string, byte[]?> lerArquivo,
        Func<IEnumerable<string>> listarArquivos)
    {
        var bytesDoCatalogo = lerArquivo(trava.Catalogo);
        if (bytesDoCatalogo is null)
        {
            return [new FalhaDeConferencia(trava.Catalogo, "catálogo não encontrado.")];
        }

        var catalogo = LerCatalogo(bytesDoCatalogo);
        if (catalogo is null)
        {
            return [new FalhaDeConferencia(trava.Catalogo, "catálogo inválido.")];
        }

        var falhas = new List<FalhaDeConferencia>();
        foreach (var contrato in trava.Contratos)
        {
            if (!catalogo.TryGetValue(contrato.Id, out var hashDoCatalogo))
            {
                falhas.Add(new(contrato.CaminhoLocal, $"contrato {contrato.Id} não existe no catálogo do commit."));
                continue;
            }

            if (!string.Equals(hashDoCatalogo, contrato.Sha256, StringComparison.Ordinal))
            {
                falhas.Add(new(contrato.CaminhoLocal, $"o sha256 da trava difere do catálogo ({hashDoCatalogo})."));
                continue;
            }

            var bytes = lerArquivo(contrato.CaminhoLocal);
            if (bytes is null)
            {
                falhas.Add(new(contrato.CaminhoLocal, "arquivo não encontrado."));
                continue;
            }

            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            if (!string.Equals(hash, contrato.Sha256, StringComparison.Ordinal))
            {
                falhas.Add(new(contrato.CaminhoLocal, $"SHA-256 divergente: esperado {contrato.Sha256}, encontrado {hash}."));
            }
        }

        var travados = trava.Contratos.Select(c => c.CaminhoLocal).Append(trava.Catalogo).ToHashSet(StringComparer.Ordinal);
        falhas.AddRange(listarArquivos()
            .Where(arquivo => !travados.Contains(arquivo))
            .Order(StringComparer.Ordinal)
            .Select(arquivo => new FalhaDeConferencia(arquivo, "arquivo sem hash registrado na trava.")));

        return falhas;
    }

    private static Dictionary<string, string>? LerCatalogo(byte[] bytes)
    {
        try
        {
            using var documento = JsonDocument.Parse(bytes);
            if (!documento.RootElement.TryGetProperty("contracts", out var contratos))
            {
                return null;
            }

            return contratos.EnumerateArray().ToDictionary(
                c => c.GetProperty("id").GetString()!,
                c => c.GetProperty("sha256").GetString()!,
                StringComparer.Ordinal);
        }
        catch (Exception erro) when (erro is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }
}
