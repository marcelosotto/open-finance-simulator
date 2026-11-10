using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulador.Instituicoes.Aplicacao;
using Simulador.Instituicoes.Contratos;
using Simulador.Instituicoes.Dominio;

namespace Simulador.Instituicoes.Http;

/// <summary>API de administração (contracts/admin-instituicoes.md).</summary>
internal static class RotasDeInstituicoes
{
    private const string Base = "/admin/instituicoes";

    public static void Mapear(IEndpointRouteBuilder administracao)
    {
        administracao.MapGet("/admin/perfis-de-comportamento", () => Json(CatalogoDePerfis.Todos()));

        administracao.MapGet(Base, async (CadastroDeInstituicoes cadastro, IConsultaDeAnomalias anomalias, CancellationToken cancelamento) =>
        {
            var lista = new List<object>();
            foreach (var i in await cadastro.Listar(cancelamento).ConfigureAwait(false))
            {
                lista.Add(new { i.Endereco, i.Nome, i.Perfil, i.Situacao, AnomaliasLigadas = await anomalias.Ligadas(i.Endereco, cancelamento).ConfigureAwait(false) });
            }

            return Json(lista);
        });

        administracao.MapPost(Base, async (HttpContext contexto, CadastroDeInstituicoes cadastro) =>
        {
            PedidoDeCriacao? pedido;
            try
            {
                pedido = await JsonSerializer.DeserializeAsync<PedidoDeCriacao>(contexto.Request.Body, JsonDasInstituicoes.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return CorpoInvalido();
            }

            return pedido is null
                ? CorpoInvalido()
                : Responder(await cadastro.Criar(pedido, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false), StatusCodes.Status201Created);
        });

        administracao.MapGet(Base + "/{endereco}", async (string endereco, CadastroDeInstituicoes cadastro, IConsultaDeAnomalias anomalias, CancellationToken cancelamento) =>
        {
            if (await cadastro.Obter(endereco, cancelamento).ConfigureAwait(false) is not { } instituicao)
            {
                return Responder(ResultadoDoCadastro.NaoEncontrada);
            }

            var visao = Visao(instituicao);
            visao["anomaliasLigadas"] = JsonSerializer.SerializeToNode(await anomalias.Ligadas(endereco, cancelamento).ConfigureAwait(false), JsonDasInstituicoes.Opcoes);
            return Results.Json(visao, JsonDasInstituicoes.Opcoes);
        });

        administracao.MapPatch(Base + "/{endereco}", async (string endereco, HttpContext contexto, CadastroDeInstituicoes cadastro) =>
        {
            JsonObject? alteracao;
            try
            {
                alteracao = await JsonSerializer.DeserializeAsync<JsonObject>(contexto.Request.Body, JsonDasInstituicoes.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return CorpoInvalido();
            }

            return alteracao is null
                ? CorpoInvalido()
                : Responder(await cadastro.Alterar(endereco, alteracao, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false));
        });

        administracao.MapDelete(Base + "/{endereco}", async (string endereco, HttpContext contexto, CadastroDeInstituicoes cadastro) =>
        {
            var resultado = await cadastro.Remover(endereco, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false);
            return resultado.Tipo == TipoDeResultado.Ok ? Json(new { resultado = resultado.Resultado }) : Responder(resultado);
        });

        administracao.MapPost(Base + "/{endereco}/reativar", async (string endereco, HttpContext contexto, CadastroDeInstituicoes cadastro) =>
            Responder(await cadastro.Reativar(endereco, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false)));

        administracao.MapPut(Base + "/{endereco}/relogio", async (string endereco, HttpContext contexto, CadastroDeInstituicoes cadastro) =>
        {
            PedidoDeRelogio? pedido;
            try
            {
                pedido = await JsonSerializer.DeserializeAsync<PedidoDeRelogio>(contexto.Request.Body, JsonDasInstituicoes.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
            }
            catch (JsonException)
            {
                return CorpoInvalido();
            }

            return pedido is null
                ? CorpoInvalido()
                : Responder(await cadastro.MudarRelogio(endereco, pedido, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false));
        });

        administracao.MapDelete(Base + "/{endereco}/relogio", async (string endereco, HttpContext contexto, CadastroDeInstituicoes cadastro) =>
            Responder(await cadastro.VoltarAoRelogioReal(endereco, Autor(contexto), contexto.RequestAborted).ConfigureAwait(false)));

        administracao.MapGet(Base + "/{endereco}/historico", async (string endereco, CadastroDeInstituicoes cadastro, CancellationToken cancelamento) =>
            await cadastro.Obter(endereco, cancelamento).ConfigureAwait(false) is null
                && (await cadastro.Historico(endereco, cancelamento).ConfigureAwait(false)).Count == 0
                ? Responder(ResultadoDoCadastro.NaoEncontrada)
                : Json(await cadastro.Historico(endereco, cancelamento).ConfigureAwait(false)));
    }

    private static string? Autor(HttpContext contexto) => Simulador.Comum.Http.AutorDoPedido.Ler(contexto);

    private static IResult Json<T>(T valor) => Results.Json(valor, JsonDasInstituicoes.Opcoes);

    private static IResult CorpoInvalido() => Responder(ResultadoDoCadastro.Invalida("corpo com campo ou valor inválido"));

    private static IResult Responder(ResultadoDoCadastro resultado, int statusDeSucesso = StatusCodes.Status200OK) => resultado.Tipo switch
    {
        TipoDeResultado.Ok => Results.Json(Visao(resultado.Instituicao!), JsonDasInstituicoes.Opcoes, statusCode: statusDeSucesso),
        TipoDeResultado.NaoEncontrada => Erro(resultado, StatusCodes.Status404NotFound),
        TipoDeResultado.Repetida => Erro(resultado, StatusCodes.Status409Conflict),
        _ => Erro(resultado, StatusCodes.Status422UnprocessableEntity),
    };

    private static IResult Erro(ResultadoDoCadastro resultado, int status) =>
        Results.Json(new { erros = resultado.Erros }, JsonDasInstituicoes.Opcoes, statusCode: status);

    private static JsonObject Visao(Instituicao instituicao)
    {
        var visao = JsonSerializer.SerializeToNode(instituicao, JsonDasInstituicoes.Opcoes)!.AsObject();
        visao["sla"] = JsonSerializer.SerializeToNode(ComparacaoDeSla.Comparar(instituicao.Comportamento), JsonDasInstituicoes.Opcoes);
        visao["avisos"] = JsonSerializer.SerializeToNode(Avisos.Calcular(instituicao.Limites), JsonDasInstituicoes.Opcoes);
        return visao;
    }
}
