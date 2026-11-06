using System.Reflection;
using Simulador.Catalogo.Contratos;
using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Contratos;

namespace Simulador.Testes.Unidade.Marcacao;

/// <summary>RN66: nenhum perfil, instituição ou cenário desliga ou muda a marca.</summary>
public class MarcaSemDesligamentoTests
{
    private static readonly string[] PalavrasDeDesligamento = ["Desligar", "Ligar", "Ativa", "Habilit", "Desativ", "Remover", "Sem"];

    [Fact]
    public void Api_publica_da_marcacao_nao_tem_opcao_de_desligar()
    {
        var membros = typeof(IMarcador).Assembly.GetExportedTypes()
            .SelectMany(t => t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Select(m => m.Name)
            .Where(nome => PalavrasDeDesligamento.Any(p => nome.StartsWith(p, StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(membros);
    }

    [Fact]
    public void Marca_do_ambiente_nao_muda_depois_de_criada()
    {
        var setters = typeof(MarcaDoAmbiente).GetProperties()
            .Where(p => p.SetMethod is { IsPublic: true } setter
                && !setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit"))
            .ToList();

        Assert.Empty(setters);
    }

    [Fact]
    public void Outros_modulos_nao_tem_configuracao_de_marca()
    {
        var membros = new[] { typeof(ICatalogo).Assembly, typeof(IGeradorAleatorio).Assembly }
            .SelectMany(a => a.GetExportedTypes())
            .SelectMany(t => t.GetMembers().Select(m => $"{t.Name}.{m.Name}"))
            .Where(nome => nome.Contains("Marca", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(membros);
    }
}
