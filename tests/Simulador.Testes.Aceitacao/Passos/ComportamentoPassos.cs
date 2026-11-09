using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Reqnroll;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Host;

namespace Simulador.Testes.Aceitacao.Passos;

/// <summary>Estado de um cenário de comportamento, com perfis e regras ligados.</summary>
public sealed class ContextoDeComportamento : IDisposable
{
    public const string Perfil = "fixo";
    public const string SaldosId = "accountsGetAccountsAccountIdBalances";
    private static readonly string[] Tipos = ["PIX"];
    private static readonly string[] Produtos = ["contas"];

    public ContextoDeComportamento()
    {
        Fabrica = new FabricaDoSimulador().ComContadoresEmMemoria().ComComportamento();
        Admin = Fabrica.ClienteAdministracao();
        Api = Fabrica.ClienteOpenFinance();
    }

    public FabricaDoSimulador Fabrica { get; }

    public HttpClient Admin { get; }

    public HttpClient Api { get; }

    public string Banco { get; private set; } = "banco-estavel";

    public string Conta { get; private set; } = string.Empty;

    public string Documento { get; private set; } = string.Empty;

    public string ConsentId { get; private set; } = string.Empty;

    public string Token { get; private set; } = string.Empty;

    public HttpResponseMessage? Resposta { get; private set; }

    public string Corpo { get; private set; } = string.Empty;

    private string Usuario { get; set; } = string.Empty;

    private string Senha { get; set; } = string.Empty;

    public static string Endereco(string nome) => nome switch
    {
        "Banco Estável" => "banco-estavel",
        "Banco Lento" => "banco-lento",
        "Banco Timeout" => "banco-timeout",
        _ => "banco-azul",
    };

    public async Task Preparar(string nome, bool semErros = true)
    {
        Banco = Endereco(nome);
        if (Banco == "banco-azul")
        {
            using var criado = await Admin.PostAsJsonAsync(new Uri("/admin/instituicoes", UriKind.Relative), new { nome = "Banco Azul" });
            Assert.True(criado.IsSuccessStatusCode, await criado.Content.ReadAsStringAsync());
        }

        if (semErros)
        {
            await Configurar(new { comportamento = new { taxaDeErroPct = 0m, recursoIndisponivelPct = 0m } });
        }

        var massa = new AdminDaMassa(Admin);
        (Usuario, Senha, _) = await AtalhosDeConsentimento.CriarReceptora(Admin);
        await massa.Enviar<PerfilDeDados>(HttpMethod.Put, $"/perfis/{Perfil}", new { transacoesMin = 2, transacoesMax = 2, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = Tipos, produtos = Produtos });
        var cliente = await massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco }, perfil = Perfil });
        Documento = cliente.Cliente.Documento;
        Conta = cliente.Recursos.First(r => r.Tipo == "ACCOUNT").ResourceId;
        using var resposta = await AtalhosDeConsentimento.Chamar(Api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), await NovoToken("consents"), new
        {
            data = new { loggedUser = new { document = new { identification = Documento, rel = "CPF" } }, permissions = Permissoes.Para("PF", null) },
        });
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        ConsentId = corpo.RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
        await RenovarToken();
        while (Fabrica.Esperas.Duracoes.TryDequeue(out _))
        {
        }
    }

    public async Task RenovarToken() => Token = await NovoToken($"consent:{ConsentId}");

    public async Task Configurar(object alteracao)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Patch, new Uri($"/admin/instituicoes/{Banco}", UriKind.Relative)) { Content = JsonContent.Create(alteracao) };
        using var resposta = await Admin.SendAsync(requisicao);
        Corpo = await resposta.Content.ReadAsStringAsync();
        UltimoStatusDeConfiguracao = resposta.StatusCode;
    }

    public HttpStatusCode UltimoStatusDeConfiguracao { get; private set; }

    public async Task CriarRegra(object regra)
    {
        using var resposta = await Admin.PostAsJsonAsync(new Uri("/admin/cenarios/regras", UriKind.Relative), regra);
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync());
    }

    public async Task MudarRelogio(object corpo)
    {
        using var resposta = await Admin.PutAsJsonAsync(new Uri($"/admin/instituicoes/{Banco}/relogio", UriKind.Relative), corpo);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    public async Task VoltarAoReal()
    {
        using var resposta = await Admin.DeleteAsync(new Uri($"/admin/instituicoes/{Banco}/relogio", UriKind.Relative));
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    public Uri Em(string caminho) =>
        new(AtalhosDeConsentimento.Caminho(Banco, "accounts", caminho).ToString().Replace("{accountId}", Conta, StringComparison.Ordinal), UriKind.Relative);

    public Uri Saldos => Em("/accounts/{accountId}/balances");

    public HttpRequestMessage Pedido(Uri caminho)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Get, caminho);
        requisicao.Headers.Add("x-fapi-interaction-id", AtalhosDeConsentimento.Correlacao);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return requisicao;
    }

    public async Task Chamar(Uri caminho, bool renovar = true)
    {
        if (renovar)
        {
            await RenovarToken();
        }

        using var requisicao = Pedido(caminho);
        Resposta?.Dispose();
        Resposta = await Api.SendAsync(requisicao);
        Corpo = await Resposta.Content.ReadAsStringAsync();
    }

    public void Status(int esperado) => Assert.True((int)Resposta!.StatusCode == esperado, $"esperado {esperado}, veio {(int)Resposta.StatusCode}: {Corpo}");

    public void Dispose()
    {
        Resposta?.Dispose();
        Fabrica.Dispose();
    }

    private async Task<string> NovoToken(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(Api, Banco, Usuario, Senha, escopo);
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }
}

