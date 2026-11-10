using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Simulador.Cenarios.Contratos;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;
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
    private bool _comComportamento;

    /// <summary>Massa em memória dos testes sem contêiner.</summary>
    public RepositorioDeMassaEmMemoria Massa { get; } = new();

    public FabricaDoSimulador(string? marcaDoAmbiente = "hml")
    {
        _configuracao["Simulador:MarcaDoAmbiente"] = marcaDoAmbiente ?? string.Empty;
        // A receptora padrão (RN124) fica desligada nos testes de outras specs; os dela usam ComReceptoraPadrao.
        _configuracao["Simulador:ReceptoraPadrao:Nome"] = string.Empty;
        _configuracao["Simulador:PastaDasSpecs"] = Repositorio.PastaDasSpecs();
        _configuracao["Simulador:ArquivoDaTrava"] = Repositorio.Caminho("specs.lock.json");
        _configuracao["Simulador:ArquivoDaTabela"] = Repositorio.Caminho("config", "tabela-de-endpoints.json");
    }

    public FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Esperas pedidas pela etapa de comportamento.</summary>
    public EsperasRegistradas Esperas { get; } = new();

    /// <summary>Liga latência, sorteio e regras; por padrão, ficam desligados nos testes de outras specs.</summary>
    public FabricaDoSimulador ComComportamento()
    {
        _comComportamento = true;
        return this;
    }

    /// <summary>Troca uma configuração antes de subir o host.</summary>
    public FabricaDoSimulador Com(string chave, string? valor)
    {
        _configuracao[chave] = valor;
        return this;
    }

    /// <summary>Sobe com a receptora padrão de RN124; sem nome, usa o padrão XP.</summary>
    public FabricaDoSimulador ComReceptoraPadrao(string nome = "XP") => Com("Simulador:ReceptoraPadrao:Nome", nome);

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

    /// <summary>Usa os contadores informados.</summary>
    public FabricaDoSimulador ComContadores(IContadores contadores)
    {
        Contadores = contadores;
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

    /// <summary>Cliente que fala com a porta do socket informada; o cabeçalho Host pode ser trocado à parte.</summary>
    public HttpClient Cliente(int porta) =>
        CreateDefaultClient(new Uri($"http://localhost:{porta}"), new PortaLocalDoCliente(porta));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var (chave, valor) in _configuracao)
        {
            builder.UseSetting(chave, valor);
        }

        builder.ConfigureTestServices(servicos =>
        {
            servicos.AddSingleton<IStartupFilter, PortaLocalDeTeste>();
            servicos.RemoveAll<HoraReal>();
            servicos.AddSingleton(new HoraReal(Relogio));
            servicos.RemoveAll<IEsperaDaChamada>();
            servicos.AddSingleton<IEsperaDaChamada>(Esperas);
            if (!_comComportamento)
            {
                servicos.RemoveAll<IComportamentoDaChamada>();
                servicos.AddSingleton<IComportamentoDaChamada>(new ComportamentoNulo());
            }

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
                servicos.RemoveAll<IRepositorioDeCenarios>();
                servicos.AddSingleton<IRepositorioDeCenarios>(new RepositorioDeCenariosEmMemoria());
            }

            foreach (var registrar in _servicos)
            {
                registrar(servicos);
            }
        });
    }
}

/// <summary>Marca o pedido com a porta do socket, que o TestServer não preenche.</summary>
internal sealed class PortaLocalDoCliente(int porta) : DelegatingHandler
{
    public const string Cabecalho = "x-teste-porta-local";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(Cabecalho);
        request.Headers.Add(Cabecalho, porta.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>Preenche <c>Connection.LocalPort</c> como o Kestrel faria, antes do roteamento por porta (PortaLocal.cs).</summary>
internal sealed class PortaLocalDeTeste : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((contexto, proximo) =>
        {
            contexto.Connection.LocalPort = contexto.Request.Headers.TryGetValue(PortaLocalDoCliente.Cabecalho, out var porta)
                ? int.Parse(porta.ToString(), System.Globalization.CultureInfo.InvariantCulture)
                : contexto.Request.Host.Port ?? 80;
            return proximo(contexto);
        });
        next(app);
    };
}
