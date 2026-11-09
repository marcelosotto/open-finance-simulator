using Simulador.Catalogo.Contratos;

namespace Simulador.Ferramentas.Comandos;

/// <summary><c>specs diferencas</c>: lista operações novas, alteradas e removidas entre dois locks (UC24).</summary>
internal static class DiferencasSpecs
{
    public static async Task<int> Executar(Opcoes opcoes, TextWriter saida)
    {
        var caminhoBase = opcoes.Valor("base");
        var caminhoNovo = opcoes.Valor("novo");
        if (caminhoBase is null || caminhoNovo is null)
        {
            await saida.WriteLineAsync("FALHA informe --base <lock> e --novo <lock>.").ConfigureAwait(false);
            return 1;
        }

        var token = opcoes.Valor("token") ?? Environment.GetEnvironmentVariable("SPECS_TOKEN");
        var anteriores = await Preparar(caminhoBase, token, saida).ConfigureAwait(false);
        var novas = await Preparar(caminhoNovo, token, saida).ConfigureAwait(false);
        if (anteriores is null || novas is null)
        {
            return 1;
        }

        var markdown = ServicosDeSpecs.Comparar(anteriores.Value.Commit, anteriores.Value.Operacoes, novas.Value.Commit, novas.Value.Operacoes)
            .ParaMarkdown();
        await saida.WriteLineAsync(markdown).ConfigureAwait(false);
        if (opcoes.Valor("resumo") is { } resumo)
        {
            await File.AppendAllTextAsync(resumo, markdown).ConfigureAwait(false);
        }

        return 0;
    }

    /// <summary>Garante as specs do lock baixadas e conferidas e lê as operações.</summary>
    private static async Task<(string Commit, IReadOnlyList<OperacaoDoCatalogo> Operacoes)?> Preparar(string caminhoDaTrava, string? token, TextWriter saida)
    {
        var trava = TravaDasSpecs.Ler(caminhoDaTrava);
        var pasta = BaixarSpecs.PastaPadrao(caminhoDaTrava, trava.Commit);
        if (ServicosDeSpecs.Conferir(trava, pasta).Count > 0)
        {
            var erro = await BaixarSpecs.Baixar(trava, pasta, token).ConfigureAwait(false);
            var falhas = erro is null ? ServicosDeSpecs.Conferir(trava, pasta).Select(f => f.ToString()).ToList() : [erro];
            if (falhas.Count > 0)
            {
                foreach (var falha in falhas)
                {
                    await saida.WriteLineAsync($"FALHA {falha}").ConfigureAwait(false);
                }

                return null;
            }
        }

        return (trava.Commit, ServicosDeSpecs.LerOperacoes(trava, pasta));
    }
}
