using System.Text.Json.Nodes;
using Simulador.Instituicoes.Contratos;
using Simulador.Instituicoes.Dominio;

namespace Simulador.Testes.Unidade.Instituicoes;

public class EnderecoTests
{
    [Theory]
    [InlineData("Banco Estável", "banco-estavel")]
    [InlineData("Itaú", "itau")]
    [InlineData("  C6  Bank! ", "c6-bank")]
    [InlineData("áàâãä éèêë íìîï óòôõö úùûü çñ", "aaaaa-eeee-iiii-ooooo-uuuu-cn")]
    public void Gera_sem_acento_e_em_minusculas(string nome, string esperado) => Assert.Equal(esperado, Endereco.Gerar(nome));

    [Theory]
    [InlineData("!!!")]
    [InlineData("")]
    public void Nome_sem_letra_nao_gera(string nome) => Assert.Null(Endereco.Gerar(nome));

    [Fact]
    public void Respeita_o_tamanho_maximo()
    {
        Assert.NotNull(Endereco.Gerar(new string('a', Endereco.TamanhoMaximo)));
        Assert.Null(Endereco.Gerar(new string('a', Endereco.TamanhoMaximo + 1)));
    }
}

public class NomeDoBancoTests
{
    [Theory]
    [InlineData("Itaú", "hml", true, "HML-Itaú")]
    [InlineData("Itaú", "dev", true, "DEV-Itaú")]
    [InlineData("Itaú", "", true, "Itaú")]
    [InlineData("Banco Azul", "hml", false, "Banco Azul")]
    public void Aplica_o_prefixo_so_no_nome_real(string nome, string marca, bool real, string esperado) =>
        Assert.Equal(esperado, NomeDoBanco.Aplicar(nome, marca, real));
}

public class PerfisTests
{
    [Fact]
    public void Existem_os_cinco_perfis_e_os_dezesseis_bancos()
    {
        Assert.Equal(["estavel", "lento", "instavel", "timeout", "restritivo"], CatalogoDePerfis.Todos().Select(p => p.Nome));
        Assert.Equal(16, BancosProntos.Todos.Count);
        Assert.Null(CatalogoDePerfis.Obter("nao-existe"));
    }

    [Fact]
    public void Tabela_do_perfil_lento_escala_pelo_sla()
    {
        var latencia = CatalogoDePerfis.Obter("lento")!.Comportamento.LatenciaPorClasse;

        Assert.Equal(new Latencia(1_200, 1_300, 0.1m), latencia["alta"]);
        Assert.Equal(new Latencia(1_200, 1_300, 0.1m), latencia["media-alta"]);
        Assert.Equal(new Latencia(1_596, 1_729, 0.1m), latencia["media"]);
        Assert.Equal(new Latencia(3_204, 3_471, 0.1m), latencia["baixa"]);
    }

    [Fact]
    public void Instavel_nao_sorteia_503_e_tem_janela()
    {
        var comportamento = CatalogoDePerfis.Obter("instavel")!.Comportamento;

        Assert.Equal([500, 504], comportamento.PesosPorStatus.Keys);
        Assert.Equal(new Janela(2, 60), comportamento.Janela);
    }

    [Fact]
    public void Restritivo_tem_metade_do_oficial()
    {
        var limites = CatalogoDePerfis.Obter("restritivo")!.Limites;

        Assert.Equal(1_250, limites.Tpm.Alta);
        Assert.Equal(120, limites.LimiteMensal.Alta);
        Assert.Equal(200, limites.PageSizeMaximo);
        Assert.Equal(10, limites.ValidadeDaChaveMin);
    }

    [Fact]
    public void Cada_leitura_devolve_uma_copia_e_valida()
    {
        foreach (var perfil in CatalogoDePerfis.Todos())
        {
            Assert.NotSame(perfil.Comportamento, CatalogoDePerfis.Obter(perfil.Nome)!.Comportamento);
            Assert.Empty(Validacao.Validar(ValidacaoTests.Exemplo() with { Comportamento = perfil.Comportamento, Limites = perfil.Limites }, ValidacaoTests.Declarados));
        }
    }
}

public class ValidacaoTests
{
    public static readonly IReadOnlySet<int> Declarados = new HashSet<int> { 200, 429, 423, 500, 504, 529 };

    public static Instituicao Exemplo()
    {
        var perfil = CatalogoDePerfis.Obter("estavel")!;
        return new Instituicao("banco", "Banco", "org", perfil.Nome, "Banco", "ambos", ValoresOficiais.Produtos, Situacoes.Ativa, false, perfil.Comportamento, perfil.Limites);
    }

