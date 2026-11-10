using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Simulador.Catalogo.Contratos;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;
using Simulador.Comum.Comportamento;
using Simulador.Comum.Erros;
using Simulador.Consentimentos.Contratos;
using Simulador.Dados.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Limites.Contratos;

namespace Simulador.Host;

/// <summary>Devolve a hora do banco durante uma chamada com relógio alterado, e a hora real no resto (RN73).</summary>
internal sealed class RelogioDaChamada(HoraReal hora, IHttpContextAccessor acesso) : TimeProvider
{
    public const string ItemDoInstante = "sim:instante";

    public override TimeZoneInfo LocalTimeZone => hora.Relogio.LocalTimeZone;

    public override long TimestampFrequency => hora.Relogio.TimestampFrequency;

    public override DateTimeOffset GetUtcNow() =>
        acesso.HttpContext?.Items[ItemDoInstante] is DateTimeOffset instante ? instante : hora.Relogio.GetUtcNow();

    public override long GetTimestamp() => hora.Relogio.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        hora.Relogio.CreateTimer(callback, state, dueTime, period);
}

/// <summary>A hora de um banco, para quem não está numa chamada dele (RN73).</summary>
internal sealed class RelogioDosBancos(IRepositorioDeInstituicoes instituicoes, HoraReal hora)
{
    public async Task<DateTimeOffset> Agora(string banco, CancellationToken cancelamento)
    {
        var real = hora.Relogio.GetUtcNow();
        return (await instituicoes.Obter(banco, cancelamento).ConfigureAwait(false))?.Relogio?.Agora(real) ?? real;
    }
}

/// <summary>Espera em hora real, sem depender do relógio simulado (RN12, RN73).</summary>
internal sealed class EsperaReal(HoraReal hora) : IEsperaDaChamada
{
    public Task Aguardar(TimeSpan duracao, CancellationToken cancelamento) => Task.Delay(duracao, hora.Relogio, cancelamento);
}

/// <summary>Instante do banco, <c>x-sim-clock</c> e falhas no corpo (RN17, RN73).</summary>
internal sealed class FiltroDaChamada(InstituicoesDaChamada instituicoes, HoraReal hora) : IEndpointFilter
{
    public const string ItemDaFalha = "sim:falha-no-corpo";
    private const string CabecalhoDoRelogio = "x-sim-clock";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var contexto = context.HttpContext;
        var real = hora.Relogio.GetUtcNow();
        var relogio = contexto.Request.RouteValues["banco"] is string banco
            ? (await instituicoes.Obter(banco, contexto.RequestAborted).ConfigureAwait(false))?.Relogio
            : null;
        var instante = relogio?.Agora(real) ?? real;
        contexto.Items[RelogioDaChamada.ItemDoInstante] = instante;
        if (relogio is not null)
        {
            contexto.Response.Headers[CabecalhoDoRelogio] = instante.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        var resultado = await next(context).ConfigureAwait(false);
        return contexto.Items[ItemDaFalha] is string falha && resultado is IResult original ? new ResultadoCorrompido(original, falha) : resultado;
    }
}

/// <summary>Executa o resultado e corrompe o corpo que sairia (RN17).</summary>
internal sealed class ResultadoCorrompido(IResult original, string falha) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var destino = httpContext.Response.Body;
        await using var memoria = new MemoryStream();
        httpContext.Response.Body = memoria;
        await original.ExecuteAsync(httpContext).ConfigureAwait(false);
        httpContext.Response.Body = destino;
        var corpo = memoria.ToArray();
        if (corpo.Length == 0)
        {
            return;
        }

        if (falha == Efeitos.CorpoTruncado)
        {
            httpContext.Response.ContentLength = corpo.Length;
            await destino.WriteAsync(corpo.AsMemory(0, corpo.Length / 2)).ConfigureAwait(false);
            await destino.FlushAsync().ConfigureAwait(false);
            httpContext.Abort();
            return;
        }

        var quebrado = corpo.AsMemory(0, corpo.Length - 1);
        httpContext.Response.ContentLength = quebrado.Length;
        await destino.WriteAsync(quebrado).ConfigureAwait(false);
    }
}

