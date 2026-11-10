using System.Text.Json.Nodes;
using Simulador.Dados.Aplicacao;
using Simulador.Dados.Dominio;

namespace Simulador.Testes.Unidade.Dados;

public class DominioDosDadosTests
{
    private static readonly DateOnly Hoje = new(2026, 11, 5);
    private static readonly byte[] Segredo = [1, 2, 3];
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("accounts", "ACCOUNT", true)]
    [InlineData("funds", "FUND", false)]
    public void Familia_traz_tipo_e_futuro(string contrato, string tipo, bool futuro)
    {
        var familia = Familias.De(contrato)!;
        Assert.Equal(tipo, familia.TipoDeRecurso);
        Assert.Equal(futuro, familia.AceitaFuturo);
    }

    [Fact]
    public void Contrato_fora_das_familias_nao_e_atendido() => Assert.Null(Familias.De("consents"));

    [Theory]
    [InlineData("/personal/identifications", "Cadastral", null)]
    [InlineData("/accounts", "ListaDeRecursos", null)]
    [InlineData("/accounts/{accountId}", "Recurso", "accountId")]
    [InlineData("/broker-notes/{brokerNoteId}", "NotaDeNegociacao", "brokerNoteId")]
    [InlineData("/accounts/{accountId}/transactions", "Transacoes", "accountId")]
    [InlineData("/accounts/{accountId}/transactions-current", "TransacoesRecentes", "accountId")]
    [InlineData("/accounts/{id}/bills", "Faturas", "id")]
    [InlineData("/accounts/{id}/bills/{billId}/transactions", "TransacoesDaFatura", "id")]
    [InlineData("/contracts/{contractId}/warranties", "Sublista", "contractId")]
    [InlineData("/operations/{operationId}/events", "Sublista", "operationId")]
    [InlineData("/accounts/{accountId}/balances", "Detalhe", "accountId")]
    public void Classifica_o_caminho(string caminho, string tipo, string? parametro) =>
        Assert.Equal((Enum.Parse<TipoDeOperacao>(tipo), parametro), Classificador.De(caminho));

    [Theory]
    [InlineData(null, null, 25, 1000, 1, 25)]
    [InlineData("3", "5", 1, 1000, 3, 5)]
    [InlineData("1", "5", 25, 1000, 1, 25)]
    [InlineData("", "", 25, 1000, 1, 25)]
    [InlineData("1", "5000", 1, 800, 1, 800)]
    public void Aplica_minimo_e_maximo(string? pagina, string? tamanho, int minimo, int maximo, int esperadaPagina, int esperadoTamanho)
    {
        var pedido = PedidoDePagina.Ler(pagina, tamanho, minimo, maximo)!;
        Assert.Equal(esperadaPagina, pedido.Pagina);
        Assert.Equal(esperadoTamanho, pedido.Tamanho);
    }

    [Theory]
    [InlineData("0", null)]
    [InlineData("x", null)]
    [InlineData(null, "0")]
    [InlineData(null, "x")]
    public void Recusa_pagina_ou_tamanho_invalido(string? pagina, string? tamanho) =>
        Assert.Null(PedidoDePagina.Ler(pagina, tamanho, 1, 1000));

    [Fact]
    public void Calcula_paginas_e_faixa()
    {
        var pedido = new PedidoDePagina(2, 25);
        Assert.Equal(0, pedido.TotalDePaginas(0));
        Assert.Equal(2, pedido.TotalDePaginas(47));
        Assert.False(pedido.ForaDaFaixa(26));
        Assert.True(pedido.ForaDaFaixa(25));
        Assert.False(new PedidoDePagina(1, 25).ForaDaFaixa(0));
    }

    [Theory]
    [InlineData(null, null, false, false, "Nenhum")]
    [InlineData("2026-11-01", null, false, false, "NaoInformado")]
    [InlineData(null, "2026-11-01", false, false, "NaoInformado")]
    [InlineData("2026-xx-01", "2026-11-01", false, false, "Invalido")]
    [InlineData("2026-11-01", "2026-xx-01", false, false, "Invalido")]
    [InlineData("2026-11-02", "2026-11-01", false, false, "Invalido")]
    [InlineData("2025-11-05", "2026-11-05", false, false, "Nenhum")]
    [InlineData("2025-11-04", "2026-11-05", false, false, "Invalido")]
    [InlineData("2026-10-30", "2026-11-05", true, false, "Nenhum")]
    [InlineData("2026-10-29", "2026-11-05", true, false, "Invalido")]
    [InlineData("2026-11-05", "2026-11-06", false, false, "Invalido")]
    [InlineData("2026-11-05", "2027-11-05", false, true, "Nenhum")]
    [InlineData("2026-11-05", "2027-11-06", false, true, "Invalido")]
    public void Janela_de_transacoes(string? de, string? ate, bool recente, bool futuro, string erro) =>
        Assert.Equal(Enum.Parse<ErroDeData>(erro), JanelaDeDatas.Transacoes(de, ate, Hoje, recente, futuro).Erro);

    [Fact]
    public void Sem_datas_vale_hoje() => Assert.Equal(new Periodo(Hoje, Hoje), JanelaDeDatas.Transacoes(null, null, Hoje, false, false));

    [Theory]
    [InlineData(false, 0u, "PAGAMENTO", null)]
    [InlineData(false, 1u, "TARIFA", "feeType")]
    [InlineData(false, 2u, "OPERACOES_CREDITO_CONTRATADAS_CARTAO", "otherCreditsType")]
    [InlineData(false, 3u, "OUTROS", "transactionalAdditionalInfo")]
    [InlineData(true, 0u, "PAGAMENTO_FATURA", null)]
    [InlineData(true, 3u, "OUTROS", "transactionalAdditionalInfo")]
    public void Tipo_do_cartao_traz_o_campo_condicional(bool credito, uint semente, string tipo, string? campo)
    {
        var valores = new Dictionary<string, string>(StringComparer.Ordinal);

        TiposDoCartao.Preencher(valores, credito, semente);

        Assert.Equal(tipo, valores["transactionType"]);
        Assert.Equal(campo is null ? 1 : 2, valores.Count);
        Assert.True(campo is null || valores.ContainsKey(campo));
    }

    [Fact]
    public void Faturas_aceitam_datas_isoladas_e_12_meses()
    {
        Assert.Equal(new Periodo(Hoje.AddMonths(-12), Hoje.AddMonths(1)), JanelaDeDatas.Faturas(null, null, Hoje));
        Assert.Equal(new DateOnly(2026, 1, 1), JanelaDeDatas.Faturas("2026-01-01", null, Hoje).De);
        Assert.Equal(new DateOnly(2026, 1, 1), JanelaDeDatas.Faturas(null, "2026-01-01", Hoje).Ate);
        Assert.Equal(ErroDeData.Invalido, JanelaDeDatas.Faturas("x", null, Hoje).Erro);
        Assert.Equal(ErroDeData.Invalido, JanelaDeDatas.Faturas(null, "x", Hoje).Erro);
        Assert.Equal(ErroDeData.Invalido, JanelaDeDatas.Faturas("2026-02-01", "2026-01-01", Hoje).Erro);
        Assert.Equal(Hoje.AddMonths(-12), JanelaDeDatas.Faturas("2020-01-01", null, Hoje).De);
        Assert.Equal(ErroDeData.Nenhum, JanelaDeDatas.Faturas("2020-01-01", "2020-02-01", Hoje).Erro);
    }

    [Fact]
    public void Transacoes_da_fatura_exigem_o_par_e_ficam_na_fatura()
    {
        var inicio = new DateOnly(2026, 10, 1);
        var fim = new DateOnly(2026, 10, 31);
        Assert.Equal(new Periodo(inicio, fim), JanelaDeDatas.DaFatura(null, null, inicio, fim));
        Assert.Equal(ErroDeData.NaoInformado, JanelaDeDatas.DaFatura("2026-10-02", null, inicio, fim).Erro);
        Assert.Equal(new Periodo(inicio, fim), JanelaDeDatas.DaFatura("2026-09-01", "2026-11-30", inicio, fim));
        Assert.Equal(new Periodo(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 3)), JanelaDeDatas.DaFatura("2026-10-02", "2026-10-03", inicio, fim));
    }

    [Fact]
    public void Chave_valida_devolve_a_emissao()
    {
        var real = Agora.AddHours(3);
        var chave = ChaveDePaginacao.Emitir(Agora, real, "c", Segredo);
        Assert.Equal(new LeituraDaChave(Agora, real, null), ChaveDePaginacao.Abrir(chave, "c", Segredo, Agora.AddMinutes(60), 60));
    }

    [Fact]
    public void Chave_recusada()
    {
        var chave = ChaveDePaginacao.Emitir(Agora, Agora, "c", Segredo);
        Assert.Null(ChaveDePaginacao.Abrir(chave, "c", Segredo, Agora.AddMinutes(61), 60));
        Assert.Null(ChaveDePaginacao.Abrir(chave, "outra", Segredo, Agora, 60));
        Assert.Null(ChaveDePaginacao.Abrir(null, "c", Segredo, Agora, 60));
        Assert.Null(ChaveDePaginacao.Abrir("x.1.y", "c", Segredo, Agora, 60));
        Assert.Null(ChaveDePaginacao.Abrir("1.x.y", "c", Segredo, Agora, 60));
        Assert.Null(ChaveDePaginacao.Abrir("1.1.y", "c", Segredo, Agora, 60));
        var partes = chave.Split('.');
        Assert.Null(ChaveDePaginacao.Abrir($"9999999999999999.{partes[1]}.{partes[2]}", "c", Segredo, Agora, 60));
    }

    /// <summary>Chave no formato anterior, sem o instante real, é inválida (RN36, RN37).</summary>
    [Fact]
    public void Chave_sem_hora_real_e_recusada()
    {
        var corpo = Agora.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var assinatura = System.Buffers.Text.Base64Url.EncodeToString(System.Security.Cryptography.HMACSHA256.HashData(Segredo, System.Text.Encoding.UTF8.GetBytes($"{corpo}|c")));
        Assert.Null(ChaveDePaginacao.Abrir($"{corpo}.{assinatura}", "c", Segredo, Agora, 60));
    }

    [Fact]
    public void Ids_derivados_levam_ao_recurso()
    {
        var mes = new DateOnly(2026, 10, 1);
        var fatura = IdsDerivados.DaFatura("r1", mes);
        Assert.Equal(mes, IdsDerivados.MesDaFatura(fatura, "r1"));
        Assert.Null(IdsDerivados.MesDaFatura(fatura, "r2"));
        Assert.Null(IdsDerivados.MesDaFatura("r1-bxx", "r1"));
        Assert.Equal("r1", IdsDerivados.RecursoDaNota(IdsDerivados.DaNota("r1")));
        Assert.Null(IdsDerivados.RecursoDaNota("r1"));
    }

    [Fact]
    public void Documentos_sao_repetiveis_e_validos()
    {
        Assert.Equal(Documentos.Semente("a"), Documentos.Semente("a"));
        var cpf = Documentos.Cpf(12345u);
        Assert.Equal(11, cpf.Length);
        Assert.StartsWith("9", cpf, StringComparison.Ordinal);
        Assert.Equal(14, Documentos.Cnpj(12345u).Length);
    }

    [Fact]
    public void Gerador_usa_valor_que_cabe_enum_exemplo_e_formato()
    {
        var schema = JsonNode.Parse("""
        {"type":"object","required":["a","b","c","d","e","f","g","h"],"properties":{
          "a":{"type":"string","pattern":"^\\d+$"},
          "b":{"type":"string","enum":["X","Y"]},
          "c":{"type":"string","example":"ok","maxLength":5},
          "d":{"type":"string","format":"date"},
          "e":{"type":"string","format":"date-time"},
          "f":{"type":"integer","minimum":3},
          "g":{"type":"number","example":1.5},
          "h":{"type":"boolean"},
          "opcional":{"type":"string"},
          "comValor":{"type":"object","properties":{"amount":{"type":"string"}}},
          "lista":{"type":"array","minItems":2,"items":{"type":"string"}}
        }}
        """);
        var valores = new Dictionary<string, string>(StringComparer.Ordinal) { ["a"] = "nao", ["comValor.amount"] = "10.00", ["lista"] = "v" };

        var saida = GeradorPorSchema.Gerar(schema, new Preenchimento(valores, "base", 1u))!;

        Assert.Equal("A", saida["a"]!.GetValue<string>());
        Assert.Equal("X", saida["b"]!.GetValue<string>());
        Assert.Equal("ok", saida["c"]!.GetValue<string>());
        Assert.Equal("2026-01-15", saida["d"]!.GetValue<string>());
        Assert.Equal("2026-01-15T12:00:00Z", saida["e"]!.GetValue<string>());
        Assert.Equal(3L, saida["f"]!.GetValue<long>());
        Assert.Equal(1.5m, saida["g"]!.GetValue<decimal>());
        Assert.False(saida["h"]!.GetValue<bool>());
        Assert.Null(saida["opcional"]);
        Assert.Equal("10.00", saida["comValor"]!["amount"]!.GetValue<string>());
        Assert.Equal(2, saida["lista"]!.AsArray().Count);
    }

    [Fact]
    public void Gerador_deriva_id_e_documento()
    {
        var schema = JsonNode.Parse("""
        {"type":"object","required":["contaId","cpfNumber","ignorado"],"properties":{
          "contaId":{"type":"string","maxLength":100},
          "cpfNumber":{"type":"string","pattern":"^\\d{11}$"},
          "ignorado":{"type":"string","minLength":3}
        }}
        """);

        var saida = GeradorPorSchema.Gerar(schema, new Preenchimento(new Dictionary<string, string>(StringComparer.Ordinal), "base", 7u))!;

        Assert.Equal("base-contaId", saida["contaId"]!.GetValue<string>());
        Assert.Equal(Documentos.Cpf(7u), saida["cpfNumber"]!.GetValue<string>());
        Assert.Equal("AAA", saida["ignorado"]!.GetValue<string>());
        Assert.Null(GeradorPorSchema.Gerar(null, new Preenchimento(new Dictionary<string, string>(StringComparer.Ordinal), "b", 1u)));
    }
}
