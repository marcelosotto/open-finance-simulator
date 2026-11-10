using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Cenarios.Contratos;
using Simulador.Cenarios.Persistencia;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Cenarios;

/// <summary>Anomalias no MongoDB real: um documento por banco e anomalia (research R04).</summary>
public sealed class AnomaliasNoMongoTests : IDisposable
{
    private readonly RepositorioMongo _repositorio = new(MongoDeTeste.Conexao, MongoDeTeste.NovaBase());
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public void Dispose() => _repositorio.Dispose();

    [Fact]
    public async Task Gravacoes_simultaneas_no_mesmo_banco_nao_se_perdem()
    {
        var agora = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        await Task.WhenAll(
            _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("banco", "revogado-com-403", true, agora), _ct),
            _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("banco", "interacao-diferente", true, agora), _ct),
            _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("outro", "revogado-com-403", true, agora), _ct));
        await _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("outro", "revogado-com-403", false, agora), _ct);

        var lidas = await _repositorio.ListarAnomalias(_ct);
        Assert.Equal(3, lidas.Count);
        Assert.Equal(2, lidas.Count(a => a.Instituicao == "banco" && a.Ligada));
        Assert.False(lidas.Single(a => a.Instituicao == "outro").Ligada);
        Assert.Empty(await _repositorio.ListarRegras(_ct));
    }

    [Fact]
    public async Task Apagar_as_anomalias_do_banco_nao_toca_outro_banco_de_prefixo_parecido()
    {
        var agora = new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
        await _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("banco", "revogado-com-403", true, agora), _ct);
        await _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("banco", "interacao-diferente", true, agora), _ct);
        await _repositorio.SalvarAnomalia(new AnomaliaDaInstituicao("banco-x", "revogado-com-403", true, agora), _ct);

        await _repositorio.ApagarAnomalias("banco", _ct);

        Assert.Equal(["banco-x"], (await _repositorio.ListarAnomalias(_ct)).Select(a => a.Instituicao));
    }
}

/// <summary>Defeitos e anomalias no host (spec 009, contracts/admin-defeitos-e-anomalias.md).</summary>
public sealed class DefeitosEAnomaliasTests : IDisposable
{
    private const string Banco = "banco-estavel";
    private static readonly string[] Tipos = ["PIX"];
    private static readonly string[] Produtos = ["contas"];
    private readonly FabricaDoSimulador _fabrica = new FabricaDoSimulador().ComComportamento().ComContadoresEmMemoria();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;
    private string _usuario = string.Empty;
    private string _senha = string.Empty;
    private string _consentId = string.Empty;
    private string _conta = string.Empty;

