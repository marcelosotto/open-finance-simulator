using System.Text.RegularExpressions;
using Simulador.Massa.Contratos;
using Simulador.Massa.Dominio;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Massa;

internal sealed class AmbienteFalso(string marca = "hml") : IAmbienteDaMassa
{
    public string Marca => marca;

    public string Identificador(string natureza, string corpo) => $"{marca}-{natureza[0].ToString().Replace("p", "p", StringComparison.Ordinal)}-{corpo}";

    public string ConsentId(string natureza, string corpo) => $"urn:{marca}:{natureza[0]}-{corpo}";

    public string MarcarTexto(string texto) => $"[{marca.ToUpperInvariant()}] {texto}";

    public Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ProdutosDosBancos(CancellationToken cancelamento) => throw new NotSupportedException();

    public Task RegistrarHistorico(EventoDaMassa evento, CancellationToken cancelamento) => throw new NotSupportedException();

    public Task<IReadOnlyList<EventoDaMassa>> LerHistorico(CancellationToken cancelamento) => throw new NotSupportedException();

    public Task<int> ContarInstituicoesCriadas(IReadOnlySet<string> exceto, CancellationToken cancelamento) => throw new NotSupportedException();

    public Task ApagarInstituicoesCriadas(IReadOnlySet<string> exceto, string autor, CancellationToken cancelamento) => throw new NotSupportedException();
}

public class SorteioTests
{
    [Fact]
    public void Mesma_semente_mesma_sequencia()
    {
        var a = new Sorteio(42);
        var b = new Sorteio(42);

        Assert.Equal(Enumerable.Range(0, 50).Select(_ => a.Proximo()), Enumerable.Range(0, 50).Select(_ => b.Proximo()));
    }

    [Fact]
    public void Sequencia_nao_muda_entre_versoes()
    {
        Assert.Equal(13679457532755275413UL, new Sorteio(42).Proximo());
    }

    [Fact]
    public void Derivar_depende_dos_rotulos()
    {
        Assert.Equal(Sorteio.Derivar(1, "a", 2), Sorteio.Derivar(1, "a", 2));
        Assert.NotEqual(Sorteio.Derivar(1, "a", 2), Sorteio.Derivar(1, "a", 3));
    }

    [Fact]
    public void Entre_respeita_os_limites()
    {
        var sorteio = Sorteio.Para(1, "x");

        Assert.All(Enumerable.Range(0, 1_000), _ => Assert.InRange(sorteio.Entre(3, 5), 3, 5));
    }

    [Fact]
    public void Chance_nos_extremos()
    {
        var sorteio = new Sorteio(7);

        Assert.All(Enumerable.Range(0, 200), _ => Assert.False(sorteio.Chance(0)));
        Assert.All(Enumerable.Range(0, 200), _ => Assert.True(sorteio.Chance(100)));
    }
}

public class DocumentosTests
{
    private static bool CpfValido(string cpf)
    {
        int Digito(int tamanho)
        {
            var soma = cpf.Take(tamanho).Select((c, i) => (c - '0') * (tamanho + 1 - i)).Sum();
            return soma * 10 % 11 % 10;
        }

        return cpf.Length == 11 && Digito(9) == cpf[9] - '0' && Digito(10) == cpf[10] - '0';
    }

