using Simulador.Consentimentos.Contratos;

namespace Simulador.Consentimentos.Dominio;

/// <summary>Pedido de criação pelo painel (contracts/admin-painel.md).</summary>
internal sealed record PedidoDoPainel(
    string? Cliente,
    string? Banco,
    string? Receptora,
    IReadOnlyList<string>? Permissoes,
    IReadOnlyList<string>? Contas = null,
    IReadOnlyList<string>? Cartoes = null,
    int? PrazoMeses = null,
    int? AprovacoesPendentes = null);

/// <summary>Resultado da avaliação: erros ou o que o consentimento leva.</summary>
internal sealed record AvaliacaoDoPedido(IReadOnlyList<string> Erros, IReadOnlyList<string> Permissoes, IReadOnlyList<string> Recursos);

/// <summary>Um grupo de permissões no menu.</summary>
internal sealed record GrupoDoMenu(string Nome, IReadOnlyList<string> Permissoes);

/// <summary>O que o menu mostra para um cliente em um banco (RN95, RN96).</summary>
internal sealed record OpcoesDoMenu(
    bool SemProduto,
    IReadOnlyList<GrupoDoMenu> Grupos,
    IReadOnlyList<string> Contas,
    IReadOnlyList<string> Cartoes,
    IReadOnlyList<string> Agrupados);

/// <summary>Regras do menu de criação (RN95, RN96).</summary>
internal static class Painel
{
    public const string PedidoInvalido = "PEDIDO_INVALIDO";
    public const string SemOrigem = "SEM_ORIGEM_SELECIONADA";
    public const string OrigemInvalida = "ORIGEM_INVALIDA";
    public const string SemProduto = "CLIENTE_SEM_PRODUTO";
    public const string PrazoInvalido = "PRAZO_INVALIDO";
    public const string AlcadaInvalida = "ALCADA_INVALIDA";
    public const string ReceptoraInexistente = "RECEPTORA_INEXISTENTE";

    public const int PrazoMaximoMeses = 12;

    private const string Pj = "PJ";
    private const string Conta = "ACCOUNT";
    private const string Cartao = "CREDIT_CARD_ACCOUNT";

    public static OpcoesDoMenu Opcoes(DadosDoCliente cliente, string banco, IReadOnlyList<string> produtosDoBanco)
    {
        var doBanco = cliente.Recursos.Where(r => r.Banco == banco).ToList();
        var oferecidos = doBanco.Where(r => Permissoes.Oferecido(r.Tipo, produtosDoBanco)).ToList();
        var grupos = Permissoes.GruposNomeados
            .Where(g => g.Pessoa is null || g.Pessoa == cliente.Tipo)
            .Where(g => Permissoes.Aplicaveis(g.Permissoes, produtosDoBanco).Count == g.Permissoes.Length)
            .Select(g => new GrupoDoMenu(g.Nome, g.Permissoes))
            .ToList();

        // Só contas e cartões dependem dos produtos do banco; os agrupados ficam, como nos grupos (RN08, RN96).
        return new OpcoesDoMenu(
            doBanco.Count == 0,
            grupos,
            [.. oferecidos.Where(r => r.Tipo == Conta).Select(r => r.ResourceId)],
            [.. oferecidos.Where(r => r.Tipo == Cartao).Select(r => r.ResourceId)],
            [.. doBanco.Where(r => Permissoes.Agrupado(r.Tipo)).Select(r => r.Tipo).Distinct(StringComparer.Ordinal)]);
    }

