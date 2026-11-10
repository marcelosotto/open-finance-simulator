using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulador.Catalogo.Contratos;
using Simulador.Cenarios.Contratos;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;
using Simulador.Consentimentos.Contratos;
using Simulador.Dados.Contratos;
using Simulador.Host;
using Simulador.Instituicoes.Contratos;
using Simulador.Limites.Contratos;
using Simulador.Marcacao.Contratos;
using Simulador.Massa.Contratos;

var builder = WebApplication.CreateBuilder(args);
var portas = Portas.Ler(builder.Configuration);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(portas.OpenFinance);
    kestrel.ListenAnyIP(portas.Administracao);
});

// As rotas de cada porta valem só na porta do socket (PortaLocal.cs).
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.AspNetCore.Routing.MatcherPolicy, PoliticaDePorta>());
builder.Services.AddSingleton(new HoraReal(TimeProvider.System));
builder.Services.AddSingleton<TimeProvider, RelogioDaChamada>();
builder.Services.AddSingleton<IGeradorAleatorio>(_ =>
    new GeradorAleatorioComSemente(SementeDoAmbiente.De(builder.Configuration[SementeDoAmbiente.Chave])));
builder.AdicionarObservabilidade();
builder.Services.AddCatalogo(builder.Configuration);
builder.Services.AddMarcacao(builder.Configuration);
builder.Services.AddInstituicoes(builder.Configuration).AddAdaptadoresDeInstituicoes();
builder.Services.AddMassa(builder.Configuration).AddAdaptadoresDaMassa();
builder.Services.AddConsentimentos(builder.Configuration).AddAdaptadoresDeConsentimentos();
builder.Services.AddDados(builder.Configuration).AddAdaptadoresDeDados();
builder.Services.AddLimites(builder.Configuration).AddAdaptadoresDeLimites();
builder.Services.AddCenarios(builder.Configuration).AddAdaptadoresDeCenarios();

var app = builder.Build();

var openFinance = app.MapGroup(string.Empty).WithMetadata(new PortaDoGrupo(portas.OpenFinance)).ComMarcacao().AddEndpointFilter<RouteGroupBuilder, FiltroDaChamada>();
var administracao = app.MapGroup(string.Empty).WithMetadata(new PortaDoGrupo(portas.Administracao));
administracao.MapGet("/saude/vivo", () => Results.Ok());
administracao.MapearAmbiente(portas.OpenFinance);
openFinance.MapearCatalogo(administracao);
administracao.MapearInstituicoes();
administracao.MapearCenarios();
administracao.MapearMassa();
openFinance.MapearConsentimentos(administracao);
app.UsarBackOffice(administracao, portas.Administracao);

await app.RunAsync().ConfigureAwait(false);

/// <summary>Ponto de entrada do simulador; público para os testes de integração.</summary>
public partial class Program;
