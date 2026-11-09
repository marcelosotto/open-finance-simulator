using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using Simulador.Catalogo.Contratos;

namespace Simulador.Ferramentas.Comandos;

/// <summary><c>specs baixar</c>: baixa o commit travado e grava os contratos em <c>.specs/{commit}</c>.</summary>
internal static class BaixarSpecs
{
    public static async Task<int> Executar(Opcoes opcoes, TextWriter saida, HttpMessageHandler? handler = null)
    {
        var caminhoDaTrava = opcoes.Valor("lock", "specs.lock.json");
        var trava = TravaDasSpecs.Ler(caminhoDaTrava);
        var token = opcoes.Valor("token") ?? Environment.GetEnvironmentVariable("SPECS_TOKEN");
        var pasta = opcoes.Valor("pasta") ?? PastaPadrao(caminhoDaTrava, trava.Commit);

        var erro = await Baixar(trava, pasta, token, handler).ConfigureAwait(false);
        if (erro is not null)
        {
            await saida.WriteLineAsync(erro).ConfigureAwait(false);
            return 1;
        }

        await saida.WriteLineAsync($"{trava.Contratos.Count} contratos de {trava.Commit} gravados em {pasta}").ConfigureAwait(false);
        return 0;
    }

    public static string PastaPadrao(string caminhoDaTrava, string commit) =>
        Path.Combine(Path.GetDirectoryName(Path.GetFullPath(caminhoDaTrava))!, ".specs", commit);

    /// <summary>Baixa e extrai só o catálogo e os contratos travados. Devolve a mensagem de erro, ou nulo.</summary>
    public static async Task<string?> Baixar(TravaDasSpecs trava, string pasta, string? token, HttpMessageHandler? handler = null)
    {
        using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("simulador-open-finance", "1.0"));
        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var url = new Uri($"https://codeload.github.com/{trava.Repositorio}/tar.gz/{trava.Commit}");
        using var resposta = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return $"commit {trava.Commit} não encontrado em {trava.Repositorio}.";
        }

        if (!resposta.IsSuccessStatusCode)
        {
            return $"falha ao baixar {url}: HTTP {(int)resposta.StatusCode}.";
        }

        var desejados = trava.Contratos.Select(c => c.CaminhoLocal).Append(trava.Catalogo).ToHashSet(StringComparer.Ordinal);
        if (Directory.Exists(pasta))
        {
            Directory.Delete(pasta, recursive: true);
        }

        var stream = await resposta.Content.ReadAsStreamAsync().ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var gzip = new GZipStream(stream, CompressionMode.Decompress);
            await using (gzip.ConfigureAwait(false))
            {
                var leitor = new TarReader(gzip);
                await using (leitor.ConfigureAwait(false))
                {
                    while (await leitor.GetNextEntryAsync().ConfigureAwait(false) is { } entrada)
                    {
                        var barra = entrada.Name.IndexOf('/', StringComparison.Ordinal);
                        var relativo = barra < 0 ? entrada.Name : entrada.Name[(barra + 1)..];
                        if (entrada.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                            || !desejados.Contains(relativo))
                        {
                            continue;
                        }

                        var destino = Path.Combine(pasta, relativo);
                        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
                        await entrada.ExtractToFileAsync(destino, overwrite: true).ConfigureAwait(false);
                    }
                }
            }
        }

        return null;
    }
}
