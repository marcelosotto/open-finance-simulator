using System.Collections.Concurrent;
using Simulador.Limites.Contratos;

namespace Simulador.Testes.Host;

/// <summary>Nunca acumula: os testes de outras specs não esbarram em limites.</summary>
public sealed class ContadoresNulos : IContadores
{
    public Task<long> Somar(string chave, TimeSpan validade, CancellationToken cancelamento) => Task.FromResult(0L);

    public Task<long> Ler(string chave, CancellationToken cancelamento) => Task.FromResult(0L);
}

/// <summary>Contadores em memória, inspecionáveis pelos cenários.</summary>
public sealed class ContadoresEmMemoria : IContadores
{
    private readonly ConcurrentDictionary<string, long> _totais = new(StringComparer.Ordinal);

    /// <summary>Soma de todas as cotas mensais.</summary>
    public long TotalDaCota => _totais.Where(t => t.Key.StartsWith("lim:cota:", StringComparison.Ordinal)).Sum(t => t.Value);

    public Task<long> Somar(string chave, TimeSpan validade, CancellationToken cancelamento) =>
        Task.FromResult(_totais.AddOrUpdate(chave, 1, (_, atual) => atual + 1));

    public Task<long> Ler(string chave, CancellationToken cancelamento) =>
        Task.FromResult(_totais.GetValueOrDefault(chave));

    public void Definir(string chave, long total) => _totais[chave] = total;

    /// <summary>Define o total de todas as cotas já existentes cuja chave contém o trecho.</summary>
    public void DefinirCota(string trecho, long total)
    {
        foreach (var chave in _totais.Keys.Where(c => c.StartsWith("lim:cota:", StringComparison.Ordinal) && c.Contains(trecho, StringComparison.Ordinal)))
        {
            _totais[chave] = total;
        }
    }
}
