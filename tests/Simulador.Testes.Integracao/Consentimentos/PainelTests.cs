using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Simulador.Massa.Contratos;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Consentimentos;

/// <summary>Spec 008: administração do painel, alçadas e estado do recurso no consentimento (RN95 a RN99).</summary>
public sealed class PainelTests : IDisposable
{
    private const string Banco = "banco-teste";
    private static readonly string[] Contas = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ"];
    private static readonly string[] ContasECartoes = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ", "CREDIT_CARDS_ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_LIMITS_READ"];
    private static readonly string[] Incompleto = ["ACCOUNTS_READ"];
    private static readonly string[] Desconhecida = ["ACCOUNTS_READ", "NAO_EXISTE"];
    private static readonly string[] SemCartoes = ["contas", "emprestimos", "financiamentos", "adiantamentos", "direitos-creditorios", "investimentos", "cambio", "fundos", "renda-variavel", "renda-fixa-credito", "tesouro-direto"];
    private static readonly DateTimeOffset Inicio = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private readonly FabricaDoSimulador _fabrica = new();
    private readonly HttpClient _admin;
    private readonly HttpClient _api;
    private (string Usuario, string Senha, string OrganisationId) _receptora;

    public PainelTests()
    {
        _admin = _fabrica.ClienteAdministracao();
        _api = _fabrica.ClienteOpenFinance();
    }

    private static CancellationToken Cancelamento => TestContext.Current.CancellationToken;

    private async Task<MassaGerada> Cliente(string tipo = "PF")
    {
        if (_receptora.Usuario is null)
        {
            _receptora = await AtalhosDeConsentimento.CriarReceptora(_admin);
        }

        return await new AdminDaMassa(_admin).Enviar<MassaGerada>(HttpMethod.Post, "/clientes", new { tipo, bancos = new[] { Banco } });
    }

