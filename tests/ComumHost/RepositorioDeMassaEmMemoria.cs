using Simulador.Massa.Contratos;

namespace Simulador.Testes.Host;

/// <summary>Massa em memória dos testes sem contêiner.</summary>
public sealed class RepositorioDeMassaEmMemoria : IRepositorioDeMassa
{
    private readonly Lock _trava = new();
    private readonly Dictionary<string, PerfilDeDados> _perfis = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DistribuicaoNomeada> _distribuicoes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Lote> _lotes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _cancelados = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClienteSimulado> _clientes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Consentimento> _consentimentos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Recurso> _recursos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Ajuste> _ajustes = new(StringComparer.Ordinal);
    private MundoVirtual? _mundo;
    private long _posicoes;

    /// <summary>Quando definido, cada gravação de massa espera por uma liberação do semáforo.</summary>
    public SemaphoreSlim? Portao { get; set; }

    /// <summary>Se verdadeiro, a gravação de massa falha.</summary>
    public bool FalharAoGravar { get; set; }

    public long Posicoes
    {
        get => Ler(() => _posicoes);
        set => Ler(() => _posicoes = value);
    }

    public Task Preparar(CancellationToken cancelamento) => Task.CompletedTask;

    public Task<IReadOnlyList<PerfilDeDados>> ListarPerfis(CancellationToken cancelamento) => Lista(() => _perfis.Values);

    public Task<PerfilDeDados?> ObterPerfil(string nome, CancellationToken cancelamento) => Task.FromResult(Ler(() => _perfis.GetValueOrDefault(nome)));

    public Task SalvarPerfil(PerfilDeDados perfil, CancellationToken cancelamento) => Faz(() => _perfis[perfil.Nome] = perfil);

    public Task ApagarPerfil(string nome, CancellationToken cancelamento) => Faz(() => _perfis.Remove(nome));

    public Task<bool> PerfilEmUso(string nome, CancellationToken cancelamento) => Task.FromResult(Ler(() => _recursos.Values.Any(r => r.Perfil == nome)));

    public Task<IReadOnlyList<DistribuicaoNomeada>> ListarDistribuicoes(CancellationToken cancelamento) => Lista(() => _distribuicoes.Values);

    public Task SalvarDistribuicao(DistribuicaoNomeada distribuicao, CancellationToken cancelamento) => Faz(() => _distribuicoes[distribuicao.Nome] = distribuicao);

    public Task ApagarDistribuicao(string nome, CancellationToken cancelamento) => Faz(() => _distribuicoes.Remove(nome));

    public Task<long> ReservarPosicoes(int quantidade, CancellationToken cancelamento) => Task.FromResult(Ler(() =>
    {
        var inicio = _posicoes;
        _posicoes += quantidade;
        return inicio;
    }));

    public Task DevolverPosicoes(int quantidade, CancellationToken cancelamento) => Faz(() => _posicoes -= quantidade);

    public Task InserirLote(Lote lote, CancellationToken cancelamento) => Faz(() => _lotes.Add(lote.Id, lote));

    public Task AtualizarLote(Lote lote, CancellationToken cancelamento) => Faz(() => _lotes[lote.Id] = lote);

    public Task<Lote?> ObterLote(string id, CancellationToken cancelamento) => Task.FromResult(Ler(() => _lotes.GetValueOrDefault(id)));

    public Task<IReadOnlyList<Lote>> ListarLotes(CancellationToken cancelamento) => Lista(() => _lotes.Values);

    public Task SolicitarCancelamento(string id, CancellationToken cancelamento) => Faz(() => _cancelados.Add(id));

    public Task<bool> CancelamentoSolicitado(string id, CancellationToken cancelamento) => Task.FromResult(Ler(() => _cancelados.Contains(id)));

    public async Task GravarMassa(IReadOnlyList<MassaGerada> massa, CancellationToken cancelamento)
    {
        if (Portao is { } portao)
        {
            await portao.WaitAsync(cancelamento);
        }

        if (FalharAoGravar)
        {
            throw new InvalidOperationException("Falha simulada.");
        }

        lock (_trava)
        {
            foreach (var item in massa)
            {
                _clientes.Add(item.Cliente.Documento, item.Cliente);
                item.Consentimentos.ToList().ForEach(c => _consentimentos.Add(c.ConsentId, c));
                item.Recursos.ToList().ForEach(r => _recursos.Add(r.ResourceId, r));
            }
        }
    }

