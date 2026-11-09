using System.Text.Json.Nodes;
using Simulador.Catalogo.Contratos;
using Simulador.Consentimentos.Contratos;
using Simulador.Dados.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Marcacao.Contratos;
using Simulador.Massa.Contratos;

namespace Simulador.Host;

/// <summary>Liga Dados a Consentimentos, Massa, Instituições e Marcação, sem referência entre os módulos (ADR-007).</summary>
internal sealed class AmbienteDosDados(IConsultaDeInstituicoes instituicoes, IConsultaDeMassa consulta) : IAmbienteDosDados
{
    public ConsentimentoDaChamada? Consentimento(HttpContext contexto) =>
        ExtensoesDosConsentimentos.ConsentimentoDaChamada(contexto) is { } c ? new ConsentimentoDaChamada(c.ConsentId, c.Cliente, c.Recursos, c.Natureza) : null;

    public async Task<RecursoDeDados?> Recurso(string resourceId, CancellationToken cancelamento) =>
        await consulta.ObterRecurso(resourceId, cancelamento).ConfigureAwait(false) is { } r
            ? new RecursoDeDados(r.ResourceId, r.Cliente, r.Tipo, r.Estado, r.Natureza, r.InicioDoHistorico)
            : null;

    public async Task<ClienteDeDados?> Cliente(string documento, CancellationToken cancelamento) =>
        await consulta.ObterCliente(documento, cancelamento).ConfigureAwait(false) is { } m
            ? new ClienteDeDados(m.Cliente.Documento, m.Cliente.Nome, m.Cliente.Natureza)
            : null;

    public async Task<IReadOnlyList<TransacaoDeDados>> Transacoes(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, CancellationToken cancelamento) =>
        [.. (await consulta.TransacoesEm(resourceId, de, ate, instante, cancelamento).ConfigureAwait(false) ?? [])
            .Select(t => new TransacaoDeDados(t.Id, t.Dia, t.Posicao, t.Valor, t.Direcao == "CREDITO", t.Tipo, t.Nome, t.Estado))];

    public async Task<decimal> Saldo(string resourceId, CancellationToken cancelamento) =>
        await consulta.Saldo(resourceId, cancelamento).ConfigureAwait(false) ?? 0m;

    public async Task<LimitesDePaginacao> Limites(string banco, CancellationToken cancelamento)
    {
        var limites = (await instituicoes.Obter(banco, cancelamento).ConfigureAwait(false))!.Limites;
        return new LimitesDePaginacao(limites.PageSizeMaximo, limites.ValidadeDaChaveMin);
    }

    public void RegistrarNatureza(HttpContext contexto, string natureza) =>
        contexto.RequestServices.GetRequiredService<IRegistroDeNatureza>().Registrar(natureza == Naturezas.Virtual ? Natureza.Virtual : Natureza.Persistido);
}

/// <summary>Publica as famílias de dados no catálogo, levando o que o contrato declara.</summary>
internal sealed class ManipuladorDasFamilias(IDadosDasApis dados) : IManipuladorDeFamilia
{
    public bool Atende(OperacaoDoCatalogo operacao) => dados.Atende(operacao.Contrato);

    public Task<IResult> Manipular(HttpContext contexto, OperacaoDoCatalogo operacao)
    {
        var tamanho = operacao.Parametros.FirstOrDefault(p => p is { Nome: "page-size", Local: "query" });
        int? minimo = tamanho is null ? null : JsonNode.Parse(tamanho.Schema)?["minimum"]?.GetValue<int>() ?? 1;
        return dados.Responder(contexto, new OperacaoDeDados(
            operacao.Contrato,
            operacao.OperationId,
            operacao.Caminho,
            operacao.Respostas["200"],
            minimo,
            operacao.Parametros.Any(p => p.Nome == "pagination-key")));
    }
}

internal static class ExtensoesDosAdaptadoresDeDados
{
    public static IServiceCollection AddAdaptadoresDeDados(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IAmbienteDosDados, AmbienteDosDados>();
        servicos.AddSingleton<IManipuladorDeFamilia, ManipuladorDasFamilias>();
        return servicos;
    }
}
