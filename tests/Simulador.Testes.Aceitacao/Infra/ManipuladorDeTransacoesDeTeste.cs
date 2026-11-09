using System.Globalization;
using Microsoft.AspNetCore.Http;
using Simulador.Catalogo.Contratos;
using Simulador.Marcacao.Contratos;

namespace Simulador.Testes.Aceitacao.Infra;

/// <summary>Itens que o manipulador de teste devolve, um por natureza.</summary>
public sealed class DadosDeTeste
{
    public List<Natureza> Naturezas { get; } = [Natureza.Persistido];
}

/// <summary>
/// Comportamento de teste para <c>GET /accounts/{accountId}/transactions</c>, registrado só no host
/// de teste. Monta o corpo com os serviços reais de marcação; nenhum código de teste vai para produção.
/// </summary>
public sealed class ManipuladorDeTransacoesDeTeste(
    DadosDeTeste dados,
    IMarcador marcador,
    IRegistroDeNatureza registro,
    TimeProvider relogio) : IManipuladorDeOperacao
{
    public const string Operacao = "accountsGetAccountsAccountIdTransactions";
    public const int TamanhoOficialDoNome = 200;

    /// <summary>Texto original maior que o campo, para provar o corte.</summary>
    public static readonly string NomeOriginal = "PIX recebido " + new string('x', 250);

    public string OperationId => Operacao;

    public Task<IResult> Manipular(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var agora = relogio.GetUtcNow().UtcDateTime;
        var itens = dados.Naturezas.Select(natureza =>
        {
            registro.Registrar(natureza);
            return new Dictionary<string, object>
            {
                ["transactionId"] = marcador.NovoIdentificador(natureza),
                ["completedAuthorisedPaymentType"] = "TRANSACAO_EFETIVADA",
                ["creditDebitType"] = "CREDITO",
                ["transactionName"] = marcador.MarcarTexto(NomeOriginal, TamanhoOficialDoNome),
                ["type"] = "PIX",
                ["transactionAmount"] = new Dictionary<string, string> { ["amount"] = "100.00", ["currency"] = "BRL" },
                ["transactionDateTime"] = agora.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            };
        }).ToList();

        var corpo = new Dictionary<string, object>
        {
            ["data"] = itens,
            ["links"] = new Dictionary<string, string> { ["self"] = $"https://simulador.local{contexto.Request.Path}" },
            ["meta"] = new Dictionary<string, string> { ["requestDateTime"] = agora.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture) },
        };
        return Task.FromResult(Results.Json(corpo));
    }
}