/// <summary>Etapa de regra de cenário e perfil (RN39), com a instituição já lida na chamada.</summary>
internal sealed class ComportamentoDaChamada(
    InstituicoesDaChamada instituicoes,
    ICenarios cenarios,
    ICatalogo catalogo,
    TimeProvider relogio,
    IHttpContextAccessor acesso) : IComportamentoDaChamada
{
    private const string ParametroDoConsentimento = "consentId";
    private const string ParametroDaNota = "brokerNoteId";
    private const int MuitasChamadas = 429;
    private readonly ConcurrentDictionary<string, IReadOnlySet<int>> _declarados = new(StringComparer.Ordinal);

    public async Task<DesfechoDoComportamento?> Decidir(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (Operacao(contexto) is not { } operacao || await Instituicao(contexto).ConfigureAwait(false) is not { } instituicao)
        {
            return null;
        }

        var decisao = await cenarios.Decidir(Chamada(contexto, operacao, RecursoDaChamada(contexto, operacao)), Perfil(instituicao.Comportamento, operacao), relogio.GetUtcNow(), Declarados(operacao), contexto.RequestAborted).ConfigureAwait(false);
        if (decisao.Transporte == Efeitos.ConexaoCortada)
        {
            return new DesfechoDoComportamento(decisao.Espera, Cortar: true);
        }

        if (decisao.Transporte is { } falha)
        {
            contexto.Items[FiltroDaChamada.ItemDaFalha] = falha;
        }

        return new DesfechoDoComportamento(decisao.Espera, decisao.Status is { } status ? Erro(contexto, status, operacao, instituicao.Limites) : null);
    }

    public async Task<bool> RecursoIndisponivel(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        return Operacao(contexto) is not null
            && await Instituicao(contexto).ConfigureAwait(false) is { } instituicao
            && cenarios.Sorteio() * 100 < (double)instituicao.Comportamento.RecursoIndisponivelPct;
    }

    public async Task<string> EstadoDoRecurso(string resourceId, string estado, CancellationToken cancelamento)
    {
        if (acesso.HttpContext is not { } contexto || Operacao(contexto) is not { } operacao)
        {
            return estado;
        }

        return await cenarios.EstadoSobreposto(Chamada(contexto, operacao, resourceId), relogio.GetUtcNow(), cancelamento).ConfigureAwait(false) ?? estado;
    }

    private static OperacaoDoCatalogo? Operacao(HttpContext contexto) => contexto.Items[OperacaoDoCatalogo.ItemDaChamada] as OperacaoDoCatalogo;

    private static string Banco(HttpContext contexto) => contexto.Request.RouteValues["banco"] as string ?? string.Empty;

    private static ChamadaAvaliada Chamada(HttpContext contexto, OperacaoDoCatalogo operacao, string? recurso) =>
        new(Banco(contexto), operacao.OperationId, ExtensoesDosConsentimentos.ConsentimentoDaChamada(contexto)?.Cliente, recurso);

    /// <summary>O primeiro parâmetro do caminho que não é o consentimento.</summary>
    internal static string? RecursoDaChamada(HttpContext contexto, OperacaoDoCatalogo operacao)
    {
        var parametro = operacao.Caminho.Split('/').FirstOrDefault(s => s.StartsWith('{') && s != "{" + ParametroDoConsentimento + "}")?.Trim('{', '}');
        var valor = parametro is null ? null : contexto.Request.RouteValues[parametro] as string;
        return parametro == ParametroDaNota && valor is not null ? IdentificadoresDeDados.RecursoDaNota(valor) : valor;
    }

    private static string? NomeDaClasse(ClasseDoEndpoint? classe) => classe?.Frequencia switch
    {
        Frequencia.Alta => "alta",
        Frequencia.MediaAlta => "media-alta",
        Frequencia.Media => "media",
        Frequencia.Baixa => "baixa",
        _ => null,
    };

    private Task<Instituicao?> Instituicao(HttpContext contexto) => instituicoes.Obter(Banco(contexto), contexto.RequestAborted);

    private PerfilDaChamada Perfil(Comportamento comportamento, OperacaoDoCatalogo operacao)
    {
        var latencia = NomeDaClasse(catalogo.ClasseDe(operacao)) is { } classe ? comportamento.LatenciaPorClasse.GetValueOrDefault(classe) : null;
        return new PerfilDaChamada(latencia?.TipicoMs ?? 0, latencia?.VariacaoMs ?? 0, latencia?.CaudaLentaPct ?? 0m, comportamento.TaxaDeErroPct, comportamento.PesosPorStatus, comportamento.Janela?.DuracaoMin, comportamento.Janela?.IntervaloMin);
    }

    private IReadOnlySet<int> Declarados(OperacaoDoCatalogo operacao) =>
        _declarados.GetOrAdd(operacao.OperationId, _ => operacao.Respostas.Keys.Select(k => int.TryParse(k, NumberStyles.None, CultureInfo.InvariantCulture, out var s) ? s : 0).Where(s => s > 0).ToHashSet());

    private IResult Erro(HttpContext contexto, int status, OperacaoDoCatalogo operacao, Instituicoes.Contratos.Limites limites)
    {
        if (status == MuitasChamadas && limites.RetryAfterLigado)
        {
            contexto.Response.Headers.RetryAfter = limites.RetryAfterSeg.ToString(CultureInfo.InvariantCulture);
        }

        return Results.Json(
            EnvelopeDeErro.Criar(CodigosDeErro.De(status, operacao.Contrato, operacao.Metodo), "Erro na operação", "A operação não pôde ser concluída.", relogio),
            EnvelopeDeErro.OpcoesJson,
            statusCode: status);
    }
}

