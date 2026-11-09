namespace Simulador.Host;

/// <summary>Portas das APIs Open Finance e da administração (RN71, research R09).</summary>
internal sealed record Portas(int OpenFinance, int Administracao)
{
    public static Portas Ler(IConfiguration configuracao) => new(
        configuracao.GetValue("Simulador:PortaOpenFinance", 8080),
        configuracao.GetValue("Simulador:PortaAdministracao", 8081));
}
