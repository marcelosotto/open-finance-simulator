using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Catalogo.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Host;

/// <summary>Atalhos para os testes da spec 004.</summary>
public static class AtalhosDeConsentimento
{
    public const string Correlacao = "d78fc4e5-37ca-4da3-adf2-9b082bf92280";
    public const string IpDoCliente = "172.217.22.14";
    public const string AgenteDoCliente = "Mozilla/5.0 (teste)";

    /// <summary>Cabeçalhos que o contrato exige em <c>/extends</c>.</summary>
    public static IReadOnlyDictionary<string, string> CabecalhosDeRenovacao { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["x-fapi-customer-ip-address"] = IpDoCliente,
        ["x-customer-user-agent"] = AgenteDoCliente,
    };

    private static readonly Lazy<IReadOnlyList<OperacaoDoCatalogo>> Operacoes = new(() =>
        ServicosDeSpecs.LerOperacoes(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs()));

    /// <summary>Caminho publicado no banco, lido da spec travada.</summary>
    public static Uri Caminho(string banco, string contrato, string caminho, string? consentId = null)
    {
        var operacao = Operacoes.Value.First(o => o.Contrato == contrato && o.Caminho == caminho);
        return new Uri(
            operacao.CaminhoPublicado.Replace("{banco}", banco, StringComparison.Ordinal)
                .Replace("{consentId}", consentId ?? string.Empty, StringComparison.Ordinal),
            UriKind.Relative);
    }

    public static async Task<(string Usuario, string Senha, string OrganisationId)> CriarReceptora(HttpClient admin, string nome = "Coletor")
    {
        using var resposta = await admin.PostAsJsonAsync(new Uri("/admin/receptoras", UriKind.Relative), new { nome });
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var raiz = corpo.RootElement;
        return (raiz.GetProperty("usuario").GetString()!, raiz.GetProperty("senha").GetString()!, raiz.GetProperty("organisationId").GetString()!);
    }

    public static Task<HttpResponseMessage> PedirToken(HttpClient openFinance, string banco, string usuario, string senha, string escopo) =>
        openFinance.PostAsync(
            new Uri($"/{banco}/token", UriKind.Relative),
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = usuario,
                ["client_secret"] = senha,
                ["scope"] = escopo,
            }));

    /// <summary>Cria uma receptora nova e devolve um token dela.</summary>
    public static async Task<string> Token(HttpClient admin, HttpClient openFinance, string banco, string escopo)
    {
        var (usuario, senha, _) = await CriarReceptora(admin);
        using var resposta = await PedirToken(openFinance, banco, usuario, senha, escopo);
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    public static async Task<HttpResponseMessage> Chamar(HttpClient openFinance, HttpMethod metodo, Uri caminho, string? token, object? corpo = null, IReadOnlyDictionary<string, string>? cabecalhos = null)
    {
        using var requisicao = new HttpRequestMessage(metodo, caminho);
        requisicao.Headers.Add("x-fapi-interaction-id", Correlacao);
        if (token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        foreach (var (nome, valor) in cabecalhos ?? new Dictionary<string, string>())
        {
            requisicao.Headers.TryAddWithoutValidation(nome, valor);
        }

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        return await openFinance.SendAsync(requisicao);
    }
}