/// <summary>Passos de comportamento.feature (spec 007).</summary>
[Binding]
[Scope(Feature = "Simular o comportamento real das instituições")]
public sealed partial class ComportamentoPassos(ContextoDeComportamento c)
{
    private const int TotalDeChamadas = 10_000;
    private readonly List<int> _status = [];
    private string _fixadoEm = string.Empty;
    private bool _preparado;
    private string _falha = string.Empty;
    private bool _falhou;

    private async Task Garantir(string nome = "Banco Estável")
    {
        if (!_preparado)
        {
            await c.Preparar(nome);
            _preparado = true;
        }
    }

    private async Task Regra(string texto, string? pacote = null)
    {
        await Garantir();
        var m = TextoDaRegra().Match(texto);
        Assert.True(m.Success, texto);
        var banco = m.Groups["quem"].Value == "Banco Estável";
        await c.CriarRegra(new
        {
            instituicao = banco ? c.Banco : null,
            cliente = banco ? null : c.Documento,
            endpoint = m.Groups["saldos"].Success ? ContextoDeComportamento.SaldosId : null,
            efeito = new { tipo = "status", status = int.Parse(m.Groups["status"].Value, System.Globalization.CultureInfo.InvariantCulture) },
            pacote,
        });
    }

    [Given(@"^10\.000 chamadas ao endpoint de saldos do ""(.*)""$")]
    public async Task DezMilChamadas(string banco)
    {
        await c.Preparar(banco, semErros: false);
        await c.Configurar(new { limites = new { capacidadeTps = 100_000_000, limiteOperacionalLigado = false, tpm = new { alta = 100_000_000 } } });
        for (var i = 0; i < TotalDeChamadas; i++)
        {
            await c.Chamar(c.Saldos, renovar: false);
            _status.Add((int)c.Resposta!.StatusCode);
        }
    }

    [Then(@"^o p95 deve ficar (abaixo|acima) de 1\.500 ms$")]
    public void P95(string lado)
    {
        var ordenadas = c.Fabrica.Esperas.Duracoes.Select(d => d.TotalMilliseconds).Order().ToList();
        Assert.True(ordenadas.Count > TotalDeChamadas * 0.95, $"esperas: {ordenadas.Count}");
        var p95 = ordenadas[(int)Math.Ceiling(0.95 * ordenadas.Count) - 1];
        Assert.True(lado == "abaixo" ? p95 < 1500 : p95 > 1500, $"p95 = {p95}");
    }

    [Then(@"^todas as chamadas devem terminar$")]
    public void TodasTerminam() => Assert.Equal(TotalDeChamadas, _status.Count);

    [When(@"^uma chamada ao ""(.*)"" é afetada pelo timeout$")]
    public async Task ChamadaComTimeout(string banco)
    {
        await c.Preparar(banco);
        await c.Configurar(new { comportamento = new { latenciaPorClasse = new { alta = new { caudaLentaPct = 100m } } } });
        await c.Chamar(c.Saldos);
    }

