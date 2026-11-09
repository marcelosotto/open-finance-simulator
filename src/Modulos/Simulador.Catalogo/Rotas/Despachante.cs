using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Catalogo.Contratos;
using Simulador.Comum.Erros;

namespace Simulador.Catalogo.Rotas;

/// <summary>
/// Leva a chamada ao manipulador da operação; sem manipulador, responde 501 (FR-006).
/// Os manipuladores vêm do escopo da chamada, porque podem usar serviços por requisição.
/// </summary>
internal sealed class Despachante(IResolvedorDeInstituicao resolvedor, TimeProvider relogio)
{
    public static HashSet<string> Implementadas(IServiceProvider servicos) =>
        servicos.GetServices<IManipuladorDeOperacao>().Select(m => m.OperationId).ToHashSet(StringComparer.Ordinal);

    public async Task<IResult> Despachar(HttpContext contexto, OperacaoDoCatalogo operacao)
    {
        var banco = contexto.Request.RouteValues["banco"] as string ?? string.Empty;
        var situacao = await resolvedor.Situacao(banco, contexto.RequestAborted).ConfigureAwait(false);
        if (situacao != SituacaoDoBanco.Ativo)
        {
            return situacao == SituacaoDoBanco.Inativo ? Respostas.ForaDoAr(relogio) : Respostas.NaoEncontrado(relogio);
        }

        if (contexto.RequestServices.GetService<IPortaoDeOperacao>() is { } portao
            && await portao.Avaliar(contexto, operacao).ConfigureAwait(false) is { } recusa)
        {
            return recusa;
        }

        var manipulador = contexto.RequestServices.GetServices<IManipuladorDeOperacao>()
            .FirstOrDefault(m => string.Equals(m.OperationId, operacao.OperationId, StringComparison.Ordinal));
        return manipulador is null
            ? Respostas.NaoImplementado(relogio)
            : await manipulador.Manipular(contexto).ConfigureAwait(false);
    }
}

/// <summary>Respostas padrão no envelope de erro oficial (contracts/respostas-padrao.md).</summary>
internal static class Respostas
{
    public static IResult NaoImplementado(TimeProvider relogio) => Results.Json(
        EnvelopeDeErro.Criar(
            "NAO_IMPLEMENTADO",
            "Operação não implementada",
            "A operação existe na especificação oficial, mas ainda não está disponível neste simulador.",
            relogio),
        EnvelopeDeErro.OpcoesJson,
        statusCode: StatusCodes.Status501NotImplemented);

    /// <summary>Instituição fora do ar, sem explicar o motivo (RN06, RN18).</summary>
    public static IResult ForaDoAr(TimeProvider relogio) => Results.Json(
        EnvelopeDeErro.Criar(
            "SERVICO_INDISPONIVEL",
            "Serviço indisponível",
            "O serviço não está disponível no momento.",
            relogio),
        EnvelopeDeErro.OpcoesJson,
        statusCode: StatusCodes.Status503ServiceUnavailable);

    public static IResult NaoEncontrado(TimeProvider relogio) => Results.Json(
        EnvelopeDeErro.Criar(
            "NAO_ENCONTRADO",
            "Recurso não encontrado",
            "O caminho solicitado não existe nesta instituição.",
            relogio),
        EnvelopeDeErro.OpcoesJson,
        statusCode: StatusCodes.Status404NotFound);
}
