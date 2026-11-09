namespace Simulador.Catalogo.Contratos;

/// <summary>Parâmetro de uma operação, lido da spec travada.</summary>
/// <param name="Local"><c>path</c>, <c>query</c> ou <c>header</c>.</param>
/// <param name="Schema">Schema do parâmetro em JSON, com os <c>$ref</c> resolvidos.</param>
public sealed record ParametroDaOperacao(string Nome, string Local, bool Obrigatorio, string Schema);

/// <summary>Uma operação de um contrato travado (data-model: OperacaoDoCatalogo).</summary>
/// <param name="Contrato">Id do contrato no <c>catalog.json</c> (ex.: <c>accounts</c>).</param>
/// <param name="VersaoDoContrato"><c>info.version</c> da spec (ex.: <c>2.5.1</c>).</param>
/// <param name="CaminhoBase">Caminho de <c>servers[0].url</c> da spec.</param>
/// <param name="Respostas">Código HTTP → schema da resposta em JSON, com os <c>$ref</c> resolvidos.</param>
public sealed record OperacaoDoCatalogo(
    string Contrato,
    string VersaoDoContrato,
    string CaminhoBase,
    string Metodo,
    string Caminho,
    string OperationId,
    IReadOnlyList<ParametroDaOperacao> Parametros,
    IReadOnlyList<string> CabecalhosExigidos,
    IReadOnlyDictionary<string, string> Respostas,
    IReadOnlyList<string>? Escopos = null,
    IReadOnlyList<string>? Permissoes = null)
{
    /// <summary>Segmento da API no caminho base (ex.: <c>accounts</c>).</summary>
    public string Api => CaminhoBase.Split('/', StringSplitOptions.RemoveEmptyEntries)[^2];

    /// <summary>Segmento da versão no caminho base (ex.: <c>v2</c>).</summary>
    public string Versao => CaminhoBase.Split('/', StringSplitOptions.RemoveEmptyEntries)[^1];

    /// <summary>Caminho publicado sob o prefixo da instituição (ADR-007).</summary>
    public string CaminhoPublicado => "/{banco}" + CaminhoBase + Caminho;

    /// <summary>Chave única: contrato, método e caminho.</summary>
    public (string Contrato, string Metodo, string Caminho) Chave => (Contrato, Metodo, Caminho);
}

/// <summary>Catálogo carregado na subida: operações publicadas, classes e prontidão.</summary>
public interface ICatalogo
{
    string Commit { get; }

    /// <summary>Verdadeiro quando specs e tabela foram lidas e conferidas.</summary>
    bool Pronto { get; }

    /// <summary>Motivos de não estar pronto.</summary>
    IReadOnlyList<string> Problemas { get; }

    /// <summary>Operações publicadas (sem as excluídas da tabela).</summary>
    IReadOnlyList<OperacaoDoCatalogo> Operacoes { get; }

    /// <summary>Classe da tabela oficial; nula se a operação não tiver linha.</summary>
    ClasseDoEndpoint? ClasseDe(OperacaoDoCatalogo operacao);
}

/// <summary>Situação do banco chamado pelo endereço.</summary>
public enum SituacaoDoBanco
{
    Inexistente,
    Ativo,
    Inativo,
}

/// <summary>Resolve o primeiro segmento <c>{banco}</c> do endereço (ADR-007).</summary>
public interface IResolvedorDeInstituicao
{
    ValueTask<SituacaoDoBanco> Situacao(string banco, CancellationToken cancelamento);
}

/// <summary>Primeira etapa da avaliação da chamada: cabeçalhos e segurança (RN39, ADR-008).</summary>
public interface IPortaoDeOperacao
{
    /// <summary>A recusa, ou nulo para seguir.</summary>
    Task<Microsoft.AspNetCore.Http.IResult?> Avaliar(Microsoft.AspNetCore.Http.HttpContext contexto, OperacaoDoCatalogo operacao);
}

/// <summary>Comportamento de uma operação do catálogo, entregue pela spec que cuida dela.</summary>
public interface IManipuladorDeOperacao
{
    string OperationId { get; }

    Task<Microsoft.AspNetCore.Http.IResult> Manipular(Microsoft.AspNetCore.Http.HttpContext contexto);
}
