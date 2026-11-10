using Simulador.Consentimentos.Contratos;
using Simulador.Consentimentos.Dominio;

namespace Simulador.Testes.Unidade.Consentimentos;

public class PainelTests
{
    private const string Banco = "banco-estavel";
    private const string Outro = "banco-outro";
    private static readonly DateTimeOffset Agora = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
    private static readonly string[] Todos = ["contas", "cartoes", "emprestimos"];
    private static readonly string[] Contas = ["ACCOUNTS_READ", "ACCOUNTS_BALANCES_READ"];
    private static readonly string[] Cartoes = ["CREDIT_CARDS_ACCOUNTS_READ", "CREDIT_CARDS_ACCOUNTS_LIMITS_READ"];
    private static readonly string[] Credito =
    [
        "LOANS_READ", "LOANS_WARRANTIES_READ", "LOANS_SCHEDULED_INSTALMENTS_READ", "LOANS_PAYMENTS_READ",
        "FINANCINGS_READ", "FINANCINGS_WARRANTIES_READ", "FINANCINGS_SCHEDULED_INSTALMENTS_READ", "FINANCINGS_PAYMENTS_READ",
        "UNARRANGED_ACCOUNTS_OVERDRAFT_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_WARRANTIES_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_SCHEDULED_INSTALMENTS_READ", "UNARRANGED_ACCOUNTS_OVERDRAFT_PAYMENTS_READ",
        "INVOICE_FINANCINGS_READ", "INVOICE_FINANCINGS_WARRANTIES_READ", "INVOICE_FINANCINGS_SCHEDULED_INSTALMENTS_READ", "INVOICE_FINANCINGS_PAYMENTS_READ",
    ];

    private static DadosDoRecurso Recurso(string id, string tipo, string banco = Banco, string estado = "AVAILABLE") =>
        new(id, "doc", banco, tipo, estado, "persistido");

    private static DadosDoCliente Cliente(string tipo = "PF", params DadosDoRecurso[] recursos) =>
        new("doc", tipo, "persistido", recursos.Length == 0 ? Padrao : recursos, []);

    private static readonly DadosDoRecurso[] Padrao =
    [
        Recurso("conta-1", "ACCOUNT"),
        Recurso("conta-2", "ACCOUNT"),
        Recurso("cartao-1", "CREDIT_CARD_ACCOUNT"),
        Recurso("emprestimo-1", "LOAN"),
        Recurso("conta-outro", "ACCOUNT", Outro),
    ];

    private static AvaliacaoDoPedido Avaliar(
        IReadOnlyList<string>? permissoes,
        DadosDoCliente? cliente = null,
        IReadOnlyList<string>? contas = null,
        IReadOnlyList<string>? cartoes = null,
        int? prazo = null,
        int? alcadas = null,
        IReadOnlyList<string>? produtos = null,
        bool receptora = true) =>
        Painel.Avaliar(new PedidoDoPainel("doc", Banco, "org", permissoes, contas, cartoes, prazo, alcadas), cliente ?? Cliente(), Banco, produtos ?? Todos, receptora);

    private static DadosDoConsentimento Consentimento(int pendentes = 0, bool recusada = false, IReadOnlyList<DadosDaDecisao>? decisoes = null, string estado = Ciclo.Autorizado) =>
        new("c-1", "doc", Banco, estado, Contas, ["conta-1"], Agora, "hml", "persistido", null, null, "org", null, null, Agora, null, pendentes, [], 0, recusada, decisoes);

    [Fact]
    public void Contas_e_cartoes_sem_selecao_levam_tudo_do_banco_com_RESOURCES_READ()
    {
        var avaliacao = Avaliar([.. Contas, .. Cartoes]);
        Assert.Empty(avaliacao.Erros);
        Assert.Contains("RESOURCES_READ", avaliacao.Permissoes);
        Assert.Equal(["conta-1", "conta-2", "cartao-1"], avaliacao.Recursos);
    }

    [Fact]
    public void Selecao_escolhe_contas_e_credito_compartilha_tudo()
    {
        var avaliacao = Avaliar([.. Contas, .. Credito], contas: ["conta-2"]);
        Assert.Empty(avaliacao.Erros);
        Assert.Equal(["conta-2", "emprestimo-1"], avaliacao.Recursos);
    }

    [Fact]
    public void Selecao_vazia_com_o_grupo_e_recusada() =>
        Assert.Equal([Painel.SemOrigem], Avaliar(Contas, contas: []).Erros);

    [Fact]
    public void Selecao_vazia_sem_opcao_e_aceita()
    {
        var cliente = Cliente("PF", Recurso("emprestimo-1", "LOAN"));
        var avaliacao = Avaliar(Contas, cliente, contas: []);
        Assert.Empty(avaliacao.Erros);
        Assert.Empty(avaliacao.Recursos);
    }

