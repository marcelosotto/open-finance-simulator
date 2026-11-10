using Simulador.Catalogo.Contratos;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;
using Simulador.Comum.Erros;
using Simulador.Consentimentos.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Marcacao.Contratos;
using Simulador.Massa.Contratos;

namespace Simulador.Host;

/// <summary>Liga Consentimentos a Massa, Instituições e Marcação, sem referência entre os módulos (ADR-007).</summary>
internal sealed class AmbienteDosConsentimentos(
    InstituicoesDaChamada instituicoes,
    IConsultaDeMassa consulta,
    IEscritaDeMassa escrita,
    IAmbienteDaMassa ambiente,
    IMarcador marcador,
    TimeProvider relogio,
    RelogioDosBancos relogios,
    IComportamentoDaChamada comportamento) : IAmbienteDosConsentimentos
{
    public string Marca => marcador.Marca.Valor;

    public async Task<DadosDoBanco?> Banco(string banco, CancellationToken cancelamento) =>
        await instituicoes.Obter(banco, cancelamento).ConfigureAwait(false) is { } i
            ? new DadosDoBanco(i.Situacao == Situacoes.Ativa, i.Produtos, i.Limites.PreparacaoDeRecursosSeg)
            : null;

    public async Task<DadosDoCliente?> Cliente(string documento, CancellationToken cancelamento) =>
        await consulta.ObterCliente(documento, cancelamento).ConfigureAwait(false) is { } m
            ? new DadosDoCliente(m.Cliente.Documento, m.Cliente.Tipo, m.Cliente.Natureza, [.. m.Recursos.Select(Mapear)], [.. m.Consentimentos.Select(Mapear)])
            : null;

    public async Task<DadosDoConsentimento?> Consentimento(string consentId, CancellationToken cancelamento) =>
        await consulta.ObterConsentimento(consentId, cancelamento).ConfigureAwait(false) is { } c ? Mapear(c) : null;

    public async Task<DadosDoRecurso?> Recurso(string resourceId, CancellationToken cancelamento) =>
        await consulta.ObterRecurso(resourceId, cancelamento).ConfigureAwait(false) is { } r ? Mapear(r) with { Estado = await comportamento.EstadoDoRecurso(r.ResourceId, r.Estado, cancelamento).ConfigureAwait(false) } : null;

    public Task<string?> NovoConsentId(CancellationToken cancelamento) => escrita.NovoConsentId(cancelamento);

    public Task Inserir(DadosDoConsentimento consentimento, CancellationToken cancelamento) => escrita.InserirConsentimento(Mapear(consentimento), cancelamento);

    public Task<bool> Atualizar(DadosDoConsentimento consentimento, CancellationToken cancelamento) => escrita.AtualizarConsentimento(Mapear(consentimento), cancelamento);

    public async Task<bool> MudarEstado(DadosDoRecurso recurso, string estado, CancellationToken cancelamento) =>
        await consulta.ObterRecurso(recurso.ResourceId, cancelamento).ConfigureAwait(false) is { } atual
        && atual.Versao == recurso.Versao
        && await escrita.MudarEstadoDoRecurso(atual, estado, cancelamento).ConfigureAwait(false);

    public async Task<DadosDoRecurso?> AdicionarProduto(string documento, string banco, string produto, string estado, CancellationToken cancelamento) =>
        await escrita.AdicionarProduto(documento, banco, produto, estado, cancelamento).ConfigureAwait(false) is { } r ? Mapear(r) : null;

    public string NovoOrganisationId() => marcador.NovoIdentificador(Natureza.Persistido);

    public Task<DateTimeOffset> AgoraNoBanco(string banco, CancellationToken cancelamento) => relogios.Agora(banco, cancelamento);

    public void RegistrarNatureza(HttpContext contexto, string natureza) =>
        contexto.RequestServices.GetRequiredService<IRegistroDeNatureza>().Registrar(natureza == Naturezas.Virtual ? Natureza.Virtual : Natureza.Persistido);

    public Task RegistrarHistorico(string? autor, string acao, string campo, string? antes, string? depois, CancellationToken cancelamento) =>
        ambiente.RegistrarHistorico(new EventoDaMassa(relogio.GetUtcNow(), string.IsNullOrWhiteSpace(autor) ? "operador" : autor, acao, [new MudancaDaMassa(campo, antes, depois)]), cancelamento);

    private static DadosDoRecurso Mapear(Recurso r) => new(r.ResourceId, r.Cliente, r.Banco, r.Tipo, r.Estado, r.Natureza, r.Versao);

    private static DadosDoConsentimento Mapear(Consentimento c) => new(
        c.ConsentId, c.Cliente, c.Banco, c.Estado, c.Permissoes, c.Recursos, c.CriadoEm, c.Marca, c.Natureza, c.LoteId, c.ExpiraEm, c.Receptora, c.RejeitadoPor, c.Razao, c.AtualizadoEm, c.UsuarioLogado, c.AprovacoesPendentes,
        [.. (c.Extensoes ?? []).Select(e => new DadosDaExtensao(e.NovaValidade, e.ValidadeAnterior, e.Documento, e.Rel, e.SolicitadaEm, e.Ip, e.UserAgent))],
        c.Versao);

    private static Consentimento Mapear(DadosDoConsentimento c) => new(
        c.ConsentId, c.Cliente, c.Banco, c.Estado, c.Permissoes, c.Recursos, c.CriadoEm, c.Marca, c.Natureza, c.LoteId, c.ExpiraEm, c.Receptora, c.RejeitadoPor, c.Razao, c.AtualizadoEm, c.UsuarioLogado, c.AprovacoesPendentes,
        [.. c.Extensoes.Select(e => new Extensao(e.NovaValidade, e.ValidadeAnterior, e.Documento, e.Rel, e.SolicitadaEm, e.Ip, e.UserAgent))],
        c.Versao);
}

