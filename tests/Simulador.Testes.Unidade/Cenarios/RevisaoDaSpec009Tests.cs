using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using Simulador.Cenarios.Aplicacao;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;

namespace Simulador.Testes.Unidade.Cenarios;

/// <summary>Achados da revisão independente da spec 009, no domínio e na aplicação.</summary>
public sealed class RevisaoDaSpec009Tests
{
    private const long Semente = 42;
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly ChamadaAvaliada Chamada = new("banco", "transacoes", "cli", "conta");
    private static readonly HashSet<int> Declarados = [500];
    private static readonly PerfilDaChamada Normal = new(100, 100, 0m, 0m, new Dictionary<int, decimal>());
    private static readonly PerfilDaChamada SempreErro = new(100, 100, 0m, 100m, new Dictionary<int, decimal> { [500] = 1m });

    private static readonly JsonObject Schema = JsonNode.Parse("""
        {"properties":{"data":{"type":"array","items":{
          "required":["transactionId","type","transactionAmount","creditDebitType"],
          "properties":{
            "transactionId":{"type":"string"},
            "type":{"enum":["PIX"]},
            "creditDebitType":{"enum":["CREDITO"]},
            "transactionAmount":{"type":"object"},
            "transactionDateTime":{"type":"string"},
            "bookingDate":{"type":"string","format":"date"}}}}}}
        """)!.AsObject();

    private static JsonObject Corpo(JsonObject item) => new() { ["data"] = new JsonArray(item) };

    private static JsonObject Item() => new()
    {
        ["transactionId"] = "t0",
        ["type"] = "PIX",
        ["creditDebitType"] = "CREDITO",
        ["transactionAmount"] = new JsonObject { ["amount"] = "10.00", ["currency"] = "BRL" },
        ["transactionDateTime"] = "2026-11-05T00:59:59.000Z",
        ["bookingDate"] = "2026-11-05",
    };

    private static bool Aplicar(JsonObject corpo, string defeito, string? campo = null, int? quantidade = null, bool seguinte = false) =>
        AplicacaoDeDefeito.Aplicar(corpo, Schema, new DefeitoDaChamada(defeito, campo, quantidade), Agora, seguinte);

    private static JsonObject Primeiro(JsonObject corpo) => corpo["data"]![0]!.AsObject();

    private static RegraDeCenario RegraDeDefeito(decimal frequencia, long? semente) =>
        new("d-1", null, "banco", null, null, null, new EfeitoDaRegra(Efeitos.Defeito, Defeito: Defeitos.EnumInvalido), frequencia, null, null, semente, true, null, Agora, Agora);

    private static ServicoDeCenarios Servico(Ambiente ambiente, IGeradorAleatorio comum, IGeradorAleatorio dosDefeitos, params RegraDeCenario[] regras) =>
        new(new Repositorio(regras), ambiente, new HoraReal(new FakeTimeProvider(Agora)), comum, new GeradorDosDefeitos(dosDefeitos));