    [Then(@"^a resposta só deve terminar depois de 15 segundos$")]
    public void DepoisDe15() => Assert.Equal(TimeSpan.FromSeconds(15), c.Fabrica.Esperas.Duracoes.Last());

    [Then(@"^a resposta deve ter status (\d+)$")]
    public void StatusDaResposta(int status) => c.Status(status);

    [Given(@"^uma regra ""(.*)""$")]
    public Task UmaRegra(string texto) => Regra(texto);

    [Given(@"^uma regra criada depois ""(.*)""$")]
    public Task RegraDepois(string texto)
    {
        c.Fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
        return Regra(texto);
    }

    [When(@"^Mariana é consultada duas vezes$")]
    public async Task DuasVezes()
    {
        await c.Chamar(c.Saldos);
        _status.Add((int)c.Resposta!.StatusCode);
        await c.Chamar(c.Saldos);
        _status.Add((int)c.Resposta!.StatusCode);
    }

    [Then(@"^as duas respostas devem ter status (\d+)$")]
    public void DuasRespostas(int status) => Assert.Equal([status, status], _status);

    [When(@"^Mariana é consultada$")]
    [When(@"^o saldo de Mariana é consultado$")]
    public Task Consultada() => c.Chamar(c.Saldos);

    [Given(@"^um pacote ""(.*)"" com duas regras$")]
    public async Task Pacote(string nome)
    {
        await Garantir();
        using var pacote = await c.Admin.PutAsync(new Uri($"/admin/cenarios/pacotes/{Uri.EscapeDataString(nome)}", UriKind.Relative), null);
        Assert.True(pacote.IsSuccessStatusCode, await pacote.Content.ReadAsStringAsync());
        await c.CriarRegra(new { cliente = c.Documento, endpoint = ContextoDeComportamento.SaldosId, efeito = new { tipo = "status", status = 500 }, pacote = nome });
        await c.CriarRegra(new { cliente = c.Documento, endpoint = "accountsGetAccounts", efeito = new { tipo = "indisponibilidade", resultado = "503" }, pacote = nome });
        await c.Chamar(c.Saldos);
        c.Status(200);
        _falha = nome;
    }

    [When(@"^Bruno liga o pacote$")]
    public async Task LigaPacote()
    {
        using var resposta = await c.Admin.PostAsync(new Uri($"/admin/cenarios/pacotes/{Uri.EscapeDataString(_falha)}/ligar", UriKind.Relative), null);
        Assert.True(resposta.IsSuccessStatusCode, await resposta.Content.ReadAsStringAsync());
    }

    [Then(@"^as duas regras devem valer$")]
    public async Task DuasValem()
    {
        await c.Chamar(c.Saldos);
        c.Status(500);
        await c.Chamar(c.Em("/accounts"));
        c.Status(503);
    }

    [Given(@"^que os pesos do ""(.*)"" incluem 429$")]
    public Task Pesos(string banco) => c.Preparar(banco);

    [When(@"^Diego salva a configuração$")]
    public Task Salva() => c.Configurar(new { comportamento = new { pesosPorStatus = new Dictionary<string, decimal> { ["429"] = 1m } } });

    [Then(@"^a configuração deve ser recusada$")]
    public void Recusada() => Assert.Equal(HttpStatusCode.UnprocessableEntity, c.UltimoStatusDeConfiguracao);

    private DateTimeOffset Amanha(int hora, int minuto)
    {
        var hoje = c.Fabrica.Relogio.GetUtcNow().UtcDateTime.Date.AddDays(1).AddHours(hora).AddMinutes(minuto);
        return new DateTimeOffset(hoje, TimeSpan.Zero);
    }

    [Given(@"^uma janela do ""(.*)"" das 10h às 10h30$")]
    public async Task Janela(string banco)
    {
        await c.Preparar(banco);
        _preparado = true;
        await c.CriarRegra(new { instituicao = c.Banco, efeito = new { tipo = "indisponibilidade", resultado = "503" }, inicio = Amanha(10, 0), fim = Amanha(10, 30) });
    }

    [When(@"^uma chamada chega às 10h(\d+)$")]
    public async Task ChegaAs(int minuto)
    {
        var alvo = c.Fabrica.Relogio.GetUtcNow().UtcDateTime.Hour == 10
            ? new DateTimeOffset(c.Fabrica.Relogio.GetUtcNow().UtcDateTime.Date.AddHours(10).AddMinutes(minuto), TimeSpan.Zero)
            : Amanha(10, minuto);
        c.Fabrica.Relogio.SetUtcNow(alvo);
        await c.Chamar(c.Saldos);
    }

