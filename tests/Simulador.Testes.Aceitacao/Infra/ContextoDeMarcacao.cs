using Microsoft.Extensions.DependencyInjection;
using Simulador.Catalogo.Contratos;
using Simulador.Marcacao.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Infra;

/// <summary>Estado de um cenário: marca, dados de teste, host e última resposta.</summary>
public sealed class ContextoDeMarcacao : IDisposable
{
    private readonly List<FabricaDoSimulador> _fabricas = [];
    private FabricaDoSimulador? _fabrica;

    public string Marca { get; set; } = "hml";

    public DadosDeTeste Dados { get; } = new();

    public HttpResponseMessage? Resposta { get; set; }

    public string? Corpo { get; set; }

    public string? Identificador { get; set; }

    public string? ConsentId { get; set; }

    public string? Texto { get; set; }

    /// <summary>Host com a marca atual e o manipulador de teste; criado na primeira vez.</summary>
    public FabricaDoSimulador Fabrica => _fabrica ??= NovaFabrica(Marca);

    public IMarcador Marcador => Fabrica.Services.GetRequiredService<IMarcador>();

    /// <summary>Sobe outro host com outra marca (por exemplo, para simular a cópia da massa).</summary>
    public FabricaDoSimulador NovaFabrica(string marca)
    {
        var fabrica = new FabricaDoSimulador(marca).SemPortao().ComServicos(servicos =>
        {
            servicos.AddSingleton(Dados);
            servicos.AddScoped<IManipuladorDeOperacao, ManipuladorDeTransacoesDeTeste>();
        });
        _fabricas.Add(fabrica);
        return fabrica;
    }

    public void Dispose()
    {
        Resposta?.Dispose();
        foreach (var fabrica in _fabricas)
        {
            fabrica.Dispose();
        }
    }
}
