using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Cenarios.Aplicacao;
using Simulador.Cenarios.Http;
using Simulador.Cenarios.Persistencia;
using Simulador.Comum.Aleatorio;

namespace Simulador.Cenarios.Contratos;

/// <summary>Registro do módulo.</summary>
public static class ExtensoesDosCenarios
{
    /// <summary>Desloca a semente do ambiente, para o gerador dos defeitos não repetir a sequência comum.</summary>
    private const int DeslocamentoDosDefeitos = 0x0009_D3F3;

    public static IServiceCollection AddCenarios(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IRepositorioDeCenarios>(_ => new RepositorioMongo(
            configuracao["Simulador:Mongo:Conexao"] ?? "mongodb://localhost:27017",
            configuracao["Simulador:Mongo:Base"] ?? "simulador"));
        servicos.AddSingleton(_ => new GeradorDosDefeitos(new GeradorAleatorioComSemente(
            SementeDoAmbiente.Derivada(configuracao[SementeDoAmbiente.Chave], DeslocamentoDosDefeitos))));
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
