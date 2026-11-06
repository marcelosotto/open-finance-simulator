using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Catalogo.Contratos;
using Simulador.Comum.Erros;

namespace Simulador.Catalogo.Rotas;

/// <summary>Aceita qualquer identificador de banco no formato do endereço, até a spec 002 (research R04).</summary>
internal sealed partial class ResolvedorProvisorio : IResolvedorDeInstituicao
{
    public bool Existe(string banco) => FormatoDoBanco().IsMatch(banco);

    [GeneratedRegex("^[a-z0-9-]{1,64}$")]
    private static partial Regex FormatoDoBanco();
}

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
        if (!resolvedor.Existe(banco))
        {
            return Respostas.NaoEncontrado(relogio);
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

    public static IResult NaoEncontrado(TimeProvider relogio) => Results.Json(
        EnvelopeDeErro.Criar(
            "NAO_ENCONTRADO",
            "Recurso não encontrado",
            "O caminho solicitado não existe nesta instituição.",
            relogio),
        EnvelopeDeErro.OpcoesJson,
        statusCode: StatusCodes.Status404NotFound);
}
