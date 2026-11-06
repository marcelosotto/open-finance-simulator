using System.Text.Json;

namespace Simulador.Testes.Infra;

/// <summary>Localiza a raiz do repositório e a pasta das specs baixadas.</summary>
internal static class Repositorio
{
    public static string Raiz { get; } = AcharRaiz();

    public static string Caminho(params string[] partes) => Path.Combine([Raiz, .. partes]);

    public static string CommitTravado()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Caminho("specs.lock.json")));
        return json.RootElement.GetProperty("commit").GetString()!;
    }

    public static string PastaDasSpecs()
    {
        var pasta = Caminho(".specs", CommitTravado());
        if (!Directory.Exists(pasta))
        {
            throw new InvalidOperationException(
                $"Specs não encontradas em {pasta}. Rode: dotnet run --project tools/Simulador.Ferramentas -- specs baixar");
        }

        return pasta;
    }

    private static string AcharRaiz()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Simulador.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
    }
}
