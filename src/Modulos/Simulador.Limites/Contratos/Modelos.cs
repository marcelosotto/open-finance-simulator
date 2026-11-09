using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulador.Limites.Aplicacao;
using Simulador.Limites.Persistencia;

namespace Simulador.Limites.Contratos;

/// <summary>Contadores compartilhados entre as instâncias (RN30, ADR-003).</summary>
public interface IContadores
{
    /// <summary>Soma 1 de forma atômica e devolve o total; indisponível, devolve 0 (RN30).</summary>
    Task<long> Somar(string chave, TimeSpan validade, CancellationToken cancelamento);

    /// <summary>Total atual; indisponível, devolve 0 (RN30).</summary>
    Task<long> Ler(string chave, CancellationToken cancelamento);
}

/// <summary>O que a etapa de capacidade e tráfego precisa saber da chamada.</summary>
/// <param name="Tpm">Nulo quando o endpoint não tem limite de tráfego (RN24).</param>
public sealed record EntradaDaChamada(string Banco, string OperationId, string? Receptora, int Capacidade, int? Tpm, bool RetryAfterLigado, int RetryAfterSeg);

/// <summary>Identifica a cota mensal da chamada (RN25) e o limite dela.</summary>
public sealed record CotaDaChamada(string Banco, string OperationId, string Objeto, string Cliente, string Receptora, int Limite);

/// <summary>Aplica capacidade, tráfego e limite operacional (UC18 a UC20).</summary>
public interface ILimites
{
    /// <summary>Capacidade e, depois, tráfego (RN39); nulo quando a chamada pode seguir.</summary>
    Task<IResult?> Avaliar(HttpContext contexto, EntradaDaChamada entrada);

    Task<bool> CotaEsgotada(CotaDaChamada cota, CancellationToken cancelamento);

    Task Contar(CotaDaChamada cota, CancellationToken cancelamento);
}

/// <summary>Registro do módulo.</summary>
public static class ExtensoesDosLimites
{
    public static IServiceCollection AddLimites(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IContadores>(p => new ContadoresRedis(
            configuracao["Simulador:Redis:Conexao"] ?? "localhost:6379",
            p.GetRequiredService<ILogger<ContadoresRedis>>(),
            p.GetRequiredService<TimeProvider>()));
        servicos.AddSingleton<ILimites, ServicoDeLimites>();
        return servicos;
    }
}
