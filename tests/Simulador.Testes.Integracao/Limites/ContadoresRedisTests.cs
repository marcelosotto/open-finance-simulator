using Microsoft.Extensions.Logging.Abstractions;
using Simulador.Limites.Persistencia;
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

    [Fact]
    public async Task Duas_instancias_somam_no_mesmo_total()
    {
        await using var a = new ContadoresRedis(_redis.GetConnectionString(), NullLogger<ContadoresRedis>.Instance);
        await using var b = new ContadoresRedis(_redis.GetConnectionString(), NullLogger<ContadoresRedis>.Instance);
        await Task.WhenAll(Enumerable.Range(0, 200).Select(i => (i % 2 == 0 ? a : b).Somar("total", Validade, Cancelamento)));
        Assert.Equal(200, await a.Ler("total", Cancelamento));
        Assert.Equal(0, await b.Ler("ausente", Cancelamento));
    }

    [Fact]
    public async Task Indisponivel_devolve_zero_sem_erro()
    {
        await using var contadores = new ContadoresRedis("127.0.0.1:1", NullLogger<ContadoresRedis>.Instance);
        Assert.Equal(0, await contadores.Somar("x", Validade, Cancelamento));
        Assert.Equal(0, await contadores.Ler("x", Cancelamento));
    }
}
