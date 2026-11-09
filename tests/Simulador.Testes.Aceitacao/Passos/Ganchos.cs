using System.Text.RegularExpressions;
using Reqnroll;
using Reqnroll.UnitTestProvider;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>
/// ADR-009: todos os <c>.feature</c> de <c>docs/</c> rodam. Cenário de spec ainda não entregue
/// (<c>@spec-NNN</c>) e cenário de tela (<c>@tela</c>) aparecem como ignorados, com o motivo.
/// </summary>
[Binding]
public sealed partial class Ganchos
{
    [BeforeScenario(Order = 0)]
    public static void PularOQueAindaNaoFoiEntregue(ScenarioContext cenario, IUnitTestRuntimeProvider execucao)
    {
        ArgumentNullException.ThrowIfNull(cenario);
        ArgumentNullException.ThrowIfNull(execucao);
        var etiquetas = cenario.ScenarioInfo.CombinedTags;

        if (etiquetas.Select(e => EtiquetaDeSpec().Match(e)).FirstOrDefault(m => m.Success) is { } spec)
        {
            execucao.TestIgnore($"Fica para a spec {spec.Groups[1].Value} (ADR-009).");
        }
        else if (etiquetas.Contains("tela"))
        {
            execucao.TestIgnore("Cenário de tela: fica para a spec 010 (back-office).");
        }
    }

    [GeneratedRegex("^spec-(\\d{3})$")]
    private static partial Regex EtiquetaDeSpec();
}
