using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Simulador.Comum.Comportamento;

/// <summary>A hora real, para o que mede tráfego ou tempo de verdade (RN73).</summary>
public sealed record HoraReal(TimeProvider Relogio);

/// <summary>Segura a resposta em hora real (RN12, RN73).</summary>
public interface IEsperaDaChamada
{
    Task Aguardar(TimeSpan duracao, CancellationToken cancelamento);
}

/// <summary>O que a etapa decidiu: quanto esperar e, se houver, a resposta que troca a normal.</summary>
public sealed record DesfechoDoComportamento(TimeSpan Espera, IResult? Resposta = null, bool Cortar = false)
{
    /// <summary>A chamada segue para a resposta normal (2XX).</summary>
    public bool Segue => Resposta is null && !Cortar;
}

/// <summary>Etapa de regra de cenário e perfil (RN39); o host implementa.</summary>
public interface IComportamentoDaChamada
{
    /// <summary>Decide sem esperar; nulo quando a chamada não passa pela etapa.</summary>
    Task<DesfechoDoComportamento?> Decidir(HttpContext contexto);

    /// <summary>Sorteio de recurso indisponível no acesso direto (RN109).</summary>
    Task<bool> RecursoIndisponivel(HttpContext contexto);

    /// <summary>O estado do recurso no instante da chamada, com a regra de estado que valer (RN108).</summary>
    Task<string> EstadoDoRecurso(string resourceId, string estado, CancellationToken cancelamento);
}

/// <summary>Acesso à etapa pelos módulos que respondem às operações.</summary>
public static class EtapaDeComportamento
{
    public static Task<DesfechoDoComportamento?> Decidir(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return contexto.RequestServices?.GetService<IComportamentoDaChamada>()?.Decidir(contexto) ?? Task.FromResult<DesfechoDoComportamento?>(null);
    }

    /// <summary>Segura a resposta pelo tempo decidido.</summary>
    public static async Task Esperar(HttpContext contexto, DesfechoDoComportamento? desfecho)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (desfecho is { Espera: var espera } && espera > TimeSpan.Zero && contexto.RequestServices?.GetService<IEsperaDaChamada>() is { } esperador)
        {
            await esperador.Aguardar(espera, contexto.RequestAborted).ConfigureAwait(false);
        }
    }

    /// <summary>Espera e devolve a resposta trocada (ou corta a conexão); nulo quando a chamada segue.</summary>
    public static async Task<IResult?> Concluir(HttpContext contexto, DesfechoDoComportamento? desfecho)
    {
        await Esperar(contexto, desfecho).ConfigureAwait(false);
        if (desfecho is { Cortar: true })
        {
            contexto.Abort();
            return Results.Empty;
        }

        return desfecho?.Resposta;
    }

    /// <summary>Decide, espera e devolve a resposta trocada; nulo quando a chamada segue.</summary>
    public static async Task<IResult?> Aplicar(HttpContext contexto) =>
        await Concluir(contexto, await Decidir(contexto).ConfigureAwait(false)).ConfigureAwait(false);

    public static Task<bool> RecursoIndisponivel(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return contexto.RequestServices?.GetService<IComportamentoDaChamada>()?.RecursoIndisponivel(contexto) ?? Task.FromResult(false);
    }
}
