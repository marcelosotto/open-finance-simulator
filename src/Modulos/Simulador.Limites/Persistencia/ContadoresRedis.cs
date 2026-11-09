using Microsoft.Extensions.Logging;
using Simulador.Limites.Contratos;
using StackExchange.Redis;

namespace Simulador.Limites.Persistencia;

/// <summary>Contadores no Redis. Indisponível, a chamada segue como dentro dos limites (RN30).</summary>
internal sealed partial class ContadoresRedis : IContadores, IAsyncDisposable
{
    private const int TempoMaximoMs = 100;
    private const int TempoDeConexaoMs = 500;

    private const string Script = """
        local total = redis.call('INCR', KEYS[1])
        if total == 1 then redis.call('EXPIRE', KEYS[1], ARGV[1]) end
        return total
        """;

    private readonly Lazy<Task<ConnectionMultiplexer>> _conexao;
    private readonly ILogger<ContadoresRedis> _log;

    public ContadoresRedis(string conexao, ILogger<ContadoresRedis> log)
    {
        _log = log;
        _conexao = new Lazy<Task<ConnectionMultiplexer>>(() =>
        {
            var opcoes = ConfigurationOptions.Parse(conexao);
            opcoes.AbortOnConnectFail = false;
            opcoes.BacklogPolicy = BacklogPolicy.FailFast;
            opcoes.AsyncTimeout = TempoMaximoMs;
            opcoes.ConnectTimeout = TempoDeConexaoMs;
            return ConnectionMultiplexer.ConnectAsync(opcoes);
        });
    }

    public Task<long> Somar(string chave, TimeSpan validade, CancellationToken cancelamento) =>
        Executar(async banco => (long)await banco.ScriptEvaluateAsync(Script, [chave], [(long)validade.TotalSeconds]).ConfigureAwait(false));

    public Task<long> Ler(string chave, CancellationToken cancelamento) =>
        Executar(async banco => (long?)await banco.StringGetAsync(chave).ConfigureAwait(false) ?? 0);

    public async ValueTask DisposeAsync()
    {
        if (_conexao.IsValueCreated)
        {
            await (await _conexao.Value.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<long> Executar(Func<IDatabase, Task<long>> operacao)
    {
        try
        {
            var conexao = await _conexao.Value.ConfigureAwait(false);
            return await operacao(conexao.GetDatabase()).ConfigureAwait(false);
        }
        catch (Exception erro) when (erro is RedisException or TimeoutException)
        {
            Indisponivel(_log, erro);
            return 0;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Contadores indisponíveis; a chamada segue sem limite.")]
    private static partial void Indisponivel(ILogger log, Exception erro);
}