    [Then(@"^a resposta deve ser normal$")]
    public void Normal() => c.Status(200);

    [Given(@"^uma regra de transporte ""(.*)"" para o endpoint de contas$")]
    public async Task RegraDeTransporte(string falha)
    {
        await Garantir();
        _falha = falha;
        await c.CriarRegra(new
        {
            endpoint = "accountsGetAccounts",
            efeito = new
            {
                tipo = "transporte",
                falha = falha switch
                {
                    "conexão cortada" => "conexao-cortada",
                    "corpo truncado" => "corpo-truncado",
                    _ => "json-invalido",
                },
            },
        });
    }

    [When(@"^Ana consulta contas$")]
    public async Task ConsultaContas()
    {
        await Garantir();
        await c.RenovarToken();
        using var requisicao = c.Pedido(c.Em("/accounts"));
        try
        {
            using var resposta = await c.Api.SendAsync(requisicao, HttpCompletionOption.ResponseHeadersRead);
            _status.Add((int)resposta.StatusCode);
            var esperado = resposta.Content.Headers.ContentLength;
            var bytes = await resposta.Content.ReadAsByteArrayAsync();
            _falha = _falha.Length == 0 ? string.Empty : _falha;
            CorpoLido = System.Text.Encoding.UTF8.GetString(bytes);
            Incompleto = esperado is { } total && total > bytes.Length;
        }
        catch (Exception erro) when (erro is IOException or HttpRequestException or OperationCanceledException or InvalidOperationException)
        {
            _falhou = true;
        }
    }

    private string CorpoLido { get; set; } = string.Empty;

    private bool Incompleto { get; set; }

