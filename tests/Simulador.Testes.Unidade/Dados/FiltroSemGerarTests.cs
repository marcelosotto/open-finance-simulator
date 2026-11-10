using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Time.Testing;
using Simulador.Dados.Aplicacao;
using Simulador.Dados.Contratos;

namespace Simulador.Testes.Unidade.Dados;

/// <summary>O filtro compara só o campo filtrado: os itens excluídos não são gerados.</summary>
public class FiltroSemGerarTests
{
    /// <summary>Gerar o item inteiro falharia: <c>venenoId</c> tem padrão inválido e o gerador o testaria.</summary>
    private const string Schema = """
        {"type":"object","properties":{"data":{"type":"array","items":{"type":"object","required":["creditDebitType","venenoId"],"properties":{
          "creditDebitType":{"type":"string","enum":["CREDITO","DEBITO"]},
          "venenoId":{"type":"string","pattern":"["}
        }}}}}
        """;

    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Item_fora_do_filtro_nao_e_gerado()
    {
        var servico = new ServicoDeDados(new Ambiente(), new FakeTimeProvider(Agora), new SegredoDaChave([1, 2, 3]));
        var contexto = new DefaultHttpContext();
        contexto.Request.Path = "/banco/accounts/v2/accounts/r1/transactions";
        contexto.Request.QueryString = new QueryString("?creditDebitIndicator=NENHUM");
        contexto.Request.RouteValues["accountId"] = "r1";
        contexto.Request.RouteValues["banco"] = "banco";
        var operacao = new OperacaoDeDados("accounts", "op", "/accounts/{accountId}/transactions", Schema, 1, false, ["creditDebitIndicator"]);

        var resposta = Assert.IsType<ContentHttpResult>(await servico.Responder(contexto, operacao));

        Assert.Contains("\"data\":[]", resposta.ResponseContent, StringComparison.Ordinal);
    }

    private sealed class Ambiente : IAmbienteDosDados
    {
        private static readonly RecursoDeDados Conta = new("r1", "c1", "ACCOUNT", "AVAILABLE", "persistido", new DateOnly(2026, 1, 1));

        public ConsentimentoDaChamada? Consentimento(HttpContext contexto) => new("urn:hml:p-1", "c1", ["r1"], "persistido");

        public Task<RecursoDeDados?> Recurso(string resourceId, CancellationToken cancelamento) => Task.FromResult<RecursoDeDados?>(Conta);

        public Task<ClienteDeDados?> Cliente(string documento, CancellationToken cancelamento) => Task.FromResult<ClienteDeDados?>(null);

        public Task<IReadOnlyList<TransacaoDeDados>> Transacoes(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, CancellationToken cancelamento) =>
            Task.FromResult<IReadOnlyList<TransacaoDeDados>>([new("t1", de, 1, 10m, true, "PIX", "n", "TRANSACAO_EFETIVADA")]);

        public Task<decimal> Saldo(string resourceId, CancellationToken cancelamento) => Task.FromResult(0m);

        public Task<LimitesDePaginacao> Limites(string banco, CancellationToken cancelamento) => Task.FromResult(new LimitesDePaginacao(1000, 60));

        public void RegistrarNatureza(HttpContext contexto, string natureza)
        {
        }

        public Task<ICotaDeDados?> Cota(HttpContext contexto, OperacaoDeDados operacao, ConsentimentoDaChamada consentimento, CancellationToken cancelamento) => Task.FromResult<ICotaDeDados?>(null);
    }
}
