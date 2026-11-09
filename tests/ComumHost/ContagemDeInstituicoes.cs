using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Instituicoes.Contratos;

namespace Simulador.Testes.Host;

/// <summary>Quantas vezes a instituição foi lida no cadastro.</summary>
public sealed class ContagemDeLeituras
{
    private int _obter;

    public int Obter
    {
        get => _obter;
        set => _obter = value;
    }

    public void Somar() => Interlocked.Increment(ref _obter);
}

/// <summary>Conta as leituras e repassa ao cadastro real.</summary>
public class ConsultaContada : DispatchProxy
{
    private IConsultaDeInstituicoes _interna = null!;
    private ContagemDeLeituras _contagem = null!;

    public static IConsultaDeInstituicoes Criar(IConsultaDeInstituicoes interna, ContagemDeLeituras contagem)
    {
        var proxy = Create<IConsultaDeInstituicoes, ConsultaContada>();
        var contada = (ConsultaContada)(object)proxy;
        contada._interna = interna;
        contada._contagem = contagem;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == nameof(IConsultaDeInstituicoes.Obter))
        {
            _contagem.Somar();
        }

        return targetMethod.Invoke(_interna, args);
    }
}

/// <summary>Registro do contador de leituras.</summary>
public static class ExtensoesDaContagem
{
    public static FabricaDoSimulador ComContagemDeInstituicoes(this FabricaDoSimulador fabrica, ContagemDeLeituras contagem)
    {
        ArgumentNullException.ThrowIfNull(fabrica);
        return fabrica.ComServicos(s =>
        {
            var original = s.Last(d => d.ServiceType == typeof(IConsultaDeInstituicoes));
            s.Remove(original);
            s.AddSingleton(p => ConsultaContada.Criar((IConsultaDeInstituicoes)original.ImplementationFactory!(p), contagem));
        });
    }
}
