using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Simulador.Comum.Aleatorio;
using Simulador.Consentimentos.Contratos;
using Simulador.Instituicoes.Contratos;
using Simulador.Limites.Contratos;
using Simulador.Massa.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Host;

/// <summary>
/// Sobe o simulador em memória com relógio fixo, semente fixa e as specs baixadas em <c>.specs</c>.
/// </summary>
public class FabricaDoSimulador : WebApplicationFactory<Program>
{
    public const int PortaOpenFinance = 8080;
    public const int PortaAdministracao = 8081;
    public const int Semente = 20261105;

    private readonly Dictionary<string, string?> _configuracao = new(StringComparer.Ordinal);
    private readonly List<Action<IServiceCollection>> _servicos = [];
    private bool _usarMongo;

    /// <summary>Massa em memória dos testes sem contêiner.</summary>
    public RepositorioDeMassaEmMemoria Massa { get; } = new();

    public FabricaDoSimulador(string? marcaDoAmbiente = "hml")
    {
        _configuracao["Simulador:MarcaDoAmbiente"] = marcaDoAmbiente ?? string.Empty;
        _configuracao["Simulador:PastaDasSpecs"] = Repositorio.PastaDasSpecs();
        _configuracao["Simulador:ArquivoDaTrava"] = Repositorio.Caminho("specs.lock.json");
        _configuracao["Simulador:ArquivoDaTabela"] = Repositorio.Caminho("config", "tabela-de-endpoints.json");
    }

    public FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Troca uma configuração antes de subir o host.</summary>
    public FabricaDoSimulador Com(string chave, string? valor)
    {
        _configuracao[chave] = valor;
        return this;
    }

    /// <summary>Usa o MongoDB em contêiner, em vez do cadastro em memória.</summary>
    public FabricaDoSimulador ComMongo()
    {
        _usarMongo = true;
        return Com("Simulador:Mongo:Conexao", MongoDeTeste.Conexao).Com("Simulador:Mongo:Base", MongoDeTeste.NovaBase());
    }

    /// <summary>Liga contadores reais em memória; por padrão, nenhum limite é aplicado nos testes de outras specs.</summary>
    public FabricaDoSimulador ComContadoresEmMemoria(ContadoresEmMemoria? compartilhados = null)
    {
        Contadores = compartilhados ?? new ContadoresEmMemoria();
        return this;
    }

    /// <summary>Observa as leituras do serviço de dados.</summary>
    public FabricaDoSimulador ComEspiaoDeDados(EspiaoDeDados espiao) => ComServicos(s =>
    {
        s.RemoveAll<Simulador.Dados.Contratos.IAmbienteDosDados>();
        s.AddSingleton<Simulador.Dados.Contratos.IAmbienteDosDados>(p =>
            new AmbienteEspiao(ActivatorUtilities.CreateInstance<Simulador.Host.AmbienteDosDados>(p), espiao));
    });

    /// <summary>Contadores desta fábrica.</summary>
    public IContadores Contadores { get; private set; } = new ContadoresNulos();

    /// <summary>Registra serviços extras (por exemplo, manipuladores de teste).</summary>
    public FabricaDoSimulador ComServicos(Action<IServiceCollection> registrar)
    {
        _servicos.Add(registrar);
        return this;
    }

    /// <summary>Sem o portão de segurança e sem as APIs de dados, para os testes de rota e marcação.</summary>
    public FabricaDoSimulador SemPortao() => ComServicos(s =>
    {
        s.RemoveAll<Simulador.Catalogo.Contratos.IPortaoDeOperacao>();
        s.RemoveAll<Simulador.Catalogo.Contratos.IManipuladorDeFamilia>();
    });

    public HttpClient ClienteOpenFinance() => Cliente(PortaOpenFinance);

    public HttpClient ClienteAdministracao() => Cliente(PortaAdministracao);

    private HttpClient Cliente(int porta) =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://localhost:{porta}"), AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var (chave, valor) in _configuracao)
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureTestServices(servicos =>
        {
            servicos.RemoveAll<TimeProvider>();
            servicos.AddSingleton<TimeProvider>(Relogio);
            servicos.RemoveAll<IGeradorAleatorio>();
            servicos.AddSingleton<IGeradorAleatorio>(new GeradorAleatorioComSemente(Semente));
            servicos.RemoveAll<IContadores>();
            servicos.AddSingleton(Contadores);
            if (!_usarMongo)
            {
                servicos.RemoveAll<IRepositorioDeInstituicoes>();
                servicos.AddSingleton<IRepositorioDeInstituicoes>(new RepositorioEmMemoria());
                servicos.RemoveAll<IRepositorioDeMassa>();
                servicos.AddSingleton<IRepositorioDeMassa>(Massa);
                servicos.RemoveAll<IRepositorioDeReceptoras>();
                servicos.AddSingleton<IRepositorioDeReceptoras>(new RepositorioDeReceptorasEmMemoria());
            }

            foreach (var registrar in _servicos)
            {
                registrar(servicos);
            }
        });
    }
}
