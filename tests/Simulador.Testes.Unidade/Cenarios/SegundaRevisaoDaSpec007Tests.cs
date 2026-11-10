using Microsoft.Extensions.Time.Testing;
using Simulador.Cenarios.Aplicacao;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;

namespace Simulador.Testes.Unidade.Cenarios;

/// <summary>Achados da 2ª revisão da spec 007 no cache de regras (R08, RNF12).</summary>
public sealed class SegundaRevisaoDaSpec007Tests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);
    private static readonly HashSet<int> Declarados = [500];
    private static readonly ChamadaAvaliada Chamada = new("banco", "saldos", "cli", "conta");
    private static readonly PerfilDaChamada Perfil = new(0, 0, 0m, 0m, new Dictionary<int, decimal>());

    private static RegraDeCenario Regra500 => new("r-1", null, "banco", null, null, null, new EfeitoDaRegra(Efeitos.Status, Status: 500), 100m, null, null, null, true, null, Agora, Agora);

    private static PedidoDeRegra Pedido500 => new(null, "banco", null, null, null, new EfeitoDaRegra(Efeitos.Status, Status: 500), null, null, null, null, null, null);

    private static ServicoDeCenarios Servico(IRepositorioDeCenarios repositorio, FakeTimeProvider relogio) =>
        new(repositorio, new AmbienteSimples(), new HoraReal(relogio), new GeradorAleatorioComSemente(1));

    /// <summary>Achado 1: leitura que falha não fica guardada; a chamada seguinte lê de novo, sem escrita no meio.</summary>
    [Fact]
    public async Task Leitura_que_falha_e_descartada()
    {
        var repositorio = new RepositorioControlado { FalharNaPrimeira = true };
        await repositorio.SalvarRegra(Regra500, CancellationToken.None);
        var servico = Servico(repositorio, new FakeTimeProvider(Agora));

        await Assert.ThrowsAsync<InvalidOperationException>(() => servico.Decidir(Chamada, Perfil, Agora, Declarados, CancellationToken.None));

        Assert.Equal(500, (await servico.Decidir(Chamada, Perfil, Agora, Declarados, CancellationToken.None)).Status);
    }

    /// <summary>Achado 6: com todos os que esperavam cancelados, a leitura não fica pendurada e não é servida depois dos 5 s.</summary>
    [Fact]
    public async Task Leitura_abandonada_nao_e_servida_depois_da_validade()
    {
        var relogio = new FakeTimeProvider(Agora);
        var repositorio = new RepositorioControlado { Segurar = true };
        var servico = Servico(repositorio, relogio);
        using var cancelamento = new CancellationTokenSource();

        var abandonada = servico.Decidir(Chamada, Perfil, Agora, Declarados, cancelamento.Token);
        await repositorio.Iniciada.WaitAsync(TestContext.Current.CancellationToken);
        await cancelamento.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandonada);
        await repositorio.SalvarRegra(Regra500, CancellationToken.None);
        repositorio.Liberar();
        relogio.Advance(TimeSpan.FromSeconds(6));

        Assert.Equal(500, (await servico.Decidir(Chamada, Perfil, Agora, Declarados, CancellationToken.None)).Status);
    }

    /// <summary>Achado 6: quem esperava uma leitura atravessada por escrita lê de novo, e não recebe a geração antiga.</summary>
    [Fact]
    public async Task Espera_atravessada_por_escrita_le_de_novo()
    {
        var repositorio = new RepositorioControlado { Segurar = true };
        var servico = Servico(repositorio, new FakeTimeProvider(Agora));

        var primeira = servico.Decidir(Chamada, Perfil, Agora, Declarados, CancellationToken.None);
        await repositorio.Iniciada.WaitAsync(TestContext.Current.CancellationToken);
        await servico.Criar(Pedido500, null, CancellationToken.None);
        repositorio.Liberar();

        Assert.Equal(500, (await primeira).Status);
    }

    /// <summary>Achado 8: a leitura começa fora da trava; uma escrita em outra linha não fica bloqueada por ela.</summary>
    [Fact]
    public async Task Leitura_comeca_fora_da_trava()
    {
        var repositorio = new RepositorioControlado();
        var servico = Servico(repositorio, new FakeTimeProvider(Agora));
        repositorio.Sonda = () => Task.Run(() => servico.Criar(Pedido500, null, CancellationToken.None)).Wait(TimeSpan.FromSeconds(2));

        await servico.Decidir(Chamada, Perfil, Agora, Declarados, CancellationToken.None);

        Assert.True(repositorio.TravaLivre, "a leitura começou com a trava tomada");
    }

    private sealed class AmbienteSimples : IAmbienteDosCenarios
    {
        public IReadOnlySet<int> StatusDeclarados { get; } = new HashSet<int> { 500 };

        public bool OperacaoExiste(string operationId) => true;

        public Task<bool> InstituicaoExiste(string instituicao, CancellationToken cancelamento) => Task.FromResult(true);

        public Task<long> ProximaPosicao(string chave, CancellationToken cancelamento) => Task.FromResult(0L);

        public Task RegistrarHistorico(string alvo, string? autor, string acao, string? antes, string? depois, CancellationToken cancelamento) => Task.CompletedTask;
    }

    private sealed class RepositorioControlado : IRepositorioDeCenarios
    {
        private readonly TaskCompletionSource _liberacao = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _iniciada = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<RegraDeCenario> _regras = [];
        private int _leituras;

        public bool FalharNaPrimeira { get; init; }

        public bool Segurar { get; init; }

        /// <summary>Roda na parte síncrona da primeira leitura; devolve se terminou a tempo.</summary>
        public Func<bool>? Sonda { get; set; }

        public bool TravaLivre { get; private set; }

        public Task Iniciada => _iniciada.Task;

        public void Liberar() => _liberacao.TrySetResult();

        public Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento)
        {
            var leitura = Interlocked.Increment(ref _leituras);
            if (leitura == 1 && Sonda is { } sonda)
            {
                TravaLivre = sonda();
            }

            if (leitura == 1 && FalharNaPrimeira)
            {
                return Task.FromException<IReadOnlyList<RegraDeCenario>>(new InvalidOperationException("Mongo fora"));
            }

            IReadOnlyList<RegraDeCenario> copia;
            lock (_regras)
            {
                copia = [.. _regras];
            }

            _iniciada.TrySetResult();
            return Segurar ? Esperar(copia) : Task.FromResult(copia);
        }

        public Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento)
        {
            lock (_regras)
            {
                return Task.FromResult(_regras.FirstOrDefault(r => r.Id == id));
            }
        }

        public Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento)
        {
            lock (_regras)
            {
                _regras.Add(regra);
            }

            return Task.CompletedTask;
        }

        public Task ApagarRegra(string id, CancellationToken cancelamento) => Task.CompletedTask;

        public Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<PacoteDeCenario>>([]);

        public Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento) => Task.FromResult<PacoteDeCenario?>(null);

        public Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento) => Task.CompletedTask;

        public Task ApagarPacote(string nome, CancellationToken cancelamento) => Task.CompletedTask;

        public Task<IReadOnlyList<AnomaliaDaInstituicao>> ListarAnomalias(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<AnomaliaDaInstituicao>>([]);

        public Task SalvarAnomalia(AnomaliaDaInstituicao anomalia, CancellationToken cancelamento) => Task.CompletedTask;

        private async Task<IReadOnlyList<RegraDeCenario>> Esperar(IReadOnlyList<RegraDeCenario> copia)
        {
            await _liberacao.Task.ConfigureAwait(false);
            return copia;
        }
    }
}
