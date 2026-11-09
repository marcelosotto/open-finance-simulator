using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulador.Instituicoes.Contratos;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Massa;

internal static class Listas
{
    public static readonly string[] Pix = ["PIX"];
    public static readonly string[] Contas = ["contas"];
    public static readonly string[] Nada = ["NADA"];
}

public abstract class TesteDaMassa : IDisposable
{
    protected const string Banco = "banco-estavel";
    protected const string Outro = "banco-lento";

    private readonly HttpClient _http;

    protected TesteDaMassa()
    {
        Fabrica = new FabricaDoSimulador();
        _http = Fabrica.ClienteAdministracao();
        Admin = new AdminDaMassa(_http);
    }

    protected FabricaDoSimulador Fabrica { get; }

    protected AdminDaMassa Admin { get; }

    protected DateOnly Hoje => DateOnly.FromDateTime(Fabrica.Relogio.GetUtcNow().UtcDateTime);

    protected static object Distribuicao(params (string Banco, decimal Pct)[] bancos) => new
    {
        bancos = bancos.ToDictionary(b => b.Banco, b => b.Pct),
        perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100m },
    };

    protected static object PerfilFixo(string produto = "contas") => new
    {
        transacoesMin = 2,
        transacoesMax = 2,
        valorMin = 100m,
        valorMax = 100m,
        saldoInicial = 1_000m,
        tiposDeLancamento = Listas.Pix,
        produtos = new[] { produto },
    };

    protected Task<MassaGerada> CriarCliente(string perfil = "cliente-comum", params string[] bancos) =>
        Admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", perfil, bancos = bancos.Length == 0 ? [Banco] : bancos, semente = 7 });

    protected Task<MundoVirtual> DefinirMundo(int total = 1_000, long semente = 42) =>
        Admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total, semente, distribuicao = Distribuicao((Banco, 50m), (Outro, 50m)), confirmar = true });

    protected Task<IReadOnlyList<MassaGerada>> Itens(int inicio, int quantidade, string? banco = null) =>
        Admin.Ler<IReadOnlyList<MassaGerada>>($"/mundo-virtual/itens?inicio={inicio}&quantidade={quantidade}" + (banco is null ? string.Empty : $"&banco={banco}"));

    public void Dispose()
    {
        _http.Dispose();
        Fabrica.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class AdminMassaTests : TesteDaMassa
{
    [Fact]
    public async Task Lista_os_perfis_prontos()
    {
        var perfis = await Admin.Ler<IReadOnlyList<PerfilDeDados>>("/perfis");

        Assert.Contains(perfis, p => p.Nome == "alto-volume");
    }

    [Fact]
    public async Task Cria_altera_e_apaga_perfil()
    {
        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/meu", PerfilFixo());
        Assert.Contains(await Admin.Ler<IReadOnlyList<PerfilDeDados>>("/perfis"), p => p.Nome == "meu");

        Assert.Equal(HttpStatusCode.NoContent, (await Admin.Enviar(HttpMethod.Delete, "/perfis/meu")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Delete, "/perfis/meu")).Status);
        Assert.Contains(await Admin.Ler<IReadOnlyList<EventoDaMassa>>("/historico"), e => e.Acao == "perfil-apagado");
    }

    [Fact]
    public async Task Perfil_pronto_nao_muda_nem_sai()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Put, "/perfis/alto-volume", PerfilFixo())).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Delete, "/perfis/alto-volume")).Status);
    }

    [Fact]
    public async Task Perfil_invalido_e_corpo_invalido_sao_recusados()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Put, "/perfis/x", new { transacoesMin = 5, transacoesMax = 1, valorMin = 1m, valorMax = 1m, saldoInicial = 0m, tiposDeLancamento = Listas.Pix, produtos = Listas.Contas })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Put, "/perfis/x", new { nada = 1 })).Status);
    }

    [Fact]
    public async Task Perfil_em_uso_nao_sai()
    {
        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/usado", PerfilFixo());
        await CriarCliente("usado");

        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Delete, "/perfis/usado")).Status);
    }

    [Fact]
    public async Task Perfil_em_distribuicao_nao_sai()
    {
        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/usado", PerfilFixo());
        await Admin.Enviar<DistribuicaoNomeada>(HttpMethod.Put, "/distribuicoes/d", new
        {
            bancos = new Dictionary<string, decimal> { [Banco] = 100m },
            perfis = new Dictionary<string, decimal> { ["usado"] = 100m },
        });

        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Delete, "/perfis/usado")).Status);
    }

    [Fact]
    public async Task Salva_lista_e_apaga_distribuicao()
    {
        await Admin.Enviar<DistribuicaoNomeada>(HttpMethod.Put, "/distribuicoes/minha", Distribuicao((Banco, 100m)));
        Assert.Single(await Admin.Ler<IReadOnlyList<DistribuicaoNomeada>>("/distribuicoes"));

        Assert.Equal(HttpStatusCode.NoContent, (await Admin.Enviar(HttpMethod.Delete, "/distribuicoes/minha")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Delete, "/distribuicoes/minha")).Status);
    }

    [Fact]
    public async Task Distribuicao_com_banco_ou_perfil_inexistente_e_recusada()
    {
        var (status, corpo) = await Admin.Enviar(HttpMethod.Put, "/distribuicoes/x", new
        {
            bancos = new Dictionary<string, decimal> { ["nao-existe"] = 100m },
            perfis = new Dictionary<string, decimal> { ["nao-existe"] = 100m },
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Contains("banco desconhecido", corpo, StringComparison.Ordinal);
        Assert.Contains("perfil desconhecido", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cliente_em_dois_bancos_tem_produtos_em_cada_um()
    {
        var massa = await CriarCliente("cliente-comum", Banco, Outro);

        Assert.Equal([Banco, Outro], massa.Recursos.Select(r => r.Banco).Distinct());
        Assert.StartsWith("9", massa.Cliente.Documento, StringComparison.Ordinal);
        Assert.StartsWith("[HML] ", massa.Cliente.Nome, StringComparison.Ordinal);
        Assert.Equal(Naturezas.Persistido, massa.Cliente.Natureza);
        Assert.Contains("-p-", massa.Recursos[0].ResourceId, StringComparison.Ordinal);

        var lida = await Admin.Ler<MassaGerada>($"/clientes/{massa.Cliente.Documento}");
        Assert.Equal(massa.Recursos.Count, lida.Recursos.Count);
    }

    [Fact]
    public async Task Nome_informado_e_semente_gerada()
    {
        var massa = await Admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PJ", bancos = new[] { Banco }, nome = "Comércio Sol" });

        Assert.Equal("[HML] Comércio Sol", massa.Cliente.Nome);
        Assert.Equal(14, massa.Cliente.Documento.Length);
    }

    [Fact]
    public async Task Documento_informado_e_recusado()
    {
        var (status, _) = await Admin.Enviar(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco }, documento = "12345678909" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
    }

    [Fact]
    public async Task Cliente_invalido_e_recusado()
    {
        var (status, corpo) = await Admin.Enviar(HttpMethod.Post, "/clientes", new { tipo = "XX", bancos = Array.Empty<string>(), perfil = "nao-existe" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Contains("tipo", corpo, StringComparison.Ordinal);
        Assert.Contains("perfil desconhecido", corpo, StringComparison.Ordinal);
        Assert.Contains("pelo menos um banco", corpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cliente_sem_saldo_de_documentos_e_recusado()
    {
        Fabrica.Massa.Posicoes = 49_999_999;

        var (status, _) = await Admin.Enviar(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Equal(49_999_999, Fabrica.Massa.Posicoes);
    }

    [Fact]
    public async Task Cliente_desconhecido_responde_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/clientes/123")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/consentimentos/urn:hml:p-x")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/recursos/hml-p-x/transacoes")).Status);
    }

    [Fact]
    public async Task Instituicao_com_cliente_e_inativada_em_vez_de_apagada()
    {
        using var http = Fabrica.ClienteAdministracao();
        using var conteudo = System.Net.Http.Json.JsonContent.Create(new { nome = "Banco Massa" });
        using var criacao = await http.PostAsync(new Uri("/admin/instituicoes", UriKind.Relative), conteudo, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, criacao.StatusCode);
        await CriarCliente("cliente-comum", "banco-massa");

        using var remocao = await http.DeleteAsync(new Uri("/admin/instituicoes/banco-massa", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Contains("inativada", await remocao.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }
}

public class LotesTests : TesteDaMassa
{
    [Fact]
    public async Task Lote_de_100_conclui_com_resumo_e_consentimentos_autorizados()
    {
        var lote = await Admin.GerarLote(new { quantidade = 100, semente = 42 });

        Assert.Equal(SituacoesDeLote.Concluido, lote.Situacao);
        Assert.Equal(100, lote.Processados);
        Assert.Equal(100, lote.Consentimentos);
        Assert.Equal(1, lote.VersaoDoGerador);
        Assert.All(await Admin.MassaDoLote(lote.Id), m => Assert.All(m.Consentimentos, c => Assert.Equal("AUTHORISED", c.Estado)));
        Assert.Single(await Admin.Ler<IReadOnlyList<Lote>>("/lotes"));
    }

    [Fact]
    public async Task Lote_so_com_a_quantidade_usa_os_padroes_e_mostra_a_semente()
    {
        var lote = await Admin.GerarLote(new { quantidade = 2 });

        Assert.True(lote.Semente > 0);
        Assert.Equal(100m, lote.Distribuicao.Bancos[Banco]);
        Assert.Equal(1, lote.ConsentimentosPorCliente);
    }

    [Fact]
    public async Task Lote_usa_distribuicao_salva()
    {
        await Admin.Enviar<DistribuicaoNomeada>(HttpMethod.Put, "/distribuicoes/d", Distribuicao((Outro, 100m)));

        var lote = await Admin.GerarLote(new { quantidade = 1, distribuicaoNome = "d" });

        Assert.Equal(Outro, (await Admin.MassaDoLote(lote.Id))[0].Consentimentos[0].Banco);
    }

    [Fact]
    public async Task Lotes_com_a_mesma_semente_mudam_so_identidade()
    {
        var a = (await Admin.MassaDoLote((await Admin.GerarLote(new { quantidade = 3, semente = 42 })).Id)).OrderBy(m => m.Cliente.Nome, StringComparer.Ordinal).ToList();
        var b = (await Admin.MassaDoLote((await Admin.GerarLote(new { quantidade = 3, semente = 42 })).Id)).OrderBy(m => m.Cliente.Nome, StringComparer.Ordinal).ToList();

        Assert.Empty(a.Select(m => m.Cliente.Documento).Intersect(b.Select(m => m.Cliente.Documento), StringComparer.Ordinal));
        Assert.Equal(a.Select(m => m.Cliente.Nome), b.Select(m => m.Cliente.Nome));
    }

    [Theory]
    [InlineData(0, 1, 12)]
    [InlineData(1, 0, 12)]
    [InlineData(1, 1, 0)]
    public async Task Parametros_invalidos_sao_recusados(int quantidade, int consentimentos, int meses)
    {
        var (status, _) = await Admin.Enviar(HttpMethod.Post, "/lotes", new { quantidade, consentimentosPorCliente = consentimentos, historicoMeses = meses });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
    }

    [Fact]
    public async Task Permissao_ou_distribuicao_desconhecida_e_recusada()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/lotes", new { quantidade = 1, permissoes = Listas.Nada })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/lotes", new { quantidade = 1, distribuicaoNome = "nao-existe" })).Status);
    }

    [Fact]
    public async Task Lote_acima_do_saldo_e_recusado()
    {
        var (status, corpo) = await Admin.Enviar(HttpMethod.Post, "/lotes", new { quantidade = 50_000_000 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Contains("saldo", corpo, StringComparison.Ordinal);
        Assert.Equal(0, Fabrica.Massa.Posicoes);
    }

    [Fact]
    public async Task Cancelar_mantem_o_que_foi_criado_como_parcial()
    {
        using var portao = new SemaphoreSlim(1);
        Fabrica.Massa.Portao = portao;
        await Admin.Ler<IReadOnlyList<Lote>>("/lotes");
        var lote = await Admin.Enviar<Lote>(HttpMethod.Post, "/lotes", new { quantidade = 1_200, semente = 1 });

        for (var i = 0; i < 3_000 && (await Admin.Ler<Lote>($"/lotes/{lote.Id}")).Processados == 0; i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.Equal(HttpStatusCode.Accepted, (await Admin.Enviar(HttpMethod.Post, $"/lotes/{lote.Id}/cancelar")).Status);
        portao.Release(5);

        var final = await Admin.Aguardar(lote.Id);
        Assert.Equal(SituacoesDeLote.Parcial, final.Situacao);
        Assert.Equal("cancelado", final.Motivo);
        Assert.InRange(final.Processados, 500, 1_000);
    }

    [Fact]
    public async Task Cancelar_lote_terminado_ou_inexistente_e_recusado()
    {
        var lote = await Admin.GerarLote(new { quantidade = 1 });

        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Post, $"/lotes/{lote.Id}/cancelar")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Post, "/lotes/nao-existe/cancelar")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/lotes/nao-existe")).Status);
    }

    [Fact]
    public async Task Falha_na_gravacao_vira_parcial()
    {
        Fabrica.Massa.FalharAoGravar = true;

        var lote = await Admin.GerarLote(new { quantidade = 1 });

        Assert.Equal(SituacoesDeLote.Parcial, lote.Situacao);
        Assert.Equal("falha", lote.Motivo);
    }

    [Fact]
    public async Task Lote_parado_ao_subir_vira_interrompido()
    {
        using var fabrica = new FabricaDoSimulador();
        var agora = fabrica.Relogio.GetUtcNow();
        var distribuicao = new Distribuicao(new Dictionary<string, decimal> { [Banco] = 100m }, new Dictionary<string, decimal> { ["cliente-comum"] = 100m });
        Lote Parado(string id, DateTimeOffset em) => new(id, 10, distribuicao, 1, null, 1, 1, 12, 0, agora, 0, SituacoesDeLote.EmAndamento, null, 0, 0, em);
        await fabrica.Massa.InserirLote(Parado("velho", agora.AddMinutes(-3)), TestContext.Current.CancellationToken);
        await fabrica.Massa.InserirLote(Parado("recente", agora), TestContext.Current.CancellationToken);
        using var http = fabrica.ClienteAdministracao();
        var admin = new AdminDaMassa(http);

        var velho = await admin.Ler<Lote>("/lotes/velho");
        var recente = await admin.Ler<Lote>("/lotes/recente");

        Assert.Equal("interrompido", velho.Motivo);
        Assert.Equal(SituacoesDeLote.EmAndamento, recente.Situacao);
    }
}

public class MundoVirtualTests : TesteDaMassa
{
    [Fact]
    public async Task Sem_mundo_responde_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/mundo-virtual")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/mundo-virtual/itens?inicio=0&quantidade=1")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/consentimentos/urn:hml:v-c0x0")).Status);
    }

    [Fact]
    public async Task Faixa_de_um_mundo_de_10_milhoes_e_repetivel_e_nao_grava()
    {
        await DefinirMundo(10_000_000);

        var a = await Itens(9_999_000, 1_000);
        var b = await Itens(9_999_000, 1_000);

        Assert.Equal(1_000, a.Count);
        Assert.Equal(JsonSerializer.Serialize(a, JsonDaMassa.Opcoes), JsonSerializer.Serialize(b, JsonDaMassa.Opcoes));
        Assert.Equal(new ResumoDeMassa(0, 0, 0, 0), await Fabrica.Massa.ContarTudo(TestContext.Current.CancellationToken));
        Assert.All(a, m => Assert.Equal(Naturezas.Virtual, m.Cliente.Natureza));
    }

    [Fact]
    public async Task Divide_entre_os_bancos_e_filtra()
    {
        await DefinirMundo();

        var todos = await Itens(0, 1_000);
        Assert.InRange(todos.Count(m => m.Consentimentos[0].Banco == Banco), 400, 600);

        var filtrados = await Itens(0, 100, Outro);
        Assert.Equal(100, filtrados.Count);
        Assert.All(filtrados, m => Assert.Equal(Outro, m.Consentimentos[0].Banco));
        Assert.Empty(await Itens(0, 10, "itau"));
    }

    [Fact]
    public async Task Faixa_alem_do_total_devolve_o_que_existe()
    {
        await DefinirMundo(10);

        Assert.Equal(3, (await Itens(7, 100)).Count);
        Assert.Empty(await Itens(50, 10));
    }

    [Theory]
    [InlineData("inicio=-1&quantidade=1")]
    [InlineData("inicio=0&quantidade=0")]
    [InlineData("inicio=0")]
    public async Task Faixa_invalida_e_recusada(string consulta)
    {
        await DefinirMundo();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Get, "/mundo-virtual/itens?" + consulta)).Status);
    }

    [Fact]
    public async Task Ids_e_documentos_virtuais_sao_resolvidos()
    {
        await DefinirMundo();
        var item = (await Itens(5, 1))[0];

        Assert.Equal(item.Consentimentos[0].ConsentId, (await Admin.Ler<Consentimento>($"/consentimentos/{item.Consentimentos[0].ConsentId}")).ConsentId);
        Assert.Equal(item.Cliente.Nome, (await Admin.Ler<MassaGerada>($"/clientes/{item.Cliente.Documento}")).Cliente.Nome);
        Assert.Equal(HttpStatusCode.OK, (await Admin.Enviar(HttpMethod.Get, $"/recursos/{item.Recursos[0].ResourceId}/transacoes")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/consentimentos/urn:hml:v-czzzzzzx0")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, "/recursos/hml-v-rzzzzzzx0/transacoes")).Status);
        Assert.Equal(1_000, (await Admin.Ler<MundoVirtual>("/mundo-virtual")).Total);
    }

    [Fact]
    public async Task Trocar_a_semente_sem_confirmar_avisa_e_nao_muda()
    {
        await DefinirMundo();

        var (status, corpo) = await Admin.Enviar(HttpMethod.Put, "/mundo-virtual", new { total = 1_000, semente = 43, distribuicao = Distribuicao((Banco, 50m), (Outro, 50m)) });

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Contains("confirme", corpo, StringComparison.Ordinal);
        Assert.Equal(42, (await Admin.Ler<MundoVirtual>("/mundo-virtual")).Semente);
    }

    [Fact]
    public async Task Mudar_a_distribuicao_exige_confirmar_e_o_total_nao()
    {
        await DefinirMundo();

        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Put, "/mundo-virtual", new { total = 1_000, semente = 42, distribuicao = Distribuicao((Banco, 100m)) })).Status);
        Assert.Equal(HttpStatusCode.OK, (await Admin.Enviar(HttpMethod.Put, "/mundo-virtual", new { total = 2_000, semente = 42, distribuicao = Distribuicao((Banco, 50m), (Outro, 50m)) })).Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50_000_001)]
    public async Task Total_fora_da_faixa_e_recusado(int total)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Put, "/mundo-virtual", new { total })).Status);
    }

    [Fact]
    public async Task Semente_gerada_e_mantida()
    {
        var mundo = await Admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total = 10 });

        var mantido = await Admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total = 20 });

        Assert.Equal(mundo.Semente, mantido.Semente);
    }

    [Fact]
    public async Task Banco_no_mundo_conta_como_com_clientes()
    {
        await DefinirMundo();
        var consulta = Fabrica.Services.GetRequiredService<IConsultaDeClientes>();

        Assert.True(await consulta.TemClientes(Banco, TestContext.Current.CancellationToken));
        Assert.False(await consulta.TemClientes("itau", TestContext.Current.CancellationToken));
    }
}

