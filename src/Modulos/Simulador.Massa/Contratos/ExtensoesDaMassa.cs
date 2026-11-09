using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Massa.Aplicacao;
using Simulador.Massa.Http;
using Simulador.Massa.Persistencia;

namespace Simulador.Massa.Contratos;

/// <summary>Registro do módulo Massa.</summary>
public static class ExtensoesDaMassa
{
    public static IServiceCollection AddMassa(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IRepositorioDeMassa>(_ => new RepositorioMongo(
            configuracao["Simulador:Mongo:Conexao"] ?? "mongodb://localhost:27017",
            configuracao["Simulador:Mongo:Base"] ?? "simulador"));
        servicos.AddSingleton<BaseDaMassa>();
        servicos.AddSingleton<ConsultaDeMassa>();
        servicos.AddSingleton<IConsultaDeMassa>(provedor => provedor.GetRequiredService<ConsultaDeMassa>());
        servicos.AddSingleton<CadastroDeMassa>();
        servicos.AddSingleton<FilaDeLotes>();
        servicos.AddSingleton<ServicoDeLotes>();
        servicos.AddSingleton<ServicoDoMundo>();
        servicos.AddSingleton<ServicoDeAjustes>();
        servicos.AddSingleton<ServicoDeLimpeza>();
        servicos.AddSingleton<IEscritaDeMassa, EscritaDeMassa>();
        servicos.AddHostedService<InicioDaMassa>();
        servicos.AddHostedService<ExecutorDeLotes>();
        return servicos;
    }

    /// <summary>Publica a API de administração da massa.</summary>
    public static void MapearMassa(this IEndpointRouteBuilder administracao) => RotasDaMassa.Mapear(administracao);
}
