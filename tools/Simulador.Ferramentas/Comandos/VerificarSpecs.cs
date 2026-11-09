using Simulador.Catalogo.Contratos;

namespace Simulador.Ferramentas.Comandos;

/// <summary><c>specs verificar</c>: confere os SHA-256 contra a trava e o catálogo (FR-002).</summary>
internal static class VerificarSpecs
{
    public static async Task<int> Executar(Opcoes opcoes, TextWriter saida)
    {
        var caminhoDaTrava = opcoes.Valor("lock", "specs.lock.json");
        var trava = TravaDasSpecs.Ler(caminhoDaTrava);
        var pasta = opcoes.Valor("pasta") ?? BaixarSpecs.PastaPadrao(caminhoDaTrava, trava.Commit);

        var falhas = ServicosDeSpecs.Conferir(trava, pasta);
        foreach (var falha in falhas)
        {
            await saida.WriteLineAsync($"FALHA {falha}").ConfigureAwait(false);
        }

        if (falhas.Count > 0)
        {
            return 1;
        }

        await saida.WriteLineAsync($"OK: {trava.Contratos.Count} contratos conferidos no commit {trava.Commit}.").ConfigureAwait(false);
        return 0;
    }
}