    [Fact]
    public void Selecao_vazia_sem_o_grupo_e_ignorada() =>
        Assert.Empty(Avaliar(Credito, cartoes: []).Erros);

    [Theory]
    [InlineData("conta-outro")]
    [InlineData("cartao-1")]
    [InlineData("inexistente")]
    public void Selecao_com_id_de_fora_e_recusada(string id) =>
        Assert.Equal([Painel.OrigemInvalida], Avaliar(Contas, contas: [id]).Erros);

    [Fact]
    public void Selecao_sem_a_permissao_do_grupo_e_recusada() =>
        Assert.Equal([Painel.OrigemInvalida], Avaliar(Credito, cartoes: ["cartao-1"]).Erros);

    [Fact]
    public void Selecao_com_permissao_invalida_ainda_confere_os_ids()
    {
        Assert.Equal([Permissoes.Combinacao, Painel.OrigemInvalida], Avaliar(["ACCOUNTS_READ"], contas: ["x"]).Erros);
        Assert.Equal([Permissoes.Combinacao], Avaliar(["ACCOUNTS_READ"], contas: ["conta-1"]).Erros);
    }

    [Fact]
    public void Grupo_incompleto_e_combinacao_incorreta_e_permissao_desconhecida_e_pedido_invalido()
    {
        Assert.Equal([Permissoes.Combinacao], Avaliar(["ACCOUNTS_READ"]).Erros);
        Assert.Equal([Painel.PedidoInvalido], Avaliar(["NAO_EXISTE"]).Erros);
        Assert.Equal([Painel.PedidoInvalido], Avaliar([.. Contas, "NAO_EXISTE"]).Erros);
        Assert.Equal([Permissoes.Combinacao], Avaliar([]).Erros);
    }

    [Fact]
    public void Permissoes_do_tipo_errado_seguem_RN84()
    {
        Assert.Equal([Permissoes.PjSemDados], Avaliar(["CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ"]).Erros);
        Assert.Equal([Permissoes.PjIncorretas], Avaliar(["CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ"], Cliente("PJ")).Erros);
        Assert.Equal([Permissoes.PfPjJuntas], Avaliar(["CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ", "CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ"]).Erros);
    }

    [Fact]
    public void Produto_individual_nao_oferecido_sai_e_sem_funcional_e_recusado()
    {
        Assert.Equal([Permissoes.SemFuncionais], Avaliar(Contas, produtos: ["cartoes"]).Erros);
        var avaliacao = Avaliar([.. Contas, .. Cartoes], produtos: ["cartoes"]);
        Assert.DoesNotContain("ACCOUNTS_READ", avaliacao.Permissoes);
        Assert.Equal(["cartao-1"], avaliacao.Recursos);
    }

    [Fact]
    public void Sem_permissoes_e_pedido_invalido() => Assert.Equal([Painel.PedidoInvalido], Avaliar(null).Erros);

