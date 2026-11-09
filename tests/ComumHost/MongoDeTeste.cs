using Testcontainers.MongoDb;

namespace Simulador.Testes.Host;

/// <summary>Um MongoDB em contêiner por processo de teste (ADR-009); cada teste usa uma base própria.</summary>
public static class MongoDeTeste
{
    private static readonly Lazy<string> Instancia = new(() =>
    {
        var contêiner = new MongoDbBuilder("mongo:8.0").Build();
        contêiner.StartAsync().GetAwaiter().GetResult();
        return contêiner.GetConnectionString();
    });

    public static string Conexao => Instancia.Value;

    public static string NovaBase() => $"teste_{Guid.NewGuid():N}";
}
