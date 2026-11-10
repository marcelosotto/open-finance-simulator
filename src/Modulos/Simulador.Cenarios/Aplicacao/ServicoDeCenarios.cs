using System.Globalization;
using System.Text.Json;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;

namespace Simulador.Cenarios.Aplicacao;

/// <summary>Corpo de criação e alteração de regra.</summary>
internal sealed record PedidoDeRegra(
    string? Nome,
    string? Instituicao,
    string? Endpoint,
    string? Cliente,
    string? Recurso,
    EfeitoDaRegra? Efeito,
    decimal? FrequenciaPct,
    DateTimeOffset? Inicio,
    DateTimeOffset? Fim,
    long? Semente,
    bool? Ligada,
    string? Pacote);

internal enum TipoDeSaida
{
    Ok,
    NaoEncontrado,
    Conflito,
    Invalido,
}

internal sealed record Saida<T>(TipoDeSaida Tipo, T? Valor = default, IReadOnlyList<string>? Erros = null);

/// <summary>O que o cache guarda: regras ativas e anomalias ligadas por banco (RNF12).</summary>
internal sealed record RetratoDosCenarios(IReadOnlyList<RegraDeCenario> Regras, IReadOnlyDictionary<string, IReadOnlySet<string>> Anomalias);

/// <summary>
/// Gerador só dos sorteios de defeito sem semente: como eles correm em toda chamada que chega à etapa,
/// usar o gerador comum deslocaria o perfil, as outras regras e os identificadores (RN111, princípio VI).
/// </summary>
internal sealed class GeradorDosDefeitos(IGeradorAleatorio gerador)
{
    public IGeradorAleatorio Gerador { get; } = gerador;
}