    [Fact]
    public void Junta_todos_os_erros()
    {
        var cliente = Cliente("PF", Recurso("conta-outro", "ACCOUNT", Outro));
        var avaliacao = Avaliar(Contas, cliente, prazo: 13, alcadas: 2, receptora: false);
        Assert.Equal([Painel.SemProduto, Painel.ReceptoraInexistente, Painel.PrazoInvalido, Painel.AlcadaInvalida], avaliacao.Erros);
        Assert.Empty(avaliacao.Permissoes);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    [InlineData(13, false)]
    public void Prazo_de_1_a_12_meses(int meses, bool valido) =>
        Assert.Equal(valido, Avaliar(Contas, prazo: meses).Erros.Count == 0);

    [Theory]
    [InlineData("PJ", 0, false)]
    [InlineData("PJ", 1, true)]
    [InlineData("PJ", 10, true)]
    [InlineData("PJ", 11, false)]
    [InlineData("PF", 2, false)]
    public void Alcadas_so_em_PJ_de_1_a_10(string tipo, int pendentes, bool valido) =>
        Assert.Equal(valido, Avaliar(Contas, Cliente(tipo), alcadas: pendentes).Erros.Count == 0);

    [Fact]
    public void Opcoes_PF_sem_grupos_PJ_e_com_origens_marcadas()
    {
        var opcoes = Painel.Opcoes(Cliente(), Banco, Todos);
        Assert.False(opcoes.SemProduto);
        Assert.DoesNotContain(opcoes.Grupos, g => g.Permissoes.Any(p => p.StartsWith("CUSTOMERS_BUSINESS_", StringComparison.Ordinal)));
        Assert.Contains(opcoes.Grupos, g => g.Nome == "cadastro-identificacao" && g.Permissoes.Contains("CUSTOMERS_PERSONAL_IDENTIFICATIONS_READ"));
        Assert.Equal(["conta-1", "conta-2"], opcoes.Contas);
        Assert.Equal(["cartao-1"], opcoes.Cartoes);
        Assert.Equal(["LOAN"], opcoes.Agrupados);
    }

    [Fact]
    public void Opcoes_PJ_so_com_grupos_PJ_e_sem_cartoes_nao_oferecidos()
    {
        var opcoes = Painel.Opcoes(Cliente("PJ", Recurso("conta-outro", "ACCOUNT", Outro)), Banco, ["contas"]);
        Assert.True(opcoes.SemProduto);
        Assert.DoesNotContain(opcoes.Grupos, g => g.Permissoes.Any(p => p.StartsWith("CUSTOMERS_PERSONAL_", StringComparison.Ordinal)));
        Assert.DoesNotContain(opcoes.Grupos, g => g.Nome.StartsWith("cartoes", StringComparison.Ordinal));
        Assert.Contains(opcoes.Grupos, g => g.Nome == "operacoes-de-credito");
        Assert.Empty(opcoes.Contas);
    }

    [Fact]
    public void Menu_e_selecao_so_com_contas_e_cartoes_que_o_banco_oferece()
    {
        var opcoes = Painel.Opcoes(Cliente(), Banco, ["contas"]);
        Assert.Equal(["conta-1", "conta-2"], opcoes.Contas);
        Assert.Empty(opcoes.Cartoes);
        Assert.Equal(["LOAN"], opcoes.Agrupados);
        Assert.Contains(opcoes.Grupos, g => g.Nome == "operacoes-de-credito");

        Assert.Equal([Painel.OrigemInvalida], Avaliar([.. Contas, .. Cartoes], cartoes: ["cartao-1"], produtos: ["contas"]).Erros);
        var semSelecao = Avaliar([.. Contas, .. Cartoes], produtos: ["contas"]);
        Assert.Empty(semSelecao.Erros);
        Assert.DoesNotContain("cartao-1", semSelecao.Recursos);
    }

    [Fact]
    public void Permissao_fora_da_tabela_e_pedido_malformado()
    {
        Assert.True(Painel.TemPermissaoDesconhecida(new PedidoDoPainel("doc", Banco, "org", ["ACCOUNTS_READ", "NAO_EXISTE"])));
        Assert.False(Painel.TemPermissaoDesconhecida(new PedidoDoPainel("doc", Banco, "org", Contas)));
        Assert.False(Painel.TemPermissaoDesconhecida(new PedidoDoPainel("doc", Banco, "org", null)));
    }

    [Fact]
    public void Visao_de_consentimento_rejeitado_nao_mostra_aprovacao_pendente()
    {
        Assert.Equal((SituacaoDaAlcada.Pendente, 2), Alcadas.NaVisao(Consentimento(2), Agora));
        Assert.Equal((SituacaoDaAlcada.Encerrada, 0), Alcadas.NaVisao(Consentimento(2, estado: Ciclo.Rejeitado), Agora));
        Assert.Equal((SituacaoDaAlcada.Encerrada, 0), Alcadas.NaVisao(Consentimento(2, estado: Ciclo.Rejeitado), Agora.AddDays(16)));
        Assert.Equal((SituacaoDaAlcada.Recusada, 0), Alcadas.NaVisao(Consentimento(1, recusada: true, estado: Ciclo.Rejeitado), Agora));
        Assert.Equal((SituacaoDaAlcada.Aprovada, 0), Alcadas.NaVisao(Consentimento(decisoes: [new("b", Alcadas.Aprovada, Agora)], estado: Ciclo.Rejeitado), Agora));
        Assert.Equal("UNAVAILABLE", Alcadas.EstadoNoConsentimento(Consentimento(2, estado: Ciclo.Rejeitado), "AVAILABLE", Agora));
        Assert.Equal("UNAVAILABLE", Alcadas.EstadoNoConsentimento(Consentimento(2, estado: Ciclo.Rejeitado), "PENDING_AUTHORISATION", Agora));
        Assert.Equal("AVAILABLE", Alcadas.EstadoNoConsentimento(Consentimento(estado: Ciclo.Rejeitado), "AVAILABLE", Agora));
        Assert.Equal((SituacaoDaAlcada.SemAlcada, 0), Alcadas.NaVisao(Consentimento(estado: Ciclo.Rejeitado), Agora));
    }

    [Fact]
    public void Situacao_da_alcada()
    {
        Assert.Equal(SituacaoDaAlcada.SemAlcada, Alcadas.Situacao(Consentimento(), Agora));
        Assert.Equal(SituacaoDaAlcada.Pendente, Alcadas.Situacao(Consentimento(2), Agora.AddDays(15).AddTicks(-1)));
        Assert.Equal(SituacaoDaAlcada.Vencida, Alcadas.Situacao(Consentimento(2), Agora.AddDays(15)));
        Assert.Equal(SituacaoDaAlcada.Pendente, Alcadas.Situacao(Consentimento(2), Agora.AddDays(-1)));
        Assert.Equal(SituacaoDaAlcada.Recusada, Alcadas.Situacao(Consentimento(1, recusada: true), Agora));
        Assert.Equal(SituacaoDaAlcada.Aprovada, Alcadas.Situacao(Consentimento(decisoes: [new("b", Alcadas.Aprovada, Agora)]), Agora));
        Assert.Equal(SituacaoDaAlcada.SemAlcada, Alcadas.Situacao(Consentimento(decisoes: []), Agora));
    }

    [Theory]
    [InlineData(0, "sem-alcada")]
    [InlineData(1, "pendente")]
    [InlineData(2, "vencida")]
    [InlineData(3, "recusada")]
    [InlineData(4, "aprovada")]
    [InlineData(5, "encerrada")]
    public void Nome_da_situacao(int situacao, string nome) => Assert.Equal(nome, Alcadas.Nome((SituacaoDaAlcada)situacao));

    [Fact]
    public void Fim_do_prazo_so_com_alcada()
    {
        Assert.Null(Alcadas.FimDoPrazo(Consentimento()));
        Assert.Null(Alcadas.FimDoPrazo(Consentimento(decisoes: [])));
        Assert.Equal(Agora.AddDays(15), Alcadas.FimDoPrazo(Consentimento(1)));
        Assert.Equal(Agora.AddDays(15), Alcadas.FimDoPrazo(Consentimento(recusada: true)));
        Assert.Equal(Agora.AddDays(15), Alcadas.FimDoPrazo(Consentimento(decisoes: [new("b", Alcadas.Aprovada, Agora)])));
    }

    [Theory]
    [InlineData(0, false, 0, "TEMPORARILY_UNAVAILABLE", "TEMPORARILY_UNAVAILABLE")]
    [InlineData(2, false, 0, "AVAILABLE", "PENDING_AUTHORISATION")]
    [InlineData(2, false, 0, "TEMPORARILY_UNAVAILABLE", "PENDING_AUTHORISATION")]
    [InlineData(2, false, 0, "UNAVAILABLE", "UNAVAILABLE")]
    [InlineData(2, false, 16, "AVAILABLE", "UNAVAILABLE")]
    [InlineData(2, true, 0, "AVAILABLE", "UNAVAILABLE")]
    public void Estado_do_recurso_no_consentimento(int pendentes, bool recusada, int dias, string gravado, string esperado) =>
        Assert.Equal(esperado, Alcadas.EstadoNoConsentimento(Consentimento(pendentes, recusada), gravado, Agora.AddDays(dias)));

    [Fact]
    public void Funcao_publica_usa_a_mesma_regra() =>
        Assert.Equal("PENDING_AUTHORISATION", ExtensoesDosConsentimentos.EstadoNoConsentimento(Consentimento(1), "AVAILABLE", Agora));

    [Fact]
    public void Erro_da_decisao()
    {
        Assert.Null(Alcadas.ErroDaDecisao(Consentimento(1), Agora));
        Assert.Equal("consentimento rejeitado", Alcadas.ErroDaDecisao(Consentimento(1, estado: Ciclo.Rejeitado), Agora));
        Assert.Equal("alçada recusada", Alcadas.ErroDaDecisao(Consentimento(1, recusada: true), Agora));
        Assert.Equal("prazo da alçada encerrado", Alcadas.ErroDaDecisao(Consentimento(1), Agora.AddDays(16)));
        Assert.Equal("sem aprovação pendente", Alcadas.ErroDaDecisao(Consentimento(), Agora));
    }

    [Fact]
    public void Aprovar_desconta_e_recusar_encerra()
    {
        var aprovado = Alcadas.Aprovar(Alcadas.Aprovar(Consentimento(2), "bruno", Agora), "bruno", Agora);
        Assert.Equal(0, aprovado.AprovacoesPendentes);
        Assert.Equal(2, aprovado.Decisoes!.Count);
        Assert.Equal(SituacaoDaAlcada.Aprovada, Alcadas.Situacao(aprovado, Agora));

        var recusado = Alcadas.Recusar(Consentimento(1), "bruno", Agora);
        Assert.True(recusado.AlcadaRecusada);
        Assert.Equal(new DadosDaDecisao("bruno", Alcadas.Recusada, Agora), Assert.Single(recusado.Decisoes!));
        Assert.Equal(3, Alcadas.Recusar(aprovado, "ana", Agora).Decisoes!.Count);
    }
}