public class AjustesTests : TesteDaMassa
{
    private async Task<(Recurso Recurso, Transacao Transacao, decimal Saldo)> RecursoFixo()
    {
        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/fixo", PerfilFixo());
        var recurso = (await CriarCliente("fixo")).Recursos[0];
        var extrato = await Admin.Ler<JsonElement>($"/recursos/{recurso.ResourceId}/transacoes");
        var transacao = extrato.GetProperty("transacoes")[0].Deserialize<Transacao>(JsonDaMassa.Opcoes)!;
        return (recurso, transacao, extrato.GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task Ajuste_vence_o_calculo_e_reflete_no_saldo()
    {
        var (recurso, transacao, saldo) = await RecursoFixo();

        await Admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = recurso.ResourceId, dia = transacao.Dia, posicao = transacao.Posicao, valor = 90m });

        var extrato = await Admin.Ler<JsonElement>($"/recursos/{recurso.ResourceId}/transacoes");
        Assert.Equal(90m, extrato.GetProperty("transacoes")[0].GetProperty("valor").GetDecimal());
        Assert.Equal(saldo + (transacao.Direcao == "CREDITO" ? -10m : 10m), extrato.GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task Pedidos_incompletos_sao_recusados()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "outro" })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = "x" })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = "x", estado = "AUTHORISED" })).Status);
    }

    [Fact]
    public async Task Alvo_inexistente_responde_404()
    {
        var (recurso, transacao, _) = await RecursoFixo();

        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = recurso.ResourceId, dia = transacao.Dia, posicao = 99, valor = 1m })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = "hml-p-x", dia = transacao.Dia, posicao = 0, valor = 1m })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = "urn:hml:p-x", estado = "REJECTED" })).Status);
    }

    [Fact]
    public async Task Consentimento_persistido_nao_recebe_ajuste()
    {
        var lote = await Admin.GerarLote(new { quantidade = 1 });
        var consentimento = (await Admin.MassaDoLote(lote.Id))[0].Consentimentos[0];

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = consentimento.ConsentId, estado = "REJECTED" })).Status);
    }

    [Fact]
    public async Task Revogar_virtual_vale_nos_itens_e_troca_de_semente_apaga()
    {
        await DefinirMundo();
        var id = (await Itens(0, 1))[0].Consentimentos[0].ConsentId;

        await Admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = id, estado = "REJECTED" });

        Assert.Equal("REJECTED", (await Admin.Ler<Consentimento>($"/consentimentos/{id}")).Estado);
        Assert.Equal("USER", (await Itens(0, 1))[0].Consentimentos[0].RejeitadoPor);

        await DefinirMundo(semente: 43);

        Assert.Equal("AUTHORISED", (await Admin.Ler<Consentimento>($"/consentimentos/{id}")).Estado);
    }

    [Fact]
    public async Task Transacao_virtual_pode_ser_ajustada()
    {
        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/fixo", PerfilFixo());
        await Admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new
        {
            total = 5,
            semente = 1,
            distribuicao = new { bancos = new Dictionary<string, decimal> { [Banco] = 100m }, perfis = new Dictionary<string, decimal> { ["fixo"] = 100m } },
        });
        var recurso = (await Itens(0, 1))[0].Recursos[0];

        var ajuste = await Admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = recurso.ResourceId, dia = Hoje, posicao = 0, valor = 50m });

        Assert.True(ajuste.DoMundo);
    }
}

