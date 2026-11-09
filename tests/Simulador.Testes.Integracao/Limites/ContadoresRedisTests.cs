using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Simulador.Limites.Persistencia;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Simulador.Testes.Integracao.Limites;

/// <summary>Contadores no Redis real (RN30, ADR-003).</summary>
public sealed class ContadoresRedisTests : IAsyncLifetime
{
    private static readonly TimeSpan Validade = TimeSpan.FromMinutes(1);
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.4-alpine").Build();

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await _redis.StartAsync(Cancelamento);

    public async ValueTask DisposeAsync() => await _redis.DisposeAsync();

    private ContadoresRedis Novo() => new(_redis.GetConnectionString(), NullLogger<ContadoresRedis>.Instance, TimeProvider.System);

    [Fact]
    public async Task Duas_instancias_somam_no_mesmo_total()
    {
        await using var a = Novo();
        await using var b = Novo();
        await Task.WhenAll(Enumerable.Range(0, 200).Select(i => (i % 2 == 0 ? a : b).Somar("total", Validade, Cancelamento)));
        Assert.Equal(200, await a.Ler("total", Cancelamento));
        Assert.Equal(0, await b.Ler("ausente", Cancelamento));
    }

    [Fact]
    public async Task Chave_expira_e_a_validade_nao_e_estendida()
    {
        await using var contadores = Novo();
        await using var leitura = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
        var banco = leitura.GetDatabase();

        await contadores.Somar("expira", Validade, Cancelamento);
        await contadores.Somar("expira", TimeSpan.FromDays(1), Cancelamento);

        var restante = await banco.KeyTimeToLiveAsync("expira");
        Assert.NotNull(restante);
        Assert.InRange(restante.Value, TimeSpan.FromSeconds(1), Validade);
    }

    [Fact]
    public async Task Indisponivel_devolve_zero_e_registra_no_log()
    {
        var log = new CapturaDeLog();
        await using var contadores = new ContadoresRedis("127.0.0.1:1", log, TimeProvider.System);

        Assert.Equal(0, await contadores.Somar("x", Validade, Cancelamento));
        Assert.Equal(0, await contadores.Ler("x", Cancelamento));

        Assert.Equal(2, log.Avisos);
    }

    [Fact]
    public async Task Quem_nao_responde_cai_na_indisponibilidade_em_100_ms()
    {
        var relogio = new FakeTimeProvider();
        var log = new CapturaDeLog();
        await using var contadores = new ContadoresRedis(() => new TaskCompletionSource<IDatabase>().Task, log, relogio);

        var soma = contadores.Somar("x", Validade, Cancelamento);
        relogio.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(0, await soma.WaitAsync(TimeSpan.FromSeconds(10), Cancelamento));
        Assert.Equal(1, log.Avisos);
    }

    private sealed class CapturaDeLog : ILogger<ContadoresRedis>
    {
        public int Avisos { get; private set; }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Avisos++;
            }
        }
    }
}
