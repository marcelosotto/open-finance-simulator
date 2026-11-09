using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Simulador.Comum.Comportamento;

/// <summary>A hora real, para o que mede tráfego ou tempo de verdade (RN73).</summary>
public sealed record HoraReal(TimeProvider Relogio);

/// <summary>Etapa de regra de cenário e perfil (RN39); o host implementa.</summary>
public interface IComportamentoDaChamada
{
    /// <summary>Espera o tempo sorteado e devolve a resposta forçada; nulo quando a chamada segue.</summary>
    Task<IResult?> Aplicar(HttpContext contexto);

    /// <summary>Sorteio de recurso indisponível no acesso direto (RN109).</summary>
    Task<bool> RecursoIndisponivel(HttpContext contexto);

    /// <summary>O estado do recurso nesta chamada, com a regra de estado que valer (RN108).</summary>
    Task<string> EstadoDoRecurso(string resourceId, string estado, CancellationToken cancelamento);
}

/// <summary>Acesso à etapa pelos módulos que respondem às operações.</summary>
public static class EtapaDeComportamento
{
    public static Task<IResult?> Aplicar(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return contexto.RequestServices.GetService<IComportamentoDaChamada>()?.Aplicar(contexto) ?? Task.FromResult<IResult?>(null);
    }

    public static Task<bool> RecursoIndisponivel(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return contexto.RequestServices.GetService<IComportamentoDaChamada>()?.RecursoIndisponivel(contexto) ?? Task.FromResult(false);
    }
}