/// <summary>Liga Cenários a Instituições, Catálogo e Limites, sem referência entre os módulos (ADR-007).</summary>
internal sealed class AmbienteDosCenarios(IAmbienteDasInstituicoes instituicoes, IRepositorioDeInstituicoes repositorio, ICatalogo catalogo, IContadores contadores, HoraReal hora) : IAmbienteDosCenarios
{
    private static readonly TimeSpan ValidadeDaSequencia = TimeSpan.FromDays(3650);
    private readonly Lazy<HashSet<string>> _operacoes = new(() => catalogo.Operacoes.Select(o => o.OperationId).ToHashSet(StringComparer.Ordinal));

    public IReadOnlySet<int> StatusDeclarados => instituicoes.StatusDeclarados;

    public bool OperacaoExiste(string operationId) => _operacoes.Value.Contains(operationId);

    public Task<long> ProximaPosicao(string chave, CancellationToken cancelamento) => contadores.Somar(chave, ValidadeDaSequencia, cancelamento);

    public Task RegistrarHistorico(string alvo, string? autor, string acao, string? antes, string? depois, CancellationToken cancelamento) =>
        repositorio.AcrescentarHistorico(new EntradaDeHistorico(alvo, hora.Relogio.GetUtcNow(), string.IsNullOrWhiteSpace(autor) ? "operador" : autor, acao, [new Mudanca("regra", antes, depois)]), cancelamento);
}

internal static class ExtensoesDosAdaptadoresDeCenarios
{
    public static IServiceCollection AddAdaptadoresDeCenarios(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IAmbienteDosCenarios, AmbienteDosCenarios>();
        servicos.AddSingleton<IComportamentoDaChamada, ComportamentoDaChamada>();
        servicos.AddSingleton<IEsperaDaChamada, EsperaReal>();
        servicos.AddSingleton<RelogioDosBancos>();
        servicos.AddSingleton<FiltroDaChamada>();
        return servicos;
    }
}
