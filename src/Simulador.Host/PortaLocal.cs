using Microsoft.AspNetCore.Routing.Matching;

namespace Simulador.Host;

/// <summary>A porta em que o grupo de rotas responde, conferida no socket e nunca no cabeçalho Host.</summary>
internal sealed record PortaDoGrupo(int Porta);

/// <summary>
/// Descarta os endpoints de outra porta antes da escolha da rota. Com <c>RequireHost</c>, um cabeçalho Host forjado
/// (<c>x:8081</c> enviado à 8080) alcançava a administração pela porta pública; aqui vale só
/// <see cref="ConnectionInfo.LocalPort"/>, a porta em que o Kestrel aceitou a conexão (FR-025 da 002).
/// </summary>
internal sealed class PoliticaDePorta : MatcherPolicy, IEndpointSelectorPolicy
{
    // Antes das políticas do framework, para os endpoints da outra porta nem entrarem na disputa.
    public override int Order => -2000;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.Any(endpoint => endpoint.Metadata.GetMetadata<PortaDoGrupo>() is not null);
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(candidates);
        var porta = httpContext.Connection.LocalPort;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates.IsValidCandidate(i) && candidates[i].Endpoint.Metadata.GetMetadata<PortaDoGrupo>() is { } grupo && grupo.Porta != porta)
            {
                candidates.SetValidity(i, false);
            }
        }

        return Task.CompletedTask;
    }
}