/// <summary>
/// Leva a operação do catálogo ao portão do módulo. As anomalias trocam o comportamento dessa etapa
/// (ADR-008, RN113): 401 vira 403, e o <c>x-fapi-interaction-id</c> devolvido muda.
/// </summary>
internal sealed class PortaoPeloConsentimento(IPortaoDeSeguranca portao, IAnomaliasDaChamada anomalias, IGeradorAleatorio aleatorio, TimeProvider relogio) : IPortaoDeOperacao
{
    private const string CabecalhoDeCorrelacao = "x-fapi-interaction-id";

    public async Task<IResult?> Avaliar(HttpContext contexto, OperacaoDoCatalogo operacao)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(operacao);
        var resultado = await portao.Avaliar(contexto, new OperacaoProtegida(operacao.Contrato, operacao.Escopos ?? [], operacao.Permissoes ?? [])).ConfigureAwait(false);
        var banco = contexto.Request.RouteValues["banco"] as string ?? string.Empty;
        if (resultado is IStatusCodeHttpResult { StatusCode: StatusCodes.Status401Unauthorized }
            && await anomalias.Ligada(banco, Anomalias.RevogadoCom403, contexto.RequestAborted).ConfigureAwait(false))
        {
            resultado = Results.Json(EnvelopeDeErro.Criar("FORBIDDEN", "Acesso proibido", "Acesso proibido", relogio), EnvelopeDeErro.OpcoesJson, statusCode: StatusCodes.Status403Forbidden);
        }

        var recebido = contexto.Request.Headers[CabecalhoDeCorrelacao].ToString();
        if (recebido.Length > 0
            && contexto.Response.Headers[CabecalhoDeCorrelacao].ToString() == recebido
            && await anomalias.Ligada(banco, Anomalias.InteracaoDiferente, contexto.RequestAborted).ConfigureAwait(false))
        {
            contexto.Response.Headers[CabecalhoDeCorrelacao] = OutraCorrelacao(recebido);
        }

        return resultado;
    }

    /// <summary>UUID versão 4 do gerador injetado, diferente do recebido.</summary>
    private string OutraCorrelacao(string recebido)
    {
        const string Hexadecimal = "0123456789abcdef";
        while (true)
        {
            var d = new string([.. Enumerable.Range(0, 32).Select(i => i switch
            {
                12 => '4',
                16 => '8',
                _ => Hexadecimal[aleatorio.ProximoInteiro(Hexadecimal.Length)],
            })]);
            var novo = $"{d[..8]}-{d[8..12]}-{d[12..16]}-{d[16..20]}-{d[20..]}";
            if (!string.Equals(novo, recebido, StringComparison.OrdinalIgnoreCase))
            {
                return novo;
            }
        }
    }
}

/// <summary>Publica uma operação do módulo no catálogo.</summary>
internal sealed class ManipuladorDoConsentimento(string operationId, Func<HttpContext, Task<IResult>> acao) : IManipuladorDeOperacao
{
    public string OperationId => operationId;

    public Task<IResult> Manipular(HttpContext contexto) => acao(contexto);
}

internal static class ExtensoesDosAdaptadoresDeConsentimentos
{
    public static IServiceCollection AddAdaptadoresDeConsentimentos(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IAmbienteDosConsentimentos, AmbienteDosConsentimentos>();
        servicos.AddSingleton<IPortaoDeOperacao, PortaoPeloConsentimento>();
        foreach (var (id, acao) in ExtensoesDosConsentimentos.Operacoes)
        {
            servicos.AddSingleton<IManipuladorDeOperacao>(new ManipuladorDoConsentimento(id, acao));
        }

        return servicos;
    }
}