    private static Instituicao ComComportamento(Func<Comportamento, Comportamento> mudar)
    {
        var exemplo = Exemplo();
        return exemplo with { Comportamento = mudar(exemplo.Comportamento) };
    }

    private static Instituicao ComLimites(Func<Simulador.Instituicoes.Contratos.Limites, Simulador.Instituicoes.Contratos.Limites> mudar)
    {
        var exemplo = Exemplo();
        return exemplo with { Limites = mudar(exemplo.Limites) };
    }

    private static Instituicao ComLatencia(Latencia latencia) => ComComportamento(c => c with
    {
        LatenciaPorClasse = c.LatenciaPorClasse.ToDictionary(p => p.Key, p => p.Key == "alta" ? latencia : p.Value),
    });

    private static Instituicao ComPesos(params (int Status, decimal Peso)[] pesos) => ComComportamento(c => c with
    {
        PesosPorStatus = pesos.ToDictionary(p => p.Status, p => p.Peso),
    });

    public static TheoryData<string, Instituicao> Invalidas() => new()
    {
        { "nome vazio", Exemplo() with { Nome = " " } },
        { "publico", Exemplo() with { Publico = "X" } },
        { "produto", Exemplo() with { Produtos = ["nao-existe"] } },
        { "produto repetido", Exemplo() with { Produtos = ["contas", "contas"] } },
        { "classe faltando", ComComportamento(c => c with { LatenciaPorClasse = c.LatenciaPorClasse.Where(p => p.Key != "alta").ToDictionary() }) },
        { "classe trocada", ComComportamento(c => c with { LatenciaPorClasse = c.LatenciaPorClasse.ToDictionary(p => p.Key == "alta" ? "outra" : p.Key, p => p.Value) }) },
        { "tipico", ComLatencia(new Latencia(-1, 0, 0)) },
        { "variacao", ComLatencia(new Latencia(0, -1, 0)) },
        { "cauda", ComLatencia(new Latencia(0, 0, 101)) },
        { "taxa alta", ComComportamento(c => c with { TaxaDeErroPct = 100.1m }) },
        { "taxa negativa", ComComportamento(c => c with { TaxaDeErroPct = -1 }) },
        { "recurso", ComComportamento(c => c with { RecursoIndisponivelPct = 101 }) },
        { "418", ComPesos((418, 1)) },
        { "503", ComPesos((503, 1)) },
        { "429", ComPesos((429, 1)) },
        { "423", ComPesos((423, 1)) },
        { "529", ComPesos((529, 1)) },
        { "peso negativo", ComPesos((500, -1), (504, 1)) },
        { "pesos zerados", ComPesos((500, 0)) },
        { "janela duracao", ComComportamento(c => c with { Janela = new Janela(0, 60) }) },
        { "janela intervalo", ComComportamento(c => c with { Janela = new Janela(2, 0) }) },
        { "qca", ComLimites(l => l with { Qca = -1 }) },
        { "qca receptora", ComLimites(l => l with { QcaPorReceptora = new Dictionary<string, long> { ["org"] = -1 } }) },
        { "tpm alta", ComLimites(l => l with { Tpm = l.Tpm with { Alta = 0 } }) },
        { "capacidade", ComLimites(l => l with { CapacidadeTps = 0 }) },
        { "page-size", ComLimites(l => l with { PageSizeMaximo = 1_001 }) },
        { "preparacao", ComLimites(l => l with { PreparacaoDeRecursosSeg = -1 }) },
    };

    [Theory]
    [MemberData(nameof(Invalidas))]
    public void Recusa(string caso, Instituicao instituicao) => Assert.True(Validacao.Validar(instituicao, Declarados).Count > 0, caso);

    public static TheoryData<string, Instituicao> Validas() => new()
    {
        { "exemplo", Exemplo() },
        { "PF", Exemplo() with { Publico = "PF" } },
        { "PJ", Exemplo() with { Publico = "PJ" } },
        { "taxa zero sem pesos", ComComportamento(c => c with { TaxaDeErroPct = 0, PesosPorStatus = new Dictionary<int, decimal>() }) },
        { "janela", ComComportamento(c => c with { Janela = new Janela(2, 60) }) },
        { "status de sucesso declarado", ComPesos((200, 1)) },
        { "tpm alta informado", ComLimites(l => l with { Tpm = l.Tpm with { Alta = 10 } }) },
        { "qca por receptora", ComLimites(l => l with { QcaPorReceptora = new Dictionary<string, long> { ["org"] = 5 } }) },
    };