public class LimpezaTests : TesteDaMassa
{
    private Task<ResultadoLido> Limpar(string alvo, string? id, bool confirmar) =>
        Admin.Enviar<ResultadoLido>(HttpMethod.Post, "/limpezas", new { alvo, id, confirmar });

    public sealed record ResultadoLido(string Alvo, string? Id, bool Removido, long Clientes, long Consentimentos, long Recursos, long Ajustes, long Lotes, long Perfis, long Instituicoes);

    [Fact]
    public async Task Previa_nao_remove_e_confirmar_remove_so_o_lote()
    {
        var a = await Admin.GerarLote(new { quantidade = 2, semente = 1 });
        var b = await Admin.GerarLote(new { quantidade = 2, semente = 2 });

        var previa = await Limpar("lote", a.Id, false);
        Assert.False(previa.Removido);
        Assert.Equal(2, previa.Clientes);
        Assert.Equal(HttpStatusCode.OK, (await Admin.Enviar(HttpMethod.Get, $"/lotes/{a.Id}")).Status);

        Assert.True((await Limpar("lote", a.Id, true)).Removido);

        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, $"/lotes/{a.Id}")).Status);
        Assert.Equal(2, (await Admin.MassaDoLote(b.Id)).Count);
        Assert.Contains(await Admin.Ler<IReadOnlyList<EventoDaMassa>>("/historico"), e => e.Acao == "limpeza" && e.Mudancas[0].Antes == a.Id);
    }

    [Fact]
    public async Task Instituicao_apaga_o_banco_e_o_cliente_so_sem_produto_em_outro()
    {
        var soAqui = await CriarCliente("cliente-comum", Banco);
        var emDois = await CriarCliente("cliente-comum", Banco, Outro);

        var previa = await Limpar("instituicao", Banco, true);

        Assert.Equal(1, previa.Clientes);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Get, $"/clientes/{soAqui.Cliente.Documento}")).Status);
        var restante = await Admin.Ler<MassaGerada>($"/clientes/{emDois.Cliente.Documento}");
        Assert.All(restante.Recursos, r => Assert.Equal(Outro, r.Banco));
    }

    [Fact]
    public async Task Ajustes_do_mundo_voltam_ao_calculado()
    {
        await DefinirMundo();
        var id = (await Itens(0, 1))[0].Consentimentos[0].ConsentId;
        await Admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = id, estado = "REJECTED" });

        Assert.Equal(1, (await Limpar("ajustes-mundo", null, false)).Ajustes);
        await Limpar("ajustes-mundo", null, true);

        Assert.Equal("AUTHORISED", (await Admin.Ler<Consentimento>($"/consentimentos/{id}")).Estado);
    }

    [Fact]
    public async Task Tudo_apaga_massa_e_criados_mas_guarda_prontos_e_o_que_o_mundo_usa()
    {
        using var http = Fabrica.ClienteAdministracao();
        foreach (var nome in new[] { "Banco Solto", "Banco Mundo" })
        {
            using var conteudo = System.Net.Http.Json.JsonContent.Create(new { nome });
            using var criada = await http.PostAsync(new Uri("/admin/instituicoes", UriKind.Relative), conteudo, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, criada.StatusCode);
        }

        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/solto", PerfilFixo());
        await Admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/do-mundo", PerfilFixo());
        await Admin.Enviar<DistribuicaoNomeada>(HttpMethod.Put, "/distribuicoes/d", Distribuicao((Banco, 100m)));
        await Admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new
        {
            total = 5,
            distribuicao = new { bancos = new Dictionary<string, decimal> { ["banco-mundo"] = 100m }, perfis = new Dictionary<string, decimal> { ["do-mundo"] = 100m } },
        });
        await Admin.GerarLote(new { quantidade = 2 });

        var previa = await Limpar("tudo", null, false);
        Assert.Equal(1, previa.Perfis);
        Assert.Equal(1, previa.Lotes);
        Assert.True(previa.Instituicoes >= 1);

        await Limpar("tudo", null, true);

        Assert.Empty(await Admin.Ler<IReadOnlyList<Lote>>("/lotes"));
        Assert.Empty(await Admin.Ler<IReadOnlyList<DistribuicaoNomeada>>("/distribuicoes"));
        var perfis = await Admin.Ler<IReadOnlyList<PerfilDeDados>>("/perfis");
        Assert.DoesNotContain(perfis, p => p.Nome == "solto");
        Assert.Contains(perfis, p => p.Nome == "do-mundo");
        Assert.Contains(perfis, p => p.Nome == "alto-volume");
        Assert.Equal(0, Fabrica.Massa.Posicoes);
        using var solto = await http.GetAsync(new Uri("/admin/instituicoes/banco-solto", UriKind.Relative), TestContext.Current.CancellationToken);
        using var doMundo = await http.GetAsync(new Uri("/admin/instituicoes/banco-mundo", UriKind.Relative), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, solto.StatusCode);
        Assert.Equal(HttpStatusCode.OK, doMundo.StatusCode);
    }

    [Fact]
    public async Task Pedidos_invalidos_sao_recusados()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "outro" })).Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "lote" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "lote", id = "x" })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "instituicao", id = "x" })).Status);
    }

    [Fact]
    public async Task Lote_em_andamento_bloqueia_a_limpeza()
    {
        using var portao = new SemaphoreSlim(0);
        Fabrica.Massa.Portao = portao;
        await Admin.Ler<IReadOnlyList<Lote>>("/lotes");
        var lote = await Admin.Enviar<Lote>(HttpMethod.Post, "/lotes", new { quantidade = 1 });

        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "lote", id = lote.Id })).Status);
        Assert.Equal(HttpStatusCode.Conflict, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "tudo" })).Status);
        Assert.Equal(HttpStatusCode.OK, (await Admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "ajustes-mundo" })).Status);

        portao.Release();
        await Admin.Aguardar(lote.Id);
    }
}

