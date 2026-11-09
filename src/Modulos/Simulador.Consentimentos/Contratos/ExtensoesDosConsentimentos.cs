using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulador.Consentimentos.Dominio;
using Simulador.Consentimentos.Http;
using Simulador.Consentimentos.Persistencia;

namespace Simulador.Consentimentos.Contratos;

/// <summary>Registro do módulo Consentimentos.</summary>
public static class ExtensoesDosConsentimentos
{
    /// <summary>Operações oficiais entregues pelo módulo, por <c>operationId</c>.</summary>
    public static IReadOnlyDictionary<string, Func<HttpContext, Task<IResult>>> Operacoes { get; } =
        new Dictionary<string, Func<HttpContext, Task<IResult>>>(StringComparer.Ordinal)
        {
            ["consentsPostConsents"] = c => c.RequestServices.GetRequiredService<ApiOficial>().Criar(c),
            ["consentsGetConsentsConsentId"] = c => c.RequestServices.GetRequiredService<ApiOficial>().Consultar(c),
            ["consentsDeleteConsentsConsentId"] = c => c.RequestServices.GetRequiredService<ApiOficial>().Revogar(c),
            ["consentsPostConsentsConsentIdExtends"] = c => c.RequestServices.GetRequiredService<ApiOficial>().Renovar(c),
            ["consentsGetConsentsConsentIdExtensions"] = c => c.RequestServices.GetRequiredService<ApiOficial>().Extensoes(c),
            ["resourcesGetResources"] = c => c.RequestServices.GetRequiredService<ApiOficial>().Recursos(c),
        };

    /// <summary>Consentimento validado pelo portão nesta chamada; nulo sem portão.</summary>
    public static DadosDoConsentimento? ConsentimentoDaChamada(HttpContext contexto) =>
        contexto.Items[PortaoDeSeguranca.ItemDoConsentimento] as DadosDoConsentimento;

    /// <summary>Receptora do token validado nesta chamada; nula sem portão.</summary>
    public static string? ReceptoraDaChamada(HttpContext contexto) =>
        (contexto.Items[PortaoDeSeguranca.ItemDoToken] as TokenDeAcesso)?.OrganisationId;

    public static IServiceCollection AddConsentimentos(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IRepositorioDeReceptoras>(_ => new RepositorioMongo(
            configuracao["Simulador:Mongo:Conexao"] ?? "mongodb://localhost:27017",
            configuracao["Simulador:Mongo:Base"] ?? "simulador"));
        servicos.AddSingleton(_ => new ChaveDoToken(
            configuracao["Simulador:Token:Chave"] is { Length: > 0 } chave ? Encoding.UTF8.GetBytes(chave) : RandomNumberGenerator.GetBytes(32)));
        servicos.AddSingleton<IPortaoDeSeguranca, PortaoDeSeguranca>();
        servicos.AddSingleton<ApiOficial>();
        servicos.AddHostedService<InicioDosConsentimentos>();
        return servicos;
    }

    /// <summary>Publica <c>POST /{banco}/token</c> e a administração.</summary>
    public static void MapearConsentimentos(this IEndpointRouteBuilder openFinance, IEndpointRouteBuilder administracao)
    {
        Rotas.MapearToken(openFinance);
        Rotas.MapearAdministracao(administracao);
    }
}

/// <summary>Prepara os índices na subida.</summary>
internal sealed class InicioDosConsentimentos(IRepositorioDeReceptoras repositorio) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => repositorio.Preparar(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
