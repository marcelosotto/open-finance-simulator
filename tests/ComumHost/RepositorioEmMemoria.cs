using System.Collections.Concurrent;
using Simulador.Instituicoes.Contratos;
using Simulador.Instituicoes.Dominio;

namespace Simulador.Testes.Host;

/// <summary>Cadastro em memória dos testes sem contêiner; já traz o <c>banco-teste</c>.</summary>
public sealed class RepositorioEmMemoria : IRepositorioDeInstituicoes
{
    private readonly ConcurrentDictionary<string, Instituicao> _instituicoes = new(StringComparer.Ordinal);
    private readonly List<EntradaDeHistorico> _historico = [];
    private readonly Lock _trava = new();

    public RepositorioEmMemoria()
    {
        var perfil = CatalogoDePerfis.Obter("estavel")!;
        _instituicoes["banco-teste"] = new Instituicao(
            "banco-teste", "Banco Teste", "org-teste", perfil.Nome, "Banco Teste", "ambos", ValoresOficiais.Produtos, Situacoes.Ativa, false, perfil.Comportamento, perfil.Limites);
    }

    public Task<Instituicao?> Obter(string endereco, CancellationToken cancelamento) =>
        Task.FromResult(_instituicoes.GetValueOrDefault(endereco));

    public Task<IReadOnlyList<Instituicao>> Listar(CancellationToken cancelamento) =>
        Task.FromResult<IReadOnlyList<Instituicao>>([.. _instituicoes.Values.OrderBy(i => i.Endereco, StringComparer.Ordinal)]);

    public Task<bool> Inserir(Instituicao instituicao, CancellationToken cancelamento) =>
        Task.FromResult(_instituicoes.TryAdd(instituicao.Endereco, instituicao));

    public Task Substituir(Instituicao instituicao, CancellationToken cancelamento)
    {
        _instituicoes[instituicao.Endereco] = instituicao;
        return Task.CompletedTask;
    }

    public Task Apagar(string endereco, CancellationToken cancelamento)
    {
        _instituicoes.TryRemove(endereco, out _);
        return Task.CompletedTask;
    }

    public Task AcrescentarHistorico(EntradaDeHistorico entrada, CancellationToken cancelamento)
    {
        lock (_trava)
        {
            _historico.Add(entrada);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EntradaDeHistorico>> LerHistorico(string endereco, CancellationToken cancelamento)
    {
        lock (_trava)
        {
            return Task.FromResult<IReadOnlyList<EntradaDeHistorico>>([.. _historico.Where(e => e.Instituicao == endereco).Reverse()]);
        }
    }
}

/// <summary>Dublê do cadastro de clientes, até a spec 003 (RN06).</summary>
public sealed class ClientesDeTeste : IConsultaDeClientes
{
    public HashSet<string> Enderecos { get; } = new(StringComparer.Ordinal);

    public Task<bool> TemClientes(string endereco, CancellationToken cancelamento) => Task.FromResult(Enderecos.Contains(endereco));
}
