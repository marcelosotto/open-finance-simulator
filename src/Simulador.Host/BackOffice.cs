using Microsoft.Extensions.FileProviders;

namespace Simulador.Host;

/// <summary>
/// Serve o build do back-office só na porta de administração, na mesma origem de <c>/admin</c> (spec 010, Q3).
/// Sem a pasta do build, nada é servido e nada muda.
/// </summary>
internal static class BackOffice
{
    public const string ChaveDaPasta = "Simulador:PastaDoBackOffice";
    private const string Indice = "index.html";

    // Rotas da tela: sem extensão e fora de /admin e /saude, que mantêm 404 e 405 como antes.
    private const string RotaDaTela = "{**caminho:nonfile:regex(^(?!(admin|saude)(/|$)).*$)}";

    public static void UsarBackOffice(this WebApplication app, RouteGroupBuilder administracao, int portaDeAdministracao)
    {
        var configurada = app.Configuration[ChaveDaPasta];
        var pasta = Path.GetFullPath(string.IsNullOrWhiteSpace(configurada) ? Path.Combine(AppContext.BaseDirectory, "backoffice") : configurada);
        var indice = Path.Combine(pasta, Indice);
        if (!File.Exists(indice))
        {
            return;
        }

        var arquivos = new PhysicalFileProvider(pasta);
        app.UseWhen(
            contexto => contexto.Request.Host.Port == portaDeAdministracao,
            ramo => ramo.UseStaticFiles(new StaticFileOptions { FileProvider = arquivos }));
        administracao.MapGet("/", () => Pagina(indice));
        administracao.MapGet(RotaDaTela, () => Pagina(indice));
    }

    private static IResult Pagina(string indice) => Results.File(indice, "text/html; charset=utf-8");
}
