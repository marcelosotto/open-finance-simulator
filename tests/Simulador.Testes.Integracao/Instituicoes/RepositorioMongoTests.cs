using Simulador.Instituicoes.Contratos;
using Simulador.Instituicoes.Dominio;
using Simulador.Instituicoes.Persistencia;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Instituicoes;

/// <summary>MongoDB real em contêiner (ADR-009): ida e volta, chave única e histórico.</summary>
public sealed class RepositorioMongoTests : IDisposable
{
    private readonly RepositorioMongo _repositorio = new(MongoDeTeste.Conexao, MongoDeTeste.NovaBase());
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public void Dispose() => _repositorio.Dispose();

    private static Instituicao Exemplo(string perfil = "estavel")
    {
        var modelo = CatalogoDePerfis.Obter(perfil)!;
        return new Instituicao("banco-a", "HML-Itaú", "org", modelo.Nome, "Marca", "ambos", ValoresOficiais.Produtos, Situacoes.Ativa, true, modelo.Comportamento, modelo.Limites with
        {
            Qca = 7_000_000,
            QcaPorReceptora = new Dictionary<string, long> { ["org-1"] = 3_000_000_000 },
        });
    }

    [Fact]
    public async Task Grava_e_le_igual()
    {
        var original = Exemplo();

        Assert.True(await _repositorio.Inserir(original, _ct));

        var lida = await _repositorio.Obter("banco-a", _ct);
        Assert.NotNull(lida);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(original, JsonDasInstituicoes.Opcoes), System.Text.Json.JsonSerializer.Serialize(lida, JsonDasInstituicoes.Opcoes));
        Assert.Equal("0.2", System.Text.Json.JsonSerializer.Serialize(lida.Comportamento.TaxaDeErroPct));
        Assert.Null(await _repositorio.Obter("nao-existe", _ct));
    }

    [Fact]
    public async Task Endereco_repetido_nao_entra()
    {
        Assert.True(await _repositorio.Inserir(Exemplo(), _ct));
        Assert.False(await _repositorio.Inserir(Exemplo(), _ct));
        Assert.Single(await _repositorio.Listar(_ct));
    }

    [Fact]
    public async Task Substitui_e_apaga()
    {
        await _repositorio.Inserir(Exemplo(), _ct);

        await _repositorio.Substituir(Exemplo() with { Situacao = Situacoes.Inativa }, _ct);
        Assert.Equal(Situacoes.Inativa, (await _repositorio.Obter("banco-a", _ct))!.Situacao);

        await _repositorio.Apagar("banco-a", _ct);
        Assert.Empty(await _repositorio.Listar(_ct));
    }

    [Fact]
    public async Task Historico_vem_do_mais_novo_ao_mais_antigo()
    {
        var data = new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);
        await _repositorio.AcrescentarHistorico(new EntradaDeHistorico("banco-a", data, "a", "criada", []), _ct);
        await _repositorio.AcrescentarHistorico(new EntradaDeHistorico("banco-a", data, "b", "alterada", [new Mudanca("x", "1", null)]), _ct);
        await _repositorio.AcrescentarHistorico(new EntradaDeHistorico("banco-b", data, "c", "criada", []), _ct);

        var historico = await _repositorio.LerHistorico("banco-a", _ct);

        Assert.Equal(["b", "a"], historico.Select(e => e.Autor));
        Assert.Equal(new Mudanca("x", "1", null), historico[0].Mudancas[0]);
        Assert.Equal(data, historico[0].Data);
    }
}
