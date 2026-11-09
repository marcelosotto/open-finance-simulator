using Simulador.Ferramentas;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Ferramentas;

public class ComandosTests
{
    private static readonly string Trava = Repositorio.Caminho("specs.lock.json");

    [Fact]
    public async Task Diferencas_entre_o_mesmo_commit_escreve_resumo_vazio()
    {
        var resumo = Path.Combine(Directory.CreateTempSubdirectory().FullName, "resumo.md");
        using var saida = new StringWriter();

        var codigo = await Roteador.Executar(["specs", "diferencas", "--base", Trava, "--novo", Trava, "--resumo", resumo], saida);

        Assert.Equal(0, codigo);
        var markdown = await File.ReadAllTextAsync(resumo, TestContext.Current.CancellationToken);
        Assert.Contains("## Specs: 7bdb9c1 → 7bdb9c1", markdown, StringComparison.Ordinal);
        Assert.Contains("### Novas (0)", markdown, StringComparison.Ordinal);
        Assert.Contains("### Alteradas (0)", markdown, StringComparison.Ordinal);
        Assert.Contains("### Removidas (0)", markdown, StringComparison.Ordinal);
        Assert.Contains("## Specs:", saida.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Diferencas_sem_base_mostra_erro()
    {
        using var saida = new StringWriter();

        var codigo = await Roteador.Executar(["specs", "diferencas", "--novo", Trava], saida);

        Assert.Equal(1, codigo);
        Assert.Contains("--base", saida.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verificar_falha_com_byte_alterado_e_cita_o_arquivo()
    {
        var copia = CopiarSpecs();
        await File.AppendAllTextAsync(Path.Combine(copia, "contracts/expansion/snapshots/accounts-2.5.1.yaml"), " ", TestContext.Current.CancellationToken);
        using var saida = new StringWriter();

        var codigo = await Roteador.Executar(["specs", "verificar", "--lock", Trava, "--pasta", copia], saida);

        Assert.Equal(1, codigo);
        Assert.Contains("contracts/expansion/snapshots/accounts-2.5.1.yaml", saida.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Verificar_passa_com_as_specs_baixadas()
    {
        using var saida = new StringWriter();

        var codigo = await Roteador.Executar(["specs", "verificar", "--lock", Trava], saida);

        Assert.Equal(0, codigo);
        Assert.Contains("OK: 15 contratos", saida.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Trava_ausente_falha_com_mensagem()
    {
        using var saida = new StringWriter();

        var codigo = await Roteador.Executar(["specs", "verificar", "--lock", "nao-existe.json"], saida);

        Assert.Equal(1, codigo);
        Assert.Contains("não encontrado", saida.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Comando_desconhecido_mostra_ajuda()
    {
        using var saida = new StringWriter();

        var codigo = await Roteador.Executar(["xpto"], saida);

        Assert.Equal(1, codigo);
        Assert.Contains("Uso:", saida.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Opcoes_leem_pares_e_padroes()
    {
        var opcoes = new Opcoes(["--a", "1", "--vazio", "--b", "2", "solto"]);

        Assert.Equal("1", opcoes.Valor("a"));
        Assert.Equal("2", opcoes.Valor("b"));
        Assert.Null(opcoes.Valor("vazio"));
        Assert.Equal("padrao", opcoes.Valor("c", "padrao"));
    }

    internal static string CopiarSpecs()
    {
        var origem = Repositorio.PastaDasSpecs();
        var copia = Directory.CreateTempSubdirectory().FullName;
        foreach (var arquivo in Directory.EnumerateFiles(origem, "*", SearchOption.AllDirectories))
        {
            var destino = Path.Combine(copia, Path.GetRelativePath(origem, arquivo));
            Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
            File.Copy(arquivo, destino);
        }

        return copia;
    }
}