    [Fact]
    public void Cpfs_tem_digitos_validos_e_comecam_com_9()
    {
        var cpfs = Enumerable.Range(0, 5_000).SelectMany(i => new[] { Documentos.Cpf(i, true), Documentos.Cpf(i, false) }).ToList();

        Assert.All(cpfs, cpf =>
        {
            Assert.True(CpfValido(cpf), cpf);
            Assert.StartsWith("9", cpf, StringComparison.Ordinal);
        });
        Assert.Equal(cpfs.Count, cpfs.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Virtual_e_persistido_nao_colidem_nos_extremos()
    {
        Assert.NotEqual(Documentos.Cpf(Documentos.CapacidadeVirtual - 1, true), Documentos.Cpf(0, false));
        Assert.NotEqual("99999999999", Documentos.Cpf(Documentos.CapacidadePersistida - 1, false));
    }

    [Fact]
    public void Cnpj_tem_o_formato_oficial_e_digitos_validos()
    {
        var cnpjs = Enumerable.Range(0, 5_000).SelectMany(i => new[] { Documentos.Cnpj(i, true), Documentos.Cnpj(i, false) }).ToList();

        Assert.All(cnpjs, c => Assert.Matches("^[0-9A-Z]{12}[0-9]{2}$", c));
        Assert.Equal(cnpjs.Count, cnpjs.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(cnpjs, c => c.Any(char.IsLetter));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(123_456L)]
    public void Documento_virtual_devolve_a_posicao(long posicao)
    {
        Assert.Equal(posicao, Documentos.IndiceVirtual(Documentos.Cpf(posicao, true)));
        Assert.Equal(posicao, Documentos.IndiceVirtual(Documentos.Cnpj(posicao, true)));
    }

    [Fact]
    public void Outros_documentos_nao_sao_virtuais()
    {
        var cpfVirtual = Documentos.Cpf(5, true);
        var cnpjVirtual = Documentos.Cnpj(5, true);

        Assert.Null(Documentos.IndiceVirtual(Documentos.Cpf(5, false)));
        Assert.Null(Documentos.IndiceVirtual(Documentos.Cnpj(5, false)));
        Assert.Null(Documentos.IndiceVirtual(cpfVirtual[..10] + (cpfVirtual[10] == '0' ? '1' : '0')));
        Assert.Null(Documentos.IndiceVirtual("9abcdefghij"));
        Assert.Null(Documentos.IndiceVirtual(cnpjVirtual[..13] + (cnpjVirtual[13] == '0' ? '1' : '0')));
        Assert.Null(Documentos.IndiceVirtual("V-------000100"));
        Assert.Null(Documentos.IndiceVirtual("123"));
    }

    [Fact]
    public void Base36_ida_e_volta()
    {
        Assert.Equal("0", Base36.Codificar(0));
        Assert.Equal(1_234_567L, Base36.Decodificar(Base36.Codificar(1_234_567)));
        Assert.Null(Base36.Decodificar(string.Empty));
        Assert.Null(Base36.Decodificar("A"));
        Assert.Null(Base36.Decodificar(new string('z', 20)));
    }
}

public class CicloDeBancosTests
{
    private static readonly Dictionary<string, decimal> Bancos = new(StringComparer.Ordinal) { ["a"] = 25m, ["b"] = 75m, ["c"] = 0m };

    [Fact]
    public void Respeita_a_proporcao_em_um_ciclo()
    {
        var ciclo = new CicloDeBancos(1, Bancos);

        var porBanco = Enumerable.Range(0, 10_000).GroupBy(i => ciclo.BancoDe(i), StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        Assert.Equal(2_500, porBanco["a"]);
        Assert.Equal(7_500, porBanco["b"]);
        Assert.DoesNotContain("c", porBanco.Keys);
    }

    [Fact]
    public void Enesimo_do_banco_e_do_banco_mesmo_em_ciclos_distantes()
    {
        var ciclo = new CicloDeBancos(1, Bancos);

        Assert.All(new long[] { 0, 1, 2_499, 2_500, 1_000_000 }, n => Assert.Equal("a", ciclo.BancoDe(ciclo.IndiceNoBanco("a", n)!.Value)));
    }

    [Fact]
    public void Banco_fora_da_divisao_nao_tem_itens()
    {
        var ciclo = new CicloDeBancos(1, Bancos);

        Assert.Null(ciclo.IndiceNoBanco("c", 0));
        Assert.Null(ciclo.IndiceNoBanco("z", 0));
    }

    [Fact]
    public void A_semente_muda_a_ordem()
    {
        var a = new CicloDeBancos(1, Bancos);
        var b = new CicloDeBancos(2, Bancos);

        Assert.Contains(Enumerable.Range(0, 100), i => a.BancoDe(i) != b.BancoDe(i));
    }
}

public partial class CatalogosTests
{
    private static Distribuicao Valida() => new(
        new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 100m },
        new Dictionary<string, decimal>(StringComparer.Ordinal) { ["p"] = 100m });

    private static PerfilDeDados Perfil() => new("x", 0, 1, 1m, 2m, 0m, ["PIX"], ["contas"]);

    [Fact]
    public void Prontos_tem_os_volumes_de_RN55()
    {
        Assert.Equal(["cliente-comum", "sem-movimento", "alto-volume"], PerfisProntos.Todos.Select(p => p.Nome));
        Assert.True(PerfisProntos.Eh("alto-volume"));
        Assert.False(PerfisProntos.Eh("outro"));
        Assert.All(PerfisProntos.Todos, p => Assert.Empty(Validacao.Validar(p)));
    }

    [Fact]
    public void Permissoes_conferem_com_a_spec_travada()
    {
        var pasta = Directory.EnumerateDirectories(Repositorio.PastaDasSpecs(), "*", SearchOption.AllDirectories).First(d => File.Exists(Path.Combine(d, "consents-3.3.1.yml")));
        var oficiais = PermissaoOficial().Matches(File.ReadAllText(Path.Combine(pasta, "consents-3.3.1.yml"))).Select(m => m.Value).ToHashSet(StringComparer.Ordinal);

        Assert.Equal(oficiais.Order(StringComparer.Ordinal), Permissoes.Todas.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Permissoes_tiram_as_do_outro_tipo()
    {
        Assert.DoesNotContain(Permissoes.Para(TiposDePessoa.Pf, null), p => p.StartsWith("CUSTOMERS_BUSINESS", StringComparison.Ordinal));
        Assert.DoesNotContain(Permissoes.Para(TiposDePessoa.Pj, null), p => p.StartsWith("CUSTOMERS_PERSONAL", StringComparison.Ordinal));
        Assert.Equal(["ACCOUNTS_READ"], Permissoes.Para(TiposDePessoa.Pf, ["ACCOUNTS_READ", "CUSTOMERS_BUSINESS_IDENTIFICATIONS_READ"]));
    }

    [Fact]
    public void Produtos_mapeiam_tipo_e_permissao()
    {
        Assert.True(Produtos.Existe("cambio"));
        Assert.False(Produtos.Existe("outro"));
        Assert.Equal("LOAN", Produtos.TipoDe("emprestimos"));
        Assert.True(Produtos.Coberto("contas", ["ACCOUNTS_READ"]));
        Assert.False(Produtos.Coberto("contas", ["LOANS_READ"]));
    }

    [Fact]
    public void Perfil_valido_nao_tem_erros()
    {
        Assert.Empty(Validacao.Validar(Perfil()));
        Assert.Empty(Validacao.Validar(Perfil() with { TransacoesMax = 0, TiposDeLancamento = [] }));
    }

    public static TheoryData<PerfilDeDados, string> PerfisInvalidos() => new()
    {
        { Perfil() with { Nome = " " }, "nome" },
        { Perfil() with { TransacoesMin = -1 }, "transações" },
        { Perfil() with { TransacoesMin = 5 }, "transações" },
        { Perfil() with { ValorMin = -1m }, "valores" },
        { Perfil() with { ValorMin = 5m }, "valores" },
        { Perfil() with { SaldoInicial = -1m }, "saldo" },
        { Perfil() with { TiposDeLancamento = [] }, "lançamento" },
        { Perfil() with { Produtos = [] }, "produtos" },
        { Perfil() with { Produtos = ["outro"] }, "produto desconhecido" },
    };

    [Theory]
    [MemberData(nameof(PerfisInvalidos))]
    public void Perfil_invalido_e_recusado(PerfilDeDados perfil, string trecho)
    {
        Assert.Contains(Validacao.Validar(perfil), e => e.Contains(trecho, StringComparison.Ordinal));
    }

    [Fact]
    public void Distribuicao_valida_aceita_produtos_sem_soma()
    {
        Assert.Empty(Validacao.Validar(Valida() with
        {
            Produtos = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["contas"] = 90m, ["cartoes"] = 50m, ["emprestimos"] = 20m },
            PjPct = 100m,
        }));
    }

    public static TheoryData<Distribuicao, string> DistribuicoesInvalidas() => new()
    {
        { Valida() with { Bancos = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 60m, ["b"] = 30m } }, "soma de bancos" },
        { Valida() with { Perfis = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 60m } }, "soma de perfis" },
        { Valida() with { Bancos = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 120m, ["b"] = -20m } }, "fora de 0 a 100" },
        { Valida() with { Bancos = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 99.995m, ["b"] = 0.005m } }, "2 casas" },
        { Valida() with { Produtos = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["outro"] = 1m } }, "produto desconhecido" },
        { Valida() with { Produtos = new Dictionary<string, decimal>(StringComparer.Ordinal) { ["contas"] = 101m } }, "produto fora" },
        { Valida() with { PjPct = 101m }, "PJ" },
        { Valida() with { PjPct = -1m }, "PJ" },
    };

    [Theory]
    [MemberData(nameof(DistribuicoesInvalidas))]
    public void Distribuicao_invalida_e_recusada(Distribuicao distribuicao, string trecho)
    {
        Assert.Contains(Validacao.Validar(distribuicao), e => e.Contains(trecho, StringComparison.Ordinal));
    }

    [GeneratedRegex(@"\b[A-Z_]+_READ\b")]
    private static partial Regex PermissaoOficial();
}

