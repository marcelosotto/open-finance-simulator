using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Catalogo.Leitura;
using Simulador.Catalogo.Rotas;

namespace Simulador.Catalogo.Contratos;

/// <summary>Registro e publicação do módulo Catálogo.</summary>
public static class ExtensoesDoCatalogo
{
    public static IServiceCollection AddCatalogo(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(_ => OpcoesDoCatalogo.Ler(configuracao));
        servicos.AddSingleton<ICatalogo>(provedor => CarregadorDoCatalogo.Carregar(provedor.GetRequiredService<OpcoesDoCatalogo>()));
        servicos.AddSingleton<Despachante>();
        return servicos;
    }

    /// <summary>Publica as operações e o 404 em <paramref name="openFinance"/>, e o catálogo e a prontidão em <paramref name="administracao"/>.</summary>
    public static void MapearCatalogo(this IEndpointRouteBuilder openFinance, IEndpointRouteBuilder administracao)
    {
        ArgumentNullException.ThrowIfNull(openFinance);
        ArgumentNullException.ThrowIfNull(administracao);
        var catalogo = openFinance.ServiceProvider.GetRequiredService<ICatalogo>();

        foreach (var operacao in catalogo.Operacoes)
        {
            openFinance.MapMethods(
                operacao.CaminhoPublicado,
                [operacao.Metodo],
                (HttpContext contexto, Despachante despachante) => despachante.Despachar(contexto, operacao));
        }

        openFinance.MapFallback("{**caminho}", (TimeProvider relogio) => Respostas.NaoEncontrado(relogio));

        administracao.MapGet("/admin/catalogo", (ICatalogo c, HttpContext contexto) =>
        {
            return Results.Ok(new
            {
                commit = c.Commit,
                operacoes = c.Operacoes.Select(o => new
                {
                    contrato = o.Contrato,
                    api = o.Api,
                    versao = o.Versao,
                    metodo = o.Metodo,
                    caminho = o.Caminho,
                    caminhoPublicado = o.CaminhoPublicado,
                    operationId = o.OperationId,
                    classe = c.ClasseDe(o) is { } classe ? new
                    {
                        frequencia = NomeDaFrequencia(classe.Frequencia),
                        slaMs = classe.SlaMs,
                        tpm = classe.TpmPorFaixaQca ? (object?)"faixa-qca" : classe.Tpm,
                        limiteMensal = classe.LimiteMensal,
                    } : null,
                    implementada = Despachante.Implementada(contexto.RequestServices, o),
                }),
            });
        });

        administracao.MapGet("/saude/pronto", (ICatalogo c) => c.Pronto
            ? Results.Ok(new { pronto = true })
            : Results.Json(new { pronto = false, problemas = c.Problemas }, statusCode: StatusCodes.Status503ServiceUnavailable));
    }

    internal static string NomeDaFrequencia(Frequencia frequencia) => frequencia switch
    {
        Frequencia.Alta => "alta",
        Frequencia.MediaAlta => "media-alta",
        Frequencia.Media => "media",
        _ => "baixa",
    };
}