    /// <summary>Valida o pedido e escolhe permissões e recursos; com erro, nada é criado (RN95, RN96, RN99).</summary>
    public static AvaliacaoDoPedido Avaliar(PedidoDoPainel pedido, DadosDoCliente cliente, string banco, IReadOnlyList<string> produtosDoBanco, bool receptoraExiste)
    {
        var erros = new List<string>();
        var doBanco = cliente.Recursos.Where(r => r.Banco == banco).ToList();
        if (doBanco.Count == 0)
        {
            erros.Add(SemProduto);
        }

        if (!receptoraExiste)
        {
            erros.Add(ReceptoraInexistente);
        }

        if (pedido.PrazoMeses is { } meses && meses is < 1 or > PrazoMaximoMeses)
        {
            erros.Add(PrazoInvalido);
        }

        if (pedido.AprovacoesPendentes is { } pendentes && (cliente.Tipo != Pj || pendentes is < 1 or > Alcadas.Maximo))
        {
            erros.Add(AlcadaInvalida);
        }

        var aplicaveis = AvaliarPermissoes(pedido.Permissoes, cliente.Tipo == Pj, produtosDoBanco, erros);
        var oferecidos = doBanco.Where(r => Permissoes.Oferecido(r.Tipo, produtosDoBanco)).ToList();
        var contas = Selecao(pedido.Contas, Conta, oferecidos, aplicaveis, erros);
        var cartoes = Selecao(pedido.Cartoes, Cartao, oferecidos, aplicaveis, erros);
        if (erros.Count > 0 || aplicaveis is null)
        {
            return new AvaliacaoDoPedido([.. erros.Distinct(StringComparer.Ordinal)], [], []);
        }

        var recursos = doBanco
            .Where(r => Permissoes.Cobre(r.Tipo, aplicaveis))
            .Where(r => Permissoes.Agrupado(r.Tipo) || contas.Contains(r.ResourceId) || cartoes.Contains(r.ResourceId))
            .Select(r => r.ResourceId)
            .ToList();
        return new AvaliacaoDoPedido([], aplicaveis, recursos);
    }

    private static IReadOnlyList<string>? AvaliarPermissoes(IReadOnlyList<string>? pedidas, bool pj, IReadOnlyList<string> produtosDoBanco, List<string> erros)
    {
        if (pedidas is null || pedidas.Any(p => !Permissoes.Conhecida(p)))
        {
            erros.Add(PedidoInvalido);
            return null;
        }

        var permissoes = pedidas.Append(Permissoes.Recursos).Distinct(StringComparer.Ordinal).ToList();
        if (Permissoes.Validar(permissoes, pj) is { } codigo)
        {
            erros.Add(codigo);
            return null;
        }

        var aplicaveis = Permissoes.Aplicaveis(permissoes, produtosDoBanco);
        if (!Permissoes.TemFuncional(aplicaveis))
        {
            erros.Add(Permissoes.SemFuncionais);
            return null;
        }

        return aplicaveis;
    }

    /// <summary>Permissão fora da tabela oficial: o pedido é malformado e responde 400, como a API oficial (RN96).</summary>
    public static bool TemPermissaoDesconhecida(PedidoDoPainel pedido) => pedido.Permissoes is { } pedidas && pedidas.Any(p => !Permissoes.Conhecida(p));

    /// <summary>Ausente: todas as oferecidas. Vazia com opção: recusa. Id fora do menu ou sem o grupo: recusa (RN95, RN96).</summary>
    private static HashSet<string> Selecao(IReadOnlyList<string>? selecao, string tipo, List<DadosDoRecurso> oferecidos, IReadOnlyList<string>? aplicaveis, List<string> erros)
    {
        var opcoes = oferecidos.Where(r => r.Tipo == tipo).Select(r => r.ResourceId).ToHashSet(StringComparer.Ordinal);
        if (selecao is null)
        {
            return opcoes;
        }

        var pedido = aplicaveis is not null && Permissoes.Cobre(tipo, aplicaveis);
        if (selecao.Any(id => !opcoes.Contains(id)) || (aplicaveis is not null && !pedido && selecao.Count > 0))
        {
            erros.Add(OrigemInvalida);
        }
        else if (pedido && selecao.Count == 0 && opcoes.Count > 0)
        {
            erros.Add(SemOrigem);
        }

        return selecao.ToHashSet(StringComparer.Ordinal);
    }
}
