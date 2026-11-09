using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulador.Instituicoes.Contratos;
using Simulador.Marcacao.Contratos;
using Simulador.Massa.Contratos;

namespace Simulador.Host;

/// <summary>Liga Massa a Marcação e Instituições, sem referência entre os módulos (ADR-007).</summary>
internal sealed class AmbienteDaMassa(IMarcador marcador, IRepositorioDeInstituicoes instituicoes, TimeProvider relogio, RelogioDosBancos relogios) : IAmbienteDaMassa
{
    /// <summary>Tem dois-pontos, que um endereço de instituição não aceita.</summary>
    private const string ChaveDoHistorico = "massa:geral";

    public string Marca => marcador.Marca.Valor;

    public string Identificador(string natureza, string corpo) => marcador.Identificador(Natureza(natureza), corpo);

    public string ConsentId(string natureza, string corpo) => marcador.ConsentId(Natureza(natureza), corpo);

    public string MarcarTexto(string texto) => marcador.MarcarTexto(texto, tamanhoMaximo: null);

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ProdutosDosBancos(CancellationToken cancelamento) =>
        (await instituicoes.Listar(cancelamento).ConfigureAwait(false)).ToDictionary(i => i.Endereco, i => i.Produtos, StringComparer.Ordinal);

    public Task RegistrarHistorico(EventoDaMassa evento, CancellationToken cancelamento) =>
        instituicoes.AcrescentarHistorico(
            new EntradaDeHistorico(ChaveDoHistorico, evento.Data, evento.Autor, evento.Acao, [.. evento.Mudancas.Select(m => new Mudanca(m.Campo, m.Antes, m.Depois))]),
            cancelamento);

    public async Task<IReadOnlyList<EventoDaMassa>> LerHistorico(CancellationToken cancelamento) =>
        [.. (await instituicoes.LerHistorico(ChaveDoHistorico, cancelamento).ConfigureAwait(false))
            .Select(e => new EventoDaMassa(e.Data, e.Autor, e.Acao, [.. e.Mudancas.Select(m => new MudancaDaMassa(m.Campo, m.Antes, m.Depois))]))];

    public Task<DateTimeOffset> AgoraNoBanco(string banco, CancellationToken cancelamento) => relogios.Agora(banco, cancelamento);

    public async Task<int> ContarInstituicoesCriadas(IReadOnlySet<string> exceto, CancellationToken cancelamento) =>
        (await Criadas(exceto, cancelamento).ConfigureAwait(false)).Count;

    public async Task ApagarInstituicoesCriadas(IReadOnlySet<string> exceto, string autor, CancellationToken cancelamento)
    {
        foreach (var instituicao in await Criadas(exceto, cancelamento).ConfigureAwait(false))
        {
            await instituicoes.Apagar(instituicao.Endereco, cancelamento).ConfigureAwait(false);
            await instituicoes.AcrescentarHistorico(new EntradaDeHistorico(instituicao.Endereco, relogio.GetUtcNow(), autor, "apagada", []), cancelamento).ConfigureAwait(false);
        }
    }

    private async Task<List<Instituicao>> Criadas(IReadOnlySet<string> exceto, CancellationToken cancelamento) =>
        [.. (await instituicoes.Listar(cancelamento).ConfigureAwait(false)).Where(i => !i.Pronta && !exceto.Contains(i.Endereco))];

    private static Natureza Natureza(string natureza) => natureza == Naturezas.Virtual ? Marcacao.Contratos.Natureza.Virtual : Marcacao.Contratos.Natureza.Persistido;
}

/// <summary>Instituição com massa não é apagada, só inativada (RN06).</summary>
internal sealed class ClientesPelaMassa(IConsultaDeMassa massa) : IConsultaDeClientes
{
    public Task<bool> TemClientes(string endereco, CancellationToken cancelamento) => massa.BancoTemClientes(endereco, cancelamento);
}

internal static class ExtensoesDosAdaptadoresDaMassa
{
    public static IServiceCollection AddAdaptadoresDaMassa(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IAmbienteDaMassa, AmbienteDaMassa>();
        servicos.Replace(ServiceDescriptor.Singleton<IConsultaDeClientes, ClientesPelaMassa>());
        return servicos;
    }
}
