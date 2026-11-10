using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Simulador.Comum.Comportamento;

/// <summary>Diz se uma anomalia está ligada no banco (RN113); o host implementa.</summary>
public interface IAnomaliasDaChamada
{
    Task<bool> Ligada(string instituicao, string nome, CancellationToken cancelamento);
}

/// <summary>Nomes das anomalias de protocolo (RN113) e acesso pelos módulos que respondem às operações.</summary>
public static class Anomalias
{
    public const string RevogadoCom403 = "revogado-com-403";
    public const string ChaveRecusadaCom422 = "chave-recusada-com-422";
    public const string InteracaoDiferente = "interacao-diferente";

    /// <summary>Se a anomalia está ligada no banco da rota; falso sem implementação.</summary>
    public static Task<bool> Ligada(HttpContext contexto, string nome)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return contexto.RequestServices?.GetService<IAnomaliasDaChamada>() is { } anomalias && contexto.Request.RouteValues["banco"] is string banco
            ? anomalias.Ligada(banco, nome, contexto.RequestAborted)
            : Task.FromResult(false);
    }
}
