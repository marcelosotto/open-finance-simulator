using System.Text.RegularExpressions;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Arquitetura;

/// <summary>
/// ADR-009: <c>@spec-NNN</c> marca cenário ainda não implementado. Spec entregue (todas as tarefas
/// marcadas) não pode deixar etiqueta esquecida, senão o cenário fica ignorado em silêncio.
/// </summary>
public partial class EtiquetasDeSpecTests
{
    private static readonly string PastaGherkin = Repositorio.Caminho("docs", "01-requisitos", "gherkin");

    [Fact]
    public void Spec_entregue_nao_tem_cenario_etiquetado()
    {
        var entregues = Directory.EnumerateDirectories(Repositorio.Caminho("specs"))
            .Select(pasta => (Numero: NumeroDaSpec().Match(Path.GetFileName(pasta)), Tarefas: Path.Combine(pasta, "tasks.md")))
            .Where(s => s.Numero.Success && File.Exists(s.Tarefas))
            .Where(s => !File.ReadLines(s.Tarefas).Any(l => l.TrimStart().StartsWith("- [ ]", StringComparison.Ordinal)))
            .Select(s => s.Numero.Groups[1].Value)
            .ToHashSet();

        var esquecidas = Directory.EnumerateFiles(PastaGherkin, "*.feature")
            .SelectMany(arquivo => EtiquetaDeSpec().Matches(File.ReadAllText(arquivo))
                .Where(m => entregues.Contains(m.Groups[1].Value))
                .Select(m => $"{Path.GetFileName(arquivo)}: @spec-{m.Groups[1].Value}"))
            .ToList();

        Assert.True(esquecidas.Count == 0, "Tire a etiqueta das specs entregues: " + string.Join(", ", esquecidas));
    }

    [Fact]
    public void Todo_feature_ou_e_da_spec_atual_ou_tem_etiqueta()
    {
        var semEtiqueta = Directory.EnumerateFiles(PastaGherkin, "*.feature")
            .Where(arquivo => !EtiquetaDeSpec().IsMatch(File.ReadAllText(arquivo)))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Equal(["comportamento.feature", "dados-e-transacoes.feature", "defeitos-e-anomalias.feature", "instituicoes.feature", "limites.feature", "marcacao.feature", "massa-e-semente.feature", "paginacao.feature", "recursos.feature", "seguranca.feature"], semEtiqueta.Order(StringComparer.Ordinal));
    }

    [GeneratedRegex(@"^(\d{3})-")]
    private static partial Regex NumeroDaSpec();

    [GeneratedRegex(@"@spec-(\d{3})\b")]
    private static partial Regex EtiquetaDeSpec();
}