public class GeradorTests
{
    private static readonly DateTimeOffset Data = new(2026, 11, 5, 15, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoje = new(2026, 11, 5);

    private static readonly Dictionary<string, IReadOnlyList<string>> Bancos = new(StringComparer.Ordinal)
    {
        ["a"] = ["contas", "cartoes", "emprestimos"],
        ["b"] = ["contas"],
    };

    private static readonly Dictionary<string, PerfilDeDados> Perfis = PerfisProntos.Todos.ToDictionary(p => p.Nome, StringComparer.Ordinal);

    private static Distribuicao Dist(decimal pj = 0, IReadOnlyDictionary<string, decimal>? produtos = null) => new(
        new Dictionary<string, decimal>(StringComparer.Ordinal) { ["a"] = 50m, ["b"] = 50m },
        new Dictionary<string, decimal>(StringComparer.Ordinal) { ["cliente-comum"] = 50m, ["alto-volume"] = 50m },
        produtos,
        pj);

    private static GeradorDeMassa Gerador(Distribuicao? d = null, string natureza = Naturezas.Persistido, int consentimentos = 3, IReadOnlyList<string>? permissoes = null, string marca = "hml") =>
        new(new ParametrosDeGeracao(42, natureza, d ?? Dist(), Perfis, Bancos, consentimentos, permissoes, 12, Data, "lote-0"), new AmbienteFalso(marca));

    private static string Texto(object valor) => System.Text.Json.JsonSerializer.Serialize(valor, JsonDaMassa.Opcoes);

    [Fact]
    public void Mesma_entrada_mesmo_dado()
    {
        Assert.Equal(Texto(Gerador().Gerar(7, 7)), Texto(Gerador().Gerar(7, 7)));
    }

    [Fact]
    public void Posicao_muda_so_documento_e_ids()
    {
        var a = Gerador().Gerar(7, 100);
        var b = Gerador().Gerar(7, 200);

        Assert.NotEqual(a.Cliente.Documento, b.Cliente.Documento);
        Assert.NotEqual(a.Recursos[0].ResourceId, b.Recursos[0].ResourceId);
        Assert.Equal(a.Cliente.Nome, b.Cliente.Nome);
        Assert.Equal(a.Recursos.Select(r => (r.Banco, r.Produto, r.SementeDeDados)), b.Recursos.Select(r => (r.Banco, r.Produto, r.SementeDeDados)));
    }

    [Fact]
    public void Consentimento_nasce_autorizado_sem_receptora_e_so_com_recursos_do_banco()
    {
        var massa = Gerador().Gerar(1, 1);

        Assert.All(massa.Consentimentos, c =>
        {
            Assert.Equal("AUTHORISED", c.Estado);
            Assert.Null(c.Receptora);
            Assert.All(c.Recursos, id => Assert.Equal(c.Banco, massa.Recursos.Single(r => r.ResourceId == id).Banco));
        });
    }

    [Fact]
    public void Tipo_de_pessoa_segue_a_porcentagem()
    {
        Assert.Equal(TiposDePessoa.Pj, Gerador(Dist(100)).Gerar(1, 1).Cliente.Tipo);
        Assert.Equal(14, Gerador(Dist(100)).Gerar(1, 1).Cliente.Documento.Length);
        Assert.Equal(TiposDePessoa.Pf, Gerador(Dist(0)).Gerar(1, 1).Cliente.Tipo);
        Assert.Equal(11, Gerador(Dist(0)).Gerar(1, 1).Cliente.Documento.Length);
    }

    [Fact]
    public void Produto_com_zero_por_cento_nao_e_gerado_e_omitido_vale_100()
    {
        var massa = Gerador(Dist(produtos: new Dictionary<string, decimal>(StringComparer.Ordinal) { ["cartoes"] = 0m })).Gerar(3, 3);

        Assert.DoesNotContain(massa.Recursos, r => r.Produto == "cartoes");
        Assert.Contains(massa.Recursos, r => r.Produto == "contas");
    }

    [Fact]
    public void So_gera_o_que_o_banco_oferece()
    {
        var massa = Enumerable.Range(0, 50).Select(i => Gerador().Gerar(i, i)).SelectMany(m => m.Recursos).Where(r => r.Banco == "b");

        Assert.All(massa, r => Assert.Equal("contas", r.Produto));
    }

    [Fact]
    public void Permissoes_limitam_os_recursos_do_consentimento()
    {
        var massa = Gerador(permissoes: ["ACCOUNTS_READ"]).Gerar(1, 1);

        Assert.All(massa.Consentimentos.SelectMany(c => c.Recursos), id => Assert.Equal("contas", massa.Recursos.Single(r => r.ResourceId == id).Produto));
    }

    [Fact]
    public void Avulso_tem_nome_informado_recursos_por_banco_e_nenhum_consentimento()
    {
        var massa = Gerador(consentimentos: 0).GerarAvulso(9, TiposDePessoa.Pf, "cliente-comum", "Mariana", ["a", "b"]);

        Assert.Equal("[HML] Mariana", massa.Cliente.Nome);
        Assert.Equal(["a", "b"], massa.Recursos.Select(r => r.Banco).Distinct());
        Assert.Empty(massa.Consentimentos);
    }

    [Fact]
    public void Nome_gerado_recebe_a_marca()
    {
        Assert.StartsWith("[HML] ", Gerador().Gerar(1, 1).Cliente.Nome, StringComparison.Ordinal);
        Assert.EndsWith("Ltda", Gerador(Dist(100)).Gerar(1, 1).Cliente.Nome, StringComparison.Ordinal);
    }

    [Fact]
    public void Banco_do_item_e_enesimo_conferem()
    {
        var gerador = Gerador(natureza: Naturezas.Virtual, consentimentos: 1);

        Assert.Equal("a", gerador.BancoDoItem(gerador.IndiceNoBanco("a", 10)!.Value));
    }

    private static Recurso RecursoDe(string marca = "hml") =>
        Gerador(marca: marca).Gerar(1, 1).Recursos.First(r => r.Perfil == "cliente-comum" || r.Perfil == "alto-volume") with
        {
            Marca = marca,
            InicioDoHistorico = Hoje.AddDays(-30),
            Tipo = "ACCOUNT",
        };

    private static readonly PerfilDeDados Fixo = new("f", 3, 3, 100m, 100m, 1_000m, ["PIX"], ["contas"]);

    [Fact]
    public void Transacoes_sao_repetiveis_e_respeitam_a_janela()
    {
        var recurso = RecursoDe();
        var vazio = new Dictionary<string, decimal>(StringComparer.Ordinal);

        var todas = CalculoDeTransacoes.Transacoes(recurso, Fixo, Hoje.AddDays(-90), Hoje.AddDays(90), Hoje, vazio);

        Assert.Equal((31 + 30) * 3, todas.Count);
        Assert.Equal(Hoje.AddDays(-30), todas[0].Dia);
        Assert.Equal(Hoje.AddDays(30), todas[^1].Dia);
        Assert.Equal(Texto(todas), Texto(CalculoDeTransacoes.Transacoes(recurso, Fixo, Hoje.AddDays(-90), Hoje.AddDays(90), Hoje, vazio)));
        Assert.Equal(6, CalculoDeTransacoes.Transacoes(recurso, Fixo, Hoje.AddDays(-5), Hoje.AddDays(-4), Hoje, vazio).Count);
        Assert.Contains(todas, t => t.Direcao == "CREDITO");
        Assert.Contains(todas, t => t.Direcao == "DEBITO");
        Assert.All(todas, t => Assert.StartsWith("[HML] PIX", t.Nome, StringComparison.Ordinal));
    }

    [Fact]
    public void Estado_sai_do_relogio_e_futuro_nao_entra_no_saldo()
    {
        var perfil = Fixo with { TiposDeLancamento = ["BOLETO"] };
        var recurso = RecursoDe();
        var vazio = new Dictionary<string, decimal>(StringComparer.Ordinal);

        var todas = CalculoDeTransacoes.Transacoes(recurso, perfil, Hoje.AddDays(-1), Hoje.AddDays(1), Hoje, vazio);

        Assert.All(todas.Where(t => t.Dia < Hoje), t => Assert.Equal(EstadosDeTransacao.Efetivada, t.Estado));
        Assert.All(todas.Where(t => t.Dia == Hoje), t =>
        {
            Assert.Equal(EstadosDeTransacao.Processando, t.Estado);
            Assert.Contains("-p2", t.Id, StringComparison.Ordinal);
        });
        Assert.All(todas.Where(t => t.Dia > Hoje), t => Assert.Equal(EstadosDeTransacao.Futuro, t.Estado));

        var ateOntem = CalculoDeTransacoes.Transacoes(recurso, perfil, recurso.InicioDoHistorico, Hoje.AddDays(-1), Hoje, vazio);
        var esperado = recurso.SaldoInicial + ateOntem.Sum(t => t.Direcao == "CREDITO" ? t.Valor : -t.Valor);
        Assert.Equal(esperado, CalculoDeTransacoes.Saldo(recurso, perfil, Hoje, vazio));
    }

    [Fact]
    public void So_contas_tem_futuro_e_estado()
    {
        var perfil = Fixo with { TiposDeLancamento = ["BOLETO"] };
        var recurso = RecursoDe() with { Tipo = "CREDIT_CARD_ACCOUNT" };
        var vazio = new Dictionary<string, decimal>(StringComparer.Ordinal);

        var todas = CalculoDeTransacoes.Transacoes(recurso, perfil, Hoje, Hoje.AddDays(30), Hoje, vazio);

        Assert.All(todas, t => Assert.Equal(Hoje, t.Dia));
        Assert.All(todas, t => Assert.Equal(EstadosDeTransacao.Efetivada, t.Estado));
    }

    [Fact]
    public void Depois_de_30_dias_vem_vazio()
    {
        var vazio = new Dictionary<string, decimal>(StringComparer.Ordinal);

        Assert.Empty(CalculoDeTransacoes.Transacoes(RecursoDe(), Fixo, Hoje.AddDays(31), Hoje.AddDays(60), Hoje, vazio));
    }

    [Fact]
    public void Marca_vazia_nao_concatena()
    {
        var todas = CalculoDeTransacoes.Transacoes(RecursoDe(string.Empty), Fixo, Hoje, Hoje, Hoje, new Dictionary<string, decimal>(StringComparer.Ordinal));

        Assert.All(todas, t => Assert.StartsWith("PIX", t.Nome, StringComparison.Ordinal));
    }

    [Fact]
    public void Saldo_soma_as_transacoes_e_reflete_o_ajuste()
    {
        var recurso = RecursoDe();
        var vazio = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var todas = CalculoDeTransacoes.Transacoes(recurso, Fixo, recurso.InicioDoHistorico, Hoje, Hoje, vazio);
        var esperado = recurso.SaldoInicial + todas.Sum(t => t.Direcao == "CREDITO" ? t.Valor : -t.Valor);

        Assert.Equal(esperado, CalculoDeTransacoes.Saldo(recurso, Fixo, Hoje, vazio));

        var alvo = todas[0];
        var ajustes = new Dictionary<string, decimal>(StringComparer.Ordinal) { [CalculoDeTransacoes.Chave(recurso.ResourceId, alvo.Dia, alvo.Posicao)] = 90m };
        var variacao = alvo.Direcao == "CREDITO" ? -10m : 10m;

        Assert.Equal(esperado + variacao, CalculoDeTransacoes.Saldo(recurso, Fixo, Hoje, ajustes));
        Assert.Equal(90m, CalculoDeTransacoes.Transacoes(recurso, Fixo, alvo.Dia, alvo.Dia, Hoje, ajustes).Single(t => t.Posicao == alvo.Posicao).Valor);
    }

    [Fact]
    public void Sem_movimento_nao_tem_transacao()
    {
        Assert.Empty(CalculoDeTransacoes.DoDia(RecursoDe(), Perfis["sem-movimento"], Hoje));
    }

    [Theory]
    [InlineData("hml-v-r1zx3", 71L, 3)]
    [InlineData("v-r0x0", 0L, 0)]
    public void Le_recurso_virtual(string id, long indice, int numero)
    {
        Assert.Equal(new ReferenciaVirtual(indice, numero), IdsVirtuais.Recurso(id));
    }

    [Theory]
    [InlineData("urn:hml:v-c1x0", 1L)]
    [InlineData("urn:v:c2x0", 2L)]
    public void Le_consentimento_virtual(string id, long indice)
    {
        Assert.Equal(indice, IdsVirtuais.Consentimento(id)?.Indice);
    }

    [Theory]
    [InlineData("hml-p-r1x0")]
    [InlineData("hml-v-c1x0")]
    [InlineData("hml-v-rzzzzzzzzzzzzzzzzzzzzx0")]
    [InlineData("hml-v-r1x99999999999999999999")]
    public void Outros_ids_nao_sao_recursos_virtuais(string id)
    {
        Assert.Null(IdsVirtuais.Recurso(id));
    }
}
