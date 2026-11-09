using System.Globalization;
using Simulador.Catalogo.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Marcacao.Contratos;

namespace Simulador.Host;

/// <summary>Liga Instituições a Marcação e Catálogo, sem referência entre os módulos (ADR-007).</summary>
internal sealed class AmbienteDasInstituicoes(IMarcador marcador, ICatalogo catalogo) : IAmbienteDasInstituicoes
{
    private readonly Lazy<IReadOnlySet<int>> _declarados = new(() =>
        catalogo.Operacoes
            .SelectMany(o => o.Respostas.Keys)
            .Select(chave => int.TryParse(chave, NumberStyles.None, CultureInfo.InvariantCulture, out var status) ? status : 0)
            .Where(status => status > 0)
            .ToHashSet());

    public string Marca => marcador.Marca.Valor;

    public IReadOnlySet<int> StatusDeclarados => _declarados.Value;

    public string NovoOrganisationId() => marcador.NovoIdentificador(Natureza.Persistido);
}

/// <summary>Resolve o endereço pelo cadastro, a cada chamada (RN05, RN07).</summary>
internal sealed class ResolvedorPeloCadastro(IConsultaDeInstituicoes consulta) : IResolvedorDeInstituicao
{
    public async ValueTask<SituacaoDoBanco> Situacao(string banco, CancellationToken cancelamento) =>
        await consulta.Obter(banco, cancelamento).ConfigureAwait(false) switch
        {
            null => SituacaoDoBanco.Inexistente,
            { Situacao: Situacoes.Ativa } => SituacaoDoBanco.Ativo,
            _ => SituacaoDoBanco.Inativo,
        };
}

internal static class ExtensoesDosAdaptadores
{
    public static IServiceCollection AddAdaptadoresDeInstituicoes(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IAmbienteDasInstituicoes, AmbienteDasInstituicoes>();
        servicos.AddSingleton<IResolvedorDeInstituicao, ResolvedorPeloCadastro>();
        return servicos;
    }
}
