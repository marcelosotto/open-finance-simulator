using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Cenarios.Aplicacao;
using Simulador.Cenarios.Http;
using Simulador.Cenarios.Persistencia;

namespace Simulador.Cenarios.Contratos;

/// <summary>Registro do módulo.</summary>
public static class ExtensoesDosCenarios
{
    public static IServiceCollection AddCenarios(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IRepositorioDeCenarios>(_ => new RepositorioMongo(
            configuracao["Simulador:Mongo:Conexao"] ?? "mongodb://localhost:27017",
            configuracao["Simulador:Mongo:Base"] ?? "simulador"));
        servicos.AddSingleton<ServicoDeCenarios>();
        servicos.AddSingleton<ICenarios>(p => p.GetRequiredService<ServicoDeCenarios>());
        return servicos;
    }

    public static IEndpointRouteBuilder MapearCenarios(this IEndpointRouteBuilder administracao)
    {
        RotasDeCenarios.Mapear(administracao);
        return administracao;
    }
}