    public Task SalvarConsentimento(Consentimento consentimento, CancellationToken cancelamento) => Faz(() => _consentimentos[consentimento.ConsentId] = consentimento);

    public Task SalvarRecurso(Recurso recurso, CancellationToken cancelamento) => Faz(() => _recursos[recurso.ResourceId] = recurso);

    public Task<ClienteSimulado?> ObterCliente(string documento, CancellationToken cancelamento) => Task.FromResult(Ler(() => _clientes.GetValueOrDefault(documento)));

    public Task<Consentimento?> ObterConsentimento(string consentId, CancellationToken cancelamento) => Task.FromResult(Ler(() => _consentimentos.GetValueOrDefault(consentId)));

    public Task<Recurso?> ObterRecurso(string resourceId, CancellationToken cancelamento) => Task.FromResult(Ler(() => _recursos.GetValueOrDefault(resourceId)));

    public Task<IReadOnlyList<ClienteSimulado>> ListarClientesDoLote(string id, int inicio, int quantidade, CancellationToken cancelamento) =>
        Lista(() => _clientes.Values.Where(c => c.LoteId == id).OrderBy(c => c.Documento, StringComparer.Ordinal).Skip(inicio).Take(quantidade));

    public Task<IReadOnlyList<Recurso>> ListarRecursosDoCliente(string documento, CancellationToken cancelamento) => Lista(() => _recursos.Values.Where(r => r.Cliente == documento));

    public Task<IReadOnlyList<Consentimento>> ListarConsentimentosDoCliente(string documento, CancellationToken cancelamento) => Lista(() => _consentimentos.Values.Where(c => c.Cliente == documento));

    public Task<bool> TemMassaNoBanco(string banco, CancellationToken cancelamento) =>
        Task.FromResult(Ler(() => _recursos.Values.Any(r => r.Banco == banco) || _consentimentos.Values.Any(c => c.Banco == banco)));

    public Task<ResumoDeMassa> ContarDoLote(string id, CancellationToken cancelamento) => Task.FromResult(Ler(() => new ResumoDeMassa(
        _clientes.Values.Count(c => c.LoteId == id),
        _consentimentos.Values.Count(c => c.LoteId == id),
        _recursos.Values.Count(r => r.LoteId == id),
        _ajustes.Values.Count(a => a.LoteId == id))));

    public Task<ResumoDeMassa> ContarDoBanco(string banco, CancellationToken cancelamento) => Task.FromResult(Ler(() => new ResumoDeMassa(
        SoDoBanco(banco).Count,
        _consentimentos.Values.Count(c => c.Banco == banco),
        _recursos.Values.Count(r => r.Banco == banco),
        _ajustes.Values.Count(a => a.Banco == banco && !a.DoMundo))));

    public Task<ResumoDeMassa> ContarTudo(CancellationToken cancelamento) =>
        Task.FromResult(Ler(() => new ResumoDeMassa(_clientes.Count, _consentimentos.Count, _recursos.Count, _ajustes.Count)));

    public Task ApagarLote(string id, CancellationToken cancelamento) => Faz(() =>
    {
        Remover(_ajustes, a => a.LoteId == id);
        Remover(_recursos, r => r.LoteId == id);
        Remover(_consentimentos, c => c.LoteId == id);
        Remover(_clientes, c => c.LoteId == id);
        _lotes.Remove(id);
    });

    public Task ApagarDoBanco(string banco, CancellationToken cancelamento) => Faz(() =>
    {
        var orfaos = SoDoBanco(banco);
        Remover(_ajustes, a => a.Banco == banco && !a.DoMundo);
        Remover(_recursos, r => r.Banco == banco);
        Remover(_consentimentos, c => c.Banco == banco);
        Remover(_clientes, c => orfaos.Contains(c.Documento));
    });

    public Task ApagarTudo(CancellationToken cancelamento) => Faz(() =>
    {
        _ajustes.Clear();
        _recursos.Clear();
        _consentimentos.Clear();
        _clientes.Clear();
        _lotes.Clear();
        _posicoes = 0;
    });

    public Task SalvarAjuste(Ajuste ajuste, CancellationToken cancelamento) => Faz(() => _ajustes[ajuste.Alvo] = ajuste);

    public Task<Ajuste?> ObterAjuste(string alvo, CancellationToken cancelamento) => Task.FromResult(Ler(() => _ajustes.GetValueOrDefault(alvo)));

