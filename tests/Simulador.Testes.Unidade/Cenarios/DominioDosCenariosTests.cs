using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;

namespace Simulador.Testes.Unidade.Cenarios;

public sealed class DominioDosCenariosTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 10, 0, TimeSpan.Zero);
    private static readonly HashSet<int> Declarados = [200, 403, 422, 429, 500, 504, 529];
    private static readonly HashSet<int> Todos = [400, 403, 422, 423, 429, 500, 504, 529];
    private static readonly ChamadaAvaliada Chamada = new("banco", "saldos", "cli", "conta");
    private static readonly Dictionary<int, decimal> Pesos500 = new() { [500] = 1m };

    private static RegraDeCenario Regra(
        string? instituicao = null,
        string? endpoint = null,
        string? cliente = null,
        string? recurso = null,
        EfeitoDaRegra? efeito = null,
        decimal frequencia = 100m,
        DateTimeOffset? inicio = null,
        DateTimeOffset? fim = null,
        DateTimeOffset? criadaEm = null,
        string id = "r") =>
        new(id, null, instituicao, endpoint, cliente, recurso, efeito ?? new EfeitoDaRegra(Efeitos.Status, Status: 500), frequencia, inicio, fim, null, true, null, criadaEm ?? Agora, criadaEm ?? Agora);

    private static PerfilDaChamada Perfil(decimal cauda = 0m, decimal taxa = 0m, IReadOnlyDictionary<int, decimal>? pesos = null, int? duracao = null, int? intervalo = null) =>
        new(200, 600, cauda, taxa, pesos ?? Pesos500, duracao, intervalo);

    private static Func<double> Sequencia(params double[] valores)
    {
        var fila = new Queue<double>(valores);
        return () => fila.Count > 0 ? fila.Dequeue() : 0.99;
    }

    [Fact]
    public void Vence_recurso_depois_cliente_depois_endpoint_depois_instituicao()
    {
        var regras = new[]
        {
            Regra(instituicao: "banco", id: "i"), Regra(endpoint: "saldos", id: "e"), Regra(cliente: "cli", id: "c"), Regra(recurso: "conta", id: "r"),
        };
        Assert.Equal("r", SelecaoDeRegras.Vencedora(regras, Chamada, Agora, Declarados)!.Id);
        Assert.Equal("c", SelecaoDeRegras.Vencedora(regras[..3], Chamada, Agora, Declarados)!.Id);
        Assert.Equal("e", SelecaoDeRegras.Vencedora(regras[..2], Chamada, Agora, Declarados)!.Id);
    }

    [Fact]
    public void Com_o_mesmo_alcance_vencem_mais_filtros_e_depois_a_mais_recente()
    {
        Assert.Equal("dois", SelecaoDeRegras.Vencedora([Regra(cliente: "cli", id: "um"), Regra(cliente: "cli", endpoint: "saldos", id: "dois")], Chamada, Agora, Declarados)!.Id);
        Assert.Equal("nova", SelecaoDeRegras.Vencedora([Regra(cliente: "cli", id: "velha", criadaEm: Agora.AddHours(-1)), Regra(cliente: "cli", id: "nova")], Chamada, Agora, Declarados)!.Id);
    }

    [Fact]
    public void Regra_com_filtro_diferente_nao_casa()
    {
        Assert.Null(SelecaoDeRegras.Vencedora([Regra(instituicao: "x")], Chamada, Agora, Declarados));
        Assert.Null(SelecaoDeRegras.Vencedora([Regra(endpoint: "x")], Chamada, Agora, Declarados));
        Assert.Null(SelecaoDeRegras.Vencedora([Regra(cliente: "x")], Chamada, Agora, Declarados));
        Assert.Null(SelecaoDeRegras.Vencedora([Regra(recurso: "x")], Chamada, Agora, Declarados));
    }

    [Fact]
    public void Periodo_inclui_o_inicio_e_exclui_o_fim()
    {
        Assert.NotNull(SelecaoDeRegras.Vencedora([Regra(cliente: "cli", inicio: Agora, fim: Agora.AddMinutes(1))], Chamada, Agora, Declarados));
        Assert.Null(SelecaoDeRegras.Vencedora([Regra(cliente: "cli", inicio: Agora.AddMinutes(-1), fim: Agora)], Chamada, Agora, Declarados));
        Assert.Null(SelecaoDeRegras.Vencedora([Regra(cliente: "cli", inicio: Agora.AddSeconds(1))], Chamada, Agora, Declarados));
    }

    [Theory]
    [InlineData(423, false)]
    [InlineData(503, true)]
    [InlineData(200, true)]
    [InlineData(500, true)]
    public void Status_nao_declarado_nao_age_salvo_503(int status, bool age)
    {
        var regra = Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Status, Status: status));
        Assert.Equal(age, SelecaoDeRegras.Vencedora([regra], Chamada, Agora, Declarados) is not null);
    }

    [Fact]
    public void Regra_de_estado_disputa_a_parte()
    {
        var estado = Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Estado, Estado: "UNAVAILABLE"), id: "estado");
        var status = Regra(recurso: "conta", id: "status");
        Assert.Equal("status", SelecaoDeRegras.Vencedora([estado, status], Chamada, Agora, Declarados)!.Id);
        Assert.Equal("estado", SelecaoDeRegras.VencedoraDeEstado([estado, status], Chamada, Agora)!.Id);
    }

    [Fact]
    public void Sequencia_com_semente_se_repete_e_varia_por_posicao()
    {
        Assert.Equal(SelecaoDeRegras.SorteioDaSequencia(7, 3), SelecaoDeRegras.SorteioDaSequencia(7, 3));
        Assert.NotEqual(SelecaoDeRegras.SorteioDaSequencia(7, 3), SelecaoDeRegras.SorteioDaSequencia(7, 4));
        Assert.InRange(SelecaoDeRegras.SorteioDaSequencia(7, 3), 0d, 0.9999999999);
    }

    [Theory]
    [InlineData(0.5, 200)]
    [InlineData(0.95, 800)]
    [InlineData(0, 0)]
    [InlineData(1, 1400)]
    public void Amostra_acerta_p50_e_p95(double sorteio, int esperado) =>
        Assert.Equal(esperado, AvaliacaoDoPerfil.Amostra(200, 600, sorteio).TotalMilliseconds, 3);

    [Fact]
    public void Amostra_sem_variacao_e_o_tipico() =>
        Assert.Equal(3000, AvaliacaoDoPerfil.Amostra(3000, 0, 0.1).TotalMilliseconds, 3);

    [Fact]
    public void Janela_conta_ciclos_a_partir_da_meia_noite()
    {
        var hora = new DateTimeOffset(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
        Assert.True(AvaliacaoDoPerfil.JanelaAtiva(2, 60, hora));
        Assert.True(AvaliacaoDoPerfil.JanelaAtiva(2, 60, hora.AddMinutes(1).AddSeconds(59)));
        Assert.False(AvaliacaoDoPerfil.JanelaAtiva(2, 60, hora.AddMinutes(2)));
        Assert.False(AvaliacaoDoPerfil.JanelaAtiva(null, null, hora));
        Assert.False(AvaliacaoDoPerfil.JanelaAtiva(2, null, hora));
    }

    [Fact]
    public void Cauda_espera_15_segundos_e_termina_em_504()
    {
        var decisao = AvaliacaoDoPerfil.Decidir(Perfil(cauda: 5m), null, Agora, Declarados, Sequencia(0.01));
        Assert.Equal(504, decisao.Status);
        Assert.Equal(TimeSpan.FromSeconds(15), decisao.Espera);
    }

    [Fact]
    public void Sem_sorteio_de_erro_a_chamada_segue_com_a_latencia()
    {
        var decisao = AvaliacaoDoPerfil.Decidir(Perfil(), null, Agora, Declarados, Sequencia(0.99, 0.5, 0.99));
        Assert.Null(decisao.Status);
        Assert.Null(decisao.Transporte);
        Assert.Equal(TimeSpan.FromMilliseconds(200), decisao.Espera);
    }

    [Fact]
    public void Erro_sorteado_sai_dos_pesos_declarados()
    {
        var pesos = new Dictionary<int, decimal> { [422] = 1m, [500] = 1m, [400] = 5m };
        var decisao = AvaliacaoDoPerfil.Decidir(Perfil(taxa: 100m, pesos: pesos), null, Agora, Declarados, Sequencia(0.99, 0.5, 0.01, 0.9));
        Assert.Equal(500, decisao.Status);
    }

    [Theory]
    [InlineData(0.1, 422)]
    [InlineData(0.6, 500)]
    [InlineData(0.99, 504)]
    public void Peso_zero_fica_fora_e_o_sorteio_percorre_os_pesos(double ponto, int esperado)
    {
        var pesos = new Dictionary<int, decimal> { [403] = 0m, [422] = 1m, [500] = 1m, [504] = 1m };
        Assert.Equal(esperado, AvaliacaoDoPerfil.Decidir(Perfil(taxa: 100m, pesos: pesos), null, Agora, Declarados, Sequencia(0.99, 0.5, 0.01, ponto)).Status);
    }

    [Fact]
    public void Instituicao_igual_casa_e_inicio_sem_fim_vale()
    {
        Assert.NotNull(SelecaoDeRegras.Vencedora([Regra(instituicao: "banco", inicio: Agora.AddDays(-1))], Chamada, Agora, Declarados));
        Assert.Empty(Validar(Regra(instituicao: "banco", inicio: Agora)));
        Assert.Empty(Validar(Regra(instituicao: "banco", fim: Agora)));
    }

    [Theory]
    [InlineData("banco", "saldos", "cli", "conta", true)]
    [InlineData("x", "saldos", "cli", "conta", false)]
    [InlineData("banco", "x", "cli", "conta", false)]
    [InlineData("banco", "saldos", "x", "conta", false)]
    [InlineData("banco", "saldos", "cli", "x", false)]
    public void Todos_os_filtros_precisam_casar(string instituicao, string endpoint, string cliente, string recurso, bool casa)
    {
        var regra = Regra(instituicao, endpoint, cliente, recurso, inicio: Agora.AddHours(-1), fim: Agora.AddHours(1));
        Assert.Equal(casa, SelecaoDeRegras.Vencedora([regra], Chamada, Agora, Declarados) is not null);
        Assert.Equal(casa, SelecaoDeRegras.VencedoraDeEstado([regra with { Efeito = new EfeitoDaRegra(Efeitos.Estado, Estado: "UNAVAILABLE") }], Chamada, Agora) is not null);
    }

    [Theory]
    [InlineData(null, null, null, "conta")]
    [InlineData(null, null, "cli", null)]
    [InlineData(null, "saldos", null, null)]
    public void Basta_um_filtro(string? instituicao, string? endpoint, string? cliente, string? recurso) =>
        Assert.Empty(Validar(Regra(instituicao, endpoint, cliente, recurso)));

    [Fact]
    public void Sem_peso_declarado_nao_ha_erro() =>
        Assert.Null(AvaliacaoDoPerfil.Decidir(Perfil(taxa: 100m, pesos: new Dictionary<int, decimal> { [400] = 1m }), null, Agora, Declarados, Sequencia(0.99, 0.5, 0.01)).Status);

    [Fact]
    public void Erro_504_sorteado_espera_15_segundos()
    {
        var decisao = AvaliacaoDoPerfil.Decidir(Perfil(taxa: 100m, pesos: new Dictionary<int, decimal> { [504] = 1m }), null, Agora, Declarados, Sequencia(0.99, 0.5, 0.01, 0.5));
        Assert.Equal(504, decisao.Status);
        Assert.Equal(TimeSpan.FromSeconds(15), decisao.Espera);
    }

    [Fact]
    public void Janela_do_perfil_responde_503()
    {
        var hora = new DateTimeOffset(2026, 11, 5, 10, 0, 30, TimeSpan.Zero);
        Assert.Equal(503, AvaliacaoDoPerfil.Decidir(Perfil(duracao: 2, intervalo: 60), null, hora, Declarados, Sequencia(0.5)).Status);
    }

    [Fact]
    public void Efeito_de_latencia_troca_so_a_latencia()
    {
        var decisao = AvaliacaoDoPerfil.Decidir(Perfil(taxa: 100m), new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: 3000), Agora, Declarados, Sequencia(0.01, 0.5));
        Assert.Equal(TimeSpan.FromSeconds(3), decisao.Espera);
        Assert.Equal(500, decisao.Status);
    }

    [Fact]
    public void Status_200_troca_o_sorteio_e_mantem_a_latencia()
    {
        var hora = new DateTimeOffset(2026, 11, 5, 10, 0, 30, TimeSpan.Zero);
        var decisao = AvaliacaoDoPerfil.Decidir(Perfil(cauda: 100m, taxa: 100m, duracao: 2, intervalo: 60), new EfeitoDaRegra(Efeitos.Status, Status: 200), hora, Declarados, Sequencia(0.5));
        Assert.Null(decisao.Status);
        Assert.Equal(TimeSpan.FromMilliseconds(200), decisao.Espera);
    }

    [Theory]
    [InlineData(null, 15000)]
    [InlineData(2000, 2000)]
    public void Status_504_e_timeout_seguram_a_resposta(int? espera, int esperado)
    {
        Assert.Equal(TimeSpan.FromMilliseconds(esperado), AvaliacaoDoPerfil.Decidir(Perfil(), new EfeitoDaRegra(Efeitos.Status, Status: 504, EsperaMs: espera), Agora, Declarados, Sequencia(0.5)).Espera);
        var timeout = AvaliacaoDoPerfil.Decidir(Perfil(), new EfeitoDaRegra(Efeitos.Timeout, EsperaMs: espera), Agora, Declarados, Sequencia(0.5));
        Assert.Equal(TimeSpan.FromMilliseconds(esperado), timeout.Espera);
        Assert.Equal(504, timeout.Status);
    }

    [Fact]
    public void Indisponibilidade_responde_conforme_configurado()
    {
        DecisaoDeComportamento Com(string resultado) => AvaliacaoDoPerfil.Decidir(Perfil(), new EfeitoDaRegra(Efeitos.Indisponibilidade, Resultado: resultado), Agora, Declarados, Sequencia(0.5));
        Assert.Equal(503, Com("503").Status);
        Assert.Equal(TimeSpan.FromMilliseconds(200), Com("503").Espera);
        Assert.Equal(504, Com("504").Status);
        Assert.Equal(TimeSpan.FromSeconds(15), Com("504").Espera);
        Assert.Equal(Efeitos.ConexaoCortada, Com(Efeitos.Corte).Transporte);
    }

    [Fact]
    public void Transporte_marca_a_falha() =>
        Assert.Equal(Efeitos.CorpoTruncado, AvaliacaoDoPerfil.Decidir(Perfil(), new EfeitoDaRegra(Efeitos.Transporte, Falha: Efeitos.CorpoTruncado), Agora, Declarados, Sequencia(0.5)).Transporte);

    [Theory]
    [InlineData(500, "accounts", "GET", "INTERNAL_SERVER_ERROR")]
    [InlineData(504, "accounts", "GET", "GATEWAY_TIMEOUT")]
    [InlineData(503, "accounts", "GET", "SERVICE_UNAVAILABLE")]
    [InlineData(403, "accounts", "GET", "FORBIDDEN")]
    [InlineData(400, "accounts", "GET", "BAD_REQUEST")]
    [InlineData(401, "accounts", "GET", "UNAUTHORIZED")]
    [InlineData(404, "accounts", "GET", "NOT_FOUND")]
    [InlineData(405, "accounts", "GET", "METHOD_NOT_ALLOWED")]
    [InlineData(406, "accounts", "GET", "NOT_ACCEPTABLE")]
    [InlineData(415, "consents", "POST", "UNSUPPORTED_MEDIA_TYPE")]
    [InlineData(422, "accounts", "GET", "UNPROCESSABLE_ENTITY")]
    [InlineData(423, "accounts", "GET", "LOCKED")]
    [InlineData(429, "accounts", "GET", "TOO_MANY_REQUESTS")]
    [InlineData(529, "accounts", "GET", "SITE_IS_OVERLOADED")]
    [InlineData(422, "consents", "POST", "ERRO_NAO_MAPEADO")]
    [InlineData(422, "consents", "DELETE", "CONSENTIMENTO_EM_STATUS_REJEITADO")]
    [InlineData(599, "accounts", "GET", "ERROR")]
    public void Codigos_seguem_o_contrato(int status, string contrato, string metodo, string esperado) =>
        Assert.Equal(esperado, CodigosDeErro.De(status, contrato, metodo));

    private static IReadOnlyList<string> Validar(RegraDeCenario regra) => ValidacaoDeRegra.Validar(regra, Todos, o => o == "saldos");

    [Fact]
    public void Regra_valida_nao_tem_erros()
    {
        Assert.Empty(Validar(Regra(cliente: "cli", endpoint: "saldos", inicio: Agora, fim: Agora.AddHours(1))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Status, Status: 503))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Status, Status: 200))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: 0))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Timeout))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Indisponibilidade, Resultado: "504"))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Transporte, Falha: Efeitos.JsonInvalido))));
        Assert.Empty(Validar(Regra(cliente: "cli", efeito: new EfeitoDaRegra(Efeitos.Estado, Estado: "TEMPORARILY_UNAVAILABLE"))));
    }

    public static TheoryData<EfeitoDaRegra> EfeitosInvalidos => new()
    {
        new EfeitoDaRegra("x"),
        new EfeitoDaRegra(Efeitos.Status),
        new EfeitoDaRegra(Efeitos.Status, Status: 201),
        new EfeitoDaRegra(Efeitos.Status, Status: 418),
        new EfeitoDaRegra(Efeitos.Latencia),
        new EfeitoDaRegra(Efeitos.Latencia, LatenciaMs: -1),
        new EfeitoDaRegra(Efeitos.Timeout, EsperaMs: 0),
        new EfeitoDaRegra(Efeitos.Indisponibilidade, Resultado: "500"),
        new EfeitoDaRegra(Efeitos.Indisponibilidade),
        new EfeitoDaRegra(Efeitos.Transporte, Falha: "x"),
        new EfeitoDaRegra(Efeitos.Transporte),
        new EfeitoDaRegra(Efeitos.Estado, Estado: "x"),
        new EfeitoDaRegra(Efeitos.Estado),
    };

    [Theory]
    [MemberData(nameof(EfeitosInvalidos))]
    public void Efeito_invalido_e_recusado(EfeitoDaRegra efeito) =>
        Assert.NotEmpty(Validar(Regra(cliente: "cli", efeito: efeito)));

    [Fact]
    public void Alcance_periodo_frequencia_e_endpoint_sao_conferidos()
    {
        Assert.NotEmpty(Validar(Regra()));
        Assert.NotEmpty(Validar(Regra(cliente: "cli", inicio: Agora, fim: Agora)));
        Assert.NotEmpty(Validar(Regra(cliente: "cli", frequencia: 101m)));
        Assert.NotEmpty(Validar(Regra(cliente: "cli", frequencia: -1m)));
        Assert.NotEmpty(Validar(Regra(endpoint: "desconhecido")));
        Assert.NotEmpty(Validar(Regra(cliente: "cli", frequencia: 50m, efeito: new EfeitoDaRegra(Efeitos.Estado, Estado: "UNAVAILABLE"))));
    }
}
