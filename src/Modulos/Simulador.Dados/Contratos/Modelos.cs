using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Dados.Aplicacao;

namespace Simulador.Dados.Contratos;

/// <summary>Consentimento da chamada, já validado pelo portão.</summary>
public sealed record ConsentimentoDaChamada(string ConsentId, string Cliente, IReadOnlyList<string> Recursos, string Natureza);

/// <summary>Recurso como este módulo o enxerga.</summary>
public sealed record RecursoDeDados(string ResourceId, string Cliente, string Tipo, string Estado, string Natureza, DateOnly InicioDoHistorico);

/// <summary>Cliente como este módulo o enxerga.</summary>
public sealed record ClienteDeDados(string Documento, string Nome, string Natureza);

/// <summary>Transação calculada pela massa.</summary>
public sealed record TransacaoDeDados(string Id, DateOnly Dia, int Posicao, decimal Valor, bool Credito, string Tipo, string Nome, string Estado);

/// <summary>Limites de paginação da instituição (RN32, RN35).</summary>
public sealed record LimitesDePaginacao(int PageSizeMaximo, int ValidadeDaChaveMin);

/// <summary>O que este módulo precisa da operação oficial, lido do contrato travado.</summary>
/// <param name="SchemaDaResposta">Schema da resposta 200, com os <c>$ref</c> resolvidos.</param>
/// <param name="MinimoDoTamanho"><c>minimum</c> do <c>page-size</c>; nulo se a operação não pagina.</param>
public sealed record OperacaoDeDados(string Contrato, string OperationId, string Caminho, string SchemaDaResposta, int? MinimoDoTamanho, bool TemChave, IReadOnlyList<string> ParametrosDeConsulta);

/// <summary>O que o módulo precisa dos outros; o host implementa (ADR-007).</summary>
public interface IAmbienteDosDados
{
    ConsentimentoDaChamada? Consentimento(HttpContext contexto);

    Task<RecursoDeDados?> Recurso(string resourceId, CancellationToken cancelamento);

    Task<ClienteDeDados?> Cliente(string documento, CancellationToken cancelamento);

    /// <summary>Transações vistas no instante pedido, sem ajustes posteriores (RN37).</summary>
    /// <summary>As transações até o instante do banco, com os ajustes feitos até o corte em hora real (RN37).</summary>
    Task<IReadOnlyList<TransacaoDeDados>> Transacoes(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, DateTimeOffset corte, CancellationToken cancelamento);

    Task<decimal> Saldo(string resourceId, CancellationToken cancelamento);

    Task<LimitesDePaginacao> Limites(string banco, CancellationToken cancelamento);

    /// <summary>Declara a natureza de um item da resposta (RN61, RN65).</summary>
    void RegistrarNatureza(HttpContext contexto, string natureza);

    /// <summary>A cota mensal da chamada; nula quando o endpoint não tem limite operacional (RN25, RN28).</summary>
    Task<ICotaDeDados?> Cota(HttpContext contexto, OperacaoDeDados operacao, ConsentimentoDaChamada consentimento, CancellationToken cancelamento);
}

/// <summary>Cota de uma chamada de dados (UC19).</summary>
public interface ICotaDeDados
{
    Task<bool> Esgotada(CancellationToken cancelamento);

    Task Contar(CancellationToken cancelamento);
}

/// <summary>Atende as operações das famílias de dados (UC16, UC17).</summary>
public interface IDadosDasApis
{
    bool Atende(string contrato);

    Task<IResult> Responder(HttpContext contexto, OperacaoDeDados operacao);
}

/// <summary>Registro do módulo.</summary>
public static class ExtensoesDosDados
{
    public static IServiceCollection AddDados(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AddSingleton(_ => SegredoDaChave.Ler(configuracao));
        servicos.AddSingleton<IDadosDasApis, ServicoDeDados>();
        return servicos;
    }
}
