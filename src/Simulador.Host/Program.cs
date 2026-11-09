using Simulador.Catalogo.Contratos;
using Simulador.Comum.Aleatorio;
using Simulador.Host;
using Simulador.Instituicoes.Contratos;
using Simulador.Marcacao.Contratos;

var builder = WebApplication.CreateBuilder(args);
var portas = Portas.Ler(builder.Configuration);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(portas.OpenFinance);
    kestrel.ListenAnyIP(portas.Administracao);
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IGeradorAleatorio>(_ =>
    new GeradorAleatorioComSemente(builder.Configuration.GetValue<int?>("Simulador:Semente") ?? Environment.TickCount));
builder.AdicionarObservabilidade();
builder.Services.AddCatalogo(builder.Configuration);
builder.Services.AddMarcacao(builder.Configuration);
builder.Services.AddInstituicoes(builder.Configuration).AddAdaptadoresDeInstituicoes();

var app = builder.Build();

var openFinance = app.MapGroup(string.Empty).RequireHost($"*:{portas.OpenFinance}").ComMarcacao();
var administracao = app.MapGroup(string.Empty).RequireHost($"*:{portas.Administracao}");
administracao.MapGet("/saude/vivo", () => Results.Ok());
openFinance.MapearCatalogo(administracao);
administracao.MapearInstituicoes();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Ponto de entrada do simulador; público para os testes de integração.</summary>
public partial class Program;
