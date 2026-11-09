using Simulador.Consentimentos.Contratos;
using Simulador.Consentimentos.Dominio;

namespace Simulador.Testes.Unidade.Consentimentos;

public class DominioTests
{
    private const string Banco = "banco-estavel";
    private const string Id = "urn:hml:p-1";
    private static readonly byte[] Chave = [1, 2, 3, 4];
    private static readonly DateTimeOffset Agora = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly OperacaoProtegida Criacao = new("consents", ["consents"], []);
    private static readonly OperacaoProtegida Renovacao = new("consents", ["openid", "consent:consentId"], []);
    private static readonly OperacaoProtegida Contas = new("accounts", ["consent:consentId"], ["ACCOUNTS_READ"]);
    private static readonly OperacaoProtegida Fundos = new("funds", ["consent:consentId"], []);
    private static readonly OperacaoProtegida Desconhecida = new("outra", ["consent:consentId"], []);

    private static readonly string[] ContasOk = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ", "RESOURCES_READ"];
    private static readonly string[] EstadosValidos = ["AVAILABLE", "TEMPORARILY_UNAVAILABLE", "UNAVAILABLE", "PENDING_AUTHORISATION"];
    private static readonly string[] Cadastro = ["CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", "RESOURCES_READ"];

    private static TokenDeAcesso Do(TipoDeToken tipo, string? consentId = null, string banco = Banco, int segundos = 900) =>
        new(tipo, "org", banco, consentId, Agora.AddSeconds(segundos));

    private static DadosDoConsentimento Consentimento(
        string estado = Ciclo.Autorizado,
        DateTimeOffset? expira = null,
        string[]? permissoes = null,
        string? receptora = null,
        int alcadas = 0,
        string? logado = null) =>
        new(Id, "90000000000", Banco, estado, permissoes ?? ContasOk, [], Agora, "hml", "persistido", null, expira, receptora, null, null, null, logado, alcadas, []);

    [Fact]
    public void Token_ida_e_volta()
    {
        var original = Do(TipoDeToken.Consentimento, Id);
        Assert.Equal(original, CodecDeToken.Ler(CodecDeToken.Emitir(original, Chave), Chave));
    }

    [Fact]
    public void Token_da_receptora_nao_tem_consentimento() =>
        Assert.Null(CodecDeToken.Ler(CodecDeToken.Emitir(Do(TipoDeToken.Receptora), Chave), Chave)!.ConsentId);

    [Theory]
    [InlineData(null)]
    [InlineData("sem-ponto")]
    [InlineData("a.b.c")]
    [InlineData("YWJj.assinatura")]
    public void Token_invalido_e_nulo(string? texto) => Assert.Null(CodecDeToken.Ler(texto, Chave));

    [Fact]
    public void Token_de_outra_chave_e_nulo() =>
        Assert.Null(CodecDeToken.Ler(CodecDeToken.Emitir(Do(TipoDeToken.Receptora), Chave), [9]));

