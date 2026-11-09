using System.Text;

namespace Simulador.Catalogo.Contratos;

/// <summary>Operação presente nas duas versões, com o que mudou.</summary>
public sealed record OperacaoAlterada(OperacaoDoCatalogo Operacao, IReadOnlyList<string> Mudancas);

/// <summary>Operações novas, alteradas e removidas entre dois commits (UC24, FR-009).</summary>
public sealed record DiferencaEntreVersoes(
    string CommitAnterior,
    string CommitNovo,
    IReadOnlyList<OperacaoDoCatalogo> Novas,
    IReadOnlyList<OperacaoAlterada> Alteradas,
    IReadOnlyList<OperacaoDoCatalogo> Removidas)
{
    public bool TemMudancas => Novas.Count + Alteradas.Count + Removidas.Count > 0;

    /// <summary>Resumo em Markdown (contracts/ferramenta-specs.md).</summary>
    public string ParaMarkdown()
    {
        var texto = new StringBuilder();
        texto.Append("## Specs: ").Append(CommitAnterior[..7]).Append(" → ").AppendLine(CommitNovo[..7]).AppendLine();
        if (!TemMudancas)
        {
            texto.AppendLine("Nenhuma operação nova, alterada ou removida.").AppendLine();
        }

        Secao(texto, "Novas", Novas.Select(Descrever));
        Secao(texto, "Alteradas", Alteradas.Select(a => $"{Endereco(a.Operacao)}: {string.Join("; ", a.Mudancas)}"));
        Secao(texto, "Removidas", Removidas.Select(Descrever));
        return texto.ToString();
    }

    private static void Secao(StringBuilder texto, string titulo, IEnumerable<string> itens)
    {
        var lista = itens.ToList();
        texto.Append("### ").Append(titulo).Append(" (").Append(lista.Count).AppendLine(")");
        foreach (var item in lista)
        {
            texto.Append("- ").AppendLine(item);
        }

        texto.AppendLine();
    }

    private static string Endereco(OperacaoDoCatalogo operacao) => $"{operacao.Metodo} {operacao.CaminhoBase}{operacao.Caminho}";

    private static string Descrever(OperacaoDoCatalogo operacao) => $"{Endereco(operacao)} ({operacao.Contrato} {operacao.VersaoDoContrato})";
}
