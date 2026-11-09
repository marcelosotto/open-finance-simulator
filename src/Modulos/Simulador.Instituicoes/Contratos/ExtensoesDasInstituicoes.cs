using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulador.Instituicoes.Aplicacao;
using Simulador.Instituicoes.Http;
using Simulador.Instituicoes.Persistencia;

namespace Simulador.Instituicoes.Contratos;

/// <summary>Registro do módulo Instituições.</summary>
public static class ExtensoesDasInstituicoes
{
    public static IServiceCollection AddInstituicoes(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton<IRepositorioDeInstituicoes>(_ => new RepositorioMongo(
            configuracao["Simulador:Mongo:Conexao"] ?? "mongodb://localhost:27017",
            configuracao["Simulador:Mongo:Base"] ?? "simulador"));
        servicos.TryAddSingleton<IConsultaDeClientes, SemClientes>();
        servicos.AddSingleton<CadastroDeInstituicoes>();
        servicos.AddSingleton<IConsultaDeInstituicoes>(provedor => provedor.GetRequiredService<CadastroDeInstituicoes>());
        servicos.AddHostedService<Semeadura>();
        return servicos;
    }

    /// <summary>Publica a API de administração das instituições.</summary>
    public static void MapearInstituicoes(this IEndpointRouteBuilder administracao) => RotasDeInstituicoes.Mapear(administracao);
}
