using Microsoft.AspNetCore.Http;
using Simulador.Dados.Contratos;

namespace Simulador.Testes.Host;

/// <summary>Observa o que o serviço de dados pede e, se pedido, faz a leitura de saldo falhar.</summary>
public sealed class EspiaoDeDados
{
    public int Saldos { get; set; }

    public List<(DateOnly De, DateOnly Ate)> Intervalos { get; } = [];

    public bool FalharSaldo { get; set; }
}

/// <summary>Repassa ao ambiente real, registrando no espião.</summary>
internal sealed class AmbienteEspiao(IAmbienteDosDados interno, EspiaoDeDados espiao) : IAmbienteDosDados
{
    public ConsentimentoDaChamada? Consentimento(HttpContext contexto) => interno.Consentimento(contexto);

    public Task<RecursoDeDados?> Recurso(string resourceId, CancellationToken cancelamento) => interno.Recurso(resourceId, cancelamento);

    public Task<ClienteDeDados?> Cliente(string documento, CancellationToken cancelamento) => interno.Cliente(documento, cancelamento);

    public Task<IReadOnlyList<TransacaoDeDados>> Transacoes(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, CancellationToken cancelamento)
    {
        espiao.Intervalos.Add((de, ate));
        return interno.Transacoes(resourceId, de, ate, instante, cancelamento);
    }

    public Task<decimal> Saldo(string resourceId, CancellationToken cancelamento)
    {
        espiao.Saldos++;
        return espiao.FalharSaldo ? throw new InvalidOperationException("Falha provocada pelo teste.") : interno.Saldo(resourceId, cancelamento);
    }

    public Task<LimitesDePaginacao> Limites(string banco, CancellationToken cancelamento) => interno.Limites(banco, cancelamento);

    public void RegistrarNatureza(HttpContext contexto, string natureza) => interno.RegistrarNatureza(contexto, natureza);

    public Task<ICotaDeDados?> Cota(HttpContext contexto, OperacaoDeDados operacao, ConsentimentoDaChamada consentimento, CancellationToken cancelamento) =>
        interno.Cota(contexto, operacao, consentimento, cancelamento);
}
