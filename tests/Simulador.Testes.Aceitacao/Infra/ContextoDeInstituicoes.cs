using Microsoft.Extensions.DependencyInjection;
using Simulador.Instituicoes.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Infra;

/// <summary>Estado de um cenário de instituições, com MongoDB em contêiner (ADR-009).</summary>
public sealed class ContextoDeInstituicoes : IDisposable
{
    private FabricaDoSimulador? _fabrica;

    public string Marca { get; set; } = "hml";

    public ClientesDeTeste Clientes { get; } = new();

    public HttpResponseMessage? Resposta { get; set; }

    public string? Corpo { get; set; }

    public string? BancoAtual { get; set; }

    public FabricaDoSimulador Fabrica => _fabrica ??= new FabricaDoSimulador(Marca)
        .ComMongo()
        .ComServicos(s => s.AddSingleton<IConsultaDeClientes>(Clientes));

    public void Dispose()
    {
        Resposta?.Dispose();
        _fabrica?.Dispose();
    }
}
