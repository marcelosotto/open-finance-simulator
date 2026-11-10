using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulador.Massa.Aplicacao;
using Simulador.Massa.Contratos;

namespace Simulador.Massa.Http;

/// <summary>API de administração (contracts/admin-massa.md).</summary>
internal static class RotasDaMassa
{
    private const string CabecalhoDoAutor = "x-sim-autor";
    private const string Base = "/admin/massa";

    public static void Mapear(IEndpointRouteBuilder administracao)
    {
        MapearPerfisEDistribuicoes(administracao);
        MapearClientesELotes(administracao);
        MapearMundoEAjustes(administracao);

        administracao.MapPost(Base + "/limpezas", (HttpContext c, ServicoDeLimpeza limpeza) =>
            Com<PedidoDeLimpeza>(c, async p => Responder(await limpeza.Limpar(p, Autor(c), c.RequestAborted).ConfigureAwait(false))));

        administracao.MapGet(Base + "/historico", async (CadastroDeMassa cadastro, CancellationToken t) =>
            Json(await cadastro.Historico(t).ConfigureAwait(false)));
    }

    private static void MapearPerfisEDistribuicoes(IEndpointRouteBuilder administracao)
    {
        administracao.MapGet(Base + "/perfis", async (CadastroDeMassa cadastro, CancellationToken t) =>
            Json(await cadastro.Perfis(t).ConfigureAwait(false)));

        administracao.MapPut(Base + "/perfis/{nome}", (string nome, HttpContext c, CadastroDeMassa cadastro) =>
            Com<PedidoDePerfil>(c, async p => Responder(await cadastro.SalvarPerfil(nome, p, Autor(c), c.RequestAborted).ConfigureAwait(false))));

        administracao.MapDelete(Base + "/perfis/{nome}", async (string nome, HttpContext c, CadastroDeMassa cadastro) =>
            SemCorpo(await cadastro.ApagarPerfil(nome, Autor(c), c.RequestAborted).ConfigureAwait(false)));

        administracao.MapGet(Base + "/distribuicoes", async (CadastroDeMassa cadastro, CancellationToken t) =>
            Json(await cadastro.Distribuicoes(t).ConfigureAwait(false)));

        administracao.MapPut(Base + "/distribuicoes/{nome}", (string nome, HttpContext c, CadastroDeMassa cadastro) =>
            Com<Distribuicao>(c, async d => Responder(await cadastro.SalvarDistribuicao(nome, d, Autor(c), c.RequestAborted).ConfigureAwait(false))));

        administracao.MapDelete(Base + "/distribuicoes/{nome}", async (string nome, HttpContext c, CadastroDeMassa cadastro) =>
            SemCorpo(await cadastro.ApagarDistribuicao(nome, Autor(c), c.RequestAborted).ConfigureAwait(false)));
    }

    private static void MapearClientesELotes(IEndpointRouteBuilder administracao)
    {
        administracao.MapPost(Base + "/clientes", (HttpContext c, CadastroDeMassa cadastro) =>
            Com<PedidoDeCliente>(c, async p => Responder(await cadastro.CriarCliente(p, Autor(c), c.RequestAborted).ConfigureAwait(false), StatusCodes.Status201Created)));

        administracao.MapGet(Base + "/clientes", async (string? banco, int? inicio, int? quantidade, CadastroDeMassa cadastro, CancellationToken t) =>
            Responder(await cadastro.ListarClientes(banco, inicio, quantidade, t).ConfigureAwait(false)));

        administracao.MapGet(Base + "/resumo", async (CadastroDeMassa cadastro, CancellationToken t) =>
            Json(await cadastro.Resumo(t).ConfigureAwait(false)));

        administracao.MapGet(Base + "/clientes/{documento}", async (string documento, CadastroDeMassa cadastro, CancellationToken t) =>
            await cadastro.ObterCliente(documento, t).ConfigureAwait(false) is { } massa ? Json(massa) : NaoEncontrado("cliente não encontrado"));

        administracao.MapPost(Base + "/lotes", (HttpContext c, ServicoDeLotes lotes) =>
            Com<PedidoDeLote>(c, async p => Responder(await lotes.Criar(p, Autor(c), c.RequestAborted).ConfigureAwait(false), StatusCodes.Status202Accepted)));

        administracao.MapGet(Base + "/lotes", async (ServicoDeLotes lotes, CancellationToken t) =>
            Json(await lotes.Listar(t).ConfigureAwait(false)));

        administracao.MapGet(Base + "/lotes/{id}", async (string id, ServicoDeLotes lotes, CancellationToken t) =>
            await lotes.Obter(id, t).ConfigureAwait(false) is { } lote ? Json(lote) : NaoEncontrado("lote não encontrado"));

        administracao.MapGet(Base + "/lotes/{id}/clientes", async (string id, int? inicio, int? quantidade, ServicoDeLotes lotes, CancellationToken t) =>
            Json(await lotes.Clientes(id, inicio ?? 0, quantidade ?? 100, t).ConfigureAwait(false)));

        administracao.MapPost(Base + "/lotes/{id}/cancelar", async (string id, HttpContext c, ServicoDeLotes lotes) =>
            Responder(await lotes.Cancelar(id, Autor(c), c.RequestAborted).ConfigureAwait(false), StatusCodes.Status202Accepted));
    }

