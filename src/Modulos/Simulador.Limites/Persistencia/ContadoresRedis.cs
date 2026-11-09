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

    private readonly Lazy<Task<ConnectionMultiplexer>>? _conexao;
    private readonly Func<Task<IDatabase>> _banco;
    private readonly ILogger<ContadoresRedis> _log;
    private readonly TimeProvider _relogio;

    public ContadoresRedis(string conexao, ILogger<ContadoresRedis> log, TimeProvider relogio)
        : this(log, relogio, Conectar(conexao, out var abertura), abertura)
    {
    }

    /// <summary>Com o acesso ao banco substituído, para testar quem não responde.</summary>
    internal ContadoresRedis(Func<Task<IDatabase>> banco, ILogger<ContadoresRedis> log, TimeProvider relogio)
        : this(log, relogio, banco, null)
    {
    }

    private ContadoresRedis(ILogger<ContadoresRedis> log, TimeProvider relogio, Func<Task<IDatabase>> banco, Lazy<Task<ConnectionMultiplexer>>? conexao)
    {
        _log = log;
        _relogio = relogio;
        _banco = banco;
        _conexao = conexao;
    }

    public Task<long> Somar(string chave, TimeSpan validade, CancellationToken cancelamento) =>
        Executar(async banco => (long)await banco.ScriptEvaluateAsync(Script, [chave], [(long)validade.TotalSeconds]).ConfigureAwait(false));

    public Task<long> Ler(string chave, CancellationToken cancelamento) =>
        Executar(async banco => (long?)await banco.StringGetAsync(chave).ConfigureAwait(false) ?? 0);

    public async ValueTask DisposeAsync()
    {
        if (_conexao is { IsValueCreated: true })
        {
            await (await _conexao.Value.ConfigureAwait(false)).DisposeAsync().ConfigureAwait(false);
        }
    }

    private static Func<Task<IDatabase>> Conectar(string conexao, out Lazy<Task<ConnectionMultiplexer>> abertura)
    {
        var aberta = new Lazy<Task<ConnectionMultiplexer>>(() =>
        {
            var opcoes = ConfigurationOptions.Parse(conexao);
            opcoes.AbortOnConnectFail = false;
            opcoes.BacklogPolicy = BacklogPolicy.FailFast;
            opcoes.AsyncTimeout = TempoMaximoMs;
            opcoes.ConnectTimeout = TempoDeConexaoMs;
            return ConnectionMultiplexer.ConnectAsync(opcoes);
        });
        abertura = aberta;
        return async () => (await aberta.Value.ConfigureAwait(false)).GetDatabase();
    }

    private async Task<long> Executar(Func<IDatabase, Task<long>> operacao)
    {
        try
        {
            return await Chamar(operacao).WaitAsync(TimeSpan.FromMilliseconds(TempoMaximoMs), _relogio).ConfigureAwait(false);
        }
        catch (Exception erro) when (erro is RedisException or TimeoutException)
        {
            Indisponivel(_log, erro);
            return 0;
        }
    }

    private async Task<long> Chamar(Func<IDatabase, Task<long>> operacao) =>
        await operacao(await _banco().ConfigureAwait(false)).ConfigureAwait(false);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Contadores indisponíveis; a chamada segue sem limite.")]
    private static partial void Indisponivel(ILogger log, Exception erro);
}
