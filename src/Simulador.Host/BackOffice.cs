using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Simulador.Host;

/// <summary>
/// Serve o build do back-office só na porta de administração, na mesma origem de <c>/admin</c> (spec 010, Q3).
/// Sem a pasta do build, nada é servido e nada muda.
/// </summary>
internal static partial class BackOffice
{
    public const string ChaveDaPasta = "Simulador:PastaDoBackOffice";
    private const string Indice = "index.html";
    private const string SemCache = "no-cache";
    private const string Imutavel = "public, max-age=31536000, immutable";

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
            contexto => contexto.Connection.LocalPort == portaDeAdministracao,
            ramo => ramo.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = arquivos,

                // O nome com hash muda a cada build: pode ficar em cache para sempre; o resto é conferido sempre.
                OnPrepareResponse = arquivo => arquivo.Context.Response.Headers.CacheControl = ComHash().IsMatch(arquivo.File.Name) ? Imutavel : SemCache,
            }));

        // Todos os métodos: um POST em caminho de página é 404, não 405 (revisão da 010).
        administracao.Map("/", (HttpContext contexto) => Pagina(contexto, indice));
        administracao.Map(RotaDaTela, (HttpContext contexto) => Pagina(contexto, indice));
    }

    private static IResult Pagina(HttpContext contexto, string indice)
    {
        if (!HttpMethods.IsGet(contexto.Request.Method) && !HttpMethods.IsHead(contexto.Request.Method))
        {
            return Results.NotFound();
        }

        // O índice aponta para os arquivos com hash do build atual: nunca pode ficar velho no navegador.
        contexto.Response.Headers.CacheControl = SemCache;
        return Results.File(indice, "text/html; charset=utf-8");
    }

    [GeneratedRegex(@"\.[0-9a-f]{16,}\.(js|css)(\.map)?$")]
    private static partial Regex ComHash();
}