    public DefeitosEAnomaliasTests()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
    }

    public void Dispose()
    {
        _api.Dispose();
        _admin.Dispose();
        _fabrica.Dispose();
    }

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    private long Cota => ((ContadoresEmMemoria)_fabrica.Contadores).TotalDaCota;

    private Uri Transacoes(string consulta = "") =>
        new(AtalhosDeConsentimento.Caminho(Banco, "accounts", "/accounts/{accountId}/transactions").ToString().Replace("{accountId}", _conta, StringComparison.Ordinal) + consulta, UriKind.Relative);

    private async Task Preparar()
    {
        var semCauda = new { caudaLentaPct = 0m };
        using var configuracao = new HttpRequestMessage(HttpMethod.Patch, U($"/admin/instituicoes/{Banco}"))
        {
            Content = JsonContent.Create(new
            {
                comportamento = new
                {
                    taxaDeErroPct = 0m,
                    recursoIndisponivelPct = 0m,
                    latenciaPorClasse = new Dictionary<string, object> { ["alta"] = semCauda, ["media-alta"] = semCauda, ["media"] = semCauda, ["baixa"] = semCauda },
                },
            }),
        };
        (await _admin.SendAsync(configuracao, _ct)).EnsureSuccessStatusCode();
        (_usuario, _senha, _) = await AtalhosDeConsentimento.CriarReceptora(_admin);
        var massa = new AdminDaMassa(_admin);
        await massa.Enviar<PerfilDeDados>(HttpMethod.Put, "/perfis/fixo", new { transacoesMin = 2, transacoesMax = 2, valorMin = 10m, valorMax = 90m, saldoInicial = 5000m, tiposDeLancamento = Tipos, produtos = Produtos });
        var cliente = await massa.Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo = "PF", bancos = new[] { Banco }, perfil = "fixo" });
        _conta = cliente.Recursos.First(r => r.Tipo == "ACCOUNT").ResourceId;
        using var criado = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Post, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents"), await Token("consents"), new
        {
            data = new { loggedUser = new { document = new { identification = cliente.Cliente.Documento, rel = "CPF" } }, permissions = Permissoes.Para("PF", null) },
        });
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        using var corpo = JsonDocument.Parse(await criado.Content.ReadAsStringAsync(_ct));
        _consentId = corpo.RootElement.GetProperty("data").GetProperty("consentId").GetString()!;
    }

    private async Task<string> Token(string escopo)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, _usuario, _senha, escopo);
        resposta.EnsureSuccessStatusCode();
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct));
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<HttpResponseMessage> Consultar(Uri caminho, bool comToken = true) =>
        await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, caminho, comToken ? await Token($"consent:{_consentId}") : null);

    private async Task Ligar(string anomalia, bool ligar = true)
    {
        using var resposta = await _admin.PostAsync(U($"/admin/cenarios/anomalias/{Banco}/{anomalia}/{(ligar ? "ligar" : "desligar")}"), null, _ct);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private async Task Regra(object corpo)
    {
        using var resposta = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), corpo, _ct);
        Assert.True(resposta.StatusCode == HttpStatusCode.Created, await resposta.Content.ReadAsStringAsync(_ct));
    }

    [Fact]
    public async Task Administracao_das_anomalias_com_catalogo_estado_aviso_e_historico()
    {
        using var catalogo = JsonDocument.Parse(await _admin.GetStringAsync(U("/admin/cenarios/anomalias"), _ct));
        Assert.Equal(3, catalogo.RootElement.GetArrayLength());
        using var defeitos = JsonDocument.Parse(await _admin.GetStringAsync(U("/admin/cenarios/defeitos"), _ct));
        Assert.Equal(8, defeitos.RootElement.GetArrayLength());

        var estado = await _admin.GetFromJsonAsync<List<EstadoDaAnomalia>>(U($"/admin/cenarios/anomalias/{Banco}"), JsonDosCenarios.Opcoes, _ct);
        Assert.All(estado!, a => Assert.False(a.Ligada));

        using var ligada = await _admin.PostAsync(U($"/admin/cenarios/anomalias/{Banco}/revogado-com-403/ligar"), null, _ct);
        Assert.Equal(new EstadoDaAnomalia("revogado-com-403", true), await ligada.Content.ReadFromJsonAsync<EstadoDaAnomalia>(JsonDosCenarios.Opcoes, _ct));
        estado = await _admin.GetFromJsonAsync<List<EstadoDaAnomalia>>(U($"/admin/cenarios/anomalias/{Banco}"), JsonDosCenarios.Opcoes, _ct);
        Assert.Equal(["revogado-com-403"], estado!.Where(a => a.Ligada).Select(a => a.Nome));

        using var detalhe = JsonDocument.Parse(await _admin.GetStringAsync(U($"/admin/instituicoes/{Banco}"), _ct));
        Assert.Equal("revogado-com-403", detalhe.RootElement.GetProperty("anomaliasLigadas")[0].GetString());

        await Ligar("revogado-com-403", ligar: false);
        using var lista = JsonDocument.Parse(await _admin.GetStringAsync(U("/admin/instituicoes"), _ct));
        Assert.All(lista.RootElement.EnumerateArray(), b => Assert.Equal(0, b.GetProperty("anomaliasLigadas").GetArrayLength()));

        var historico = await _admin.GetStringAsync(U($"/admin/instituicoes/{Banco}/historico"), _ct);
        Assert.Contains("anomalia-ligada", historico, StringComparison.Ordinal);
        Assert.Contains("anomalia-desligada", historico, StringComparison.Ordinal);
        Assert.Contains("\"campo\":\"anomalia\"", historico, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anomalia_ou_banco_inexistente_responde_404_e_defeito_invalido_422()
    {
        using var semBanco = await _admin.PostAsync(U("/admin/cenarios/anomalias/nao-existe/revogado-com-403/ligar"), null, _ct);
        Assert.Equal(HttpStatusCode.NotFound, semBanco.StatusCode);
        using var semNome = await _admin.PostAsync(U($"/admin/cenarios/anomalias/{Banco}/outra/ligar"), null, _ct);
        Assert.Equal(HttpStatusCode.NotFound, semNome.StatusCode);
        using var estado = await _admin.GetAsync(U("/admin/cenarios/anomalias/nao-existe"), _ct);
        Assert.Equal(HttpStatusCode.NotFound, estado.StatusCode);

        using var invalido = await _admin.PostAsJsonAsync(U("/admin/cenarios/regras"), new { instituicao = Banco, efeito = new { tipo = "defeito", defeito = "valor-incoerente", campo = "type" } }, _ct);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalido.StatusCode);
    }

    [Fact]
    public async Task Ligar_anomalias_ao_mesmo_tempo_nao_perde_nenhuma()
    {
        string[] nomes = ["revogado-com-403", "chave-recusada-com-422", "interacao-diferente"];
        await Task.WhenAll(nomes.Select(n => Ligar(n)));
        var estado = await _admin.GetFromJsonAsync<List<EstadoDaAnomalia>>(U($"/admin/cenarios/anomalias/{Banco}"), JsonDosCenarios.Opcoes, _ct);
        Assert.All(estado!, a => Assert.True(a.Ligada));
    }

    [Fact]
    public async Task Revogado_com_403_troca_todo_401_do_portao_mas_nao_o_do_token()
    {
        await Preparar();
        using (var antes = await Consultar(Transacoes(), comToken: false))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, antes.StatusCode);
        }

        await Ligar("revogado-com-403");
        using var depois = await Consultar(Transacoes(), comToken: false);
        Assert.Equal(HttpStatusCode.Forbidden, depois.StatusCode);
        Assert.Contains("\"code\":\"FORBIDDEN\"", await depois.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);

        using var token = await AtalhosDeConsentimento.PedirToken(_api, Banco, _usuario, "senha-errada", "consents");
        Assert.Equal(HttpStatusCode.Unauthorized, token.StatusCode);
    }

    [Fact]
    public async Task Interacao_diferente_vale_tambem_nos_erros_depois_do_portao_e_nao_no_400()
    {
        await Preparar();
        await Ligar("interacao-diferente");
        using var erro = await Consultar(Transacoes(), comToken: false);
        Assert.Equal(HttpStatusCode.Unauthorized, erro.StatusCode);
        Assert.NotEqual(AtalhosDeConsentimento.Correlacao, erro.Headers.GetValues("x-fapi-interaction-id").Single());

        using var semCabecalho = new HttpRequestMessage(HttpMethod.Get, Transacoes());
        using var gerado = await _api.SendAsync(semCabecalho, _ct);
        Assert.Equal(HttpStatusCode.BadRequest, gerado.StatusCode);
        Assert.True(Guid.TryParse(gerado.Headers.GetValues("x-fapi-interaction-id").Single(), out _));
    }

    [Fact]
    public async Task Chave_recusada_com_422_nao_conta_na_cota_e_nao_afeta_chave_valida()
    {
        await Preparar();
        using var primeira = await Consultar(Transacoes());
        using var json = JsonDocument.Parse(await primeira.Content.ReadAsStringAsync(_ct));
        var self = new Uri(new Uri(json.RootElement.GetProperty("links").GetProperty("self").GetString()!).PathAndQuery, UriKind.Relative);
        var ruim = new Uri(self + "x", UriKind.Relative);
        await Ligar("chave-recusada-com-422");

        var cota = Cota;
        using (var valida = await Consultar(self))
        {
            Assert.Equal(HttpStatusCode.OK, valida.StatusCode);
        }

        using (var recusada = await Consultar(ruim))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, recusada.StatusCode);
            Assert.Contains("PARAMETRO_INVALIDO", await recusada.Content.ReadAsStringAsync(_ct), StringComparison.Ordinal);
        }

        Assert.Equal(cota, Cota);

        await Ligar("chave-recusada-com-422", ligar: false);
        using var nova = await Consultar(ruim);
        Assert.Equal(HttpStatusCode.OK, nova.StatusCode);
        Assert.Equal(cota + 1, Cota);
    }

    [Fact]
    public async Task Resposta_com_defeito_conta_na_cota_e_vem_antes_da_falha_de_transporte()
    {
        await Preparar();
        await Regra(new { instituicao = Banco, efeito = new { tipo = "defeito", defeito = "enum-invalido" } });
        await Regra(new { instituicao = Banco, efeito = new { tipo = "transporte", falha = "json-invalido" } });

        var cota = Cota;
        using var resposta = await Consultar(Transacoes());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync(_ct);
        Assert.Contains("VALOR_FORA_DA_LISTA", corpo, StringComparison.Ordinal);
        Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(corpo).Dispose());
        Assert.Equal(cota + 1, Cota);
    }

    [Fact]
    public async Task Data_fora_da_janela_usa_o_relogio_do_banco()
    {
        await Preparar();
        await Regra(new { instituicao = Banco, efeito = new { tipo = "defeito", defeito = "data-fora-da-janela" } });
        using (var relogio = await _admin.PutAsJsonAsync(U($"/admin/instituicoes/{Banco}/relogio"), new { fixadoEm = "2026-11-20T12:00:00Z" }, _ct))
        {
            relogio.EnsureSuccessStatusCode();
        }

        using var resposta = await Consultar(Transacoes());
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(_ct));
        var itens = json.RootElement.GetProperty("data");
        Assert.Equal("2024-11-20T00:00:00.000Z", itens[0].GetProperty("transactionDateTime").GetString());
        Assert.StartsWith("2026-11-20", itens[1].GetProperty("transactionDateTime").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Defeito_nao_age_em_erro_nem_em_resposta_sem_corpo()
    {
        await Preparar();
        await Regra(new { instituicao = Banco, efeito = new { tipo = "defeito", defeito = "campo-obrigatorio-ausente" } });
        await Regra(new { instituicao = Banco, endpoint = "accountsGetAccountsAccountIdTransactions", efeito = new { tipo = "status", status = 500 } });

        using (var erro = await Consultar(Transacoes()))
        {
            Assert.Equal(HttpStatusCode.InternalServerError, erro.StatusCode);
            Assert.Empty(ValidadorDeContrato.Validar("accountsGetAccountsAccountIdTransactions", await erro.Content.ReadAsStringAsync(_ct), "500"));
        }

        using var revogado = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Delete, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents/{consentId}", _consentId), await Token("consents"));
        Assert.Equal(HttpStatusCode.NoContent, revogado.StatusCode);
        Assert.Empty(await revogado.Content.ReadAsByteArrayAsync(_ct));
    }

    /// <summary>Revisão da 009, achado 1: o banco recriado com o mesmo nome nasce sem as anomalias do apagado.</summary>
    [Fact]
    public async Task Banco_recriado_com_o_mesmo_nome_nasce_sem_anomalias()
    {
        async Task<string> Criar()
        {
            using var criado = await _admin.PostAsJsonAsync(U("/admin/instituicoes"), new { nome = "Banco Passageiro" }, _ct);
            Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
            using var json = JsonDocument.Parse(await criado.Content.ReadAsStringAsync(_ct));
            return json.RootElement.GetProperty("endereco").GetString()!;
        }

        var endereco = await Criar();
        using (var ligada = await _admin.PostAsync(U($"/admin/cenarios/anomalias/{endereco}/revogado-com-403/ligar"), null, _ct))
        {
            Assert.Equal(HttpStatusCode.OK, ligada.StatusCode);
        }

        using (var detalhe = JsonDocument.Parse(await _admin.GetStringAsync(U($"/admin/instituicoes/{endereco}"), _ct)))
        {
            Assert.Equal(1, detalhe.RootElement.GetProperty("anomaliasLigadas").GetArrayLength());
        }

        using (var apagado = await _admin.DeleteAsync(U($"/admin/instituicoes/{endereco}"), _ct))
        {
            Assert.True(apagado.IsSuccessStatusCode, await apagado.Content.ReadAsStringAsync(_ct));
        }

        Assert.Equal(endereco, await Criar());
        var estado = await _admin.GetFromJsonAsync<List<EstadoDaAnomalia>>(U($"/admin/cenarios/anomalias/{endereco}"), JsonDosCenarios.Opcoes, _ct);
        Assert.All(estado!, a => Assert.False(a.Ligada));
        using var recriado = JsonDocument.Parse(await _admin.GetStringAsync(U($"/admin/instituicoes/{endereco}"), _ct));
        Assert.Equal(0, recriado.RootElement.GetProperty("anomaliasLigadas").GetArrayLength());
    }
}
