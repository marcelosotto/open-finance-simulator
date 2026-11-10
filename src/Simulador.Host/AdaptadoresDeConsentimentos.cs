using Simulador.Catalogo.Contratos;
using Simulador.Comum.Comportamento;
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

/// <summary>Leva a operação do catálogo ao portão do módulo.</summary>
internal sealed class PortaoPeloConsentimento(IPortaoDeSeguranca portao) : IPortaoDeOperacao
{
    public Task<IResult?> Avaliar(HttpContext contexto, OperacaoDoCatalogo operacao) =>
        portao.Avaliar(contexto, new OperacaoProtegida(operacao.Contrato, operacao.Escopos ?? [], operacao.Permissoes ?? []));
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
