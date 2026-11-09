using Testcontainers.Redis;

namespace Simulador.Testes.Host;

/// <summary>Um Redis em contêiner por processo de teste (ADR-009).</summary>
public static class RedisDeTeste
{
    private static readonly Lazy<string> Instancia = new(() =>
    {
        var conteiner = new RedisBuilder("redis:7.4-alpine").Build();
        conteiner.StartAsync().GetAwaiter().GetResult();
        return conteiner.GetConnectionString();
    });

    public static string Conexao => Instancia.Value;
}
