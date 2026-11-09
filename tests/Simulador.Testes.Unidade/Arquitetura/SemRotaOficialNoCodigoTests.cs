using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Arquitetura;

/// <summary>PR06: nenhuma rota oficial escrita à mão. Os caminhos vêm só das specs travadas.</summary>
public class SemRotaOficialNoCodigoTests
{
    [Fact]
    public void Nenhum_arquivo_de_src_contem_caminho_open_banking()
    {
        var violacoes = Directory.EnumerateFiles(Repositorio.Caminho("src"), "*.cs", SearchOption.AllDirectories)
            .Where(arquivo => !arquivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(arquivo => File.ReadAllText(arquivo).Contains("/open-banking", StringComparison.Ordinal))
            .Select(arquivo => Path.GetRelativePath(Repositorio.Raiz, arquivo))
            .ToList();

        Assert.Empty(violacoes);
    }
}
