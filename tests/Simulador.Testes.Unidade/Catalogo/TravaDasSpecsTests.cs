using Simulador.Catalogo.Contratos;

namespace Simulador.Testes.Unidade.Catalogo;

public class TravaDasSpecsTests
{
    private const string Commit = "7bdb9c1a6c4c265d7aabcdbc662f94c61718135e";
    private const string Hash = "ff5348ad631138aa66c60bf74de00eb03cb5a9e9643b937d948269e7b34a3905";

    private const string ContratoValido =
        "[{\"id\":\"accounts\",\"caminhoLocal\":\"contracts/expansion/snapshots/accounts-2.5.1.yaml\",\"sha256\":\"" + Hash + "\"}]";

    private static string Json(
        string repositorio = "marcelosotto/open-finance-specs",
        string commit = Commit,
        string contratos = ContratoValido,
        string? catalogo = null)
    {
        var campoCatalogo = catalogo is null ? string.Empty : $"\"catalogo\": \"{catalogo}\",";
        return $"{{\"repositorio\": \"{repositorio}\", \"commit\": \"{commit}\", {campoCatalogo} \"contratos\": {contratos}}}";
    }

    [Fact]
    public void Aceita_trava_valida_e_usa_o_catalogo_padrao()
    {
        var trava = TravaDasSpecs.Interpretar(Json());

        Assert.Equal("marcelosotto/open-finance-specs", trava.Repositorio);
        Assert.Equal(Commit, trava.Commit);
        Assert.Equal("contracts/expansion/catalog.json", trava.Catalogo);
        var contrato = Assert.Single(trava.Contratos);
        Assert.Equal("accounts", contrato.Id);
        Assert.Equal("contracts/expansion/snapshots/accounts-2.5.1.yaml", contrato.CaminhoLocal);
        Assert.Equal(Hash, contrato.Sha256);
    }

    [Fact]
    public void Aceita_catalogo_informado()
    {
        var trava = TravaDasSpecs.Interpretar(Json(catalogo: "outro/catalog.json"));

        Assert.Equal("outro/catalog.json", trava.Catalogo);
    }

    [Theory]
    [InlineData("sem-barra")]
    [InlineData("dono/nome/extra")]
    [InlineData("")]
    public void Recusa_repositorio_invalido(string repositorio) =>
        AssertRecusa(Json(repositorio: repositorio), "repositorio");

    [Theory]
    [InlineData("7bdb9c1")]
    [InlineData("7BDB9C1A6C4C265D7AABCDBC662F94C61718135E")]
    public void Recusa_commit_invalido(string commit) => AssertRecusa(Json(commit: commit), "commit");

    [Fact]
    public void Recusa_sha256_invalido() =>
        AssertRecusa(Json(contratos: "[{\"id\":\"accounts\",\"caminhoLocal\":\"a.yaml\",\"sha256\":\"abc\"}]"), "sha256");

    [Fact]
    public void Recusa_lista_vazia() => AssertRecusa(Json(contratos: "[]"), "contratos");

    [Fact]
    public void Recusa_id_repetido()
    {
        var item = "{\"id\":\"accounts\",\"caminhoLocal\":\"a.yaml\",\"sha256\":\"" + Hash + "\"}";
        AssertRecusa(Json(contratos: $"[{item},{item}]"), "accounts");
    }

    [Fact]
    public void Recusa_contrato_sem_id_ou_caminho() =>
        AssertRecusa(Json(contratos: "[{\"id\":\"\",\"caminhoLocal\":\"\",\"sha256\":\"" + Hash + "\"}]"), "contrato");

    [Fact]
    public void Catalogo_nulo_usa_o_padrao()
    {
        var json = Json().Replace("\"commit\"", "\"catalogo\": null, \"commit\"", StringComparison.Ordinal);

        Assert.Equal(TravaDasSpecs.CatalogoPadrao, TravaDasSpecs.Interpretar(json).Catalogo);
    }

    [Theory]
    [InlineData("{\"repositorio\":\"a/b\",\"commit\":\"" + Commit + "\"}", "contratos")]
    [InlineData("{\"repositorio\":\"a/b\",\"commit\":\"" + Commit + "\",\"contratos\":{}}", "contratos")]
    [InlineData("{\"repositorio\":\"a/b\",\"commit\":\"" + Commit + "\",\"contratos\":[\"x\"]}", "id")]
    [InlineData("{\"repositorio\":\"a/b\",\"commit\":1}", "commit")]
    [InlineData("[]", "repositorio")]
    public void Recusa_estrutura_invalida(string json, string trecho) => AssertRecusa(json, trecho);

    [Fact]
    public void Recusa_json_malformado() => AssertRecusa("{ nao e json", "JSON");

    [Fact]
    public void Recusa_campo_obrigatorio_ausente() => AssertRecusa("{\"repositorio\":\"a/b\"}", "commit");

    [Fact]
    public void Recusa_arquivo_ausente()
    {
        var caminho = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "specs.lock.json");

        var erro = Assert.Throws<TravaInvalidaException>(() => TravaDasSpecs.Ler(caminho));

        Assert.Contains("não encontrado", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Le_a_trava_do_repositorio()
    {
        var trava = TravaDasSpecs.Ler(Infra.Repositorio.Caminho("specs.lock.json"));

        Assert.Equal(15, trava.Contratos.Count);
    }

    private static void AssertRecusa(string json, string trecho)
    {
        var erro = Assert.Throws<TravaInvalidaException>(() => TravaDasSpecs.Interpretar(json));
        Assert.Contains(trecho, erro.Message, StringComparison.Ordinal);
    }
}
