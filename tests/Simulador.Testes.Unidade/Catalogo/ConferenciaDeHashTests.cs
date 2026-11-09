using System.Security.Cryptography;
using System.Text;
using Simulador.Catalogo.Contratos;
using Simulador.Catalogo.Dominio;

namespace Simulador.Testes.Unidade.Catalogo;

public class ConferenciaDeHashTests
{
    private const string CaminhoCatalogo = "contracts/expansion/catalog.json";
    private const string CaminhoAccounts = "contracts/expansion/snapshots/accounts-2.5.1.yaml";
    private static readonly byte[] BytesAccounts = Encoding.UTF8.GetBytes("openapi: 3.0.0\n");
    private static readonly string HashAccounts = Convert.ToHexStringLower(SHA256.HashData(BytesAccounts));

    private static TravaDasSpecs Trava(string hash) => new(
        "marcelosotto/open-finance-specs",
        new string('a', 40),
        CaminhoCatalogo,
        [new ContratoTravado("accounts", CaminhoAccounts, hash)]);

    private static string Catalogo(string id = "accounts", string? hash = null) =>
        "{\"contracts\":[{\"id\":\"" + id + "\",\"localPath\":\"" + CaminhoAccounts + "\",\"sha256\":\"" + (hash ?? HashAccounts) + "\"}]}";

    private static Dictionary<string, byte[]> Arquivos(string catalogo, byte[]? accounts = null)
    {
        var arquivos = new Dictionary<string, byte[]>
        {
            [CaminhoCatalogo] = Encoding.UTF8.GetBytes(catalogo),
        };
        if (accounts is not null)
        {
            arquivos[CaminhoAccounts] = accounts;
        }

        return arquivos;
    }

    private static IReadOnlyList<FalhaDeConferencia> Conferir(TravaDasSpecs trava, Dictionary<string, byte[]> arquivos) =>
        ConferenciaDeHash.Conferir(trava, arquivos.GetValueOrDefault, () => arquivos.Keys);

    [Fact]
    public void Passa_quando_o_hash_bate_com_a_trava_e_o_catalogo()
    {
        var falhas = Conferir(Trava(HashAccounts), Arquivos(Catalogo(), BytesAccounts));

        Assert.Empty(falhas);
    }

    [Fact]
    public void Falha_quando_o_arquivo_diverge_da_trava()
    {
        var alterado = Encoding.UTF8.GetBytes("openapi: 3.0.1\n");

        var falha = Assert.Single(Conferir(Trava(HashAccounts), Arquivos(Catalogo(), alterado)));

        Assert.Equal(CaminhoAccounts, falha.Arquivo);
        Assert.Contains("SHA-256", falha.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_quando_a_trava_diverge_do_catalogo()
    {
        var falha = Assert.Single(Conferir(Trava(HashAccounts), Arquivos(Catalogo(hash: new string('b', 64)), BytesAccounts)));

        Assert.Equal(CaminhoAccounts, falha.Arquivo);
        Assert.Contains("catálogo", falha.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_quando_falta_o_arquivo()
    {
        var falha = Assert.Single(Conferir(Trava(HashAccounts), Arquivos(Catalogo())));

        Assert.Equal(CaminhoAccounts, falha.Arquivo);
        Assert.Contains("não encontrado", falha.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_quando_o_contrato_nao_existe_no_catalogo()
    {
        var falha = Assert.Single(Conferir(Trava(HashAccounts), Arquivos(Catalogo(id: "outro"), BytesAccounts)));

        Assert.Equal(CaminhoAccounts, falha.Arquivo);
        Assert.Contains("não existe no catálogo", falha.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_quando_sobra_arquivo_sem_hash_registrado()
    {
        var arquivos = Arquivos(Catalogo(), BytesAccounts);
        arquivos["contracts/expansion/snapshots/extra.yaml"] = [1, 2, 3];

        var falha = Assert.Single(Conferir(Trava(HashAccounts), arquivos));

        Assert.Equal("contracts/expansion/snapshots/extra.yaml", falha.Arquivo);
        Assert.Contains("sem hash", falha.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_quando_falta_o_catalogo()
    {
        var falha = Assert.Single(ConferenciaDeHash.Conferir(Trava(HashAccounts), _ => null, () => []));

        Assert.Equal(CaminhoCatalogo, falha.Arquivo);
    }

    [Theory]
    [InlineData("{ nao e json")]
    [InlineData("{\"outro\":1}")]
    public void Falha_quando_o_catalogo_e_invalido(string catalogo)
    {
        var falha = Assert.Single(Conferir(Trava(HashAccounts), Arquivos(catalogo, BytesAccounts)));

        Assert.Equal(CaminhoCatalogo, falha.Arquivo);
        Assert.Contains("inválido", falha.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    public void Falha_e_legivel_como_texto()
    {
        var falha = new FalhaDeConferencia("a.yaml", "motivo");

        Assert.Equal("a.yaml: motivo", falha.ToString());
    }
}
