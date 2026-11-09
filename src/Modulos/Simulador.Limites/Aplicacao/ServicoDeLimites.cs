using System.Globalization;
using Microsoft.AspNetCore.Http;
using Simulador.Comum.Erros;
using Simulador.Limites.Contratos;
using Simulador.Limites.Dominio;

namespace Simulador.Limites.Aplicacao;

/// <summary>Conta as chamadas e recusa o excesso (UC18 a UC20, RN39).</summary>
internal sealed class ServicoDeLimites(IContadores contadores, TimeProvider relogio) : ILimites
{
    private const int StatusDeSobrecarga = 529;

    public async Task<IResult?> Avaliar(HttpContext contexto, EntradaDaChamada entrada)
    {
        var agora = relogio.GetUtcNow();
        var cancelamento = contexto.RequestAborted;
        var capacidade = await contadores.Somar(ChavesDeContador.Capacidade(entrada.Banco, agora), ChavesDeContador.ValidadeDaCapacidade, cancelamento).ConfigureAwait(false);
        if (capacidade > entrada.Capacidade)
        {
            return Erro(StatusDeSobrecarga, "SITE_IS_OVERLOADED", "Site sobrecarregado", "A operação foi recusada porque o limite de requisições por segundo foi atingido.");
        }

        if (entrada is not { Tpm: { } tpm, Receptora: { } receptora })
        {
            return null;
        }

        var trafego = await contadores.Somar(ChavesDeContador.Trafego(entrada.Banco, entrada.OperationId, receptora, agora), ChavesDeContador.ValidadeDoTrafego, cancelamento).ConfigureAwait(false);
        if (trafego <= tpm)
        {
            return null;
        }

        if (entrada.RetryAfterLigado)
        {
            contexto.Response.Headers.RetryAfter = entrada.RetryAfterSeg.ToString(CultureInfo.InvariantCulture);
        }

        return Erro(StatusCodes.Status429TooManyRequests, "TOO_MANY_REQUESTS", "Muitas requisições", "A operação foi recusada porque muitas solicitações foram feitas em um determinado período.");
    }

    public async Task<bool> CotaEsgotada(CotaDaChamada cota, CancellationToken cancelamento) =>
        await contadores.Ler(Chave(cota), cancelamento).ConfigureAwait(false) >= cota.Limite;

    public Task Contar(CotaDaChamada cota, CancellationToken cancelamento) =>
        contadores.Somar(Chave(cota), ChavesDeContador.ValidadeDaCota, cancelamento);

    private string Chave(CotaDaChamada cota) =>
        ChavesDeContador.Cota(cota.Banco, cota.OperationId, cota.Objeto, cota.Cliente, cota.Receptora, relogio.GetUtcNow());

    private IResult Erro(int status, string codigo, string titulo, string detalhe) =>
        Results.Json(EnvelopeDeErro.Criar(codigo, titulo, detalhe, relogio), EnvelopeDeErro.OpcoesJson, statusCode: status);
}
