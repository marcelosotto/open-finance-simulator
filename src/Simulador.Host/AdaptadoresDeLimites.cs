using Simulador.Catalogo.Contratos;
using Simulador.Consentimentos.Contratos;
using Simulador.Dados.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Limites.Contratos;
using Simulador.Limites.Dominio;
using LimitesDaInstituicao = Simulador.Instituicoes.Contratos.Limites;

namespace Simulador.Host;

/// <summary>Capacidade e tráfego depois do portão, com os valores da instituição e da tabela (RN39).</summary>
internal sealed class EtapaPelosLimites(ILimites limites, IConsultaDeInstituicoes instituicoes, ICatalogo catalogo) : IEtapaDeEntrada
{
    private const string StatusDeSobrecarga = "529";

    public async Task<IResult?> Avaliar(HttpContext contexto, OperacaoDoCatalogo operacao)
    {
        if (!operacao.Respostas.ContainsKey(StatusDeSobrecarga))
        {
            return null;
        }

        var banco = contexto.Request.RouteValues["banco"] as string ?? string.Empty;
        var valores = (await instituicoes.Obter(banco, contexto.RequestAborted).ConfigureAwait(false))!.Limites;
        var receptora = ExtensoesDosConsentimentos.ReceptoraDaChamada(contexto);
        return await limites.Avaliar(contexto, new EntradaDaChamada(
            banco,
            operacao.OperationId,
            receptora,
            valores.CapacidadeTps,
            Tpm(valores, catalogo.ClasseDe(operacao), receptora),
            valores.RetryAfterLigado,
            valores.RetryAfterSeg)).ConfigureAwait(false);
    }

    internal static int? Tpm(LimitesDaInstituicao valores, ClasseDoEndpoint? classe, string? receptora)
    {
        if (classe is null || receptora is null || (classe.Tpm is null && !classe.TpmPorFaixaQca))
        {
            return null;
        }

        return classe.Frequencia switch
        {
            Frequencia.Alta => valores.TpmDaAlta(receptora),
            Frequencia.MediaAlta => valores.Tpm.MediaAlta,
            Frequencia.Media => valores.Tpm.Media,
            _ => valores.Tpm.Baixa,
        };
    }
}

/// <summary>A cota da chamada de dados, com o limite da classe (RN25, RN26).</summary>
internal sealed class CotaPelosLimites(ILimites limites, CotaDaChamada cota) : ICotaDeDados
{
    public Task<bool> Esgotada(CancellationToken cancelamento) => limites.CotaEsgotada(cota, cancelamento);

    public Task Contar(CancellationToken cancelamento) => limites.Contar(cota, cancelamento);
}

internal static class RegrasDaCotaDoHost
{
    private const int MensalDeSaldosELimites = 420;

    /// <summary>Limite mensal da instituição; nulo sem limite operacional (RN26, RN28).</summary>
    public static int? Limite(LimitesDaInstituicao valores, ClasseDoEndpoint? classe)
    {
        if (!valores.LimiteOperacionalLigado || classe?.LimiteMensal is not { } oficial)
        {
            return null;
        }

        if (oficial == MensalDeSaldosELimites)
        {
            return valores.LimiteMensal.SaldosELimites;
        }

        return classe.Frequencia switch
        {
            Frequencia.Alta => valores.LimiteMensal.Alta,
            Frequencia.MediaAlta => valores.LimiteMensal.MediaAlta,
            Frequencia.Media => valores.LimiteMensal.Media,
            _ => valores.LimiteMensal.Baixa,
        };
    }

    public static string Objeto(HttpContext contexto, string caminho, string consentId) =>
        ObjetoDaCota.Parametro(caminho) is { } parametro ? contexto.Request.RouteValues[parametro] as string ?? consentId : consentId;
}

internal static class ExtensoesDosAdaptadoresDeLimites
{
    public static IServiceCollection AddAdaptadoresDeLimites(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IEtapaDeEntrada, EtapaPelosLimites>();
        return servicos;
    }
}