    /// <summary>Achado 2: quantidade de -1000 a 1000, sem o zero; fora disso é erro de validação.</summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    [InlineData(Defeitos.QuantidadeMaxima, true)]
    [InlineData(-Defeitos.QuantidadeMaxima, true)]
    [InlineData(Defeitos.QuantidadeMaxima + 1, false)]
    [InlineData(-Defeitos.QuantidadeMaxima - 1, false)]
    [InlineData(0, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(int.MinValue, false)]
    public void Quantidade_fica_entre_menos_mil_e_mil(int quantidade, bool valida)
    {
        var regra = RegraDeDefeito(100m, null) with { Efeito = new EfeitoDaRegra(Efeitos.Defeito, Defeito: Defeitos.DadoMudaNaPaginacao, Quantidade: quantidade) };
        var erros = ValidacaoDeRegra.Validar(regra, Declarados, _ => true);
        Assert.Equal(valida, erros.Count == 0);
        Assert.Equal(!valida, erros.Contains("quantidade deve ser de -1000 a 1000, diferente de 0"));
    }

    /// <summary>Achado 2: a aplicação também limita, e <c>int.MinValue</c> não estoura em <c>-quantidade</c>.</summary>
    [Theory]
    [InlineData(int.MinValue, 0)]
    [InlineData(int.MaxValue, 1 + Defeitos.QuantidadeMaxima)]
    public void Quantidade_extrema_e_limitada_na_aplicacao(int quantidade, int itens)
    {
        var corpo = Corpo(Item());
        corpo["meta"] = new JsonObject { ["totalRecords"] = 1 };
        Assert.True(Aplicar(corpo, Defeitos.DadoMudaNaPaginacao, quantidade: quantidade, seguinte: true));
        Assert.Equal(itens, corpo["data"]!.AsArray().Count);
        Assert.Equal(itens, corpo["meta"]!["totalRecords"]!.GetValue<int>());
    }

    /// <summary>Achado 3: sem o alvo padrão no item, o defeito usa o primeiro campo elegível presente.</summary>
    [Fact]
    public void Alvo_padrao_ausente_usa_o_primeiro_elegivel_presente()
    {
        var semValor = Item();
        semValor.Remove("transactionAmount");
        var corpo = Corpo(semValor);
        Assert.True(Aplicar(corpo, Defeitos.CampoObrigatorioAusente));
        Assert.False(Primeiro(corpo).ContainsKey("creditDebitType"));

        var semDataHora = Item();
        semDataHora.Remove("transactionDateTime");
        corpo = Corpo(semDataHora);
        Assert.True(Aplicar(corpo, Defeitos.FormatoInvalido));
        Assert.Equal("05/11/2026", Primeiro(corpo)["bookingDate"]!.GetValue<string>());

        var semTipo = Item();
        semTipo.Remove("type");
        corpo = Corpo(semTipo);
        Assert.True(Aplicar(corpo, Defeitos.EnumInvalido));
        Assert.Equal("VALOR_FORA_DA_LISTA", Primeiro(corpo)["creditDebitType"]!.GetValue<string>());

        var dataEmNumero = Item();
        dataEmNumero["transactionDateTime"] = 3;
        corpo = Corpo(dataEmNumero);
        Assert.True(Aplicar(corpo, Defeitos.FormatoInvalido));
        Assert.Equal(3, Primeiro(corpo)["transactionDateTime"]!.GetValue<int>());
        Assert.Equal("05/11/2026", Primeiro(corpo)["bookingDate"]!.GetValue<string>());

        var semNenhum = new JsonObject { ["transactionId"] = "t0" };
        Assert.False(Aplicar(Corpo(semNenhum), Defeitos.FormatoInvalido));
        Assert.False(Aplicar(Corpo(semNenhum.DeepClone().AsObject()), Defeitos.EnumInvalido));
    }

    /// <summary>Achado 3: o campo informado é respeitado; ausente, a resposta sai normal.</summary>
    [Fact]
    public void Campo_informado_ausente_sai_normal()
    {
        var corpo = Corpo(Item());
        Assert.False(Aplicar(corpo, Defeitos.EnumInvalido, campo: "inexistente"));
        Assert.False(Aplicar(corpo, Defeitos.FormatoInvalido, campo: "inexistente"));
        Assert.Equal("PIX", Primeiro(corpo)["type"]!.GetValue<string>());
    }

    /// <summary>Achado 9: formato-invalido com campo objeto deixa a resposta normal, sem trocar o objeto por texto.</summary>
    [Fact]
    public void Formato_invalido_em_campo_objeto_nao_age()
    {
        var corpo = Corpo(Item());
        Assert.False(Aplicar(corpo, Defeitos.FormatoInvalido, campo: "transactionAmount"));
        Assert.IsType<JsonObject>(Primeiro(corpo)["transactionAmount"]);
    }

    /// <summary>Achado 4 (Q2): a chamada com erro do perfil também avança a sequência do defeito.</summary>
    [Fact]
    public async Task Chamada_com_erro_do_perfil_tambem_sorteia_o_defeito()
    {
        var ambiente = new Ambiente();
        var servico = Servico(ambiente, new GeradorAleatorioComSemente(1), new GeradorAleatorioComSemente(2), RegraDeDefeito(50m, Semente));

        for (var i = 0; i < 3; i++)
        {
            var erro = await servico.Decidir(Chamada, SempreErro, Agora, Declarados, CancellationToken.None);
            Assert.Equal(500, erro.Status);
            Assert.Null(erro.Defeito);
        }

        Assert.Equal(3, ambiente.Posicoes);
        var esperadas = Enumerable.Range(4, 20).Select(p => SelecaoDeRegras.SorteioDaSequencia(Semente, p) < 0.5).ToList();
        var obtidas = new List<bool>();
        for (var i = 0; i < 20; i++)
        {
            obtidas.Add((await servico.Decidir(Chamada, Normal, Agora, Declarados, CancellationToken.None)).Defeito is not null);
        }

        Assert.Equal(esperadas, obtidas);
        Assert.Contains(true, obtidas);
        Assert.Contains(false, obtidas);
    }

    /// <summary>Achado 5: o sorteio sem semente do defeito usa gerador próprio e não desloca o comum.</summary>
    [Fact]
    public async Task Sorteio_do_defeito_sem_semente_nao_desloca_o_gerador_comum()
    {
        var comDefeito = new GeradorAleatorioComSemente(7);
        var semDefeito = new GeradorAleatorioComSemente(7);
        var dosDefeitos = new GeradorContado(new GeradorAleatorioComSemente(9));
        var servicoComDefeito = Servico(new Ambiente(), comDefeito, dosDefeitos, RegraDeDefeito(50m, null));
        var servicoSemDefeito = Servico(new Ambiente(), semDefeito, new GeradorAleatorioComSemente(9));

        for (var i = 0; i < 10; i++)
        {
            await servicoComDefeito.Decidir(Chamada, Normal, Agora, Declarados, CancellationToken.None);
            await servicoSemDefeito.Decidir(Chamada, Normal, Agora, Declarados, CancellationToken.None);
        }

        Assert.Equal(10, dosDefeitos.Sorteios);
        Assert.Equal(semDefeito.ProximoTexto(16), comDefeito.ProximoTexto(16));
    }

    private sealed class GeradorContado(IGeradorAleatorio gerador) : IGeradorAleatorio
    {
        public int Sorteios { get; private set; }

        public int ProximoInteiro(int maximo)
        {
            Sorteios++;
            return gerador.ProximoInteiro(maximo);
        }

        public string ProximoTexto(int tamanho) => gerador.ProximoTexto(tamanho);
    }

    private sealed class Ambiente : IAmbienteDosCenarios
    {
        private long _posicoes;

        public long Posicoes => Interlocked.Read(ref _posicoes);

        public IReadOnlySet<int> StatusDeclarados { get; } = new HashSet<int> { 500 };

        public bool OperacaoExiste(string operationId) => true;

        public Task<bool> InstituicaoExiste(string instituicao, CancellationToken cancelamento) => Task.FromResult(true);

        public Task<long> ProximaPosicao(string chave, CancellationToken cancelamento) => Task.FromResult(Interlocked.Increment(ref _posicoes));

        public Task RegistrarHistorico(string alvo, string? autor, string acao, string? antes, string? depois, CancellationToken cancelamento) => Task.CompletedTask;
    }

    private sealed class Repositorio(IReadOnlyList<RegraDeCenario> regras) : IRepositorioDeCenarios
    {
        private readonly ConcurrentDictionary<string, AnomaliaDaInstituicao> _anomalias = new();

        public Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento) => Task.FromResult(regras);

        public Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento) => Task.FromResult(regras.FirstOrDefault(r => r.Id == id));

        public Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento) => Task.CompletedTask;

        public Task ApagarRegra(string id, CancellationToken cancelamento) => Task.CompletedTask;

        public Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<PacoteDeCenario>>([]);

        public Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento) => Task.FromResult<PacoteDeCenario?>(null);

        public Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento) => Task.CompletedTask;

        public Task ApagarPacote(string nome, CancellationToken cancelamento) => Task.CompletedTask;

        public Task<IReadOnlyList<AnomaliaDaInstituicao>> ListarAnomalias(CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<AnomaliaDaInstituicao>>([.. _anomalias.Values]);

        public Task SalvarAnomalia(AnomaliaDaInstituicao anomalia, CancellationToken cancelamento)
        {
            _anomalias[anomalia.Instituicao + ":" + anomalia.Nome] = anomalia;
            return Task.CompletedTask;
        }

        public Task ApagarAnomalias(string instituicao, CancellationToken cancelamento) => Task.CompletedTask;
    }
}
