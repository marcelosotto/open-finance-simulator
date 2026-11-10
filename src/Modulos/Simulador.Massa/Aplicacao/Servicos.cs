using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;

namespace Simulador.Massa.Aplicacao;

internal enum TipoDeResultado
{
    Ok,
    NaoEncontrado,
    Conflito,
    Invalido,
}

internal sealed record Resultado<T>(TipoDeResultado Tipo, T? Valor = default, IReadOnlyList<string>? Erros = null);

internal static class Resultados
{
    public static Resultado<T> Ok<T>(T valor) => new(TipoDeResultado.Ok, valor);

    public static Resultado<T> NaoEncontrado<T>(string erro) => new(TipoDeResultado.NaoEncontrado, Erros: [erro]);

    public static Resultado<T> Conflito<T>(string erro) => new(TipoDeResultado.Conflito, Erros: [erro]);

    public static Resultado<T> Invalido<T>(params IReadOnlyList<string> erros) => new(TipoDeResultado.Invalido, Erros: erros);
}

internal sealed record PedidoDePerfil(int TransacoesMin, int TransacoesMax, decimal ValorMin, decimal ValorMax, decimal SaldoInicial, IReadOnlyList<string> TiposDeLancamento, IReadOnlyList<string> Produtos);

internal sealed record PedidoDeCliente(string Tipo, IReadOnlyList<string> Bancos, string? Perfil = null, string? Nome = null, long? Semente = null);

internal sealed record PedidoDeLote(
    int Quantidade,
    string? DistribuicaoNome = null,
    Distribuicao? Distribuicao = null,
    int ConsentimentosPorCliente = 1,
    IReadOnlyList<string>? Permissoes = null,
    long? Semente = null,
    int HistoricoMeses = 12);

internal sealed record PedidoDeMundo(int Total, long? Semente = null, string? DistribuicaoNome = null, Distribuicao? Distribuicao = null, bool Confirmar = false);

internal sealed record PedidoDeAjuste(string Tipo, string? RecursoId = null, DateOnly? Dia = null, int? Posicao = null, decimal? Valor = null, string? ConsentId = null, string? Estado = null);

internal sealed record PedidoDeLimpeza(string Alvo, string? Id = null, bool Confirmar = false);

internal sealed record ResultadoDaLimpeza(string Alvo, string? Id, bool Removido, long Clientes, long Consentimentos, long Recursos, long Ajustes, long Lotes, long Perfis, long Instituicoes);

/// <summary>Leituras e conferências usadas por todos os casos de uso.</summary>
internal sealed class BaseDaMassa(IRepositorioDeMassa repositorio, IAmbienteDaMassa ambiente, TimeProvider relogio)
{
    public const int HistoricoPadraoMeses = 12;
    private const string BancoPadrao = "banco-estavel";
    private const string AutorPadrao = "operador";

    public static Distribuicao DistribuicaoPadrao { get; } = new(
        new Dictionary<string, decimal>(StringComparer.Ordinal) { [BancoPadrao] = 100m },
        new Dictionary<string, decimal>(StringComparer.Ordinal) { [PerfisProntos.Padrao] = 100m });

    public DateTimeOffset Agora => relogio.GetUtcNow();

    public DateOnly Hoje => DateOnly.FromDateTime(Agora.UtcDateTime);

    public async Task<IReadOnlyDictionary<string, PerfilDeDados>> Perfis(CancellationToken cancelamento)
    {
        var criados = await repositorio.ListarPerfis(cancelamento).ConfigureAwait(false);
        return PerfisProntos.Todos.Concat(criados).ToDictionary(p => p.Nome, StringComparer.Ordinal);
    }

    /// <summary>Distribuição do pedido, salva ou padrão, já conferida contra bancos e perfis.</summary>
    public async Task<Resultado<Distribuicao>> Resolver(Distribuicao? inline, string? nome, CancellationToken cancelamento)
    {
        var distribuicao = inline;
        if (nome is not null)
        {
            var salvas = await repositorio.ListarDistribuicoes(cancelamento).ConfigureAwait(false);
            distribuicao = salvas.FirstOrDefault(d => d.Nome == nome)?.Distribuicao;
            if (distribuicao is null)
            {
                return Resultados.Invalido<Distribuicao>($"distribuição desconhecida: {nome}");
            }
        }

        distribuicao ??= DistribuicaoPadrao;
        var erros = await Conferir(distribuicao, cancelamento).ConfigureAwait(false);
        return erros.Count > 0 ? Resultados.Invalido<Distribuicao>(erros) : Resultados.Ok(distribuicao);
    }

    public async Task<IReadOnlyList<string>> Conferir(Distribuicao distribuicao, CancellationToken cancelamento)
    {
        var erros = Validacao.Validar(distribuicao).ToList();
        var bancos = await ambiente.ProdutosDosBancos(cancelamento).ConfigureAwait(false);
        var perfis = await Perfis(cancelamento).ConfigureAwait(false);
        erros.AddRange(distribuicao.Bancos.Keys.Where(b => !bancos.ContainsKey(b)).Select(b => $"banco desconhecido: {b}"));
        erros.AddRange(distribuicao.Perfis.Keys.Where(p => !perfis.ContainsKey(p)).Select(p => $"perfil desconhecido: {p}"));
        return erros;
    }

    /// <summary>Reserva posições, recusando o que passaria do saldo de documentos (RN57).</summary>
    public async Task<long?> Reservar(int quantidade, CancellationToken cancelamento)
    {
        var inicio = await repositorio.ReservarPosicoes(quantidade, cancelamento).ConfigureAwait(false);
        if (inicio + quantidade <= Documentos.CapacidadePersistida)
        {
            return inicio;
        }

        await repositorio.DevolverPosicoes(quantidade, cancelamento).ConfigureAwait(false);
        return null;
    }