/// <summary>Regras e pacotes (UC04), e a avaliação na chamada.</summary>
internal sealed class ServicoDeCenarios(
    IRepositorioDeCenarios repositorio,
    IAmbienteDosCenarios ambiente,
    HoraReal hora,
    IGeradorAleatorio aleatorio,
    GeradorDosDefeitos geradorDosDefeitos) : ICenarios
{
    private const int Escala = 1_000_000;
    private const string AlvoGeral = "cenarios";
    private static readonly TimeSpan ValidadeDoCache = TimeSpan.FromSeconds(5);
    private static readonly IReadOnlySet<string> Nenhuma = new HashSet<string>(StringComparer.Ordinal);

    private readonly Lock _trava = new();
    private RetratoDosCenarios? _ativas;
    private DateTimeOffset _validoAte;
    private long _geracao;
    private Task<RetratoDosCenarios>? _carga;
    private long _geracaoDaCarga = -1;
    private long _numeroDaCarga;
    private DateTimeOffset _inicioDaCarga;

    public double Sorteio() => Sortear(aleatorio);

    private static double Sortear(IGeradorAleatorio gerador) => gerador.ProximoInteiro(Escala) / (double)Escala;

    public async Task<DecisaoDeComportamento> Decidir(ChamadaAvaliada chamada, PerfilDaChamada perfil, DateTimeOffset instante, IReadOnlySet<int> declarados, CancellationToken cancelamento)
    {
        var regras = (await Ativas(cancelamento).ConfigureAwait(false)).Regras;

        // O defeito sorteia em toda chamada que chega à etapa, para a sequência não depender do perfil (RN111).
        var deDefeito = SelecaoDeRegras.VencedoraDeDefeito(regras, chamada, instante);
        var defeito = deDefeito is not null && await Dispara(deDefeito, geradorDosDefeitos.Gerador, cancelamento).ConfigureAwait(false)
            ? new DefeitoDaChamada(deDefeito.Efeito.Defeito!, deDefeito.Efeito.Campo, deDefeito.Efeito.Quantidade)
            : null;
        var vencedora = SelecaoDeRegras.Vencedora(regras, chamada, instante, declarados);
        var efeito = vencedora is not null && await Dispara(vencedora, aleatorio, cancelamento).ConfigureAwait(false) ? vencedora.Efeito : null;
        var decisao = AvaliacaoDoPerfil.Decidir(perfil, efeito, instante, declarados, Sorteio);
        return defeito is not null && decisao.Status is null && decisao.Transporte != Efeitos.ConexaoCortada ? decisao with { Defeito = defeito } : decisao;
    }

    public async Task<string?> EstadoSobreposto(ChamadaAvaliada chamada, DateTimeOffset instante, CancellationToken cancelamento) =>
        SelecaoDeRegras.VencedoraDeEstado((await Ativas(cancelamento).ConfigureAwait(false)).Regras, chamada, instante)?.Efeito.Estado;

    public async Task<IReadOnlySet<string>> AnomaliasLigadas(string instituicao, CancellationToken cancelamento) =>
        (await Ativas(cancelamento).ConfigureAwait(false)).Anomalias.GetValueOrDefault(instituicao) ?? Nenhuma;

    public async Task ApagarAnomalias(string instituicao, CancellationToken cancelamento)
    {
        await repositorio.ApagarAnomalias(instituicao, cancelamento).ConfigureAwait(false);
        Invalidar();
    }

    /// <summary>O estado de cada anomalia no banco, lido do repositório; nulo se o banco não existe (RN113).</summary>
    public async Task<IReadOnlyList<EstadoDaAnomalia>?> AnomaliasDe(string instituicao, CancellationToken cancelamento)
    {
        if (!await ambiente.InstituicaoExiste(instituicao, cancelamento).ConfigureAwait(false))
        {
            return null;
        }

        var ligadas = (await repositorio.ListarAnomalias(cancelamento).ConfigureAwait(false)).Where(a => a.Instituicao == instituicao && a.Ligada).Select(a => a.Nome).ToHashSet(StringComparer.Ordinal);
        return [.. CatalogoDeAnomalias.Itens.Select(a => new EstadoDaAnomalia(a.Nome, ligadas.Contains(a.Nome)))];
    }

    /// <summary>Liga ou desliga uma anomalia no banco, com histórico (RN113, PR02).</summary>
    public async Task<Saida<EstadoDaAnomalia>> LigarAnomalia(string instituicao, string nome, bool ligada, string? autor, CancellationToken cancelamento)
    {
        if (!CatalogoDeAnomalias.Existe(nome))
        {
            return new Saida<EstadoDaAnomalia>(TipoDeSaida.NaoEncontrado, Erros: ["anomalia não encontrada"]);
        }

        if (!await ambiente.InstituicaoExiste(instituicao, cancelamento).ConfigureAwait(false))
        {
            return new Saida<EstadoDaAnomalia>(TipoDeSaida.NaoEncontrado, Erros: ["instituição não encontrada"]);
        }

        var antes = (await repositorio.ListarAnomalias(cancelamento).ConfigureAwait(false)).FirstOrDefault(a => a.Instituicao == instituicao && a.Nome == nome);
        var depois = new AnomaliaDaInstituicao(instituicao, nome, ligada, hora.Relogio.GetUtcNow());
        await repositorio.SalvarAnomalia(depois, cancelamento).ConfigureAwait(false);
        await Registrar(instituicao, autor, ligada ? "anomalia-ligada" : "anomalia-desligada", antes, depois, cancelamento).ConfigureAwait(false);
        return new Saida<EstadoDaAnomalia>(TipoDeSaida.Ok, new EstadoDaAnomalia(nome, ligada));
    }

    public Task<IReadOnlyList<RegraDeCenario>> Regras(CancellationToken cancelamento) => repositorio.ListarRegras(cancelamento);

    public Task<RegraDeCenario?> Regra(string id, CancellationToken cancelamento) => repositorio.ObterRegra(id, cancelamento);

    public Task<IReadOnlyList<PacoteDeCenario>> Pacotes(CancellationToken cancelamento) => repositorio.ListarPacotes(cancelamento);

    public async Task<Saida<RegraDeCenario>> Criar(PedidoDeRegra pedido, string? autor, CancellationToken cancelamento)
    {
        var agora = hora.Relogio.GetUtcNow();
        return await Gravar(Montar(pedido, $"r-{aleatorio.ProximoTexto(12)}", agora, agora), null, autor, "regra-criada", cancelamento).ConfigureAwait(false);
    }

    public async Task<Saida<RegraDeCenario>> Alterar(string id, PedidoDeRegra pedido, string? autor, CancellationToken cancelamento) =>
        await repositorio.ObterRegra(id, cancelamento).ConfigureAwait(false) is { } atual
            ? await Gravar(Montar(pedido, id, atual.CriadaEm, hora.Relogio.GetUtcNow()), atual, autor, "regra-alterada", cancelamento).ConfigureAwait(false)
            : new Saida<RegraDeCenario>(TipoDeSaida.NaoEncontrado, Erros: ["regra não encontrada"]);

    public async Task<Saida<RegraDeCenario>> Ligar(string id, bool ligada, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.ObterRegra(id, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return new Saida<RegraDeCenario>(TipoDeSaida.NaoEncontrado, Erros: ["regra não encontrada"]);
        }

        var nova = atual with { Ligada = ligada, AtualizadaEm = hora.Relogio.GetUtcNow() };
        await repositorio.SalvarRegra(nova, cancelamento).ConfigureAwait(false);
        await Registrar(nova.Instituicao, autor, ligada ? "regra-ligada" : "regra-desligada", atual, nova, cancelamento).ConfigureAwait(false);
        return new Saida<RegraDeCenario>(TipoDeSaida.Ok, nova);
    }

    public async Task<bool> Apagar(string id, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.ObterRegra(id, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return false;
        }

        await repositorio.ApagarRegra(id, cancelamento).ConfigureAwait(false);
        await Registrar(atual.Instituicao, autor, "regra-apagada", atual, null, cancelamento).ConfigureAwait(false);
        return true;
    }

    public async Task<PacoteDeCenario> CriarPacote(string nome, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.ObterPacote(nome, cancelamento).ConfigureAwait(false) is { } existente)
        {
            return existente;
        }

        var pacote = new PacoteDeCenario(nome, false, hora.Relogio.GetUtcNow());
        await repositorio.SalvarPacote(pacote, cancelamento).ConfigureAwait(false);
        await Registrar(null, autor, "pacote-criado", null, pacote, cancelamento).ConfigureAwait(false);
        return pacote;
    }

    public async Task<PacoteDeCenario?> LigarPacote(string nome, bool ligado, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.ObterPacote(nome, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return null;
        }

        var novo = atual with { Ligado = ligado };
        await repositorio.SalvarPacote(novo, cancelamento).ConfigureAwait(false);
        await Registrar(null, autor, ligado ? "pacote-ligado" : "pacote-desligado", atual, novo, cancelamento).ConfigureAwait(false);
        Invalidar();
        return novo;
    }

    public async Task<TipoDeSaida> ApagarPacote(string nome, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.ObterPacote(nome, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return TipoDeSaida.NaoEncontrado;
        }

        if ((await repositorio.ListarRegras(cancelamento).ConfigureAwait(false)).Any(r => r.Pacote == nome))
        {
            return TipoDeSaida.Conflito;
        }

        await repositorio.ApagarPacote(nome, cancelamento).ConfigureAwait(false);
        await Registrar(null, autor, "pacote-apagado", atual, null, cancelamento).ConfigureAwait(false);
        Invalidar();
        return TipoDeSaida.Ok;
    }

    private static RegraDeCenario Montar(PedidoDeRegra p, string id, DateTimeOffset criadaEm, DateTimeOffset agora) => new(
        id,
        p.Nome,
        p.Instituicao,
        p.Endpoint,
        p.Cliente,
        p.Recurso,
        p.Efeito ?? new EfeitoDaRegra(string.Empty),
        p.FrequenciaPct ?? 100m,
        p.Inicio,
        p.Fim,
        p.Semente,
        p.Ligada ?? true,
        p.Pacote,
        criadaEm,
        agora);

    private async Task<Saida<RegraDeCenario>> Gravar(RegraDeCenario regra, RegraDeCenario? antes, string? autor, string acao, CancellationToken cancelamento)
    {
        List<string> erros = [.. ValidacaoDeRegra.Validar(regra, ambiente.StatusDeclarados, ambiente.OperacaoExiste)];
        if (regra.Pacote is { } pacote && await repositorio.ObterPacote(pacote, cancelamento).ConfigureAwait(false) is null)
        {
            erros.Add("pacote não encontrado");
        }

        if (erros.Count > 0)
        {
            return new Saida<RegraDeCenario>(TipoDeSaida.Invalido, Erros: erros);
        }

        await repositorio.SalvarRegra(regra, cancelamento).ConfigureAwait(false);
        await Registrar(regra.Instituicao ?? antes?.Instituicao, autor, acao, antes, regra, cancelamento).ConfigureAwait(false);
        return new Saida<RegraDeCenario>(TipoDeSaida.Ok, regra);
    }

    private async Task Registrar<T>(string? instituicao, string? autor, string acao, T? antes, T? depois, CancellationToken cancelamento)
        where T : class
    {
        await ambiente.RegistrarHistorico(instituicao ?? AlvoGeral, autor, acao, Texto(antes), Texto(depois), cancelamento).ConfigureAwait(false);
        Invalidar();
    }

    private static string? Texto<T>(T? valor)
        where T : class => valor is null ? null : JsonSerializer.Serialize(valor, JsonDosCenarios.Opcoes);

    private void Invalidar()
    {
        lock (_trava)
        {
            _ativas = null;
            _geracao++;
        }
    }

    /// <summary>
    /// Regras em cache por até 5 s desde o início da leitura (RNF12). Chamadas simultâneas esperam a mesma
    /// leitura, que guarda o resultado sozinha; quem recebe uma leitura atravessada por escrita lê de novo (R08).
    /// </summary>
    private async Task<RetratoDosCenarios> Ativas(CancellationToken cancelamento)
    {
        while (true)
        {
            var agora = hora.Relogio.GetUtcNow();
            Task<RetratoDosCenarios> carga;
            long geracao;
            lock (_trava)
            {
                if (_ativas is not null && agora < _validoAte)
                {
                    return _ativas;
                }

                if (_carga is null || _geracaoDaCarga != _geracao || agora >= _inicioDaCarga + ValidadeDoCache)
                {
                    _numeroDaCarga++;
                    _geracaoDaCarga = _geracao;
                    _inicioDaCarga = agora;
                    _carga = Carga(_numeroDaCarga, _geracao, agora);
                }

                carga = _carga;
                geracao = _geracaoDaCarga;
            }

            var ativas = await carga.WaitAsync(cancelamento).ConfigureAwait(false);
            lock (_trava)
            {
                if (geracao == _geracao)
                {
                    return ativas;
                }
            }
        }
    }

    /// <summary>Uma leitura, iniciada fora da trava; guarda o resultado se ainda for a vez dela, e se descarta ao falhar.</summary>
    private async Task<RetratoDosCenarios> Carga(long numero, long geracao, DateTimeOffset inicio)
    {
        await Task.Yield();
        try
        {
            var ativas = await Carregar().ConfigureAwait(false);
            lock (_trava)
            {
                if (numero == _numeroDaCarga && geracao == _geracao)
                {
                    _ativas = ativas;
                    _validoAte = inicio + ValidadeDoCache;
                }
            }

            return ativas;
        }
        finally
        {
            lock (_trava)
            {
                if (numero == _numeroDaCarga)
                {
                    _carga = null;
                }
            }
        }
    }

    private async Task<RetratoDosCenarios> Carregar()
    {
        var regras = await repositorio.ListarRegras(CancellationToken.None).ConfigureAwait(false);
        var ligados = (await repositorio.ListarPacotes(CancellationToken.None).ConfigureAwait(false)).Where(p => p.Ligado).Select(p => p.Nome).ToHashSet(StringComparer.Ordinal);
        var anomalias = (await repositorio.ListarAnomalias(CancellationToken.None).ConfigureAwait(false))
            .Where(a => a.Ligada)
            .GroupBy(a => a.Instituicao, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, IReadOnlySet<string> (g) => g.Select(a => a.Nome).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
        return new RetratoDosCenarios([.. regras.Where(r => r.Ligada && (r.Pacote is null || ligados.Contains(r.Pacote)))], anomalias);
    }

    private async Task<bool> Dispara(RegraDeCenario regra, IGeradorAleatorio gerador, CancellationToken cancelamento)
    {
        if (regra.FrequenciaPct >= 100)
        {
            return true;
        }

        if (regra.FrequenciaPct <= 0)
        {
            return false;
        }

        var limite = (double)regra.FrequenciaPct / 100;
        if (regra.Semente is { } semente)
        {
            var chave = string.Create(CultureInfo.InvariantCulture, $"seq:{regra.Id}:{regra.AtualizadaEm.UtcTicks}");
            var posicao = await ambiente.ProximaPosicao(chave, cancelamento).ConfigureAwait(false);
            if (posicao > 0)
            {
                return SelecaoDeRegras.SorteioDaSequencia(semente, posicao) < limite;
            }
        }

        return Sortear(gerador) < limite;
    }
}
