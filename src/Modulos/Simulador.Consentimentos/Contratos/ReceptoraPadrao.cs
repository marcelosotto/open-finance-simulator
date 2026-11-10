using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Simulador.Consentimentos.Contratos;

/// <summary>
/// A receptora que já existe ao subir o ambiente (RN124). Nome, <c>organisationId</c> e credenciais vêm da configuração;
/// o <c>organisationId</c> padrão é fictício e leva a marca do ambiente, como todo identificador gerado (princípio IV).
/// </summary>
internal sealed record ReceptoraPadrao(string Nome, string? OrganisationId, string? Usuario, string? Senha)
{
    public const string Secao = "Simulador:ReceptoraPadrao";
    public const string NomePadrao = "XP";
    public const string Autor = "sistema";
    private const string Sufixo = "p-receptora-padrao";

    /// <summary>Nome vazio desliga a receptora padrão.</summary>
    public bool Ligada => Nome.Length > 0;

    public static ReceptoraPadrao Ler(IConfiguration configuracao)
    {
        var secao = configuracao.GetSection(Secao);
        return new ReceptoraPadrao(
            (secao["Nome"] ?? NomePadrao).Trim(),
            Preenchido(secao["OrganisationId"]),
            Preenchido(secao["Usuario"]),
            Preenchido(secao["Senha"]));
    }

    /// <summary>A padrão é reconhecida pelo sufixo, com qualquer marca (RN124).</summary>
    public static bool EhPadrao(string organisationId) =>
        organisationId == Sufixo || organisationId.EndsWith("-" + Sufixo, StringComparison.Ordinal);

    public string OrganisationIdCom(string marca) => OrganisationId ?? (marca.Length == 0 ? Sufixo : $"{marca}-{Sufixo}");

    /// <summary>
    /// Sem credencial configurada, gera uma de teste como a das outras receptoras. Usa o gerador do sistema, e não o da
    /// semente, para a subida não deslocar a sequência da simulação (princípio VI).
    /// </summary>
    public Receptora Criar(string marca, DateTimeOffset agora) => new(
        OrganisationIdCom(marca),
        Nome,
        Usuario ?? $"teste-{RandomNumberGenerator.GetHexString(8, lowercase: true)}",
        Senha ?? $"teste-senha-{RandomNumberGenerator.GetHexString(16, lowercase: true)}",
        agora);

    private static string? Preenchido(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