public sealed class RepositorioDeMassaMongoTests
{
    private static FabricaDoSimulador Fabrica() => new FabricaDoSimulador().ComMongo();

    [Fact]
    public async Task Ciclo_completo_no_mongo()
    {
        using var fabrica = Fabrica();
        using var http = fabrica.ClienteAdministracao();
        var admin = new AdminDaMassa(http);
        const string Estavel = "banco-estavel";
        const string Lento = "banco-lento";
        var dist = new { bancos = new Dictionary<string, decimal> { [Estavel] = 50m, [Lento] = 50m }, perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100m } };

        await admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/meu", new { transacoesMin = 1, transacoesMax = 1, valorMin = 1m, valorMax = 1m, saldoInicial = 0m, tiposDeLancamento = Listas.Pix, produtos = Listas.Contas });
        Assert.Contains(await admin.Ler<IReadOnlyList<PerfilDeDados>>("/perfis"), p => p.Nome == "meu");
        await admin.Enviar<DistribuicaoNomeada>(HttpMethod.Put, "/distribuicoes/d", dist);
        Assert.Single(await admin.Ler<IReadOnlyList<DistribuicaoNomeada>>("/distribuicoes"));

        var cliente = await admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", perfil = "meu", bancos = new[] { Estavel, Lento } });
        Assert.Equal(HttpStatusCode.Conflict, (await admin.Enviar(HttpMethod.Delete, "/perfis/meu")).Status);
        var lido = await admin.Ler<MassaGerada>($"/clientes/{cliente.Cliente.Documento}");
        Assert.Equal(2, lido.Recursos.Count);