    public Task<IReadOnlyList<Ajuste>> ListarAjustesDoRecurso(string recursoId, CancellationToken cancelamento) => Lista(() => _ajustes.Values.Where(a => a.RecursoId == recursoId));

    public Task<long> ContarAjustesDoMundo(CancellationToken cancelamento) => Task.FromResult(Ler(() => (long)_ajustes.Values.Count(a => a.DoMundo)));

    public Task ApagarAjustesDoMundo(CancellationToken cancelamento) => Faz(() => Remover(_ajustes, a => a.DoMundo));

    public Task<MundoVirtual?> ObterMundo(CancellationToken cancelamento) => Task.FromResult(Ler(() => _mundo));

    public Task SalvarMundo(MundoVirtual mundo, CancellationToken cancelamento) => Faz(() => _mundo = mundo);

    private HashSet<string> SoDoBanco(string banco)
    {
        var doBanco = Clientes(banco, igual: true);
        doBanco.ExceptWith(Clientes(banco, igual: false));
        return doBanco;
    }

    private HashSet<string> Clientes(string banco, bool igual) =>
        _recursos.Values.Where(r => r.Banco == banco == igual).Select(r => r.Cliente)
            .Concat(_consentimentos.Values.Where(c => c.Banco == banco == igual).Select(c => c.Cliente))
            .ToHashSet(StringComparer.Ordinal);

    private static void Remover<T>(Dictionary<string, T> mapa, Func<T, bool> condicao)
    {
        foreach (var chave in mapa.Where(k => condicao(k.Value)).Select(k => k.Key).ToList())
        {
            mapa.Remove(chave);
        }
    }

    private T Ler<T>(Func<T> leitura)
    {
        lock (_trava)
        {
            return leitura();
        }
    }

    private Task Faz<T>(Func<T> acao)
    {
        Ler(acao);
        return Task.CompletedTask;
    }

    private Task Faz(Action acao)
    {
        lock (_trava)
        {
            acao();
        }

        return Task.CompletedTask;
    }

    private Task<IReadOnlyList<T>> Lista<T>(Func<IEnumerable<T>> leitura) =>
        Task.FromResult<IReadOnlyList<T>>(Ler(() => leitura().ToList()));
}

/// <summary>Atalhos para a administração da massa nos testes.</summary>
public sealed class AdminDaMassa(HttpClient http)
{
    public const string Base = "/admin/massa";

    public async Task<(System.Net.HttpStatusCode Status, string Corpo)> Enviar(HttpMethod metodo, string caminho, object? corpo = null)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(Base + caminho, UriKind.Relative));
        if (corpo is not null)
        {
            requisicao.Content = System.Net.Http.Json.JsonContent.Create(corpo);
        }

        using var resposta = await http.SendAsync(requisicao);
        return (resposta.StatusCode, await resposta.Content.ReadAsStringAsync());
    }

    public async Task<T> Ler<T>(string caminho)
    {
        var (status, corpo) = await Enviar(HttpMethod.Get, caminho);
        Assert.True((int)status < 300, $"{caminho}: {status} {corpo}");
        return System.Text.Json.JsonSerializer.Deserialize<T>(corpo, JsonDaMassa.Opcoes)!;
    }

    public async Task<T> Enviar<T>(HttpMethod metodo, string caminho, object corpo)
    {
        var (status, texto) = await Enviar(metodo, caminho, corpo);
        Assert.True((int)status < 300, $"{caminho}: {status} {texto}");
        return System.Text.Json.JsonSerializer.Deserialize<T>(texto, JsonDaMassa.Opcoes)!;
    }

    public async Task<Lote> Aguardar(string id)
    {
        for (var i = 0; i < 3_000; i++)
        {
            var lote = await Ler<Lote>($"/lotes/{id}");
            if (lote.Situacao != SituacoesDeLote.EmAndamento)
            {
                return lote;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"O lote {id} não terminou.");
    }

    public async Task<Lote> GerarLote(object pedido) => await Aguardar((await Enviar<Lote>(HttpMethod.Post, "/lotes", pedido)).Id);

    public async Task<IReadOnlyList<MassaGerada>> MassaDoLote(string id)
    {
        var clientes = await Ler<IReadOnlyList<ClienteSimulado>>($"/lotes/{id}/clientes?quantidade=100000");
        var massa = new List<MassaGerada>();
        foreach (var cliente in clientes)
        {
            massa.Add(await Ler<MassaGerada>($"/clientes/{cliente.Documento}"));
        }

        return massa;
    }
}
