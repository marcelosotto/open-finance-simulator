using System.Reflection;
using Simulador.Catalogo.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Marcacao.Contratos;

namespace Simulador.Testes.Unidade.Arquitetura;

/// <summary>FR-024 e ADR-007: cada módulo só expõe <c>Contratos</c> e não depende de outro módulo.</summary>
public class FronteirasDosModulosTests
{
    public static TheoryData<string> Modulos() => ["Simulador.Catalogo", "Simulador.Instituicoes", "Simulador.Marcacao"];

    private static Assembly Modulo(string nome) => nome switch
    {
        "Simulador.Catalogo" => typeof(ICatalogo).Assembly,
        "Simulador.Instituicoes" => typeof(Instituicao).Assembly,
        _ => typeof(IMarcador).Assembly,
    };

    [Theory]
    [MemberData(nameof(Modulos))]
    public void So_o_namespace_Contratos_e_publico(string modulo)
    {
        var foraDeContratos = Modulo(modulo).GetExportedTypes()
            .Where(t => t.Namespace != $"{modulo}.Contratos")
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(foraDeContratos);
    }

    [Theory]
    [MemberData(nameof(Modulos))]
    public void Modulo_nao_referencia_outro_modulo(string modulo)
    {
        var outros = Modulos().Select(d => d.Data).Where(m => m != modulo).ToHashSet();

        var referencias = Modulo(modulo).GetReferencedAssemblies().Select(a => a.Name!).Where(outros.Contains).ToList();

        Assert.Empty(referencias);
    }
}
