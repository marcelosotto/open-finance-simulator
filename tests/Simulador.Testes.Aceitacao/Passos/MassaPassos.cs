using System.Globalization;
using System.Net;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Reqnroll;
using Simulador.Instituicoes.Dominio;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Aceitacao.Passos;

internal static class Listas
{
    public static readonly string[] Pix = ["PIX"];
    public static readonly string[] Contas = ["contas"];
}

/// <summary>
/// Passos de <c>massa-e-semente.feature</c>. Os passos que citam as APIs oficiais conferem, por
/// enquanto, o estado calculado na administração; as specs 004 e 005 os ligam às APIs.
/// </summary>
[Binding]
public sealed class MassaPassos : IDisposable
{
    private const string Estavel = "banco-estavel";
    private readonly string _base = MongoDeTeste.NovaBase();
    private readonly FabricaDoSimulador _fabrica;
    private readonly HttpClient _http;
    private readonly AdminDaMassa _admin;
    private readonly List<IDisposable> _extras = [];

    private Lote? _lote;
    private Lote? _outroLote;
    private string _maisaAtual = string.Empty;
    private IReadOnlyList<MassaGerada> _massa = [];
    private IReadOnlyList<MassaGerada> _outraMassa = [];
    private IReadOnlyList<MassaGerada> _itens = [];
    private IReadOnlyList<MassaGerada> _clientes = [];
    private HttpStatusCode _status;
    private Recurso? _recurso;
    private DateOnly _dia;
    private decimal _saldoAntes;
    private decimal _diferenca;
    private string _bancoFiltrado = string.Empty;
    private readonly List<string> _revogados = [];
    private ClienteSimulado? _clienteDoLote;
    private ClienteSimulado? _clienteVirtual;
    private JsonElement _extrato;

    public MassaPassos()
    {
        _fabrica = new FabricaDoSimulador().ComMongo().Com("Simulador:Mongo:Base", _base);
        _http = _fabrica.ClienteAdministracao();
        _admin = new AdminDaMassa(_http);
    }

    private DateOnly Hoje => DateOnly.FromDateTime(_fabrica.Relogio.GetUtcNow().UtcDateTime);

    private static string EnderecoDe(string nome) => Endereco.Gerar(nome.Replace("HML-", string.Empty, StringComparison.Ordinal))!;

    private static decimal Valor(string texto) => decimal.Parse(texto.Replace(".", string.Empty, StringComparison.Ordinal), new CultureInfo("pt-BR"));

    private static int Numero(string texto) => int.Parse(texto.Replace(".", string.Empty, StringComparison.Ordinal), CultureInfo.InvariantCulture);

