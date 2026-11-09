using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Simulador.Ferramentas.Comandos;

/// <summary>
/// <c>cobertura conferir</c>: soma relatórios Cobertura e exige 100% (linhas e ramos) no domínio
/// e o mínimo de linhas no total (RNF08).
/// </summary>
internal static partial class ConferirCobertura
{
    public static async Task<int> Executar(Opcoes opcoes, TextWriter saida)
    {
        var relatorios = Arquivos(opcoes.Valor("relatorios", "tests/**/coverage.cobertura.xml")).ToList();
        if (relatorios.Count == 0)
        {
            await saida.WriteLineAsync("FALHA Nenhum relatório de cobertura encontrado.").ConfigureAwait(false);
            return 1;
        }

        var minimoDoDominio = double.Parse(opcoes.Valor("dominio", "100"), CultureInfo.InvariantCulture);
        var minimoTotal = double.Parse(opcoes.Valor("total", "80"), CultureInfo.InvariantCulture);
        var filtro = Glob(opcoes.Valor("filtro-dominio", "*.Dominio.*"));

        var linhas = new Dictionary<(string Arquivo, int Numero), Linha>();
        foreach (var relatorio in relatorios)
        {
            var xml = XDocument.Load(relatorio);
            var raiz = xml.Descendants("source").Select(s => s.Value).FirstOrDefault() ?? string.Empty;
            foreach (var classe in xml.Descendants("class"))
            {
                var nome = (string)classe.Attribute("name")!;
                var arquivo = Path.GetFullPath(Path.Combine(raiz, (string)classe.Attribute("filename")!)).Replace('\\', '/');
                foreach (var linha in classe.Descendants("line"))
                {
                    var chave = (arquivo, (int)linha.Attribute("number")!);
                    var nova = Linha.Ler(nome, linha);
                    linhas[chave] = linhas.TryGetValue(chave, out var atual) ? atual.Somar(nova) : nova;
                }
            }
        }

        var dominio = linhas.Where(l => filtro.IsMatch(l.Value.Classe)).ToList();
        var linhasDoDominio = Percentual(dominio.Count(l => l.Value.Hits > 0), dominio.Count);
        var ramosDoDominio = Percentual(dominio.Sum(l => l.Value.RamosCobertos), dominio.Sum(l => l.Value.Ramos));
        var total = Percentual(linhas.Values.Count(l => l.Hits > 0), linhas.Count);

        await saida.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"Domínio: {linhasDoDominio:F1}% linhas, {ramosDoDominio:F1}% ramos. Total: {total:F1}% linhas.")).ConfigureAwait(false);

        var ok = true;
        if (linhasDoDominio < minimoDoDominio || ramosDoDominio < minimoDoDominio)
        {
            ok = false;
            foreach (var ((arquivo, numero), linha) in dominio.Where(l => l.Value.Hits == 0 || l.Value.RamosCobertos < l.Value.Ramos)
                .OrderBy(l => l.Key.Arquivo, StringComparer.Ordinal).ThenBy(l => l.Key.Numero))
            {
                await saida.WriteLineAsync($"FALHA domínio sem cobertura: {linha.Classe} ({Path.GetFileName(arquivo)}:{numero})").ConfigureAwait(false);
            }
        }

        if (total < minimoTotal)
        {
            ok = false;
            await saida.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"FALHA total {total:F1}% abaixo de {minimoTotal}%.")).ConfigureAwait(false);
        }

        return ok ? 0 : 1;
    }

    private static double Percentual(int parte, int todo) => todo == 0 ? 100 : Math.Floor(1000.0 * parte / todo) / 10;

    /// <summary>Expande um glob simples: a parte antes do primeiro curinga é a pasta; o último segmento é o nome.</summary>
    private static IEnumerable<string> Arquivos(string glob)
    {
        var normalizado = glob.Replace('\\', '/');
        var curinga = normalizado.IndexOfAny(['*', '?']);
        if (curinga < 0)
        {
            return File.Exists(glob) ? [glob] : [];
        }

        var pasta = normalizado[..(normalizado.LastIndexOf('/', curinga) + 1)];
        var nome = normalizado[(normalizado.LastIndexOf('/') + 1)..];
        pasta = pasta.Length == 0 ? "." : pasta;
        return Directory.Exists(pasta) ? Directory.EnumerateFiles(pasta, nome, SearchOption.AllDirectories) : [];
    }

    private static Regex Glob(string padrao) =>
        new("^" + Regex.Escape(padrao).Replace("\\*", ".*", StringComparison.Ordinal) + "$", RegexOptions.None, TimeSpan.FromSeconds(1));

    private sealed record Linha(string Classe, int Hits, int RamosCobertos, int Ramos)
    {
        public static Linha Ler(string classe, XElement linha)
        {
            var hits = (int)linha.Attribute("hits")!;
            var condicao = (string?)linha.Attribute("condition-coverage");
            if (condicao is null || PadraoDeRamos().Match(condicao) is not { Success: true } ramos)
            {
                return new Linha(classe, hits, 0, 0);
            }

            return new Linha(classe, hits, int.Parse(ramos.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(ramos.Groups[2].Value, CultureInfo.InvariantCulture));
        }

        public Linha Somar(Linha outra) =>
            this with { Hits = Math.Max(Hits, outra.Hits), RamosCobertos = Math.Max(RamosCobertos, outra.RamosCobertos), Ramos = Math.Max(Ramos, outra.Ramos) };
    }

    [GeneratedRegex(@"\((\d+)/(\d+)\)")]
    private static partial Regex PadraoDeRamos();
}