    [Then(@"^a receptora deve observar ""(.*)""$")]
    public void Observa(string falha)
    {
        switch (falha)
        {
            case "conexão cortada":
                Assert.True(_falhou && _status.Count == 0, "a conexão deveria cair antes do status");
                break;
            case "corpo truncado":
                Assert.Equal([200], _status);
                Assert.True(_falhou || Incompleto, "o corpo deveria vir incompleto");
                break;
            default:
                Assert.Equal([200], _status);
                Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(CorpoLido));
                break;
        }
    }

    [Given(@"^uma regra que força erro 500$")]
    public async Task ForcaErro()
    {
        await Garantir();
        await c.CriarRegra(new { endpoint = "accountsGetAccounts", efeito = new { tipo = "status", status = 500 } });
    }

    [Then(@"^o corpo da resposta deve seguir a spec$")]
    public void SegueASpec()
    {
        Assert.Equal([500], _status);
        Assert.Empty(ValidadorDeContrato.Validar("accountsGetAccounts", CorpoLido, "500"));
    }

    [Then(@"^a resposta não deve dizer qual regra agiu$")]
    public void NaoRevela()
    {
        Assert.DoesNotContain("regra", CorpoLido, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cenário", CorpoLido, StringComparison.OrdinalIgnoreCase);
    }

    [Given(@"^que o relógio do ""(.*)"" está fixado em ""(.*)""$")]
    public async Task FixadoEm(string banco, string instante)
    {
        await c.Preparar(banco);
        _fixadoEm = instante;
        await c.MudarRelogio(new { fixadoEm = instante });
    }

    [Given(@"^que o relógio do ""(.*)"" está fixado$")]
    public Task Fixado(string banco) => FixadoEm(banco, "2026-12-31T23:59:50Z");

    [When(@"^Ana consulta o saldo$")]
    public Task ConsultaSaldo() => c.Chamar(c.Saldos);

    [Then(@"^a resposta deve usar a data simulada$")]
    public void UsaData()
    {
        c.Status(200);
        using var corpo = JsonDocument.Parse(c.Corpo);
        Assert.Equal(_fixadoEm, corpo.RootElement.GetProperty("meta").GetProperty("requestDateTime").GetString());
    }

    [Then(@"^o cabeçalho ""x-sim-clock"" deve mostrar a data simulada$")]
    public void CabecalhoMostra() => Assert.Equal(_fixadoEm, c.Resposta!.Headers.GetValues("x-sim-clock").Single());

    [Given(@"^que o relógio do ""(.*)"" está acelerado$")]
    public async Task Acelerado(string banco)
    {
        await c.Preparar(banco);
        await c.Configurar(new { limites = new { limiteOperacionalLigado = false, tpm = new { alta = 2 } } });
        await c.MudarRelogio(new { fator = 3600 });
    }

    [When(@"^Ana faz chamadas dentro de um minuto real$")]
    public async Task ChamadasNoMinuto()
    {
        for (var i = 0; i < 3; i++)
        {
            await c.Chamar(c.Saldos);
            _status.Add((int)c.Resposta!.StatusCode);
            c.Fabrica.Relogio.Advance(TimeSpan.FromSeconds(10));
        }
    }

    [Then(@"^o TPM deve contar pela hora real$")]
    public void TpmReal() => Assert.Equal([200, 200, 429], _status);

    [When(@"^Bruno volta ao relógio real$")]
    public Task Volta() => c.VoltarAoReal();

    [Then(@"^o cabeçalho ""x-sim-clock"" não deve ser enviado$")]
    public async Task SemCabecalho()
    {
        await c.Chamar(c.Saldos);
        c.Status(200);
        Assert.False(c.Resposta!.Headers.Contains("x-sim-clock"));
    }

    [Given(@"^que a classe alta do ""(.*)"" tem tempo típico de 3 segundos$")]
    public async Task TresSegundos(string banco)
    {
        await c.Preparar(banco);
        await c.Configurar(new { comportamento = new { latenciaPorClasse = new { alta = new { tipicoMs = 3000, variacaoMs = 0, caudaLentaPct = 0m } } } });
        Assert.Equal(HttpStatusCode.OK, c.UltimoStatusDeConfiguracao);
        using var corpo = JsonDocument.Parse(c.Corpo);
        _baixa = corpo.RootElement.GetProperty("comportamento").GetProperty("latenciaPorClasse").GetProperty("baixa").GetRawText();
    }

    private string _baixa = string.Empty;

    [Then(@"^a resposta deve levar cerca de 3 segundos$")]
    public void TresSegundosDepois() => Assert.Equal(TimeSpan.FromSeconds(3), c.Fabrica.Esperas.Duracoes.Last());

    [Then(@"^os endpoints de classe baixa não devem mudar$")]
    public void BaixaIgual()
    {
        using var corpo = JsonDocument.Parse(c.Corpo.Length > 0 && c.Corpo.Contains("comportamento", StringComparison.Ordinal) ? c.Corpo : Instituicao());
        Assert.Equal(_baixa, corpo.RootElement.GetProperty("comportamento").GetProperty("latenciaPorClasse").GetProperty("baixa").GetRawText());
    }

    private string Instituicao() => c.Admin.GetStringAsync(new Uri($"/admin/instituicoes/{c.Banco}", UriKind.Relative)).GetAwaiter().GetResult();

    [GeneratedRegex(@"^(?<quem>Mariana|Banco Estável) (sempre recebe|responde) (?<status>\d+)(?<saldos> em saldos)?$")]
    private static partial Regex TextoDaRegra();
}

/// <summary>"Bloqueio programado" de recursos.feature (RN108).</summary>
[Binding]
[Scope(Feature = "API de Recursos", Scenario = "Bloqueio programado")]
public sealed class BloqueioProgramadoPassos(ContextoDeComportamento c)
{
    private DateTimeOffset Amanha(int minuto) =>
        new(c.Fabrica.Relogio.GetUtcNow().UtcDateTime.Date.AddDays(1).AddHours(10).AddMinutes(minuto), TimeSpan.Zero);

    [Given(@"^uma regra que bloqueia a conta às 10h$")]
    public async Task Bloqueia()
    {
        await c.Preparar("Banco Estável");
        await c.CriarRegra(new { recurso = c.Conta, efeito = new { tipo = "estado", estado = "TEMPORARILY_UNAVAILABLE" }, inicio = Amanha(0) });
    }

    [When(@"^Ana consulta a conta às 10h01$")]
    public async Task Consulta()
    {
        await c.Chamar(c.Em("/accounts/{accountId}"));
        c.Status(200);
        c.Fabrica.Relogio.SetUtcNow(Amanha(1));
        await c.Chamar(c.Em("/accounts/{accountId}"));
    }

    [Then(@"^a resposta deve ter status (\d+)$")]
    public void Status(int status) => c.Status(status);
}