    private static void MapearMundoEAjustes(IEndpointRouteBuilder administracao)
    {
        const string BaseDoMundo = Base + "/mundo-virtual";
        administracao.MapGet(BaseDoMundo, async (ServicoDoMundo mundo, CancellationToken t) =>
            await mundo.Obter(t).ConfigureAwait(false) is { } definido ? Json(definido) : NaoEncontrado("mundo virtual não definido"));

        administracao.MapPut(BaseDoMundo, (HttpContext c, ServicoDoMundo mundo) =>
            Com<PedidoDeMundo>(c, async p => Responder(await mundo.Definir(p, Autor(c), c.RequestAborted).ConfigureAwait(false))));

        administracao.MapGet(BaseDoMundo + "/itens", async (long? inicio, long? quantidade, string? banco, ServicoDoMundo mundo, ConsultaDeMassa consulta, CancellationToken t) =>
        {
            if (inicio is < 0 || quantidade is null or <= 0)
            {
                return Erro(StatusCodes.Status422UnprocessableEntity, ["informe inicio não negativo e quantidade positiva"]);
            }

            return await consulta.GeradorDoMundo(t).ConfigureAwait(false) is { } atual
                ? Results.Json(mundo.Itens(atual, inicio ?? 0, quantidade.Value, banco, t), JsonDaMassa.Opcoes)
                : NaoEncontrado("mundo virtual não definido");
        });

        administracao.MapPost(Base + "/ajustes", (HttpContext c, ServicoDeAjustes ajustes) =>
            Com<PedidoDeAjuste>(c, async p => Responder(await ajustes.Criar(p, Autor(c), c.RequestAborted).ConfigureAwait(false), StatusCodes.Status201Created)));

        administracao.MapGet(Base + "/consentimentos/{consentId}", async (string consentId, ConsultaDeMassa consulta, CancellationToken t) =>
            await consulta.ObterConsentimento(consentId, t).ConfigureAwait(false) is { } consentimento ? Json(consentimento) : NaoEncontrado("consentimento não encontrado"));

        administracao.MapGet(Base + "/recursos/{id}/transacoes", async (string id, DateOnly? de, DateOnly? ate, BaseDaMassa baseDaMassa, ConsultaDeMassa consulta, CancellationToken t) =>
        {
            var inicio = de ?? baseDaMassa.Hoje;
            var transacoes = await consulta.Transacoes(id, inicio, ate ?? inicio, t).ConfigureAwait(false);
            return transacoes is null
                ? NaoEncontrado("recurso não encontrado")
                : Json(new { transacoes, saldo = await consulta.Saldo(id, t).ConfigureAwait(false) });
        });
    }

    private static string? Autor(HttpContext contexto) => contexto.Request.Headers[CabecalhoDoAutor].FirstOrDefault();

    private static async Task<IResult> Com<T>(HttpContext contexto, Func<T, Task<IResult>> acao)
        where T : class
    {
        T? corpo;
        try
        {
            corpo = await JsonSerializer.DeserializeAsync<T>(contexto.Request.Body, JsonDaMassa.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            corpo = null;
        }

        return corpo is null ? Erro(StatusCodes.Status422UnprocessableEntity, ["corpo com campo ou valor inválido"]) : await acao(corpo).ConfigureAwait(false);
    }

    private static IResult Json<T>(T valor, int status = StatusCodes.Status200OK) => Results.Json(valor, JsonDaMassa.Opcoes, statusCode: status);

    private static IResult Responder<T>(Resultado<T> resultado, int statusDeSucesso = StatusCodes.Status200OK) =>
        resultado.Tipo == TipoDeResultado.Ok ? Json(resultado.Valor, statusDeSucesso) : Falha(resultado);

    private static IResult SemCorpo(Resultado<bool> resultado) =>
        resultado.Tipo == TipoDeResultado.Ok ? Results.NoContent() : Falha(resultado);

    private static IResult Falha<T>(Resultado<T> resultado) => Erro(
        resultado.Tipo switch
        {
            TipoDeResultado.NaoEncontrado => StatusCodes.Status404NotFound,
            TipoDeResultado.Conflito => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        },
        resultado.Erros!);

    private static IResult NaoEncontrado(string erro) => Erro(StatusCodes.Status404NotFound, [erro]);

    private static IResult Erro(int status, IReadOnlyList<string> erros) => Results.Json(new { erros }, JsonDaMassa.Opcoes, statusCode: status);
}
