using System.Text.Json.Nodes;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Dominio;
using Simulador.Comum.Comportamento;

namespace Simulador.Testes.Unidade.Cenarios;

/// <summary>Domínio dos defeitos e anomalias (spec 009, RN110 a RN117).</summary>
public sealed class DefeitosEAnomaliasTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly ChamadaAvaliada Chamada = new("banco", "transacoes", "cli", "conta");
    private static readonly HashSet<int> Declarados = [500];

    /// <summary>Schema parecido com o de transações de Contas: datas sem <c>format</c>, enum e valor.</summary>
    private const string SchemaDeLista = """
        {"properties":{"data":{"type":"array","items":{
          "required":["transactionId","type","transactionAmount","transactionDateTime"],
          "properties":{
            "transactionId":{"type":"string"},
            "completedAuthorisedPaymentType":{"enum":["A"]},
            "type":{"enum":["PIX"]},
            "transactionAmount":{"type":"object"},
            "transactionDateTime":{"type":"string","pattern":"x"},
            "bookingDate":{"type":"string","format":"date"},
            "nome":null}}},
          "meta":{"type":"object"}}}
        """;

    private static JsonObject Schema(string texto = SchemaDeLista) => JsonNode.Parse(texto)!.AsObject();

    private static JsonObject Item(string id) => new()
    {
        ["transactionId"] = id,
        ["completedAuthorisedPaymentType"] = "A",
        ["type"] = "PIX",
        ["transactionAmount"] = new JsonObject { ["amount"] = "10.00", ["currency"] = "BRL" },
        ["transactionDateTime"] = "2026-11-05T00:59:59.000Z",
        ["bookingDate"] = "2026-11-05",
    };

    private static JsonObject Corpo(int itens = 2, int? total = null, string? self = "http://x/t?page=2&page-size=25&pagination-key=k")
    {
        var lista = new JsonArray();
        for (var i = 0; i < itens; i++)
        {
            lista.Add(Item($"t{i}"));
        }

        var corpo = new JsonObject { ["data"] = lista, ["meta"] = new JsonObject { ["totalRecords"] = total ?? itens, ["totalPages"] = 1, ["requestDateTime"] = "x" } };
        if (self is not null)
        {
            corpo["links"] = new JsonObject { ["self"] = self };
        }

        return corpo;
    }

    private static bool Aplicar(JsonObject corpo, string defeito, string? campo = null, int? quantidade = null, bool seguinte = false, JsonObject? schema = null) =>
        AplicacaoDeDefeito.Aplicar(corpo, schema ?? Schema(), new DefeitoDaChamada(defeito, campo, quantidade), Agora, seguinte);

    private static JsonObject Primeiro(JsonObject corpo) => corpo["data"]![0]!.AsObject();

    [Fact]
    public void Catalogos_tem_os_nomes_das_regras()
    {
        Assert.Equal(8, Defeitos.Catalogo.Count);
        Assert.True(Defeitos.Existe(Defeitos.DadoMudaNaPaginacao));
        Assert.False(Defeitos.Existe("outro"));
        Assert.False(Defeitos.Existe(null));
        Assert.Equal([Anomalias.RevogadoCom403, Anomalias.ChaveRecusadaCom422, Anomalias.InteracaoDiferente], CatalogoDeAnomalias.Itens.Select(a => a.Nome));
        Assert.True(CatalogoDeAnomalias.Existe(Anomalias.InteracaoDiferente));
        Assert.False(CatalogoDeAnomalias.Existe("outra"));
    }

    [Fact]
    public void Sem_data_ou_sem_schema_de_data_nao_age()
    {
        Assert.False(Aplicar(new JsonObject { ["meta"] = new JsonObject() }, Defeitos.EnumInvalido));
        Assert.False(Aplicar(Corpo(), Defeitos.EnumInvalido, schema: Schema("""{"properties":{}}""")));
        Assert.False(Aplicar(Corpo(), Defeitos.EnumInvalido, schema: Schema("{}")));
        Assert.False(Aplicar(Corpo(), "desconhecido"));
    }

    [Theory]
    [InlineData(Defeitos.CampoObrigatorioAusente)]
    [InlineData(Defeitos.FormatoInvalido)]
    [InlineData(Defeitos.EnumInvalido)]
    [InlineData(Defeitos.ValorIncoerente)]
    [InlineData(Defeitos.DataForaDaJanela)]
    [InlineData(Defeitos.TransactionIdRepetido)]
    public void Lista_vazia_sai_normal(string defeito) => Assert.False(Aplicar(Corpo(0), defeito));

    [Fact]
    public void Obrigatorio_ausente_prefere_amount_depois_o_ultimo_e_aceita_campo()
    {
        var corpo = Corpo();
        Assert.True(Aplicar(corpo, Defeitos.CampoObrigatorioAusente));
        Assert.False(Primeiro(corpo).ContainsKey("transactionAmount"));
        Assert.True(corpo["data"]![1]!.AsObject().ContainsKey("transactionAmount"));

        var semValor = Corpo();
        Assert.True(Aplicar(semValor, Defeitos.CampoObrigatorioAusente, schema: Schema("""{"properties":{"data":{"type":"array","items":{"required":["transactionId",null,"type"]}}}}""")));
        Assert.False(Primeiro(semValor).ContainsKey("type"));

        var comCampo = Corpo();
        Assert.True(Aplicar(comCampo, Defeitos.CampoObrigatorioAusente, campo: "bookingDate"));
        Assert.False(Primeiro(comCampo).ContainsKey("bookingDate"));

        Assert.False(Aplicar(Corpo(), Defeitos.CampoObrigatorioAusente, campo: "inexistente"));
        Assert.False(Aplicar(Corpo(), Defeitos.CampoObrigatorioAusente, schema: Schema("""{"properties":{"data":{"type":"array","items":{}}}}""")));
        Assert.False(Aplicar(Corpo(), Defeitos.CampoObrigatorioAusente, schema: Schema("""{"properties":{"data":{"type":"array"}}}""")));
    }

    [Fact]
    public void Formato_invalido_reconhece_data_pelo_nome_ou_pelo_format()
    {
        var corpo = Corpo();
        Assert.True(Aplicar(corpo, Defeitos.FormatoInvalido));
        Assert.Equal("05/11/2026", Primeiro(corpo)["transactionDateTime"]!.GetValue<string>());

        var pelaData = Corpo();
        Assert.True(Aplicar(pelaData, Defeitos.FormatoInvalido, campo: "bookingDate"));
        Assert.Equal("05/11/2026", Primeiro(pelaData)["bookingDate"]!.GetValue<string>());

        var comFormato = Corpo();
        Assert.False(Aplicar(comFormato, Defeitos.FormatoInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"type":{},"x":{"format":"date-time"}}}}}}""")));
        Primeiro(comFormato)["x"] = "texto";
        Assert.True(Aplicar(comFormato, Defeitos.FormatoInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"type":{},"x":{"format":"date-time"}}}}}}""")));
        Assert.Equal("05/11/2026", Primeiro(comFormato)["x"]!.GetValue<string>());

        var curto = Corpo();
        Primeiro(curto)["type"] = 7;
        Assert.True(Aplicar(curto, Defeitos.FormatoInvalido, campo: "type"));
        Assert.Equal("05/11/2026", Primeiro(curto)["type"]!.GetValue<string>());

        var pequeno = Corpo();
        Assert.True(Aplicar(pequeno, Defeitos.FormatoInvalido, campo: "transactionId"));
        Assert.Equal("05/11/2026", Primeiro(pequeno)["transactionId"]!.GetValue<string>());

        var invalida = Corpo();
        Primeiro(invalida)["transactionName"] = "nao e uma data";
        Assert.True(Aplicar(invalida, Defeitos.FormatoInvalido, campo: "transactionName"));
        Assert.Equal("05/11/2026", Primeiro(invalida)["transactionName"]!.GetValue<string>());

        Assert.False(Aplicar(Corpo(), Defeitos.FormatoInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"type":{"format":"uuid"}}}}}}""")));
        Assert.False(Aplicar(Corpo(), Defeitos.FormatoInvalido, campo: "inexistente"));
    }

    [Fact]
    public void Enum_invalido_prefere_type_e_aceita_campo()
    {
        var corpo = Corpo();
        Assert.True(Aplicar(corpo, Defeitos.EnumInvalido));
        Assert.Equal("VALOR_FORA_DA_LISTA", Primeiro(corpo)["type"]!.GetValue<string>());
        Assert.Equal("A", Primeiro(corpo)["completedAuthorisedPaymentType"]!.GetValue<string>());

        var semType = Corpo();
        Assert.True(Aplicar(semType, Defeitos.EnumInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"type":{"type":"string"},"completedAuthorisedPaymentType":{"enum":["A"]}}}}}}""")));
        Assert.Equal("VALOR_FORA_DA_LISTA", Primeiro(semType)["completedAuthorisedPaymentType"]!.GetValue<string>());

        var comCampo = Corpo();
        Assert.True(Aplicar(comCampo, Defeitos.EnumInvalido, campo: "bookingDate"));
        Assert.Equal("VALOR_FORA_DA_LISTA", Primeiro(comCampo)["bookingDate"]!.GetValue<string>());

        Assert.False(Aplicar(Corpo(), Defeitos.EnumInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"x":null,"type":{}}}}}}""")));
        Assert.False(Aplicar(Corpo(), Defeitos.EnumInvalido, campo: "inexistente"));
    }

    [Fact]
    public void Casos_de_borda_do_schema()
    {
        // Lista sem items, ou items sem properties: não há campo de data nem enum.
        Assert.False(Aplicar(Corpo(), Defeitos.FormatoInvalido, schema: Schema("""{"properties":{"data":{"type":"array"}}}""")));
        Assert.False(Aplicar(Corpo(), Defeitos.FormatoInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{}}}}""")));

        // Nome terminado em Date, sem format, é data.
        var semFormato = Corpo();
        Primeiro(semFormato)["valueDate"] = "2026-11-01";
        Assert.True(Aplicar(semFormato, Defeitos.FormatoInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"type":{},"valueDate":{"type":"string"}}}}}}""")));
        Assert.Equal("01/11/2026", Primeiro(semFormato)["valueDate"]!.GetValue<string>());

        // type sem definição não conta como enum.
        var tipoNulo = Corpo();
        Primeiro(tipoNulo)["x"] = "A";
        Assert.True(Aplicar(tipoNulo, Defeitos.EnumInvalido, schema: Schema("""{"properties":{"data":{"type":"array","items":{"properties":{"type":null,"x":{"enum":["A"]}}}}}}""")));
        Assert.Equal("VALOR_FORA_DA_LISTA", Primeiro(tipoNulo)["x"]!.GetValue<string>());
        Assert.Equal("PIX", Primeiro(tipoNulo)["type"]!.GetValue<string>());

        // O primeiro objeto sem amount não é valor.
        var outroObjeto = new JsonObject { ["data"] = new JsonArray(new JsonObject { ["outro"] = new JsonObject { ["currency"] = "BRL" }, ["valor"] = new JsonObject { ["amount"] = "1.00", ["currency"] = "BRL" } }) };
        Assert.True(Aplicar(outroObjeto, Defeitos.ValorIncoerente));
        Assert.Equal("BRL", Primeiro(outroObjeto)["outro"]!["currency"]!.GetValue<string>());
        Assert.Equal("USD", Primeiro(outroObjeto)["valor"]!["currency"]!.GetValue<string>());
    }

    [Fact]
    public void Valor_incoerente_troca_a_moeda_do_primeiro_objeto_de_valor()
    {
        var corpo = Corpo();
        Assert.True(Aplicar(corpo, Defeitos.ValorIncoerente));
        Assert.Equal("USD", Primeiro(corpo)["transactionAmount"]!["currency"]!.GetValue<string>());
        Assert.Equal("BRL", corpo["data"]![1]!["transactionAmount"]!["currency"]!.GetValue<string>());

        var semValor = Corpo();
        Primeiro(semValor)["transactionAmount"] = new JsonObject { ["amount"] = "1.00" };
        Assert.False(Aplicar(semValor, Defeitos.ValorIncoerente));
    }

    [Fact]
    public void Detalhe_usa_o_objeto_data()
    {
        var detalhe = new JsonObject { ["data"] = Item("t0") };
        var schema = Schema("""{"properties":{"data":{"type":"object","required":["transactionId","transactionAmount"],"properties":{"transactionDateTime":{}}}}}""");
        Assert.True(Aplicar(detalhe, Defeitos.CampoObrigatorioAusente, schema: schema));
        Assert.False(detalhe["data"]!.AsObject().ContainsKey("transactionAmount"));
        Assert.True(Aplicar(detalhe, Defeitos.DataForaDaJanela, schema: schema));
        Assert.Equal("2024-11-05T00:00:00.000Z", detalhe["data"]!["transactionDateTime"]!.GetValue<string>());
        Assert.False(Aplicar(detalhe, Defeitos.TransactionIdRepetido, schema: schema));
        Assert.False(Aplicar(new JsonObject { ["data"] = "texto" }, Defeitos.EnumInvalido, schema: schema));
    }

    [Fact]
    public void TransactionId_repetido_copia_o_primeiro_no_segundo()
    {
        var corpo = Corpo(3);
        Assert.True(Aplicar(corpo, Defeitos.TransactionIdRepetido));
        Assert.Equal("t0", corpo["data"]![1]!["transactionId"]!.GetValue<string>());
        Assert.Equal("t2", corpo["data"]![2]!["transactionId"]!.GetValue<string>());

        Assert.False(Aplicar(Corpo(1), Defeitos.TransactionIdRepetido));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray("a", Item("b")) }, Defeitos.TransactionIdRepetido));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(Item("a"), "b") }, Defeitos.TransactionIdRepetido));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(new JsonObject(), Item("b")) }, Defeitos.TransactionIdRepetido));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(Item("a"), new JsonObject()) }, Defeitos.TransactionIdRepetido));
    }

    [Fact]
    public void Total_incoerente_soma_um_sem_mudar_as_paginas()
    {
        var corpo = Corpo(2);
        Assert.True(Aplicar(corpo, Defeitos.TotalRecordsIncoerente));
        Assert.Equal(3, corpo["meta"]!["totalRecords"]!.GetValue<int>());
        Assert.Equal(1, corpo["meta"]!["totalPages"]!.GetValue<int>());

        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray() }, Defeitos.TotalRecordsIncoerente));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(), ["meta"] = new JsonObject() }, Defeitos.TotalRecordsIncoerente));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(), ["meta"] = new JsonObject { ["totalRecords"] = "x" } }, Defeitos.TotalRecordsIncoerente));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(), ["meta"] = new JsonObject { ["totalRecords"] = new JsonObject() } }, Defeitos.TotalRecordsIncoerente));
    }

    [Fact]
    public void Data_fora_da_janela_vai_para_dois_anos_antes_do_instante()
    {
        var corpo = Corpo();
        Assert.True(Aplicar(corpo, Defeitos.DataForaDaJanela));
        Assert.Equal("2024-11-05T00:00:00.000Z", Primeiro(corpo)["transactionDateTime"]!.GetValue<string>());
        Assert.Equal("2024-11-05", Primeiro(corpo)["bookingDate"]!.GetValue<string>());
        Assert.Equal("2026-11-05", corpo["data"]![1]!["bookingDate"]!.GetValue<string>());

        var semTransacao = new JsonObject { ["data"] = new JsonArray(new JsonObject { ["accountId"] = "a" }, "x") };
        Assert.False(Aplicar(semTransacao, Defeitos.DataForaDaJanela));

        var semDatas = new JsonObject { ["data"] = new JsonArray(new JsonObject { ["transactionId"] = "t" }) };
        Assert.False(Aplicar(semDatas, Defeitos.DataForaDaJanela));
    }

    [Fact]
    public void Dado_muda_so_na_pagina_seguinte()
    {
        Assert.False(Aplicar(Corpo(), Defeitos.DadoMudaNaPaginacao));
        Assert.False(Aplicar(new JsonObject { ["data"] = Item("t") }, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.False(Aplicar(Corpo(0), Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.False(Aplicar(Corpo(), Defeitos.DadoMudaNaPaginacao, quantidade: 0, seguinte: true));
    }

    [Fact]
    public void Dado_muda_acrescenta_copias_com_novo_identificador_e_recalcula_paginas()
    {
        var corpo = Corpo(2, total: 50);
        Assert.True(Aplicar(corpo, Defeitos.DadoMudaNaPaginacao, quantidade: 2, seguinte: true));
        var ids = corpo["data"]!.AsArray().Select(i => i!["transactionId"]!.GetValue<string>()).ToList();
        Assert.Equal(["t0-N1", "t0-N2", "t0", "t1"], ids);
        Assert.Equal(52, corpo["meta"]!["totalRecords"]!.GetValue<int>());
        Assert.Equal(3, corpo["meta"]!["totalPages"]!.GetValue<int>());

        var padrao = Corpo(1, total: 1, self: "http://x/t?page-size=10");
        Assert.True(Aplicar(padrao, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.Equal(2, padrao["meta"]!["totalRecords"]!.GetValue<int>());
        Assert.Equal(1, padrao["meta"]!["totalPages"]!.GetValue<int>());
    }

    [Fact]
    public void Dado_muda_corta_o_identificador_longo()
    {
        var corpo = Corpo(1);
        Primeiro(corpo)["transactionId"] = new string('a', 99);
        Assert.True(Aplicar(corpo, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        var novo = Primeiro(corpo)["transactionId"]!.GetValue<string>();
        Assert.Equal(100, novo.Length);
        Assert.EndsWith("-N1", novo, StringComparison.Ordinal);
    }

    [Fact]
    public void Dado_muda_usa_o_primeiro_id_texto_e_desiste_sem_identificador()
    {
        var corpo = new JsonObject { ["data"] = new JsonArray(new JsonObject { ["numeroId"] = 3, ["accountId"] = "c" }) };
        Assert.True(Aplicar(corpo, Defeitos.DadoMudaNaPaginacao, seguinte: true, schema: Schema("""{"properties":{"data":{"type":"array","items":{"required":["numeroId"]}}}}""")));
        Assert.Equal("c-N1", Primeiro(corpo)["accountId"]!.GetValue<string>());

        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray(new JsonObject { ["nome"] = "x" }) }, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.False(Aplicar(new JsonObject { ["data"] = new JsonArray("x") }, Defeitos.DadoMudaNaPaginacao, seguinte: true));
    }

    [Fact]
    public void Dado_muda_com_quantidade_negativa_tira_itens()
    {
        var corpo = Corpo(3, total: 30);
        Assert.True(Aplicar(corpo, Defeitos.DadoMudaNaPaginacao, quantidade: -2, seguinte: true));
        Assert.Single(corpo["data"]!.AsArray());
        Assert.Equal(28, corpo["meta"]!["totalRecords"]!.GetValue<int>());
        Assert.Equal(2, corpo["meta"]!["totalPages"]!.GetValue<int>());

        var poucos = Corpo(1, total: 0, self: null);
        Assert.True(Aplicar(poucos, Defeitos.DadoMudaNaPaginacao, quantidade: -5, seguinte: true));
        Assert.Empty(poucos["data"]!.AsArray());
        Assert.Equal(0, poucos["meta"]!["totalRecords"]!.GetValue<int>());
        Assert.Equal(1, poucos["meta"]!["totalPages"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("http://x/t?page=1")]
    [InlineData("http://x/t?page-size=abc&page=1")]
    [InlineData("http://x/t?page-size=0")]
    public void Sem_tamanho_de_pagina_valido_mantem_totalPages(string self)
    {
        var corpo = Corpo(2, total: 100, self: self);
        Assert.True(Aplicar(corpo, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.Equal(101, corpo["meta"]!["totalRecords"]!.GetValue<int>());
        Assert.Equal(1, corpo["meta"]!["totalPages"]!.GetValue<int>());
    }

    [Fact]
    public void Sem_totalPages_ou_sem_meta_so_muda_os_itens()
    {
        var semPaginas = Corpo(1, total: 1);
        semPaginas["meta"]!.AsObject().Remove("totalPages");
        Assert.True(Aplicar(semPaginas, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.False(semPaginas["meta"]!.AsObject().ContainsKey("totalPages"));

        var semMeta = new JsonObject { ["data"] = new JsonArray(Item("t")), ["links"] = new JsonObject() };
        Assert.True(Aplicar(semMeta, Defeitos.DadoMudaNaPaginacao, seguinte: true));
        Assert.Equal(2, semMeta["data"]!.AsArray().Count);
    }

    [Fact]
    public void Regras_de_defeito_disputam_so_entre_si()
    {
        RegraDeCenario Regra(string id, EfeitoDaRegra efeito, string? cliente = null) =>
            new(id, null, "banco", null, cliente, null, efeito, 100m, null, null, null, true, null, Agora, Agora);
        var regras = new[]
        {
            Regra("status", new EfeitoDaRegra(Efeitos.Status, Status: 500)),
            Regra("geral", new EfeitoDaRegra(Efeitos.Defeito, Defeito: Defeitos.EnumInvalido)),
            Regra("cliente", new EfeitoDaRegra(Efeitos.Defeito, Defeito: Defeitos.FormatoInvalido), cliente: "cli"),
        };

        Assert.Equal("status", SelecaoDeRegras.Vencedora(regras, Chamada, Agora, Declarados)!.Id);
        Assert.Equal("cliente", SelecaoDeRegras.VencedoraDeDefeito(regras, Chamada, Agora)!.Id);
        Assert.Null(SelecaoDeRegras.Vencedora(regras[1..], Chamada, Agora, Declarados));
        Assert.Null(SelecaoDeRegras.VencedoraDeDefeito(regras[..1], Chamada, Agora));
    }

    [Theory]
    [InlineData(Defeitos.EnumInvalido, null, null, true)]
    [InlineData(Defeitos.EnumInvalido, "type", null, true)]
    [InlineData(Defeitos.ValorIncoerente, "type", null, false)]
    [InlineData(Defeitos.DadoMudaNaPaginacao, null, -3, true)]
    [InlineData(Defeitos.DadoMudaNaPaginacao, null, 0, false)]
    [InlineData(Defeitos.EnumInvalido, null, 2, false)]
    [InlineData("outro", null, null, false)]
    [InlineData(null, null, null, false)]
    public void Valida_o_efeito_de_defeito(string? defeito, string? campo, int? quantidade, bool valido)
    {
        var regra = new RegraDeCenario("r", null, "banco", null, null, null, new EfeitoDaRegra(Efeitos.Defeito, Defeito: defeito, Campo: campo, Quantidade: quantidade), 10m, null, null, 1, true, null, Agora, Agora);
        Assert.Equal(valido, ValidacaoDeRegra.Validar(regra, Declarados, _ => true).Count == 0);
    }
}