    public Task Registrar(string? autor, string acao, IReadOnlyList<MudancaDaMassa> mudancas, CancellationToken cancelamento) =>
        ambiente.RegistrarHistorico(new EventoDaMassa(relogio.GetUtcNow(), Autor(autor), acao, mudancas), cancelamento);

    public static string Autor(string? autor) => string.IsNullOrWhiteSpace(autor) ? AutorPadrao : autor;
}

/// <summary>Perfis, distribuições e cliente avulso (UC05, UC06).</summary>
internal sealed class CadastroDeMassa(
    IRepositorioDeMassa repositorio,
    IAmbienteDaMassa ambiente,
    BaseDaMassa baseDaMassa,
    IConsultaDeMassa consulta,
    IGeradorAleatorio aleatorio,
    TimeProvider relogio)
{
    public async Task<IReadOnlyList<PerfilDeDados>> Perfis(CancellationToken cancelamento) =>
        [.. (await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false)).Values];

    public async Task<Resultado<PerfilDeDados>> SalvarPerfil(string nome, PedidoDePerfil pedido, string? autor, CancellationToken cancelamento)
    {
        if (PerfisProntos.Eh(nome))
        {
            return Resultados.Invalido<PerfilDeDados>("perfil pronto não pode ser alterado");
        }

        var perfil = new PerfilDeDados(nome, pedido.TransacoesMin, pedido.TransacoesMax, pedido.ValorMin, pedido.ValorMax, pedido.SaldoInicial, pedido.TiposDeLancamento, pedido.Produtos);
        var erros = Validacao.Validar(perfil);
        if (erros.Count > 0)
        {
            return Resultados.Invalido<PerfilDeDados>(erros);
        }

        await repositorio.SalvarPerfil(perfil, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "perfil-salvo", [new MudancaDaMassa("perfil", null, nome)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(perfil);
    }

    public async Task<Resultado<bool>> ApagarPerfil(string nome, string? autor, CancellationToken cancelamento)
    {
        if (PerfisProntos.Eh(nome))
        {
            return Resultados.Conflito<bool>("perfil pronto não pode ser apagado");
        }

        if (await repositorio.ObterPerfil(nome, cancelamento).ConfigureAwait(false) is null)
        {
            return Resultados.NaoEncontrado<bool>("perfil não encontrado");
        }

        if (await EmUso(nome, cancelamento).ConfigureAwait(false))
        {
            return Resultados.Conflito<bool>("perfil em uso não pode ser apagado");
        }

        await repositorio.ApagarPerfil(nome, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "perfil-apagado", [new MudancaDaMassa("perfil", nome, null)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(true);
    }

    public Task<IReadOnlyList<DistribuicaoNomeada>> Distribuicoes(CancellationToken cancelamento) => repositorio.ListarDistribuicoes(cancelamento);

    public async Task<Resultado<DistribuicaoNomeada>> SalvarDistribuicao(string nome, Distribuicao distribuicao, string? autor, CancellationToken cancelamento)
    {
        var erros = await baseDaMassa.Conferir(distribuicao, cancelamento).ConfigureAwait(false);
        if (erros.Count > 0)
        {
            return Resultados.Invalido<DistribuicaoNomeada>(erros);
        }

        var salva = new DistribuicaoNomeada(nome, distribuicao);
        await repositorio.SalvarDistribuicao(salva, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "distribuicao-salva", [new MudancaDaMassa("distribuicao", null, nome)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(salva);
    }

    public async Task<Resultado<bool>> ApagarDistribuicao(string nome, string? autor, CancellationToken cancelamento)
    {
        var salvas = await repositorio.ListarDistribuicoes(cancelamento).ConfigureAwait(false);
        if (salvas.All(d => d.Nome != nome))
        {
            return Resultados.NaoEncontrado<bool>("distribuição não encontrada");
        }

        await repositorio.ApagarDistribuicao(nome, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "distribuicao-apagada", [new MudancaDaMassa("distribuicao", nome, null)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(true);
    }

    public async Task<Resultado<MassaGerada>> CriarCliente(PedidoDeCliente pedido, string? autor, CancellationToken cancelamento)
    {
        var erros = new List<string>();
        if (pedido.Tipo is not (TiposDePessoa.Pf or TiposDePessoa.Pj))
        {
            erros.Add("tipo precisa ser PF ou PJ");
        }

        var perfil = pedido.Perfil ?? PerfisProntos.Padrao;
        var perfis = await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false);
        if (!perfis.ContainsKey(perfil))
        {
            erros.Add($"perfil desconhecido: {perfil}");
        }

        var bancos = await ambiente.ProdutosDosBancos(cancelamento).ConfigureAwait(false);
        if (pedido.Bancos.Count == 0)
        {
            erros.Add("informe pelo menos um banco");
        }

        erros.AddRange(pedido.Bancos.Where(b => !bancos.ContainsKey(b)).Select(b => $"banco desconhecido: {b}"));
        if (erros.Count > 0)
        {
            return Resultados.Invalido<MassaGerada>(erros);
        }

        if (await baseDaMassa.Reservar(1, cancelamento).ConfigureAwait(false) is not { } posicao)
        {
            return Resultados.Invalido<MassaGerada>("saldo de documentos esgotado");
        }

        var unicos = pedido.Bancos.Distinct(StringComparer.Ordinal).ToList();
        var parametros = new ParametrosDeGeracao(
            pedido.Semente ?? aleatorio.ProximoInteiro(int.MaxValue),
            Naturezas.Persistido,
            new Distribuicao(new Dictionary<string, decimal>(StringComparer.Ordinal) { [unicos[0]] = 100m }, new Dictionary<string, decimal>(StringComparer.Ordinal) { [perfil] = 100m }),
            perfis,
            bancos,
            ConsentimentosPorCliente: 0,
            Permissoes: null,
            BaseDaMassa.HistoricoPadraoMeses,
            relogio.GetUtcNow(),
            LoteId: null);
        var massa = new GeradorDeMassa(parametros, ambiente).GerarAvulso(posicao, pedido.Tipo, perfil, pedido.Nome, unicos);
        await repositorio.GravarMassa([massa], cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "cliente-criado", [new MudancaDaMassa("cliente", null, massa.Cliente.Documento)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(massa);
    }

    public Task<MassaGerada?> ObterCliente(string documento, CancellationToken cancelamento) => consulta.ObterCliente(documento, cancelamento);

    public Task<IReadOnlyList<EventoDaMassa>> Historico(CancellationToken cancelamento) => ambiente.LerHistorico(cancelamento);

    private async Task<bool> EmUso(string nome, CancellationToken cancelamento)
    {
        var distribuicoes = await repositorio.ListarDistribuicoes(cancelamento).ConfigureAwait(false);
        var lotes = await repositorio.ListarLotes(cancelamento).ConfigureAwait(false);
        var mundo = await repositorio.ObterMundo(cancelamento).ConfigureAwait(false);
        return distribuicoes.Any(d => d.Distribuicao.Perfis.ContainsKey(nome))
            || lotes.Any(l => l.Distribuicao.Perfis.ContainsKey(nome))
            || mundo?.Distribuicao.Perfis.ContainsKey(nome) == true
            || await repositorio.PerfilEmUso(nome, cancelamento).ConfigureAwait(false);
    }
}

/// <summary>Fila dos lotes aceitos, consumida em segundo plano.</summary>
internal sealed class FilaDeLotes
{
    private readonly Channel<string> _canal = Channel.CreateUnbounded<string>();

    public bool Enfileirar(string id) => _canal.Writer.TryWrite(id);

    public IAsyncEnumerable<string> Ler(CancellationToken cancelamento) => _canal.Reader.ReadAllAsync(cancelamento);
}

/// <summary>Aceita, consulta e cancela lotes (UC07).</summary>
internal sealed class ServicoDeLotes(
    IRepositorioDeMassa repositorio,
    BaseDaMassa baseDaMassa,
    FilaDeLotes fila,
    IGeradorAleatorio aleatorio,
    TimeProvider relogio)
{
    public async Task<Resultado<Lote>> Criar(PedidoDeLote pedido, string? autor, CancellationToken cancelamento)
    {
        var erros = new List<string>();
        if (pedido.Quantidade <= 0)
        {
            erros.Add("quantidade precisa ser positiva");
        }

        if (pedido.ConsentimentosPorCliente <= 0)
        {
            erros.Add("consentimentos por cliente precisa ser positivo");
        }

        if (pedido.HistoricoMeses <= 0)
        {
            erros.Add("histórico precisa ser positivo");
        }

        erros.AddRange((pedido.Permissoes ?? []).Where(p => !Permissoes.Todas.Contains(p)).Select(p => $"permissão desconhecida: {p}"));
        var distribuicao = await baseDaMassa.Resolver(pedido.Distribuicao, pedido.DistribuicaoNome, cancelamento).ConfigureAwait(false);
        erros.AddRange(distribuicao.Erros ?? []);
        if (erros.Count > 0)
        {
            return Resultados.Invalido<Lote>(erros);
        }

        if (await baseDaMassa.Reservar(pedido.Quantidade, cancelamento).ConfigureAwait(false) is not { } posicao)
        {
            return Resultados.Invalido<Lote>("quantidade acima do saldo de documentos");
        }

        var agora = relogio.GetUtcNow();
        var lote = new Lote(
            $"lote-{Base36.Codificar(posicao)}",
            pedido.Quantidade,
            distribuicao.Valor!,
            pedido.ConsentimentosPorCliente,
            pedido.Permissoes,
            pedido.Semente ?? aleatorio.ProximoInteiro(int.MaxValue),
            GeradorDeMassa.Versao,
            pedido.HistoricoMeses,
            posicao,
            agora,
            Processados: 0,
            SituacoesDeLote.EmAndamento,
            Motivo: null,
            Consentimentos: 0,
            Recursos: 0,
            agora);
        await repositorio.InserirLote(lote, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "lote-criado", [new MudancaDaMassa("lote", null, lote.Id)], cancelamento).ConfigureAwait(false);
        fila.Enfileirar(lote.Id);
        return Resultados.Ok(lote);
    }

    public Task<Lote?> Obter(string id, CancellationToken cancelamento) => repositorio.ObterLote(id, cancelamento);

    public Task<IReadOnlyList<Lote>> Listar(CancellationToken cancelamento) => repositorio.ListarLotes(cancelamento);

    public Task<IReadOnlyList<ClienteSimulado>> Clientes(string id, int inicio, int quantidade, CancellationToken cancelamento) => repositorio.ListarClientesDoLote(id, inicio, quantidade, cancelamento);

    public async Task<Resultado<Lote>> Cancelar(string id, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.ObterLote(id, cancelamento).ConfigureAwait(false) is not { } lote)
        {
            return Resultados.NaoEncontrado<Lote>("lote não encontrado");
        }

        if (lote.Situacao != SituacoesDeLote.EmAndamento)
        {
            return Resultados.Conflito<Lote>("lote já terminou");
        }

        await repositorio.SolicitarCancelamento(id, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "lote-cancelado", [new MudancaDaMassa("lote", lote.Id, null)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(lote);
    }
}

/// <summary>Gera os lotes em blocos, em segundo plano (research R07).</summary>
internal sealed class ExecutorDeLotes(
    FilaDeLotes fila,
    IRepositorioDeMassa repositorio,
    IAmbienteDaMassa ambiente,
    BaseDaMassa baseDaMassa,
    TimeProvider relogio) : BackgroundService
{
    private const int TamanhoDoBloco = 500;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var id in fila.Ler(stoppingToken).ConfigureAwait(false))
        {
            await Processar(id, stoppingToken).ConfigureAwait(false);
        }
    }

#pragma warning disable CA1031 // Qualquer falha encerra o lote como parcial, sem derrubar a fila.
    private async Task Processar(string id, CancellationToken cancelamento)
    {
        if (await repositorio.ObterLote(id, cancelamento).ConfigureAwait(false) is not { } lote)
        {
            return;
        }

        try
        {
            await Gerar(lote, cancelamento).ConfigureAwait(false);
        }
        catch (Exception erro) when (erro is not OperationCanceledException)
        {
            var atual = await repositorio.ObterLote(id, CancellationToken.None).ConfigureAwait(false) ?? lote;
            await repositorio.AtualizarLote(atual with { Situacao = SituacoesDeLote.Parcial, Motivo = "falha", AtualizadoEm = relogio.GetUtcNow() }, CancellationToken.None).ConfigureAwait(false);
        }
    }
#pragma warning restore CA1031

    private async Task Gerar(Lote lote, CancellationToken cancelamento)
    {
        var parametros = new ParametrosDeGeracao(
            lote.Semente,
            Naturezas.Persistido,
            lote.Distribuicao,
            await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false),
            await ambiente.ProdutosDosBancos(cancelamento).ConfigureAwait(false),
            lote.ConsentimentosPorCliente,
            lote.Permissoes,
            lote.HistoricoMeses,
            lote.Data,
            lote.Id);
        var gerador = new GeradorDeMassa(parametros, ambiente);

        var atual = lote;
        while (atual.Processados < atual.Quantidade)
        {
            if (await repositorio.CancelamentoSolicitado(atual.Id, cancelamento).ConfigureAwait(false))
            {
                await repositorio.AtualizarLote(atual with { Situacao = SituacoesDeLote.Parcial, Motivo = "cancelado", AtualizadoEm = relogio.GetUtcNow() }, cancelamento).ConfigureAwait(false);
                return;
            }

            var feitos = atual.Processados;
            var bloco = Enumerable.Range(feitos, Math.Min(TamanhoDoBloco, atual.Quantidade - feitos))
                .Select(i => gerador.Gerar(i, atual.PosicaoInicial + i))
                .ToList();
            await repositorio.GravarMassa(bloco, cancelamento).ConfigureAwait(false);
            atual = atual with
            {
                Processados = feitos + bloco.Count,
                Consentimentos = atual.Consentimentos + bloco.Sum(m => m.Consentimentos.Count),
                Recursos = atual.Recursos + bloco.Sum(m => m.Recursos.Count),
                AtualizadoEm = relogio.GetUtcNow(),
            };
            await repositorio.AtualizarLote(atual, cancelamento).ConfigureAwait(false);
        }

        await repositorio.AtualizarLote(atual with { Situacao = SituacoesDeLote.Concluido }, cancelamento).ConfigureAwait(false);
    }
}

/// <summary>Prepara os índices e encerra os lotes que uma queda deixou em andamento (research R07).</summary>
internal sealed class InicioDaMassa(IRepositorioDeMassa repositorio, TimeProvider relogio) : IHostedService
{
    private static readonly TimeSpan Silencio = TimeSpan.FromMinutes(2);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await repositorio.Preparar(cancellationToken).ConfigureAwait(false);
        var agora = relogio.GetUtcNow();
        foreach (var lote in await repositorio.ListarLotes(cancellationToken).ConfigureAwait(false))
        {
            if (lote.Situacao == SituacoesDeLote.EmAndamento && agora - lote.AtualizadoEm > Silencio)
            {
                await repositorio.AtualizarLote(lote with { Situacao = SituacoesDeLote.Parcial, Motivo = "interrompido", AtualizadoEm = agora }, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Massa persistida e virtual, já com ajustes; o que as specs 004 e 005 consultam (FR-019).</summary>
internal sealed class ConsultaDeMassa(IRepositorioDeMassa repositorio, IAmbienteDaMassa ambiente, BaseDaMassa baseDaMassa, TimeProvider relogio) : IConsultaDeMassa
{
    private const string PrefixoConsentimento = "consentimento:";

    public static string AlvoDoConsentimento(string consentId) => PrefixoConsentimento + consentId;

    public static string AlvoDoRecurso(string resourceId) => "recurso:" + resourceId;

    public async Task<MassaGerada?> ObterCliente(string documento, CancellationToken cancelamento)
    {
        if (await repositorio.ObterCliente(documento, cancelamento).ConfigureAwait(false) is { } cliente)
        {
            return new MassaGerada(
                cliente,
                await repositorio.ListarConsentimentosDoCliente(documento, cancelamento).ConfigureAwait(false),
                await repositorio.ListarRecursosDoCliente(documento, cancelamento).ConfigureAwait(false));
        }

        return Documentos.IndiceVirtual(documento) is { } indice && await ItemVirtual(indice, cancelamento).ConfigureAwait(false) is { } item ? item : null;
    }

    public async Task<Consentimento?> ObterConsentimento(string consentId, CancellationToken cancelamento)
    {
        if (IdsVirtuais.Consentimento(consentId) is not { } referencia)
        {
            return await repositorio.ObterConsentimento(consentId, cancelamento).ConfigureAwait(false);
        }

        var item = await ItemVirtual(referencia.Indice, cancelamento).ConfigureAwait(false);
        return item?.Consentimentos.FirstOrDefault(c => c.ConsentId == consentId);
    }

    public async Task<Recurso?> ObterRecurso(string resourceId, CancellationToken cancelamento)
    {
        if (IdsVirtuais.Recurso(resourceId) is not { } referencia)
        {
            return await repositorio.ObterRecurso(resourceId, cancelamento).ConfigureAwait(false);
        }

        var item = await ItemVirtual(referencia.Indice, cancelamento).ConfigureAwait(false);
        return item?.Recursos.FirstOrDefault(r => r.ResourceId == resourceId);
    }

    public Task<IReadOnlyList<Transacao>?> Transacoes(string resourceId, DateOnly de, DateOnly ate, CancellationToken cancelamento) =>
        TransacoesEm(resourceId, de, ate, baseDaMassa.Agora, null, cancelamento);

    public async Task<IReadOnlyList<Transacao>?> TransacoesEm(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, DateTimeOffset? corte, CancellationToken cancelamento)
    {
        if (await ObterRecurso(resourceId, cancelamento).ConfigureAwait(false) is not { } recurso)
        {
            return null;
        }

        var perfis = await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false);
        return CalculoDeTransacoes.Transacoes(recurso, perfis[recurso.Perfil], de, ate, DateOnly.FromDateTime(instante.UtcDateTime), await Ajustes(resourceId, cancelamento, corte).ConfigureAwait(false));
    }

    public async Task<decimal?> Saldo(string resourceId, CancellationToken cancelamento)
    {
        if (await ObterRecurso(resourceId, cancelamento).ConfigureAwait(false) is not { } recurso)
        {
            return null;
        }

        var perfis = await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false);
        return CalculoDeTransacoes.Saldo(recurso, perfis[recurso.Perfil], baseDaMassa.Hoje, await Ajustes(resourceId, cancelamento).ConfigureAwait(false));
    }

    public async Task<bool> BancoTemClientes(string banco, CancellationToken cancelamento)
    {
        var mundo = await repositorio.ObterMundo(cancelamento).ConfigureAwait(false);
        return mundo?.Distribuicao.Bancos.GetValueOrDefault(banco) > 0
            || await repositorio.TemMassaNoBanco(banco, cancelamento).ConfigureAwait(false);
    }

    /// <summary>Gerador do mundo atual; nulo se o mundo não foi definido.</summary>
    public async Task<(GeradorDeMassa Gerador, MundoVirtual Mundo)?> GeradorDoMundo(CancellationToken cancelamento)
    {
        if (await repositorio.ObterMundo(cancelamento).ConfigureAwait(false) is not { } mundo)
        {
            return null;
        }

        var parametros = new ParametrosDeGeracao(
            mundo.Semente,
            Naturezas.Virtual,
            mundo.Distribuicao,
            await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false),
            await ambiente.ProdutosDosBancos(cancelamento).ConfigureAwait(false),
            ConsentimentosPorCliente: 1,
            Permissoes: null,
            BaseDaMassa.HistoricoPadraoMeses,
            relogio.GetUtcNow(),
            LoteId: null);
        return (new GeradorDeMassa(parametros, ambiente), mundo);
    }

    /// <summary>Aplica ao item os ajustes gravados (RN53).</summary>
    public async Task<MassaGerada> ComAjustes(MassaGerada item, CancellationToken cancelamento)
    {
        var consentimentos = new List<Consentimento>();
        foreach (var consentimento in item.Consentimentos)
        {
            var ajuste = await repositorio.ObterAjuste(AlvoDoConsentimento(consentimento.ConsentId), cancelamento).ConfigureAwait(false);
            consentimentos.Add(ajuste is null ? consentimento : consentimento with
            {
                Estado = ajuste.Estado ?? consentimento.Estado,
                RejeitadoPor = ajuste.RejeitadoPor,
                Razao = ajuste.Razao,
                ExpiraEm = ajuste.ExpiraEm ?? consentimento.ExpiraEm,
                AtualizadoEm = ajuste.AtualizadoEm ?? consentimento.AtualizadoEm,
                Extensoes = ajuste.Extensoes ?? consentimento.Extensoes,
                Versao = ajuste.Versao,
            });
        }

        var recursos = new List<Recurso>();
        foreach (var recurso in item.Recursos)
        {
            var ajuste = await repositorio.ObterAjuste(AlvoDoRecurso(recurso.ResourceId), cancelamento).ConfigureAwait(false);
            recursos.Add(ajuste?.Estado is { } estado ? recurso with { Estado = estado, Versao = ajuste.Versao } : recurso);
        }

        return item with { Consentimentos = consentimentos, Recursos = recursos };
    }

    private async Task<MassaGerada?> ItemVirtual(long indice, CancellationToken cancelamento)
    {
        if (await GeradorDoMundo(cancelamento).ConfigureAwait(false) is not { } atual || indice >= atual.Mundo.Total)
        {
            return null;
        }

        return await ComAjustes(atual.Gerador.Gerar(indice, indice), cancelamento).ConfigureAwait(false);
    }

    private async Task<IReadOnlyDictionary<string, decimal>> Ajustes(string resourceId, CancellationToken cancelamento, DateTimeOffset? ate = null) =>
        (await repositorio.ListarAjustesDoRecurso(resourceId, cancelamento).ConfigureAwait(false))
            .Select(a => (a.Alvo, Valor: ate is { } corte ? NoInstante.Valor(a, corte) : a.Valor))
            .Where(a => a.Valor is not null)
            .ToDictionary(a => a.Alvo, a => a.Valor!.Value, StringComparer.Ordinal);
}

/// <summary>Definição e faixas do mundo virtual (UC08).</summary>
internal sealed class ServicoDoMundo(IRepositorioDeMassa repositorio, BaseDaMassa baseDaMassa, ConsultaDeMassa consulta, IGeradorAleatorio aleatorio)
{
    public Task<MundoVirtual?> Obter(CancellationToken cancelamento) => repositorio.ObterMundo(cancelamento);

    public async Task<Resultado<MundoVirtual>> Definir(PedidoDeMundo pedido, string? autor, CancellationToken cancelamento)
    {
        var erros = new List<string>();
        if (pedido.Total <= 0 || pedido.Total > Documentos.CapacidadeVirtual)
        {
            erros.Add("total precisa estar entre 1 e 50.000.000");
        }

        var distribuicao = await baseDaMassa.Resolver(pedido.Distribuicao, pedido.DistribuicaoNome, cancelamento).ConfigureAwait(false);
        erros.AddRange(distribuicao.Erros ?? []);
        if (erros.Count > 0)
        {
            return Resultados.Invalido<MundoVirtual>(erros);
        }

        var atual = await repositorio.ObterMundo(cancelamento).ConfigureAwait(false);
        var novo = new MundoVirtual(pedido.Total, pedido.Semente ?? atual?.Semente ?? aleatorio.ProximoInteiro(int.MaxValue), distribuicao.Valor!);
        var refaz = atual is not null && (atual.Semente != novo.Semente || !Iguais(atual.Distribuicao, novo.Distribuicao));
        if (refaz)
        {
            if (!pedido.Confirmar)
            {
                var ajustes = await repositorio.ContarAjustesDoMundo(cancelamento).ConfigureAwait(false);
                return Resultados.Conflito<MundoVirtual>($"mudar a semente ou a distribuição apaga {ajustes} ajuste(s) do mundo virtual; confirme para seguir");
            }

            await repositorio.ApagarAjustesDoMundo(cancelamento).ConfigureAwait(false);
        }

        await repositorio.SalvarMundo(novo, cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "mundo-definido", [new MudancaDaMassa("semente", atual?.Semente.ToString(System.Globalization.CultureInfo.InvariantCulture), novo.Semente.ToString(System.Globalization.CultureInfo.InvariantCulture))], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(novo);
    }

    /// <summary>Calcula a faixa um item por vez, sem carregar tudo na memória (RN52).</summary>
    public async IAsyncEnumerable<MassaGerada> Itens(
        (GeradorDeMassa Gerador, MundoVirtual Mundo) atual,
        long inicio,
        long quantidade,
        string? banco,
        [EnumeratorCancellation] CancellationToken cancelamento)
    {
        var comAjustes = await repositorio.ContarAjustesDoMundo(cancelamento).ConfigureAwait(false) > 0;
        for (var n = inicio; n < inicio + quantidade; n++)
        {
            long? indice = banco is null ? n : atual.Gerador.IndiceNoBanco(banco, n);
            if (indice is not { } i || i >= atual.Mundo.Total)
            {
                yield break;
            }

            var item = atual.Gerador.Gerar(i, i);
            yield return comAjustes ? await consulta.ComAjustes(item, cancelamento).ConfigureAwait(false) : item;
        }
    }

    private static bool Iguais(Distribuicao a, Distribuicao b) =>
        Iguais(a.Bancos, b.Bancos) && Iguais(a.Perfis, b.Perfis) && Iguais(a.Produtos, b.Produtos) && a.PjPct == b.PjPct;

    private static bool Iguais(IReadOnlyDictionary<string, decimal>? a, IReadOnlyDictionary<string, decimal>? b) =>
        (a ?? new Dictionary<string, decimal>()).OrderBy(k => k.Key, StringComparer.Ordinal)
            .SequenceEqual((b ?? new Dictionary<string, decimal>()).OrderBy(k => k.Key, StringComparer.Ordinal));
}

/// <summary>Ajustes sobre dado calculado (RN53).</summary>
internal sealed class ServicoDeAjustes(IRepositorioDeMassa repositorio, BaseDaMassa baseDaMassa, ConsultaDeMassa consulta, IAmbienteDaMassa ambiente, HoraReal hora)
{
    public async Task<Resultado<Ajuste>> Criar(PedidoDeAjuste pedido, string? autor, CancellationToken cancelamento)
    {
        var resultado = pedido.Tipo switch
        {
            "transacao" => await DeTransacao(pedido, cancelamento).ConfigureAwait(false),
            "consentimento" => await DeConsentimento(pedido, cancelamento).ConfigureAwait(false),
            _ => Resultados.Invalido<Ajuste>("tipo precisa ser transacao ou consentimento"),
        };
        if (resultado.Tipo == TipoDeResultado.Ok)
        {
            var ajuste = resultado.Valor!;
            if (pedido.Tipo != "consentimento")
            {
                await repositorio.SalvarAjuste(ajuste, cancelamento).ConfigureAwait(false);
            }
            else if (!await repositorio.SalvarAjusteSeVersao(ajuste, ajuste.Versao - 1, cancelamento).ConfigureAwait(false))
            {
                return Resultados.Conflito<Ajuste>("consentimento em alteração");
            }

            await baseDaMassa.Registrar(autor, "ajuste-gravado", [new MudancaDaMassa("ajuste", null, resultado.Valor!.Alvo)], cancelamento).ConfigureAwait(false);
        }

        return resultado;
    }

    private async Task<Resultado<Ajuste>> DeTransacao(PedidoDeAjuste pedido, CancellationToken cancelamento)
    {
        if (pedido.RecursoId is null || pedido.Dia is not { } dia || pedido.Posicao is not { } posicao || pedido.Valor is not > 0m)
        {
            return Resultados.Invalido<Ajuste>("informe recursoId, dia, posicao e um valor positivo");
        }

        var recurso = await consulta.ObterRecurso(pedido.RecursoId, cancelamento).ConfigureAwait(false);
        var transacoes = await consulta.Transacoes(pedido.RecursoId, dia, dia, cancelamento).ConfigureAwait(false);
        if (recurso is null || transacoes?.All(t => t.Posicao != posicao) != false)
        {
            return Resultados.NaoEncontrado<Ajuste>("transação não encontrada");
        }

        var chave = CalculoDeTransacoes.Chave(recurso.ResourceId, dia, posicao);
        // A hora do banco é a exibida; a hora real decide a visibilidade nas páginas seguintes (RN37).
        var agora = await ambiente.AgoraNoBanco(recurso.Banco, cancelamento).ConfigureAwait(false);
        var real = hora.Relogio.GetUtcNow();
        var anterior = await repositorio.ObterAjuste(chave, cancelamento).ConfigureAwait(false);
        var historico = anterior is { Valor: { } valor, AtualizadoEm: { } desde }
            ? [.. anterior.ValoresAnteriores ?? [], new ValorAnterior(valor, desde, agora, anterior.AtualizadoEmReal ?? desde, real)]
            : anterior?.ValoresAnteriores;
        return Resultados.Ok(new Ajuste(chave, pedido.Tipo, recurso.Natureza == Naturezas.Virtual, Valor: pedido.Valor, RecursoId: recurso.ResourceId, Banco: recurso.Banco, LoteId: recurso.LoteId, AtualizadoEm: agora, ValoresAnteriores: historico, AtualizadoEmReal: real));
    }

    private async Task<Resultado<Ajuste>> DeConsentimento(PedidoDeAjuste pedido, CancellationToken cancelamento)
    {
        if (pedido.ConsentId is null || pedido.Estado != "REJECTED")
        {
            return Resultados.Invalido<Ajuste>("informe consentId e o estado REJECTED");
        }

        if (await consulta.ObterConsentimento(pedido.ConsentId, cancelamento).ConfigureAwait(false) is not { } consentimento)
        {
            return Resultados.NaoEncontrado<Ajuste>("consentimento não encontrado");
        }

        if (consentimento.Natureza != Naturezas.Virtual)
        {
            return Resultados.Invalido<Ajuste>("só consentimento virtual recebe ajuste");
        }

        return Resultados.Ok(new Ajuste(ConsultaDeMassa.AlvoDoConsentimento(consentimento.ConsentId), pedido.Tipo, DoMundo: true, Estado: pedido.Estado, RejeitadoPor: "USER", Banco: consentimento.Banco, Razao: "CUSTOMER_MANUALLY_REVOKED", ExpiraEm: consentimento.ExpiraEm, AtualizadoEm: await ambiente.AgoraNoBanco(consentimento.Banco, cancelamento).ConfigureAwait(false), Extensoes: consentimento.Extensoes, Versao: consentimento.Versao + 1));
    }
}

/// <summary>Mudanças pedidas pela spec 004 sobre consentimentos e recursos.</summary>
internal sealed class EscritaDeMassa(
    IRepositorioDeMassa repositorio,
    IAmbienteDaMassa ambiente,
    BaseDaMassa baseDaMassa,
    ConsultaDeMassa consulta,
    TimeProvider relogio) : IEscritaDeMassa
{
    public async Task<string?> NovoConsentId(CancellationToken cancelamento) =>
        await baseDaMassa.Reservar(1, cancelamento).ConfigureAwait(false) is { } posicao
            ? ambiente.ConsentId(Naturezas.Persistido, $"c{Base36.Codificar(posicao)}x0")
            : null;

    public Task InserirConsentimento(Consentimento consentimento, CancellationToken cancelamento) =>
        repositorio.SalvarConsentimento(consentimento, cancelamento);

    public Task<bool> AtualizarConsentimento(Consentimento consentimento, CancellationToken cancelamento) =>
        consentimento.Natureza == Naturezas.Virtual
            ? repositorio.SalvarAjusteSeVersao(
                new Ajuste(
                    ConsultaDeMassa.AlvoDoConsentimento(consentimento.ConsentId),
                    "consentimento",
                    DoMundo: true,
                    Estado: consentimento.Estado,
                    RejeitadoPor: consentimento.RejeitadoPor,
                    Banco: consentimento.Banco,
                    Razao: consentimento.Razao,
                    ExpiraEm: consentimento.ExpiraEm,
                    AtualizadoEm: consentimento.AtualizadoEm,
                    Extensoes: consentimento.Extensoes,
                    Versao: consentimento.Versao + 1),
                consentimento.Versao,
                cancelamento)
            : repositorio.SalvarConsentimentoSeVersao(consentimento with { Versao = consentimento.Versao + 1 }, consentimento.Versao, cancelamento);

    public Task<bool> MudarEstadoDoRecurso(Recurso recurso, string estado, CancellationToken cancelamento) =>
        recurso.Natureza == Naturezas.Virtual
            ? repositorio.SalvarAjusteSeVersao(new Ajuste(ConsultaDeMassa.AlvoDoRecurso(recurso.ResourceId), "recurso", DoMundo: true, Estado: estado, Banco: recurso.Banco, Versao: recurso.Versao + 1), recurso.Versao, cancelamento)
            : repositorio.SalvarRecursoSeVersao(recurso with { Estado = estado, Versao = recurso.Versao + 1 }, recurso.Versao, cancelamento);

    public async Task<Recurso?> AdicionarProduto(string documento, string banco, string produto, string estado, CancellationToken cancelamento)
    {
        var massa = await consulta.ObterCliente(documento, cancelamento).ConfigureAwait(false);
        if (massa is null || massa.Cliente.Natureza == Naturezas.Virtual
            || await baseDaMassa.Reservar(1, cancelamento).ConfigureAwait(false) is not { } posicao)
        {
            return null;
        }

        var perfis = await baseDaMassa.Perfis(cancelamento).ConfigureAwait(false);
        var perfil = perfis[massa.Recursos.Count > 0 ? massa.Recursos[0].Perfil : PerfisProntos.Padrao];
        var recurso = new Recurso(
            ambiente.Identificador(Naturezas.Persistido, $"r{Base36.Codificar(posicao)}a0"),
            documento,
            banco,
            Produtos.TipoDe(produto),
            produto,
            estado,
            perfil.Nome,
            perfil.SaldoInicial,
            Sorteio.Derivar(posicao, "dados", posicao, banco, produto),
            relogio.GetUtcNow(),
            baseDaMassa.Hoje.AddMonths(-BaseDaMassa.HistoricoPadraoMeses),
            ambiente.Marca,
            Naturezas.Persistido,
            massa.Cliente.LoteId);
        await repositorio.SalvarRecurso(recurso, cancelamento).ConfigureAwait(false);
        return recurso;
    }
}

/// <summary>Limpeza com prévia (UC27).</summary>
internal sealed class ServicoDeLimpeza(IRepositorioDeMassa repositorio, IAmbienteDaMassa ambiente, BaseDaMassa baseDaMassa)
{
    public async Task<Resultado<ResultadoDaLimpeza>> Limpar(PedidoDeLimpeza pedido, string? autor, CancellationToken cancelamento)
    {
        var alvo = pedido.Alvo;
        if (alvo is not ("lote" or "instituicao" or "ajustes-mundo" or "tudo"))
        {
            return Resultados.Invalido<ResultadoDaLimpeza>("alvo precisa ser lote, instituicao, ajustes-mundo ou tudo");
        }

        var lotes = await repositorio.ListarLotes(cancelamento).ConfigureAwait(false);
        if (alvo is "lote" or "instituicao" && pedido.Id is null)
        {
            return Resultados.Invalido<ResultadoDaLimpeza>("informe o id do alvo");
        }

        var loteAlvo = lotes.FirstOrDefault(l => l.Id == pedido.Id);
        if (alvo == "lote" && loteAlvo is null)
        {
            return Resultados.NaoEncontrado<ResultadoDaLimpeza>("lote não encontrado");
        }

        var bancos = await ambiente.ProdutosDosBancos(cancelamento).ConfigureAwait(false);
        if (alvo == "instituicao" && !bancos.ContainsKey(pedido.Id!))
        {
            return Resultados.NaoEncontrado<ResultadoDaLimpeza>("instituição não encontrada");
        }

        var bloqueado = alvo == "lote" ? loteAlvo!.Situacao == SituacoesDeLote.EmAndamento : alvo != "ajustes-mundo" && lotes.Any(l => l.Situacao == SituacoesDeLote.EmAndamento);
        if (bloqueado)
        {
            return Resultados.Conflito<ResultadoDaLimpeza>("há lote em andamento; espere terminar ou cancele");
        }

        var previa = await Previa(alvo, pedido.Id, lotes.Count, cancelamento).ConfigureAwait(false);
        if (!pedido.Confirmar)
        {
            return Resultados.Ok(previa);
        }

        await Apagar(alvo, pedido.Id, BaseDaMassa.Autor(autor), cancelamento).ConfigureAwait(false);
        await baseDaMassa.Registrar(autor, "limpeza", [new MudancaDaMassa(alvo, pedido.Id, null)], cancelamento).ConfigureAwait(false);
        return Resultados.Ok(previa with { Removido = true });
    }

    private async Task<ResultadoDaLimpeza> Previa(string alvo, string? id, int lotes, CancellationToken cancelamento)
    {
        switch (alvo)
        {
            case "lote":
                return Resumo(alvo, id, await repositorio.ContarDoLote(id!, cancelamento).ConfigureAwait(false), 1);
            case "instituicao":
                return Resumo(alvo, id, await repositorio.ContarDoBanco(id!, cancelamento).ConfigureAwait(false), 0);
            case "ajustes-mundo":
                var ajustes = await repositorio.ContarAjustesDoMundo(cancelamento).ConfigureAwait(false);
                return new ResultadoDaLimpeza(alvo, id, false, 0, 0, 0, ajustes, 0, 0, 0);
            default:
                var uso = await EmUsoPeloMundo(cancelamento).ConfigureAwait(false);
                var perfis = (await repositorio.ListarPerfis(cancelamento).ConfigureAwait(false)).Count(p => !uso.Perfis.Contains(p.Nome));
                var instituicoes = await ambiente.ContarInstituicoesCriadas(uso.Bancos, cancelamento).ConfigureAwait(false);
                return Resumo(alvo, id, await repositorio.ContarTudo(cancelamento).ConfigureAwait(false), lotes) with { Perfis = perfis, Instituicoes = instituicoes };
        }
    }

    private async Task Apagar(string alvo, string? id, string autor, CancellationToken cancelamento)
    {
        switch (alvo)
        {
            case "lote":
                await repositorio.ApagarLote(id!, cancelamento).ConfigureAwait(false);
                break;
            case "instituicao":
                await repositorio.ApagarDoBanco(id!, cancelamento).ConfigureAwait(false);
                break;
            case "ajustes-mundo":
                await repositorio.ApagarAjustesDoMundo(cancelamento).ConfigureAwait(false);
                break;
            default:
                await ApagarTudo(autor, cancelamento).ConfigureAwait(false);
                break;
        }
    }

    private async Task ApagarTudo(string autor, CancellationToken cancelamento)
    {
        var uso = await EmUsoPeloMundo(cancelamento).ConfigureAwait(false);
        await repositorio.ApagarTudo(cancelamento).ConfigureAwait(false);
        foreach (var distribuicao in await repositorio.ListarDistribuicoes(cancelamento).ConfigureAwait(false))
        {
            await repositorio.ApagarDistribuicao(distribuicao.Nome, cancelamento).ConfigureAwait(false);
        }

        foreach (var perfil in (await repositorio.ListarPerfis(cancelamento).ConfigureAwait(false)).Where(p => !uso.Perfis.Contains(p.Nome)))
        {
            await repositorio.ApagarPerfil(perfil.Nome, cancelamento).ConfigureAwait(false);
        }

        await ambiente.ApagarInstituicoesCriadas(uso.Bancos, autor, cancelamento).ConfigureAwait(false);
    }

    private async Task<(IReadOnlySet<string> Bancos, IReadOnlySet<string> Perfis)> EmUsoPeloMundo(CancellationToken cancelamento)
    {
        var mundo = await repositorio.ObterMundo(cancelamento).ConfigureAwait(false);
        return (new HashSet<string>(mundo?.Distribuicao.Bancos.Keys ?? [], StringComparer.Ordinal),
            new HashSet<string>(mundo?.Distribuicao.Perfis.Keys ?? [], StringComparer.Ordinal));
    }

    private static ResultadoDaLimpeza Resumo(string alvo, string? id, ResumoDeMassa resumo, long lotes) =>
        new(alvo, id, false, resumo.Clientes, resumo.Consentimentos, resumo.Recursos, resumo.Ajustes, lotes, 0, 0);
}