        var hoje = DateOnly.FromDateTime(fabrica.Relogio.GetUtcNow().UtcDateTime);
        await admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = lido.Recursos[0].ResourceId, dia = hoje, posicao = 0, valor = 9m });
        var extrato = await admin.Ler<JsonElement>($"/recursos/{lido.Recursos[0].ResourceId}/transacoes");
        Assert.Equal(9m, extrato.GetProperty("transacoes")[0].GetProperty("valor").GetDecimal());

        var lote = await admin.GerarLote(new { quantidade = 3, distribuicaoNome = "d", consentimentosPorCliente = 2 });
        Assert.Equal(SituacoesDeLote.Concluido, lote.Situacao);
        var massa = await admin.MassaDoLote(lote.Id);
        Assert.Equal(3, massa.Count);
        Assert.Equal(lote.Consentimentos, massa.Sum(m => m.Consentimentos.Count));
        Assert.Equal(massa[0].Consentimentos[0].ConsentId, (await admin.Ler<Consentimento>($"/consentimentos/{massa[0].Consentimentos[0].ConsentId}")).ConsentId);

        await admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total = 10, semente = 1, distribuicao = dist });
        var item = (await admin.Ler<IReadOnlyList<MassaGerada>>("/mundo-virtual/itens?inicio=0&quantidade=1"))[0];
        await admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = item.Consentimentos[0].ConsentId, estado = "REJECTED" });
        Assert.Equal("REJECTED", (await admin.Ler<Consentimento>($"/consentimentos/{item.Consentimentos[0].ConsentId}")).Estado);
        var limpeza = (await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "ajustes-mundo", confirmar = true })).Corpo;
        Assert.Contains("\"ajustes\":1", limpeza, StringComparison.Ordinal);

        Assert.Contains("\"clientes\":3", (await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "lote", id = lote.Id })).Corpo, StringComparison.Ordinal);
        await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "lote", id = lote.Id, confirmar = true });
        Assert.Equal(HttpStatusCode.NotFound, (await admin.Enviar(HttpMethod.Get, $"/lotes/{lote.Id}")).Status);

        Assert.Contains("\"clientes\":0", (await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "instituicao", id = Estavel })).Corpo, StringComparison.Ordinal);
        await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "instituicao", id = Estavel, confirmar = true });
        Assert.All((await admin.Ler<MassaGerada>($"/clientes/{cliente.Cliente.Documento}")).Recursos, r => Assert.Equal(Lento, r.Banco));
        await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "instituicao", id = Lento, confirmar = true });
        Assert.Equal(HttpStatusCode.NotFound, (await admin.Enviar(HttpMethod.Get, $"/clientes/{cliente.Cliente.Documento}")).Status);

        await admin.GerarLote(new { quantidade = 1 });
        Assert.Contains("\"lotes\":1", (await admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "tudo", confirmar = true })).Corpo, StringComparison.Ordinal);
        Assert.Empty(await admin.Ler<IReadOnlyList<Lote>>("/lotes"));
        Assert.StartsWith("lote-", (await admin.GerarLote(new { quantidade = 1 })).Id, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cancelamento_e_devolucao_de_posicoes_no_mongo()
    {
        using var fabrica = Fabrica();
        using var http = fabrica.ClienteAdministracao();
        var admin = new AdminDaMassa(http);
        await admin.Ler<IReadOnlyList<Lote>>("/lotes");

        var (status, _) = await admin.Enviar(HttpMethod.Post, "/lotes", new { quantidade = 49_999_999 + 1 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);

        var repositorio = fabrica.Services.GetRequiredService<IRepositorioDeMassa>();
        var token = TestContext.Current.CancellationToken;
        Assert.Equal(0, await repositorio.ReservarPosicoes(1, token));
        var dist = new Distribuicao(new Dictionary<string, decimal> { ["a"] = 100m }, new Dictionary<string, decimal> { ["p"] = 100m });
        var lote = new Lote("manual", 1, dist, 1, null, 1, 1, 12, 0, fabrica.Relogio.GetUtcNow(), 0, SituacoesDeLote.EmAndamento, null, 0, 0, fabrica.Relogio.GetUtcNow());
        await repositorio.InserirLote(lote, token);
        Assert.False(await repositorio.CancelamentoSolicitado("manual", token));
        await repositorio.SolicitarCancelamento("manual", token);
        Assert.True(await repositorio.CancelamentoSolicitado("manual", token));
        Assert.False(await repositorio.TemMassaNoBanco("a", token));
        Assert.Null(await repositorio.ObterPerfil("nada", token));
    }
}
