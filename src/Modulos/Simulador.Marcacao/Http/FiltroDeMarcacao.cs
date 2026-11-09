using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Marcacao.Contratos;
using Simulador.Marcacao.Dominio;

namespace Simulador.Marcacao.Http;

/// <summary>Guarda a natureza dos itens de uma chamada.</summary>
internal sealed class RegistroDeNatureza : IRegistroDeNatureza
{
    private readonly List<Natureza> _naturezas = [];

    public NaturezaDaResposta? Agregada => AgregadorDeNatureza.Agregar(_naturezas);

    public void Registrar(Natureza natureza) => _naturezas.Add(natureza);
}

/// <summary>
/// Envia <c>x-sim-environment</c> (se a marca não for vazia) e <c>x-sim-data-nature</c>
/// (se a resposta tiver dado) sem mudar o corpo oficial (RN61, RN65).
/// </summary>
internal sealed class FiltroDeMarcacao(MarcaDoAmbiente marca) : IEndpointFilter
{
    public const string CabecalhoDoAmbiente = "x-sim-environment";
    public const string CabecalhoDaNatureza = "x-sim-data-nature";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var resultado = await next(context).ConfigureAwait(false);
        var resposta = context.HttpContext.Response;
        if (!marca.Vazia)
        {
            resposta.Headers[CabecalhoDoAmbiente] = marca.Valor;
        }

        var registro = context.HttpContext.RequestServices.GetRequiredService<RegistroDeNatureza>();
        if (registro.Agregada is { } natureza)
        {
            resposta.Headers[CabecalhoDaNatureza] = AgregadorDeNatureza.NomeNoCabecalho(natureza);
        }

        return resultado;
    }
}