    private async Task<(HttpStatusCode Status, JsonElement Corpo)> Enviar(HttpMethod metodo, string caminho, object? corpo = null, string? autor = null)
    {
        using var requisicao = new HttpRequestMessage(metodo, new Uri(caminho, UriKind.Relative));
        if (autor is not null)
        {
            requisicao.Headers.Add("x-sim-autor", autor);
        }

        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo);
        }

        using var resposta = await _admin.SendAsync(requisicao, Cancelamento);
        var texto = await resposta.Content.ReadAsStringAsync(Cancelamento);
        return (resposta.StatusCode, texto.Length == 0 ? default : JsonDocument.Parse(texto).RootElement.Clone());
    }

    private async Task<string> Criar(MassaGerada cliente, int? alcadas = null)
    {
        var (status, corpo) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new
        {
            cliente = cliente.Cliente.Documento,
            banco = Banco,
            receptora = _receptora.OrganisationId,
            permissoes = Contas,
            aprovacoesPendentes = alcadas,
        });
        Assert.True(status == HttpStatusCode.Created, corpo.ToString());
        return corpo.GetProperty("consentId").GetString()!;
    }

    private async Task<string> Token(string consentId)
    {
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, _receptora.Usuario, _receptora.Senha, $"consent:{consentId}");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var corpo = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(Cancelamento));
        return corpo.RootElement.GetProperty("access_token").GetString()!;
    }

    private async Task<(HttpStatusCode Status, string Corpo)> Oficial(string contrato, string caminho, string token, string? conta = null)
    {
        var endereco = AtalhosDeConsentimento.Caminho(Banco, contrato, caminho).ToString().Replace("{accountId}", conta ?? string.Empty, StringComparison.Ordinal);
        using var resposta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, new Uri(endereco, UriKind.Relative), token);
        return (resposta.StatusCode, await resposta.Content.ReadAsStringAsync(Cancelamento));
    }

    private async Task<string[]> EstadosEmRecursos(string consentId)
    {
        var (status, corpo) = await Oficial("resources", "/resources", await Token(consentId));
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(ValidadorDeContrato.Validar("resourcesGetResources", corpo));
        return [.. JsonDocument.Parse(corpo).RootElement.GetProperty("data").EnumerateArray().Select(r => r.GetProperty("status").GetString()!)];
    }

    private async Task<string> Decidir(string consentId, string decisao, HttpStatusCode esperado, string? autor = null)
    {
        var (status, corpo) = await Enviar(HttpMethod.Post, $"/admin/consentimentos/{consentId}/{decisao}", autor: autor);
        Assert.True(status == esperado, corpo.ToString());
        return corpo.ToString();
    }

    private static string Conta(MassaGerada cliente) => cliente.Recursos.First(r => r.Banco == Banco && r.Tipo == "ACCOUNT").ResourceId;

    [Fact]
    public async Task Alcada_pendente_vale_so_no_proprio_consentimento_ate_a_ultima_aprovacao()
    {
        var aurora = await Cliente("PJ");
        var comAlcada = await Criar(aurora, 2);
        var semAlcada = await Criar(aurora);
        var token = await Token(comAlcada);

        Assert.All(await EstadosEmRecursos(comAlcada), e => Assert.Equal("PENDING_AUTHORISATION", e));
        Assert.All(await EstadosEmRecursos(semAlcada), e => Assert.Equal("AVAILABLE", e));

        var (status, corpo) = await Oficial("accounts", "/accounts/{accountId}", token, Conta(aurora));
        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Contains("STATUS_RESOURCE_PENDING_AUTHORISATION", corpo, StringComparison.Ordinal);
        Assert.Empty(ValidadorDeContrato.Validar("accountsGetAccountsAccountId", corpo, "403"));
        var (_, lista) = await Oficial("accounts", "/accounts", token);
        Assert.Empty(JsonDocument.Parse(lista).RootElement.GetProperty("data").EnumerateArray());

        var visao = await Enviar(HttpMethod.Get, $"/admin/consentimentos/{comAlcada}");
        Assert.Equal("pendente", visao.Corpo.GetProperty("alcada").GetProperty("situacao").GetString());
        Assert.Equal(2, visao.Corpo.GetProperty("alcada").GetProperty("aprovacoesPendentes").GetInt32());

        await Decidir(comAlcada, "aprovar", HttpStatusCode.OK, "ana");
        var aprovada = await Decidir(comAlcada, "aprovar", HttpStatusCode.OK);
        Assert.Contains("\"situacao\":\"aprovada\"", aprovada, StringComparison.Ordinal);
        Assert.Contains("\"autor\":\"ana\"", aprovada, StringComparison.Ordinal);
        Assert.Contains("\"autor\":\"operador\"", aprovada, StringComparison.Ordinal);
        Assert.All(await EstadosEmRecursos(comAlcada), e => Assert.Equal("AVAILABLE", e));
        Assert.Equal(HttpStatusCode.OK, (await Oficial("accounts", "/accounts/{accountId}", await Token(comAlcada), Conta(aurora))).Status);

        Assert.Contains("sem aprovação pendente", await Decidir(comAlcada, "aprovar", HttpStatusCode.Conflict), StringComparison.Ordinal);
        await Decidir(comAlcada, "recusar", HttpStatusCode.Conflict);
        await Decidir(semAlcada, "aprovar", HttpStatusCode.Conflict);

        var (_, historico) = await Enviar(HttpMethod.Get, "/admin/massa/historico");
        Assert.Contains("alcada-aprovada", historico.ToString(), StringComparison.Ordinal);
        Assert.Contains("consentimento-criado-no-painel", historico.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Recusa_deixa_os_recursos_indisponiveis_e_a_renovacao_recusada()
    {
        var aurora = await Cliente("PJ");
        var id = await Criar(aurora, 1);
        var recusada = await Decidir(id, "recusar", HttpStatusCode.OK);
        Assert.Contains("\"situacao\":\"recusada\"", recusada, StringComparison.Ordinal);
        Assert.Contains("\"estado\":\"AUTHORISED\"", recusada, StringComparison.Ordinal);
        Assert.All(await EstadosEmRecursos(id), e => Assert.Equal("UNAVAILABLE", e));

        var cpf = (await Cliente()).Cliente.Documento;
        using var renovacao = await AtalhosDeConsentimento.Chamar(
            _api,
            HttpMethod.Post,
            AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents/{consentId}/extends", id),
            await Token(id),
            new { data = new { loggedUser = new { document = new { identification = cpf, rel = "CPF" } }, businessEntity = new { document = new { identification = aurora.Cliente.Documento, rel = "CNPJ" } } } },
            AtalhosDeConsentimento.CabecalhosDeRenovacao);
        var corpo = await renovacao.Content.ReadAsStringAsync(Cancelamento);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, renovacao.StatusCode);
        Assert.Contains("DEPENDE_MULTIPLA_ALCADA", corpo, StringComparison.Ordinal);
        Assert.Empty(ValidadorDeContrato.Validar("consentsPostConsentsConsentIdExtends", corpo, "422"));

        Assert.Contains("alçada recusada", await Decidir(id, "aprovar", HttpStatusCode.Conflict), StringComparison.Ordinal);
        await Decidir(id, "recusar", HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Prazo_de_15_dias_vence_sem_acao()
    {
        var aurora = await Cliente("PJ");
        var id = await Criar(aurora, 1);
        _fabrica.Relogio.Advance(TimeSpan.FromDays(15) - TimeSpan.FromSeconds(1));
        Assert.All(await EstadosEmRecursos(id), e => Assert.Equal("PENDING_AUTHORISATION", e));
        _fabrica.Relogio.Advance(TimeSpan.FromSeconds(1));
        Assert.All(await EstadosEmRecursos(id), e => Assert.Equal("UNAVAILABLE", e));
        Assert.Contains("prazo da alçada encerrado", await Decidir(id, "aprovar", HttpStatusCode.Conflict), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Consentimento_revogado_nao_aceita_decisao()
    {
        var id = await Criar(await Cliente("PJ"), 1);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, $"/admin/consentimentos/{id}/revogar")).Status);
        Assert.Contains("consentimento rejeitado", await Decidir(id, "aprovar", HttpStatusCode.Conflict), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Consentimento_do_painel_serve_so_a_receptora_escolhida()
    {
        var id = await Criar(await Cliente());
        var outra = await AtalhosDeConsentimento.CriarReceptora(_admin, "Outra");
        using var resposta = await AtalhosDeConsentimento.PedirToken(_api, Banco, outra.Usuario, outra.Senha, $"consent:{id}");
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Oficial("resources", "/resources", await Token(id))).Status);
    }

    [Fact]
    public async Task Criacao_com_prazo_e_selecao_grava_o_que_foi_pedido()
    {
        var mariana = await Cliente();
        var conta = Conta(mariana);
        var (status, corpo) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new
        {
            cliente = mariana.Cliente.Documento,
            banco = Banco,
            receptora = _receptora.OrganisationId,
            permissoes = ContasECartoes,
            contas = new[] { conta },
            cartoes = Array.Empty<string>(),
            prazoMeses = 3,
        });
        Assert.True(status == HttpStatusCode.UnprocessableEntity, corpo.ToString());
        Assert.Equal("SEM_ORIGEM_SELECIONADA", Assert.Single(corpo.GetProperty("erros").EnumerateArray()).GetString());

        (status, corpo) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new
        {
            cliente = mariana.Cliente.Documento,
            banco = Banco,
            receptora = _receptora.OrganisationId,
            permissoes = Contas,
            contas = new[] { conta },
            prazoMeses = 3,
        });
        Assert.Equal(HttpStatusCode.Created, status);
        Assert.Equal(conta, Assert.Single(corpo.GetProperty("recursos").EnumerateArray()).GetProperty("resourceId").GetString());
        Assert.Equal(_fabrica.Relogio.GetUtcNow().AddMonths(3), corpo.GetProperty("expiraEm").GetDateTimeOffset());
        Assert.Contains("RESOURCES_READ", corpo.GetProperty("permissoes").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal("sem-alcada", corpo.GetProperty("alcada").GetProperty("situacao").GetString());
        Assert.False(corpo.GetProperty("alcada").TryGetProperty("prazo", out _));

        var id = corpo.GetProperty("consentId").GetString()!;
        using var consulta = await AtalhosDeConsentimento.Chamar(_api, HttpMethod.Get, AtalhosDeConsentimento.Caminho(Banco, "consents", "/consents/{consentId}", id), await AtalhosDeConsentimento.Token(_admin, _api, Banco, "consents"));
        Assert.Equal(HttpStatusCode.Forbidden, consulta.StatusCode);
    }

    [Fact]
    public async Task Pedidos_invalidos_respondem_400_422_ou_404_sem_criar()
    {
        var mariana = await Cliente();
        var documento = mariana.Cliente.Documento;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar(HttpMethod.Post, "/admin/consentimentos", new { banco = Banco })).Status);
        using (var texto = new StringContent("nao é json", System.Text.Encoding.UTF8, "application/json"))
        using (var resposta = await _admin.PostAsync(new Uri("/admin/consentimentos", UriKind.Relative), texto, Cancelamento))
        {
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        using (var semTipo = new StringContent("{}", System.Text.Encoding.UTF8, "text/plain"))
        using (var resposta = await _admin.PostAsync(new Uri("/admin/consentimentos", UriKind.Relative), semTipo, Cancelamento))
        {
            Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        }

        var desconhecido = await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = Contas, prazo = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, desconhecido.Status);
        Assert.Equal("PEDIDO_INVALIDO", Assert.Single(desconhecido.Corpo.GetProperty("erros").EnumerateArray()).GetString());
        var permissao = await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = Desconhecida });
        Assert.Equal(HttpStatusCode.BadRequest, permissao.Status);
        Assert.Equal("PEDIDO_INVALIDO", Assert.Single(permissao.Corpo.GetProperty("erros").EnumerateArray()).GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = "00000000000", banco = Banco, permissoes = Contas })).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = documento, banco = "nao-existe", permissoes = Contas })).Status);

        var (status, corpo) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = documento, banco = Banco, receptora = "nao-existe", permissoes = Incompleto, prazoMeses = 0, aprovacoesPendentes = 1 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Equal(
            ["RECEPTORA_INEXISTENTE", "PRAZO_INVALIDO", "ALCADA_INVALIDA", "COMBINACAO_PERMISSOES_INCORRETA"],
            corpo.GetProperty("erros").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = documento, banco = Banco, permissoes = Contas })).Status);

        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Get, "/admin/consentimentos/nao-existe")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Post, "/admin/consentimentos/nao-existe/aprovar")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Post, "/admin/consentimentos/nao-existe/recusar")).Status);
        Assert.Empty((await new AdminDaMassa(_admin).Ler<MassaGerada>($"/clientes/{documento}")).Consentimentos);
    }

    [Fact]
    public async Task Opcoes_do_menu()
    {
        var aurora = await Cliente("PJ");
        var (status, corpo) = await Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?cliente={aurora.Cliente.Documento}&banco={Banco}");
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("PJ", corpo.GetProperty("tipo").GetString());
        Assert.False(corpo.GetProperty("semProduto").GetBoolean());
        var permissoes = corpo.GetProperty("grupos").EnumerateArray().SelectMany(g => g.GetProperty("permissoes").EnumerateArray()).Select(p => p.GetString()!).ToList();
        Assert.Contains("CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ", permissoes);
        Assert.DoesNotContain(permissoes, p => p.StartsWith("CUSTOMERS_PERSONAL_", StringComparison.Ordinal));
        Assert.All(corpo.GetProperty("contas").EnumerateArray(), c => Assert.True(c.GetProperty("marcada").GetBoolean()));
        Assert.Contains(corpo.GetProperty("receptoras").EnumerateArray(), r => r.GetProperty("organisationId").GetString() == _receptora.OrganisationId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?banco={Banco}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?cliente=00000000000&banco={Banco}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?cliente={aurora.Cliente.Documento}&banco=nao-existe")).Status);
    }

    [Fact]
    public async Task Painel_usa_o_relogio_do_banco_na_criacao_no_prazo_na_alcada_e_na_expiracao()
    {
        var aurora = await Cliente("PJ");
        var banco = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await FixarRelogioDoBanco(banco);

        var (status, corpo) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new
        {
            cliente = aurora.Cliente.Documento,
            banco = Banco,
            receptora = _receptora.OrganisationId,
            permissoes = Contas,
            prazoMeses = 1,
            aprovacoesPendentes = 1,
        });
        Assert.True(status == HttpStatusCode.Created, corpo.ToString());
        var comPrazo = corpo.GetProperty("consentId").GetString()!;
        Assert.Equal(banco, corpo.GetProperty("criadoEm").GetDateTimeOffset());
        Assert.Equal(banco.AddMonths(1), corpo.GetProperty("expiraEm").GetDateTimeOffset());
        Assert.Equal(banco.AddDays(15), corpo.GetProperty("alcada").GetProperty("prazo").GetDateTimeOffset());
        var semPrazo = await Criar(aurora, 1);

        await FixarRelogioDoBanco(banco.AddDays(15).AddSeconds(-1));
        var aprovada = JsonDocument.Parse(await Decidir(comPrazo, "aprovar", HttpStatusCode.OK)).RootElement;
        Assert.Equal(banco.AddDays(15).AddSeconds(-1), aprovada.GetProperty("decisoes")[0].GetProperty("em").GetDateTimeOffset());

        await FixarRelogioDoBanco(banco.AddDays(15));
        Assert.Equal("vencida", (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{semPrazo}")).Corpo.GetProperty("alcada").GetProperty("situacao").GetString());
        Assert.Contains("prazo da alçada encerrado", await Decidir(semPrazo, "aprovar", HttpStatusCode.Conflict), StringComparison.Ordinal);
        Assert.Equal("AUTHORISED", (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{comPrazo}")).Corpo.GetProperty("estado").GetString());

        await FixarRelogioDoBanco(banco.AddMonths(1));
        Assert.Equal("REJECTED", (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{comPrazo}")).Corpo.GetProperty("estado").GetString());

        Assert.Equal(Inicio, _fabrica.Relogio.GetUtcNow());
        using (var solto = await _admin.DeleteAsync(new Uri($"/admin/instituicoes/{Banco}/relogio", UriKind.Relative), Cancelamento))
        {
            Assert.True(solto.IsSuccessStatusCode);
        }

        Assert.Equal("pendente", (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{semPrazo}")).Corpo.GetProperty("alcada").GetProperty("situacao").GetString());
        Assert.Equal("AUTHORISED", (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{comPrazo}")).Corpo.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Visao_de_consentimento_revogado_nao_mostra_aprovacao_pendente()
    {
        var id = await Criar(await Cliente("PJ"), 2);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, $"/admin/consentimentos/{id}/revogar")).Status);
        var alcada = (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{id}")).Corpo.GetProperty("alcada");
        Assert.Equal("encerrada", alcada.GetProperty("situacao").GetString());
        Assert.Equal(0, alcada.GetProperty("aprovacoesPendentes").GetInt32());
    }

    [Fact]
    public async Task Fluxo_do_painel_ponta_a_ponta_com_alcadas_multiplas()
    {
        var aurora = await Cliente("PJ");
        var (_, opcoes) = await Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?cliente={aurora.Cliente.Documento}&banco={Banco}");
        var conta = opcoes.GetProperty("contas")[0].GetProperty("resourceId").GetString()!;
        var grupo = opcoes.GetProperty("grupos").EnumerateArray().First(g => g.GetProperty("nome").GetString() == "contas-saldos");
        var receptora = opcoes.GetProperty("receptoras").EnumerateArray().First(r => r.GetProperty("organisationId").GetString() == _receptora.OrganisationId);

        var (status, criado) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new
        {
            cliente = aurora.Cliente.Documento,
            banco = Banco,
            receptora = receptora.GetProperty("organisationId").GetString(),
            permissoes = grupo.GetProperty("permissoes").EnumerateArray().Select(p => p.GetString()).ToArray(),
            contas = new[] { conta },
            aprovacoesPendentes = 3,
        });
        Assert.True(status == HttpStatusCode.Created, criado.ToString());
        var id = criado.GetProperty("consentId").GetString()!;
        Assert.Equal(conta, Assert.Single(criado.GetProperty("recursos").EnumerateArray()).GetProperty("resourceId").GetString());
        Assert.Equal("PENDING_AUTHORISATION", Assert.Single(await EstadosEmRecursos(id)));

        await Decidir(id, "aprovar", HttpStatusCode.OK, "ana");
        var parcial = (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{id}")).Corpo;
        Assert.Equal(2, parcial.GetProperty("alcada").GetProperty("aprovacoesPendentes").GetInt32());
        Assert.Equal("PENDING_AUTHORISATION", Assert.Single(await EstadosEmRecursos(id)));
        Assert.Equal(HttpStatusCode.Forbidden, (await Oficial("accounts", "/accounts/{accountId}", await Token(id), conta)).Status);

        await Decidir(id, "aprovar", HttpStatusCode.OK, "ana");
        var final = JsonDocument.Parse(await Decidir(id, "aprovar", HttpStatusCode.OK, "bruno")).RootElement;
        Assert.Equal("aprovada", final.GetProperty("alcada").GetProperty("situacao").GetString());
        Assert.Equal(["ana", "ana", "bruno"], final.GetProperty("decisoes").EnumerateArray().Select(d => d.GetProperty("autor").GetString()));
        Assert.Equal("AVAILABLE", Assert.Single(await EstadosEmRecursos(id)));

        var token = await Token(id);
        var (_, lista) = await Oficial("accounts", "/accounts", token);
        Assert.Equal(conta, Assert.Single(JsonDocument.Parse(lista).RootElement.GetProperty("data").EnumerateArray()).GetProperty("accountId").GetString());
        Assert.Equal(HttpStatusCode.OK, (await Oficial("accounts", "/accounts/{accountId}", token, conta)).Status);
        Assert.Contains("sem aprovação pendente", await Decidir(id, "aprovar", HttpStatusCode.Conflict), StringComparison.Ordinal);

        var historico = (await Enviar(HttpMethod.Get, "/admin/massa/historico")).Corpo.ToString();
        Assert.Contains("consentimento-criado-no-painel", historico, StringComparison.Ordinal);
        Assert.Contains("pendente:1", historico, StringComparison.Ordinal);
        Assert.Contains("aprovada:0", historico, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Origem_invalida_ou_sem_origem_responde_422_sem_criar()
    {
        var mariana = await Cliente();
        var outro = await Cliente();
        var documento = mariana.Cliente.Documento;
        var cartao = mariana.Recursos.First(r => r.Banco == Banco && r.Tipo == "CREDIT_CARD_ACCOUNT").ResourceId;

        Assert.Equal("ORIGEM_INVALIDA", await UnicoErro(new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = Contas, contas = new[] { Conta(outro) } }));
        Assert.Equal("ORIGEM_INVALIDA", await UnicoErro(new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = Contas, cartoes = new[] { cartao } }));
        Assert.Equal("SEM_ORIGEM_SELECIONADA", await UnicoErro(new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = Contas, contas = Array.Empty<string>() }));

        using (var semCartoes = JsonContent.Create(new { produtos = SemCartoes }))
        using (var alterado = await _admin.PatchAsync(new Uri($"/admin/instituicoes/{Banco}", UriKind.Relative), semCartoes, Cancelamento))
        {
            Assert.Equal(HttpStatusCode.OK, alterado.StatusCode);
        }

        var (_, opcoes) = await Enviar(HttpMethod.Get, $"/admin/consentimentos/opcoes?cliente={documento}&banco={Banco}");
        Assert.Empty(opcoes.GetProperty("cartoes").EnumerateArray());
        Assert.NotEmpty(opcoes.GetProperty("contas").EnumerateArray());
        Assert.Equal("ORIGEM_INVALIDA", await UnicoErro(new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = ContasECartoes, cartoes = new[] { cartao } }));
        Assert.Empty((await new AdminDaMassa(_admin).Ler<MassaGerada>($"/clientes/{documento}")).Consentimentos);

        var (status, criado) = await Enviar(HttpMethod.Post, "/admin/consentimentos", new { cliente = documento, banco = Banco, receptora = _receptora.OrganisationId, permissoes = ContasECartoes });
        Assert.True(status == HttpStatusCode.Created, criado.ToString());
        Assert.DoesNotContain(criado.GetProperty("recursos").EnumerateArray(), r => r.GetProperty("resourceId").GetString() == cartao);
    }

    [Fact]
    public async Task Duas_aprovacoes_na_mesma_versao_uma_vence_e_a_outra_recebe_conflito()
    {
        var id = await Criar(await Cliente("PJ"), 1);
        (HttpStatusCode Status, JsonElement Corpo) concorrente = default;
        _fabrica.Massa.AntesDaEscritaCondicional = async () => concorrente = await Enviar(HttpMethod.Post, $"/admin/consentimentos/{id}/aprovar", autor: "bruno");

        var (status, corpo) = await Enviar(HttpMethod.Post, $"/admin/consentimentos/{id}/aprovar", autor: "ana");

        Assert.Equal(HttpStatusCode.OK, concorrente.Status);
        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Contains("sem aprovação pendente", corpo.ToString(), StringComparison.Ordinal);
        var visao = (await Enviar(HttpMethod.Get, $"/admin/consentimentos/{id}")).Corpo;
        Assert.Equal("bruno", Assert.Single(visao.GetProperty("decisoes").EnumerateArray()).GetProperty("autor").GetString());
        Assert.Equal(0, visao.GetProperty("alcada").GetProperty("aprovacoesPendentes").GetInt32());
    }

    [Fact]
    public async Task Aprovacao_que_perde_a_corrida_le_de_novo_e_nao_apaga_a_outra()
    {
        var id = await Criar(await Cliente("PJ"), 2);
        (HttpStatusCode Status, JsonElement Corpo) concorrente = default;
        _fabrica.Massa.AntesDaEscritaCondicional = async () => concorrente = await Enviar(HttpMethod.Post, $"/admin/consentimentos/{id}/aprovar", autor: "bruno");

        var (status, corpo) = await Enviar(HttpMethod.Post, $"/admin/consentimentos/{id}/aprovar", autor: "ana");

        Assert.Equal(HttpStatusCode.OK, concorrente.Status);
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(["bruno", "ana"], corpo.GetProperty("decisoes").EnumerateArray().Select(d => d.GetProperty("autor").GetString()));
        Assert.Equal("aprovada", corpo.GetProperty("alcada").GetProperty("situacao").GetString());
    }

    private async Task<string?> UnicoErro(object pedido)
    {
        var (status, corpo) = await Enviar(HttpMethod.Post, "/admin/consentimentos", pedido);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        return Assert.Single(corpo.GetProperty("erros").EnumerateArray()).GetString();
    }

    private async Task FixarRelogioDoBanco(DateTimeOffset instante)
    {
        using var resposta = await _admin.PutAsJsonAsync(new Uri($"/admin/instituicoes/{Banco}/relogio", UriKind.Relative), new { fixadoEm = instante }, Cancelamento);
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    public void Dispose()
    {
        _admin.Dispose();
        _api.Dispose();
        _fabrica.Dispose();
    }
}