    [Fact]
    public void Portao_sem_token_expirado_ou_de_outro_banco_responde_401()
    {
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(null, Banco, Agora, Criacao, null, null));
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(Do(TipoDeToken.Receptora, segundos: 0), Banco, Agora, Criacao, null, null));
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(Do(TipoDeToken.Receptora, banco: "outro"), Banco, Agora, Criacao, null, null));
    }

    [Fact]
    public void Portao_confere_o_tipo_do_token()
    {
        Assert.Equal(VereditoDoPortao.Passa, RegrasDoPortao.Avaliar(Do(TipoDeToken.Receptora), Banco, Agora, Criacao, null, null));
        Assert.Equal(VereditoDoPortao.Proibido, RegrasDoPortao.Avaliar(Do(TipoDeToken.Consentimento, Id), Banco, Agora, Criacao, null, null));
        Assert.Equal(VereditoDoPortao.Proibido, RegrasDoPortao.Avaliar(Do(TipoDeToken.Receptora), Banco, Agora, Contas, null, Consentimento()));
    }

    [Fact]
    public void Portao_na_renovacao_nao_confere_estado()
    {
        Assert.False(RegrasDoPortao.ConfereEstado(Renovacao));
        Assert.Equal(VereditoDoPortao.Passa, RegrasDoPortao.Avaliar(Do(TipoDeToken.Consentimento, Id), Banco, Agora, Renovacao, Id, null));
        Assert.Equal(VereditoDoPortao.Proibido, RegrasDoPortao.Avaliar(Do(TipoDeToken.Consentimento, Id), Banco, Agora, Renovacao, "outro", null));
    }

    [Fact]
    public void Portao_consentimento_sem_validade_responde_401()
    {
        var token = Do(TipoDeToken.Consentimento, Id);
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(token, Banco, Agora, Contas, null, null));
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(token, Banco, Agora, Contas, null, Consentimento(Ciclo.Rejeitado)));
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(token, Banco, Agora, Contas, null, Consentimento(expira: Agora)));
        Assert.Equal(VereditoDoPortao.NaoAutorizado, RegrasDoPortao.Avaliar(token, Banco, Agora, Contas, null, Consentimento() with { Banco = "outro" }));
    }

    [Fact]
    public void Portao_confere_permissao_exata_familia_e_contrato_sem_familia()
    {
        var token = Do(TipoDeToken.Consentimento, Id);
        Assert.Equal(VereditoDoPortao.Passa, RegrasDoPortao.Avaliar(token, Banco, Agora, Contas, null, Consentimento()));
        Assert.Equal(VereditoDoPortao.Proibido, RegrasDoPortao.Avaliar(token, Banco, Agora, Contas, null, Consentimento(permissoes: Cadastro)));
        Assert.Equal(VereditoDoPortao.Proibido, RegrasDoPortao.Avaliar(token, Banco, Agora, Fundos, null, Consentimento()));
        Assert.Equal(VereditoDoPortao.Passa, RegrasDoPortao.Avaliar(token, Banco, Agora, Fundos, null, Consentimento(permissoes: ["FUNDS_READ"])));
        Assert.Equal(VereditoDoPortao.Proibido, RegrasDoPortao.Avaliar(token, Banco, Agora, Desconhecida, null, Consentimento()));
    }

    [Fact]
    public void Consentimento_sem_receptora_serve_a_qualquer_uma()
    {
        Assert.True(RegrasDoPortao.ServeA(Consentimento(), "org"));
        Assert.True(RegrasDoPortao.ServeA(Consentimento(receptora: "org"), "org"));
        Assert.False(RegrasDoPortao.ServeA(Consentimento(receptora: "outra"), "org"));
    }

    [Theory]
    [InlineData(false, Permissoes.PfPjJuntas, "CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", "CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ", "RESOURCES_READ")]
    [InlineData(false, Permissoes.PjSemDados, "CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ", "RESOURCES_READ")]
    [InlineData(true, Permissoes.PjIncorretas, "CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", "RESOURCES_READ")]
    [InlineData(false, Permissoes.Combinacao, "ACCOUNTS_READ", "RESOURCES_READ")]
    [InlineData(false, Permissoes.Combinacao, "ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ")]
    [InlineData(false, Permissoes.Combinacao, "RESOURCES_READ")]
    public void Combinacoes_invalidas(bool temPj, string codigo, params string[] permissoes) =>
        Assert.Equal(codigo, Permissoes.Validar(permissoes, temPj));

    [Fact]
    public void Grupos_completos_sao_validos()
    {
        Assert.Null(Permissoes.Validar(ContasOk, false));
        Assert.Null(Permissoes.Validar(["CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ", "RESOURCES_READ"], true));
    }

    [Fact]
    public void Permissao_desconhecida_ou_lista_vazia()
    {
        Assert.True(Permissoes.TodasConhecidas(ContasOk));
        Assert.False(Permissoes.TodasConhecidas(["XPTO"]));
        Assert.False(Permissoes.TodasConhecidas([]));
    }

    [Fact]
    public void Produto_individual_nao_oferecido_sai_e_agrupado_fica()
    {
        var pedidas = new[] { "ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_READ", "LOANS_READ", "RESOURCES_READ" };
        Assert.Equal(pedidas, Permissoes.Aplicaveis(pedidas, ["contas", "cartoes"]));
        Assert.Equal(["CREDIT_CARDS_ACCOUNTS_READ", "LOANS_READ", "RESOURCES_READ"], Permissoes.Aplicaveis(pedidas, ["cartoes"]));
        Assert.Equal(["ACCOUNTS_READ", "LOANS_READ", "RESOURCES_READ"], Permissoes.Aplicaveis(pedidas, ["contas"]));
    }

    [Fact]
    public void Funcional_cobertura_e_agrupamento()
    {
        Assert.False(Permissoes.TemFuncional(["RESOURCES_READ"]));
        Assert.True(Permissoes.TemFuncional(ContasOk));
        Assert.True(Permissoes.Cobre("ACCOUNT", ContasOk));
        Assert.False(Permissoes.Cobre("LOAN", ContasOk));
        Assert.False(Permissoes.Cobre("XPTO", ContasOk));
        Assert.True(Permissoes.Agrupado("LOAN"));
        Assert.False(Permissoes.Agrupado("ACCOUNT"));
        Assert.False(Permissoes.Agrupado("CREDIT_CARD_ACCOUNT"));
    }

    [Fact]
    public void Vencido_vale_como_rejeitado()
    {
        var efetivo = Ciclo.Efetivo(Consentimento(expira: Agora), Agora);
        Assert.Equal((Ciclo.Rejeitado, Ciclo.PorInstituicao, Ciclo.Vencido, (DateTimeOffset?)Agora), (efetivo.Estado, efetivo.RejeitadoPor, efetivo.Razao, efetivo.AtualizadoEm));
        Assert.Equal(Ciclo.Autorizado, Ciclo.Efetivo(Consentimento(expira: Agora.AddDays(1)), Agora).Estado);
        Assert.Equal(Ciclo.Autorizado, Ciclo.Efetivo(Consentimento(), Agora).Estado);
        var rejeitado = Consentimento(Ciclo.Rejeitado, Agora);
        Assert.Same(rejeitado, Ciclo.Efetivo(rejeitado, Agora.AddDays(1)));
    }

    [Fact]
    public void Rejeitar_grava_autor_e_razao()
    {
        var rejeitado = Ciclo.Rejeitar(Consentimento(), Ciclo.PorTpp, Ciclo.Revogado, Agora);
        Assert.Equal((Ciclo.Rejeitado, Ciclo.PorTpp, Ciclo.Revogado), (rejeitado.Estado, rejeitado.RejeitadoPor, rejeitado.Razao));
    }

    [Fact]
    public void Erros_de_renovacao()
    {
        var comData = Consentimento(expira: Agora.AddDays(30));
        Assert.Equal(Ciclo.EstadoInvalido, Ciclo.ErroDaRenovacao(Consentimento(Ciclo.Rejeitado), "d", Agora.AddDays(1), Agora));
        Assert.Equal(Ciclo.DependeAlcada, Ciclo.ErroDaRenovacao(Consentimento(alcadas: 2), "d", Agora.AddDays(1), Agora));
        Assert.Null(Ciclo.ErroDaRenovacao(comData, null, null, Agora));
        Assert.Null(Ciclo.ErroDaRenovacao(comData, "d", Agora.AddDays(60), Agora));
        Assert.Equal(Ciclo.DataInvalida, Ciclo.ErroDaRenovacao(comData, "d", Agora.AddDays(1), Agora));
        Assert.Equal(Ciclo.DataInvalida, Ciclo.ErroDaRenovacao(comData, "d", Agora.AddDays(-1), Agora));
        Assert.Equal(Ciclo.DataInvalida, Ciclo.ErroDaRenovacao(comData, "d", null, Agora));
        Assert.Equal(Ciclo.DataInvalida, Ciclo.ErroDaRenovacao(Consentimento(), "d", Agora.AddDays(1), Agora));
    }

    [Fact]
    public void Quem_pode_renovar()
    {
        Assert.True(Ciclo.PodeRenovar(Consentimento(logado: "111"), "PF", "111", true, null));
        Assert.False(Ciclo.PodeRenovar(Consentimento(logado: "111"), "PF", "222", true, null));
        Assert.True(Ciclo.PodeRenovar(Consentimento(), "PF", "90000000000", true, null));
        Assert.True(Ciclo.PodeRenovar(Consentimento(), "PJ", "222", true, "90000000000"));
        Assert.False(Ciclo.PodeRenovar(Consentimento(), "PJ", "222", false, "90000000000"));
        Assert.False(Ciclo.PodeRenovar(Consentimento(), "PJ", "222", true, "outro"));
    }

    [Theory]
    [InlineData(0, 0, "Pronta")]
    [InlineData(60, 120, "Preparando")]
    [InlineData(120, 120, "Pronta")]
    [InlineData(299, 600, "Preparando")]
    [InlineData(300, 600, "Esgotada")]
    [InlineData(300, 300, "Pronta")]
    public void Preparacao_de_recursos(int passados, int duracao, string esperada) =>
        Assert.Equal(esperada, Preparacao.Situacao(Agora, duracao, Agora.AddSeconds(passados)).ToString());

    [Theory]
    [InlineData("ACCOUNT", "AVAILABLE", "AVAILABLE", true)]
    [InlineData("ACCOUNT", "PENDING_AUTHORISATION", "AVAILABLE", true)]
    [InlineData("ACCOUNT", "AVAILABLE", "PENDING_AUTHORISATION", false)]
    [InlineData("ACCOUNT", "UNAVAILABLE", "AVAILABLE", false)]
    [InlineData("ACCOUNT", "UNAVAILABLE", "TEMPORARILY_UNAVAILABLE", false)]
    [InlineData("ACCOUNT", "AVAILABLE", "UNAVAILABLE", true)]
    [InlineData("FUND", "UNAVAILABLE", "AVAILABLE", true)]
    [InlineData("VARIABLE_INCOME", "UNAVAILABLE", "AVAILABLE", true)]
    [InlineData("ACCOUNT", "AVAILABLE", "TEMPORARILY_UNAVAILABLE", true)]
    public void Transicoes(string tipo, string de, string para, bool permitida) =>
        Assert.Equal(permitida, EstadosDeRecurso.Permitida(tipo, de, para));

    [Fact]
    public void Estados_existentes()
    {
        Assert.All(EstadosValidos, e => Assert.True(EstadosDeRecurso.Existe(e)));
        Assert.False(EstadosDeRecurso.Existe("XPTO"));
    }
}