    [Theory]
    [MemberData(nameof(Validas))]
    public void Aceita(string caso, Instituicao instituicao) => Assert.True(Validacao.Validar(instituicao, Declarados).Count == 0, caso);
}

public class LimitesTests
{
    [Theory]
    [InlineData(0, 2_500)]
    [InlineData(1_000_000, 2_500)]
    [InlineData(1_000_001, 5_000)]
    [InlineData(2_000_000, 5_000)]
    [InlineData(3_000_000, 8_000)]
    [InlineData(6_000_000, 10_000)]
    [InlineData(6_000_001, 12_000)]
    [InlineData(8_000_000, 12_000)]
    [InlineData(8_000_001, 14_000)]
    public void Faixas_de_qca(long qca, int tpm) => Assert.Equal(tpm, ValoresOficiais.TpmDaAlta(qca));

    [Fact]
    public void Qca_da_receptora_sobrescreve_o_do_banco()
    {
        var limites = ValoresOficiais.Limites() with { Qca = 3_000_000, QcaPorReceptora = new Dictionary<string, long> { ["org-1"] = 0 } };

        Assert.Equal(2_500, limites.TpmDaAlta("org-1"));
        Assert.Equal(8_000, limites.TpmDaAlta("org-2"));
        Assert.Equal(7, (limites with { Tpm = limites.Tpm with { Alta = 7 } }).TpmDaAlta("org-1"));
    }

    [Fact]
    public void Valores_oficiais_nao_geram_aviso() => Assert.Empty(Avisos.Calcular(ValoresOficiais.Limites()));

    [Fact]
    public void Avisa_abaixo_do_oficial()
    {
        var limites = ValoresOficiais.Limites() with
        {
            CapacidadeTps = 100,
            Tpm = new Tpm(1, 1, 1, 1),
            LimiteMensal = new LimiteMensal(1, 1, 1, 1, 1),
        };

        var avisos = Avisos.Calcular(limites);

        Assert.Equal(10, avisos.Count);
        Assert.Contains(new Aviso("capacidadeTps", 300, 100), avisos);
    }

    [Fact]
    public void Limite_operacional_desligado_nao_avisa_cota()
    {
        var limites = ValoresOficiais.Limites() with { LimiteOperacionalLigado = false, LimiteMensal = new LimiteMensal(1, 1, 1, 1, 1) };

        Assert.Empty(Avisos.Calcular(limites));
    }
}

public class SlaTests
{
    [Fact]
    public void Compara_cada_classe_com_o_sla()
    {
        var lento = ComparacaoDeSla.Comparar(CatalogoDePerfis.Obter("lento")!.Comportamento);
        var estavel = ComparacaoDeSla.Comparar(CatalogoDePerfis.Obter("estavel")!.Comportamento);

        Assert.Equal(new LinhaDeSla("alta", 1_500, 2_500, false), lento[0]);
        Assert.All(estavel, l => Assert.True(l.DentroDoSla));
    }
}

public class MergeTests
{
    [Fact]
    public void Aplica_merge_patch()
    {
        var alvo = JsonNode.Parse("""{"a":1,"b":{"c":2,"d":3},"e":4}""")!.AsObject();

        AplicadorDeMerge.Aplicar(alvo, JsonNode.Parse("""{"a":9,"b":{"c":null,"x":5},"e":{"f":6}}""")!.AsObject());

        Assert.Equal("""{"a":9,"b":{"d":3,"x":5},"e":{"f":6}}""", alvo.ToJsonString());
    }
}

public class DiferencasTests
{
    [Fact]
    public void Lista_so_o_que_mudou()
    {
        var antes = JsonNode.Parse("""{"a":1,"b":{"c":2,"n":null},"l":[1],"s":3}""")!.AsObject();
        var depois = JsonNode.Parse("""{"a":1,"b":{"c":5,"n":null},"l":[2],"t":4}""")!.AsObject();

        var mudancas = Diferencas.Entre(antes, depois);

        Assert.Equal(
            [new Mudanca("b.c", "2", "5"), new Mudanca("l", "[1]", "[2]"), new Mudanca("s", "3", null), new Mudanca("t", null, "4")],
            mudancas);
    }
}

public class InstituicaoTests
{
    [Fact]
    public void Diz_se_oferece_um_produto()
    {
        var instituicao = ValidacaoTests.Exemplo() with { Produtos = ["cartoes"] };

        Assert.True(instituicao.Oferece("cartoes"));
        Assert.False(instituicao.Oferece("contas"));
    }
}
