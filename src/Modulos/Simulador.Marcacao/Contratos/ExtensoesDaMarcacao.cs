using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Comum.Aleatorio;
using Simulador.Marcacao.Dominio;
using Simulador.Marcacao.Http;

namespace Simulador.Marcacao.Contratos;

/// <summary>Registro do módulo Marcação.</summary>
public static class ExtensoesDaMarcacao
{
    /// <summary>Lê <c>Simulador:MarcaDoAmbiente</c> uma vez, na subida. Não existe outra fonte da marca (RN66).</summary>
    public static IServiceCollection AddMarcacao(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(_ => new MarcaDoAmbiente(configuracao["Simulador:MarcaDoAmbiente"]));
        servicos.AddSingleton<IMarcador>(provedor =>
            new Marcador(provedor.GetRequiredService<MarcaDoAmbiente>(), provedor.GetRequiredService<IGeradorAleatorio>()));
        servicos.AddScoped<RegistroDeNatureza>();
        servicos.AddScoped<IRegistroDeNatureza>(provedor => provedor.GetRequiredService<RegistroDeNatureza>());
        servicos.AddSingleton<FiltroDeMarcacao>();
        return servicos;
    }

    /// <summary><c>GET /admin/ambiente</c>: a marca da configuração, para a tela mostrar (UI01, RN72). Vazia em produção.</summary>
    public static IEndpointRouteBuilder MapearAmbiente(this IEndpointRouteBuilder administracao)
    {
        ArgumentNullException.ThrowIfNull(administracao);
        administracao.MapGet("/admin/ambiente", (MarcaDoAmbiente marca) => Results.Json(new { marca = marca.Valor }));
        return administracao;
    }

    /// <summary>Aplica os cabeçalhos de marcação a todas as rotas do grupo, inclusive 404 e 501.</summary>
    public static RouteGroupBuilder ComMarcacao(this RouteGroupBuilder grupo)
    {
        ArgumentNullException.ThrowIfNull(grupo);
        grupo.AddEndpointFilter(async (contexto, proximo) =>
            await contexto.HttpContext.RequestServices.GetRequiredService<FiltroDeMarcacao>()
                .InvokeAsync(contexto, proximo).ConfigureAwait(false));
        return grupo;
    }
}
