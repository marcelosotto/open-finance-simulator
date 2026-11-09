using System.Collections.Concurrent;
using Simulador.Consentimentos.Contratos;

namespace Simulador.Testes.Host;

/// <summary>Receptoras em memória dos testes sem contêiner.</summary>
public sealed class RepositorioDeReceptorasEmMemoria : IRepositorioDeReceptoras
{
    private readonly ConcurrentDictionary<string, Receptora> _receptoras = new(StringComparer.Ordinal);

    public Task Preparar(CancellationToken cancelamento) => Task.CompletedTask;

    public Task<bool> Inserir(Receptora receptora, CancellationToken cancelamento) => Task.FromResult(_receptoras.TryAdd(receptora.OrganisationId, receptora));

    public Task<Receptora?> Obter(string organisationId, CancellationToken cancelamento) => Task.FromResult(_receptoras.GetValueOrDefault(organisationId));

    public Task<Receptora?> ObterPorUsuario(string usuario, CancellationToken cancelamento) => Task.FromResult(_receptoras.Values.FirstOrDefault(r => r.Usuario == usuario));

    public Task<IReadOnlyList<Receptora>> Listar(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<Receptora>>([.. _receptoras.Values]);
}
