using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulador.Cenarios.Aplicacao;
using Simulador.Cenarios.Contratos;

namespace Simulador.Cenarios.Http;

/// <summary>API de administração (contracts/admin-cenarios.md).</summary>
internal static class RotasDeCenarios
{
    private const string CabecalhoDoAutor = "x-sim-autor";
    private const string Regras = "/admin/cenarios/regras";
    private const string Pacotes = "/admin/cenarios/pacotes";
    private const string RegraInexistente = "regra não encontrada";
    private const string PacoteInexistente = "pacote não encontrado";

    public static void Mapear(IEndpointRouteBuilder administracao)
    {
        administracao.MapGet(Regras, async (ServicoDeCenarios servico, CancellationToken t) => Json(await servico.Regras(t).ConfigureAwait(false)));
        administracao.MapGet(Regras + "/{id}", async (string id, ServicoDeCenarios servico, CancellationToken t) =>
            await servico.Regra(id, t).ConfigureAwait(false) is { } regra ? Json(regra) : Erro(StatusCodes.Status404NotFound, RegraInexistente));

        administracao.MapPost(Regras, async (HttpContext contexto, ServicoDeCenarios servico) =>
            await LerPedido(contexto).ConfigureAwait(false) is { } pedido
                ? Responder(await servico.Criar(pedido, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false), StatusCodes.Status201Created)
                : CorpoInvalido());

        administracao.MapPut(Regras + "/{id}", async (string id, HttpContext contexto, ServicoDeCenarios servico) =>
            await LerPedido(contexto).ConfigureAwait(false) is { } pedido
                ? Responder(await servico.Alterar(id, pedido, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false))
                : CorpoInvalido());

        administracao.MapDelete(Regras + "/{id}", async (string id, HttpContext contexto, ServicoDeCenarios servico) =>
            await servico.Apagar(id, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false) ? Results.NoContent() : Erro(StatusCodes.Status404NotFound, RegraInexistente));

        administracao.MapPost(Regras + "/{id}/ligar", async (string id, HttpContext contexto, ServicoDeCenarios servico) =>
            Responder(await servico.Ligar(id, true, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false)));
        administracao.MapPost(Regras + "/{id}/desligar", async (string id, HttpContext contexto, ServicoDeCenarios servico) =>
            Responder(await servico.Ligar(id, false, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false)));

        administracao.MapGet(Pacotes, async (ServicoDeCenarios servico, CancellationToken t) => Json(await servico.Pacotes(t).ConfigureAwait(false)));
        administracao.MapPut(Pacotes + "/{nome}", async (string nome, HttpContext contexto, ServicoDeCenarios servico) =>
            Json(await servico.CriarPacote(nome, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false)));
        administracao.MapPost(Pacotes + "/{nome}/ligar", (string nome, HttpContext contexto, ServicoDeCenarios servico) => LigarPacote(nome, true, contexto, servico));
        administracao.MapPost(Pacotes + "/{nome}/desligar", (string nome, HttpContext contexto, ServicoDeCenarios servico) => LigarPacote(nome, false, contexto, servico));
        administracao.MapDelete(Pacotes + "/{nome}", async (string nome, HttpContext contexto, ServicoDeCenarios servico) =>
            await servico.ApagarPacote(nome, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false) switch
            {
                TipoDeSaida.Ok => Results.NoContent(),
                TipoDeSaida.Conflito => Erro(StatusCodes.Status409Conflict, "pacote tem regras"),
                _ => Erro(StatusCodes.Status404NotFound, PacoteInexistente),
            });
    }

    private static async Task<IResult> LigarPacote(string nome, bool ligado, HttpContext contexto, ServicoDeCenarios servico) =>
        await servico.LigarPacote(nome, ligado, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false) is { } pacote
            ? Json(pacote)
            : Erro(StatusCodes.Status404NotFound, PacoteInexistente);

    private static async Task<PedidoDeRegra?> LerPedido(HttpContext contexto)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<PedidoDeRegra>(contexto.Request.Body, JsonDosCenarios.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Autor(HttpContext contexto) => contexto.Request.Headers[CabecalhoDoAutor].FirstOrDefault();

    private static IResult Json<T>(T valor) => Results.Json(valor, JsonDosCenarios.Opcoes);

    private static IResult CorpoInvalido() => Erro(StatusCodes.Status422UnprocessableEntity, "corpo com campo ou valor inválido");

    private static IResult Erro(int status, params string[] erros) => Results.Json(new { erros }, JsonDosCenarios.Opcoes, statusCode: status);

    private static IResult Responder(Saida<RegraDeCenario> saida, int statusDeSucesso = StatusCodes.Status200OK) => saida.Tipo switch
    {
        TipoDeSaida.Ok => Results.Json(saida.Valor, JsonDosCenarios.Opcoes, statusCode: statusDeSucesso),
        TipoDeSaida.NaoEncontrado => Erro(StatusCodes.Status404NotFound, [.. saida.Erros!]),
        _ => Erro(StatusCodes.Status422UnprocessableEntity, [.. saida.Erros!]),
    };
}