    private static object Divisao(params string[] bancos) => new
    {
        bancos = bancos.ToDictionary(b => b, _ => 100m / bancos.Length),
        perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100m },
    };

    private static string SemMarca(object valor, string marca) =>
        JsonSerializer.Serialize(valor, JsonDaMassa.Opcoes).Replace(marca, "*", StringComparison.Ordinal).Replace(marca.ToUpperInvariant(), "*", StringComparison.Ordinal);

    private static string Atributos(MassaGerada m) => string.Join(
        '|',
        new[] { m.Cliente.Nome, m.Cliente.Tipo }.Concat(m.Recursos.Select(r => $"{r.Banco},{r.Produto},{r.SementeDeDados}")).Concat(m.Consentimentos.Select(c => c.Banco)));

    private IMongoDatabase Mongo()
    {
        var cliente = new MongoClient(MongoDeTeste.Conexao);
        _extras.Add(cliente);
        return cliente.GetDatabase(_base);
    }

    private async Task<long> Contar(string colecao) =>
        await Mongo().GetCollection<BsonDocument>(colecao).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    private Task<MundoVirtual> DefinirMundo(int total, long semente, object distribuicao) =>
        _admin.Enviar<MundoVirtual>(HttpMethod.Put, "/mundo-virtual", new { total, semente, distribuicao, confirmar = true });

    private async Task GarantirMundo()
    {
        if ((await _admin.Enviar(HttpMethod.Get, "/mundo-virtual")).Status == HttpStatusCode.NotFound)
        {
            await DefinirMundo(100_000, 42, Divisao("itau", "nubank"));
        }
    }

    private async Task PedirItens(int quantidade, string? banco = null)
    {
        await GarantirMundo();
        _itens = await _admin.Ler<IReadOnlyList<MassaGerada>>($"/mundo-virtual/itens?inicio=0&quantidade={quantidade}" + (banco is null ? string.Empty : $"&banco={banco}"));
    }

    private async Task<string> Revogar(int indice)
    {
        var id = (await _admin.Ler<IReadOnlyList<MassaGerada>>($"/mundo-virtual/itens?inicio={indice}&quantidade=1"))[0].Consentimentos[0].ConsentId;
        await _admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "consentimento", consentId = id, estado = "REJECTED" });
        _revogados.Add(id);
        return id;
    }

    private async Task<JsonElement> Extrato(DateOnly de, DateOnly ate) =>
        await _admin.Ler<JsonElement>($"/recursos/{_recurso!.ResourceId}/transacoes?de={de:yyyy-MM-dd}&ate={ate:yyyy-MM-dd}");

    [Given(@"^um lote com semente (\d+)$")]
    public async Task UmLoteComSemente(long semente)
    {
        _lote = await _admin.GerarLote(new { quantidade = 5, semente });
        _massa = await _admin.MassaDoLote(_lote.Id);
    }

    [When(@"^o mesmo lote é gerado de novo em outro ambiente limpo$")]
    public async Task MesmoLoteEmOutroAmbiente()
    {
        using var outra = new FabricaDoSimulador("dev").ComMongo();
        using var http = outra.ClienteAdministracao();
        var admin = new AdminDaMassa(http);
        await admin.Ler<IReadOnlyList<Lote>>("/lotes");
        _outroLote = await admin.GerarLote(new { quantidade = 5, semente = _lote!.Semente });
        _outraMassa = await admin.MassaDoLote(_outroLote.Id);
    }

    [Then(@"^os dados devem ser iguais, salvo a marca do ambiente$")]
    public void DadosIguais()
    {
        Assert.Equal(_massa.Select(m => SemMarca(m, "hml")), _outraMassa.Select(m => SemMarca(m, "dev")));
    }

    [When(@"^Diego gera outro lote com semente (\d+)$")]
    public async Task OutroLote(long semente)
    {
        _outroLote = await _admin.GerarLote(new { quantidade = 5, semente });
        _outraMassa = await _admin.MassaDoLote(_outroLote.Id);
    }

    [Then(@"^os documentos e IDs devem ser diferentes$")]
    public void IdentidadeDiferente()
    {
        Assert.Empty(_massa.Select(m => m.Cliente.Documento).Intersect(_outraMassa.Select(m => m.Cliente.Documento), StringComparer.Ordinal));
        Assert.Empty(_massa.SelectMany(m => m.Recursos.Select(r => r.ResourceId)).Intersect(_outraMassa.SelectMany(m => m.Recursos.Select(r => r.ResourceId)), StringComparer.Ordinal));
    }

    [Then(@"^o restante dos dados deve ser igual$")]
    public void RestanteIgual()
    {
        Assert.Equal(_massa.Select(Atributos).Order(StringComparer.Ordinal), _outraMassa.Select(Atributos).Order(StringComparer.Ordinal));
    }

    [When(@"^Diego gera um lote de (\d+) clientes$")]
    public async Task GeraLote(int quantidade)
    {
        _lote = await _admin.GerarLote(new { quantidade, semente = 1 });
        _massa = await _admin.MassaDoLote(_lote.Id);
    }

    [Then(@"^clientes, consentimentos e recursos devem estar gravados$")]
    public async Task EstaoGravados()
    {
        Assert.Equal(_lote!.Quantidade, await Contar("clientes"));
        Assert.Equal(_lote.Consentimentos, await Contar("consentimentos"));
        Assert.Equal(_lote.Recursos, await Contar("recursos"));
    }

    [Then(@"^nenhuma transação deve estar gravada$")]
    public async Task NenhumaTransacao()
    {
        var colecoes = await (await Mongo().ListCollectionNamesAsync()).ToListAsync();

        Assert.DoesNotContain(colecoes, c => c.Contains("transac", StringComparison.OrdinalIgnoreCase));
    }

    [Given(@"^uma transação calculada de R\$ ([\d.,]+)$")]
    public async Task TransacaoCalculada(string valor)
    {
        var fixo = Valor(valor);
        await _admin.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/fixo", new { transacoesMin = 1, transacoesMax = 1, valorMin = fixo, valorMax = fixo, saldoInicial = 1_000m, tiposDeLancamento = Listas.Pix, produtos = Listas.Contas });
        _recurso = (await _admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", perfil = "fixo", bancos = new[] { Estavel } })).Recursos[0];
        _dia = Hoje;
        _extrato = await Extrato(_dia, _dia);
        _saldoAntes = _extrato.GetProperty("saldo").GetDecimal();
        Assert.Equal(fixo, _extrato.GetProperty("transacoes")[0].GetProperty("valor").GetDecimal());
        var credito = _extrato.GetProperty("transacoes")[0].GetProperty("direcao").GetString() == "CREDITO";
        _diferenca = (credito ? -1 : 1) * fixo;
    }

    [When(@"^Bruno ajusta o valor para R\$ ([\d.,]+)$")]
    public async Task AjustaValor(string valor)
    {
        var novo = Valor(valor);
        await _admin.Enviar<Ajuste>(HttpMethod.Post, "/ajustes", new { tipo = "transacao", recursoId = _recurso!.ResourceId, dia = _dia, posicao = 0, valor = novo });
        _diferenca += (_diferenca < 0 ? 1 : -1) * novo;
    }

    [Then(@"^a API deve devolver R\$ ([\d.,]+)$")]
    public async Task ApiDevolve(string valor)
    {
        _extrato = await Extrato(_dia, _dia);

        Assert.Equal(Valor(valor), _extrato.GetProperty("transacoes")[0].GetProperty("valor").GetDecimal());
    }

    [Then(@"^o saldo deve refletir o ajuste$")]
    public void SaldoReflete()
    {
        Assert.Equal(_saldoAntes + _diferenca, _extrato.GetProperty("saldo").GetDecimal());
    }

    [When(@"^Bruno pede ([\d.]+) itens do mundo virtual$")]
    public Task PedeItens(string quantidade) => PedirItens(Numero(quantidade));

    [Then(@"^os IDs devem funcionar nas APIs$")]
    public async Task IdsFuncionam()
    {
        Assert.Equal(1_000, _itens.Count);
        foreach (var item in _itens.Take(20))
        {
            Assert.Equal("AUTHORISED", (await _admin.Ler<Consentimento>($"/consentimentos/{item.Consentimentos[0].ConsentId}")).Estado);
        }
    }

    [Then(@"^só o total e a semente devem estar gravados$")]
    public async Task SoTotalESemente()
    {
        Assert.Equal(1, await Contar("mundo_virtual"));
        Assert.Equal(0, await Contar("clientes"));
        Assert.Equal(0, await Contar("consentimentos"));
        Assert.Equal(0, await Contar("recursos"));
    }

    [Given(@"^uma distribuição de (\d+)% no ""([^""]+)"" e (\d+)% no ""([^""]+)""$")]
    public Task UmaDistribuicao(decimal pctA, string a, decimal pctB, string b) =>
        DefinirMundo(100_000, 42, new
        {
            bancos = new Dictionary<string, decimal> { [EnderecoDe(a)] = pctA, [EnderecoDe(b)] = pctB },
            perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100m },
        });

    [Then(@"^cerca de metade dos itens deve ser de cada banco$")]
    public void MetadeCadaBanco()
    {
        Assert.All(_itens.GroupBy(i => i.Consentimentos[0].Banco, StringComparer.Ordinal), g => Assert.InRange(g.Count(), 400, 600));
        Assert.Equal(2, _itens.Select(i => i.Consentimentos[0].Banco).Distinct(StringComparer.Ordinal).Count());
    }

    [Then(@"^cada chamada deve seguir as regras do banco do item$")]
    public void SegueRegrasDoBanco()
    {
        Assert.All(_itens, i => Assert.All(i.Recursos, r => Assert.Equal(i.Consentimentos[0].Banco, r.Banco)));
    }

    [When(@"^Bruno pede ([\d.]+) itens do ""([^""]+)""$")]
    public Task PedeItensDoBanco(string quantidade, string banco)
    {
        _bancoFiltrado = EnderecoDe(banco);
        return PedirItens(Numero(quantidade), _bancoFiltrado);
    }

    [Then(@"^todos os itens devem ser desse banco$")]
    public void TodosDesseBanco()
    {
        Assert.Equal(1_000, _itens.Count);
        Assert.All(_itens, i => Assert.Equal(_bancoFiltrado, i.Consentimentos[0].Banco));
    }

    [Given(@"^um consentimento virtual$")]
    public Task UmConsentimentoVirtual() => GarantirMundo();

    [When(@"^Bruno o revoga$")]
    public Task BrunoRevoga() => Revogar(0);

    [Then(@"^as chamadas com ele devem responder 401$")]
    public async Task Responde401()
    {
        Assert.Equal("REJECTED", (await _admin.Ler<Consentimento>($"/consentimentos/{_revogados[0]}")).Estado);
    }

    [Given(@"^um mundo virtual com um consentimento revogado$")]
    public async Task MundoComRevogado()
    {
        await GarantirMundo();
        await Revogar(0);
    }

    [When(@"^Bruno troca a semente e confirma$")]
    public Task TrocaSemente() => DefinirMundo(100_000, 43, Divisao("itau", "nubank"));

    [Then(@"^os ajustes daquele mundo devem sumir$")]
    public async Task AjustesSomem()
    {
        Assert.Equal(0, await Contar("ajustes"));
    }

    [Given(@"^um mundo virtual com (\d+) consentimentos revogados$")]
    public async Task MundoComRevogados(int quantidade)
    {
        await GarantirMundo();
        for (var i = 0; i < quantidade; i++)
        {
            await Revogar(i);
        }
    }

    [When(@"^Diego limpa os ajustes$")]
    public async Task LimpaAjustes()
    {
        (_status, _) = await _admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "ajustes-mundo", confirmar = true });
    }

    [Then(@"^os consentimentos devem voltar ao estado calculado$")]
    public async Task VoltamAoCalculado()
    {
        Assert.Equal(HttpStatusCode.OK, _status);
        foreach (var id in _revogados)
        {
            Assert.Equal("AUTHORISED", (await _admin.Ler<Consentimento>($"/consentimentos/{id}")).Estado);
        }
    }

    [Given(@"^que Mariana tem conta no ""([^""]+)"" e no ""([^""]+)""$")]
    public async Task MarianaEmDoisBancos(string a, string b)
    {
        _lote = await _admin.GerarLote(new { quantidade = 1, semente = 2, consentimentosPorCliente = 40, distribuicao = Divisao(EnderecoDe(a), EnderecoDe(b)) });
        _clientes = await _admin.MassaDoLote(_lote.Id);
    }

    [When(@"^ela consente nos dois bancos$")]
    public void ElaConsente()
    {
        Assert.Equal(2, _clientes[0].Consentimentos.Select(c => c.Banco).Distinct(StringComparer.Ordinal).Count());
    }

    [Then(@"^cada consentimento deve mostrar só os dados do seu banco$")]
    public void SoDadosDoSeuBanco()
    {
        var massa = _clientes[0];

        Assert.All(massa.Consentimentos, c => Assert.All(c.Recursos, id => Assert.Equal(c.Banco, massa.Recursos.Single(r => r.ResourceId == id).Banco)));
    }

    [Given(@"^que Mariana tem um consentimento autorizado no ""([^""]+)""$")]
    public async Task MarianaComConsentimento(string banco)
    {
        _lote = await _admin.GerarLote(new { quantidade = 1, semente = 3, consentimentosPorCliente = 2, distribuicao = Divisao(EnderecoDe(banco)) });
        _clientes = await _admin.MassaDoLote(_lote.Id);
    }

    [When(@"^ela autoriza outro consentimento no mesmo banco$")]
    public void AutorizaOutro()
    {
        Assert.Equal(2, _clientes[0].Consentimentos.Count);
        Assert.Single(_clientes[0].Consentimentos.Select(c => c.Banco).Distinct(StringComparer.Ordinal));
    }

    [Then(@"^os dois devem funcionar$")]
    public void OsDoisFuncionam()
    {
        Assert.All(_clientes[0].Consentimentos, c => Assert.Equal("AUTHORISED", c.Estado));
        Assert.Equal(2, _clientes[0].Consentimentos.Select(c => c.ConsentId).Distinct(StringComparer.Ordinal).Count());
    }

    [When(@"^Diego define 60% e 30% entre os bancos$")]
    public async Task DefineBancosErrados()
    {
        (_status, _) = await _admin.Enviar(HttpMethod.Put, "/distribuicoes/d", new
        {
            bancos = new Dictionary<string, decimal> { [Estavel] = 60m, ["banco-lento"] = 30m },
            perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100m },
        });
    }

    [Then(@"^a distribuição deve ser recusada$")]
    public void Recusada()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, _status);
    }

    [When(@"^Diego define 90% com conta, 50% com cartão e 20% com empréstimo$")]
    public async Task DefineProdutos()
    {
        (_status, _) = await _admin.Enviar(HttpMethod.Put, "/distribuicoes/d", new
        {
            bancos = new Dictionary<string, decimal> { [Estavel] = 100m },
            perfis = new Dictionary<string, decimal> { ["cliente-comum"] = 100m },
            produtos = new Dictionary<string, decimal> { ["contas"] = 90m, ["cartoes"] = 50m, ["emprestimos"] = 20m },
        });
    }

    [Then(@"^a distribuição deve ser aceita$")]
    public void Aceita()
    {
        Assert.Equal(HttpStatusCode.OK, _status);
    }

    [When(@"^Ana cria um cliente simulado$")]
    public async Task AnaCria()
    {
        _maisaAtual = (await _admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Estavel } })).Cliente.Documento;
    }

    [Then(@"^o CPF deve ter dígitos válidos$")]
    public void CpfValido()
    {
        int Digito(int tamanho) => _maisaAtual.Take(tamanho).Select((c, i) => (c - '0') * (tamanho + 1 - i)).Sum() * 10 % 11 % 10;

        Assert.Equal(11, _maisaAtual.Length);
        Assert.Equal(_maisaAtual[9] - '0', Digito(9));
        Assert.Equal(_maisaAtual[10] - '0', Digito(10));
    }

    [Then(@"^o CPF deve começar com o dígito fixo do simulador$")]
    public void ComecaCom9()
    {
        Assert.StartsWith("9", _maisaAtual, StringComparison.Ordinal);
    }

    [Given(@"^um lote com (\d+) clientes$")]
    public async Task UmLoteComClientes(int quantidade)
    {
        _lote = await _admin.GerarLote(new { quantidade, semente = 4 });
    }

    [When(@"^Diego apaga o lote$")]
    public async Task ApagaLote()
    {
        (_status, _) = await _admin.Enviar(HttpMethod.Post, "/limpezas", new { alvo = "lote", id = _lote!.Id, confirmar = true });
    }

    [Then(@"^os clientes, consentimentos, recursos e ajustes dele devem sumir$")]
    public async Task Somem()
    {
        Assert.Equal(HttpStatusCode.OK, _status);
        foreach (var colecao in new[] { "clientes", "consentimentos", "recursos", "ajustes", "lotes" })
        {
            Assert.Equal(0, await Contar(colecao));
        }
    }

    [Then(@"^o histórico deve mostrar a exclusão$")]
    public async Task HistoricoMostra()
    {
        Assert.Contains(await _admin.Ler<IReadOnlyList<EventoDaMassa>>("/historico"), e => e.Acao == "limpeza" && e.Mudancas[0].Antes == _lote!.Id);
    }

    [Given(@"^um cliente criado por lote e um item do mundo virtual$")]
    public async Task ClienteELote()
    {
        _lote = await _admin.GerarLote(new { quantidade = 1, semente = 5 });
        _clienteDoLote = (await _admin.MassaDoLote(_lote.Id))[0].Cliente;
        await PedirItens(1);
        _clienteVirtual = _itens[0].Cliente;
    }

    [Then(@"^o primeiro deve ser ""([^""]+)""$")]
    public void Primeiro(string natureza)
    {
        Assert.Equal(natureza, _clienteDoLote!.Natureza);
    }

    [Then(@"^o segundo deve ser ""([^""]+)""$")]
    public void Segundo(string natureza)
    {
        Assert.Equal(natureza, _clienteVirtual!.Natureza);
    }

    [Then(@"^os dois devem ser simulados$")]
    public void Simulados()
    {
        Assert.All(new[] { _clienteDoLote!, _clienteVirtual! }, c =>
        {
            Assert.StartsWith("9", c.Documento, StringComparison.Ordinal);
            Assert.StartsWith("[HML] ", c.Nome, StringComparison.Ordinal);
        });
    }

    [Given(@"^uma conta com o perfil ""([^""]+)""$")]
    public async Task ContaComPerfil(string perfil)
    {
        _recurso = (await _admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", perfil, bancos = new[] { Estavel }, semente = 11 })).Recursos.First(r => r.Produto == "contas");
    }

    [When(@"^Ana consulta um dia de transações$")]
    public async Task ConsultaUmDia()
    {
        _extrato = await Extrato(Hoje, Hoje);
    }

    [Then(@"^a quantidade deve ficar em ""([^""]+)""$")]
    public void QuantidadeEm(string volume)
    {
        var partes = volume.Split(" a ");

        Assert.InRange(_extrato.GetProperty("transacoes").GetArrayLength(), int.Parse(partes[0], CultureInfo.InvariantCulture), int.Parse(partes[^1], CultureInfo.InvariantCulture));
    }

    [When(@"^Diego gera um cliente com histórico padrão$")]
    public async Task ClienteComHistorico()
    {
        _recurso = (await _admin.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", perfil = "cliente-comum", bancos = new[] { Estavel }, semente = 12 })).Recursos[0];
    }

    [Then(@"^devem existir transações dos últimos 12 meses$")]
    public async Task TransacoesDe12Meses()
    {
        var inicio = Hoje.AddMonths(-12);

        Assert.True((await Extrato(inicio, inicio.AddDays(30))).GetProperty("transacoes").GetArrayLength() > 0);
        Assert.True((await Extrato(Hoje.AddDays(-30), Hoje)).GetProperty("transacoes").GetArrayLength() > 0);
        Assert.Equal(0, (await Extrato(inicio.AddDays(-30), inicio.AddDays(-1))).GetProperty("transacoes").GetArrayLength());
    }

    public void Dispose()
    {
        _extras.ForEach(e => e.Dispose());
        _http.Dispose();
        _fabrica.Dispose();
    }
}
