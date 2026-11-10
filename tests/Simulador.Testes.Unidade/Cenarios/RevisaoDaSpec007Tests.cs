using Microsoft.Extensions.Time.Testing;
using Simulador.Cenarios.Aplicacao;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;
using Simulador.Consentimentos.Dominio;

namespace Simulador.Testes.Unidade.Cenarios;

/// <summary>Achados da revisão independente da spec 007, no domínio e na aplicação.</summary>
public sealed class RevisaoDaSpec007Tests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 0, 30, TimeSpan.Zero);
    private static readonly HashSet<int> Declarados = [500, 503, 504];
    private static readonly ChamadaAvaliada Chamada = new("banco", "saldos", "cli", "conta");

    /// <summary>Achado 2 (RN15): efeito de latência mantém a janela do perfil.</summary>
    [Fact]
    public void Latencia_da_regra_mantem_a_janela_do_perfil()
    {
        var perfil = new PerfilDaChamada(200, 600, 0m, 0m, new Dictionary<int, decimal>(), 2, 60);
        var decisao = AvaliacaoDoPerfil.Decidir(perfil, new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: 3000), Agora, Declarados, () => 0.5);
        Assert.Equal(503, decisao.Status);
        Assert.Equal(TimeSpan.FromSeconds(3), decisao.Espera);
    }

    /// <summary>Achado 2 (RN15): efeito de latência troca também a cauda lenta, que é latência.</summary>
    [Fact]
    public void Latencia_da_regra_troca_a_cauda_lenta()
    {
        var perfil = new PerfilDaChamada(200, 600, 100m, 0m, new Dictionary<int, decimal>());
        var decisao = AvaliacaoDoPerfil.Decidir(perfil, new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: 3000), Agora, Declarados, () => 0.5);
        Assert.Null(decisao.Status);
        Assert.Equal(TimeSpan.FromSeconds(3), decisao.Espera);
    }

    /// <summary>Achado 3 (Q17): decorrido negativo conta como zero.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(60, false)]
    public void Preparacao_com_relogio_antes_da_criacao(int duracao, bool pronta) =>
        Assert.Equal(pronta ? SituacaoDaPreparacao.Pronta : SituacaoDaPreparacao.Preparando, Preparacao.Situacao(Agora, duracao, Agora.AddMinutes(-10)));

    /// <summary>Achado 9: uma leitura por expiração, e leitura antiga não apaga a invalidação.</summary>
    [Fact]
    public async Task Cache_le_uma_vez_e_respeita_a_invalidacao()
    {
        var repositorio = new RepositorioControlado();
        var servico = new ServicoDeCenarios(repositorio, new AmbienteSimples(), new HoraReal(new FakeTimeProvider(Agora)), new GeradorAleatorioComSemente(1), new GeradorDosDefeitos(new GeradorAleatorioComSemente(2)));
        var perfil = new PerfilDaChamada(0, 0, 0m, 0m, new Dictionary<int, decimal>());

        var primeira = servico.Decidir(Chamada, perfil, Agora, Declarados, CancellationToken.None);
        var segunda = servico.Decidir(Chamada, perfil, Agora, Declarados, CancellationToken.None);
        await servico.Criar(new PedidoDeRegra(null, "banco", null, null, null, new EfeitoDaRegra(Efeitos.Status, Status: 500), null, null, null, null, null, null), null, CancellationToken.None);
        repositorio.Liberar();
        await Task.WhenAll(primeira, segunda);

        Assert.Equal(1, repositorio.LeiturasAntesDaLiberacao);
        Assert.Equal(500, (await servico.Decidir(Chamada, perfil, Agora, Declarados, CancellationToken.None)).Status);
    }

    private sealed class AmbienteSimples : IAmbienteDosCenarios
    {
        public IReadOnlySet<int> StatusDeclarados { get; } = new HashSet<int> { 500 };

        public bool OperacaoExiste(string operationId) => true;

        public IReadOnlySet<int>? StatusDeclaradosDe(string operationId) => StatusDeclarados;

        public Task<DateTimeOffset?> HoraDoBanco(string instituicao, CancellationToken cancelamento) => Task.FromResult<DateTimeOffset?>(DateTimeOffset.UnixEpoch);

        public Task<bool> InstituicaoExiste(string instituicao, CancellationToken cancelamento) => Task.FromResult(true);

        public Task<long> ProximaPosicao(string chave, CancellationToken cancelamento) => Task.FromResult(0L);

        public Task RegistrarHistorico(string alvo, string? autor, string acao, string? antes, string? depois, CancellationToken cancelamento) => Task.CompletedTask;
    }

    private sealed class RepositorioControlado : IRepositorioDeCenarios
    {
        private readonly TaskCompletionSource _liberacao = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly List<RegraDeCenario> _regras = [];

        public int LeiturasAntesDaLiberacao { get; private set; }

        public void Liberar() => _liberacao.TrySetResult();

        public async Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento)
        {
            if (!_liberacao.Task.IsCompleted)
            {
                LeiturasAntesDaLiberacao++;
            }

            IReadOnlyList<RegraDeCenario> copia = [.. _regras];
            await _liberacao.Task.ConfigureAwait(false);
            return copia;
        }

        public Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento) => Task.FromResult(_regras.FirstOrDefault(r => r.Id == id));

        public Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento)
        {
            _regras.Add(regra);
            return Task.CompletedTask;
        }

        public Task ApagarRegra(string id, CancellationToken cancelamento) => Task.CompletedTask;

        public Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<PacoteDeCenario>>([]);

        public Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento) => Task.FromResult<PacoteDeCenario?>(null);

        public Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento) => Task.CompletedTask;

        public Task ApagarPacote(string nome, CancellationToken cancelamento) => Task.CompletedTask;

        public Task<IReadOnlyList<AnomaliaDaInstituicao>> ListarAnomalias(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<AnomaliaDaInstituicao>>([]);

        public Task SalvarAnomalia(AnomaliaDaInstituicao anomalia, CancellationToken cancelamento) => Task.CompletedTask;

        public Task ApagarAnomalias(string instituicao, CancellationToken cancelamento) => Task.CompletedTask;
    }
}
