using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using Simulador.Cenarios.Contratos;
using Simulador.Comum.Comportamento;

namespace Simulador.Testes.Host;

/// <summary>Sem latência e sem sorteio: o padrão das suítes de outras specs.</summary>
public sealed class ComportamentoNulo : IComportamentoDaChamada
{
    public Task<DesfechoDoComportamento?> Decidir(HttpContext contexto) => Task.FromResult<DesfechoDoComportamento?>(null);

    public Task<bool> RecursoIndisponivel(HttpContext contexto) => Task.FromResult(false);

    public Task<string> EstadoDoRecurso(string resourceId, string estado, CancellationToken cancelamento) => Task.FromResult(estado);
}

/// <summary>Registra as esperas; pode avançar o relógio falso, segurar a resposta e observar o momento da espera.</summary>
public sealed class EsperasRegistradas : IEsperaDaChamada
{
    private TaskCompletionSource _liberacao = NovaLiberacao();

    public ConcurrentQueue<TimeSpan> Duracoes { get; } = new();

    /// <summary>Avança este relógio pela duração de cada espera.</summary>
    public FakeTimeProvider? Avancar { get; set; }

    /// <summary>Segura cada espera até <see cref="Liberar"/>.</summary>
    public bool Segurar { get; set; }

    /// <summary>Chamado no início de cada espera.</summary>
    public Action? AoEsperar { get; set; }

    public async Task Aguardar(TimeSpan duracao, CancellationToken cancelamento)
    {
        AoEsperar?.Invoke();
        Duracoes.Enqueue(duracao);
        Avancar?.Advance(duracao);
        if (Segurar)
        {
            await _liberacao.Task.WaitAsync(cancelamento).ConfigureAwait(false);
        }
    }

    public void Liberar()
    {
        _liberacao.TrySetResult();
        _liberacao = NovaLiberacao();
    }

    private static TaskCompletionSource NovaLiberacao() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>Regras e pacotes em memória.</summary>
public sealed class RepositorioDeCenariosEmMemoria : IRepositorioDeCenarios
{
    private readonly ConcurrentDictionary<string, RegraDeCenario> _regras = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PacoteDeCenario> _pacotes = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<RegraDeCenario>>([.. _regras.Values]);

    public Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento) => Task.FromResult(_regras.GetValueOrDefault(id));

    public Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(regra);
        _regras[regra.Id] = regra;
        return Task.CompletedTask;
    }

    public Task ApagarRegra(string id, CancellationToken cancelamento)
    {
        _regras.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<PacoteDeCenario>>([.. _pacotes.Values]);

    public Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento) => Task.FromResult(_pacotes.GetValueOrDefault(nome));

    public Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pacote);
        _pacotes[pacote.Nome] = pacote;
        return Task.CompletedTask;
    }

    public Task ApagarPacote(string nome, CancellationToken cancelamento)
    {
        _pacotes.TryRemove(nome, out _);
        return Task.CompletedTask;
    }
}
