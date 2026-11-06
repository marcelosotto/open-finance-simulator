using Simulador.Catalogo.Contratos;

namespace Simulador.Catalogo.Dominio;

/// <summary>Compara as operações de dois commits pela chave contrato, método e caminho.</summary>
internal static class ComparadorDeVersoes
{
    public static DiferencaEntreVersoes Comparar(
        string commitAnterior,
        IReadOnlyList<OperacaoDoCatalogo> anteriores,
        string commitNovo,
        IReadOnlyList<OperacaoDoCatalogo> novas)
    {
        var porChaveAnterior = anteriores.ToDictionary(o => o.Chave);
        var porChaveNova = novas.ToDictionary(o => o.Chave);

        var alteradas = new List<OperacaoAlterada>();
        foreach (var nova in novas)
        {
            var mudancas = porChaveAnterior.TryGetValue(nova.Chave, out var anterior) ? Mudancas(anterior, nova) : [];
            if (mudancas.Count > 0)
            {
                alteradas.Add(new OperacaoAlterada(nova, mudancas));
            }
        }

        return new DiferencaEntreVersoes(
            commitAnterior,
            commitNovo,
            [.. novas.Where(o => !porChaveAnterior.ContainsKey(o.Chave))],
            alteradas,
            [.. anteriores.Where(o => !porChaveNova.ContainsKey(o.Chave))]);
    }

    private static List<string> Mudancas(OperacaoDoCatalogo anterior, OperacaoDoCatalogo nova)
    {
        var mudancas = new List<string>();
        if (anterior.VersaoDoContrato != nova.VersaoDoContrato)
        {
            mudancas.Add($"versão do contrato: {anterior.VersaoDoContrato} → {nova.VersaoDoContrato}");
        }

        if (!anterior.Parametros.SequenceEqual(nova.Parametros))
        {
            mudancas.Add("parâmetros mudaram");
        }

        if (!anterior.CabecalhosExigidos.SequenceEqual(nova.CabecalhosExigidos, StringComparer.Ordinal))
        {
            mudancas.Add("cabeçalhos exigidos mudaram");
        }

        foreach (var (codigo, schema) in nova.Respostas.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            if (!anterior.Respostas.TryGetValue(codigo, out var schemaAnterior))
            {
                mudancas.Add($"resposta {codigo} nova");
            }
            else if (!string.Equals(schemaAnterior, schema, StringComparison.Ordinal))
            {
                mudancas.Add($"schema da resposta {codigo} mudou");
            }
        }

        mudancas.AddRange(anterior.Respostas.Keys
            .Where(codigo => !nova.Respostas.ContainsKey(codigo))
            .Order(StringComparer.Ordinal)
            .Select(codigo => $"resposta {codigo} removida"));
        return mudancas;
    }
}
