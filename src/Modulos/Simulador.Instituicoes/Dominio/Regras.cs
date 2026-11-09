using System.Text;
using System.Text.Json.Nodes;
using Simulador.Instituicoes.Contratos;

namespace Simulador.Instituicoes.Dominio;

/// <summary>Gera o endereço a partir do nome (RN07).</summary>
internal static class Endereco
{
    public const int TamanhoMaximo = 64;

    /// <summary>Nulo quando o nome não gera um endereço válido.</summary>
    public static string? Gerar(string nome)
    {
        var endereco = new StringBuilder();
        foreach (var letra in nome.ToLowerInvariant().Select(SemAcento))
        {
            if (char.IsAsciiLetterOrDigit(letra))
            {
                endereco.Append(letra);
            }
            else if (endereco.Length > 0 && endereco[^1] != '-')
            {
                endereco.Append('-');
            }
        }

        var texto = endereco.ToString().TrimEnd('-');
        return texto.Length is > 0 and <= TamanhoMaximo ? texto : null;
    }

    private static char SemAcento(char letra) => letra switch
    {
        'á' or 'à' or 'â' or 'ã' or 'ä' => 'a',
        'é' or 'è' or 'ê' or 'ë' => 'e',
        'í' or 'ì' or 'î' or 'ï' => 'i',
        'ó' or 'ò' or 'ô' or 'õ' or 'ö' => 'o',
        'ú' or 'ù' or 'û' or 'ü' => 'u',
        'ç' => 'c',
        'ñ' => 'n',
        _ => letra,
    };
}

/// <summary>Aplica o prefixo do ambiente ao nome real (RN09).</summary>
internal static class NomeDoBanco
{
    public static string Aplicar(string nome, string marca, bool nomeReal) =>
        nomeReal && marca.Length > 0 ? $"{marca.ToUpperInvariant()}-{nome}" : nome;
}

/// <summary>Regras de validação de uma instituição (UC01, UC02, UC03, RN13).</summary>
internal static class Validacao
{
    public static IReadOnlyList<string> Validar(Instituicao instituicao, IReadOnlySet<int> statusDeclarados)
    {
        var erros = new List<string>();

        void Exigir(bool condicao, string mensagem)
        {
            if (!condicao)
            {
                erros.Add(mensagem);
            }
        }

        Exigir(!string.IsNullOrWhiteSpace(instituicao.Nome), "nome é obrigatório");
        Exigir(instituicao.Publico is "PF" or "PJ" or "ambos", "publico deve ser PF, PJ ou ambos");
        Exigir(
            instituicao.Produtos.All(ValoresOficiais.Produtos.Contains) && instituicao.Produtos.Distinct().Count() == instituicao.Produtos.Count,
            "produtos contém valor desconhecido ou repetido");

        var comportamento = instituicao.Comportamento;
        Exigir(
            comportamento.LatenciaPorClasse.Count == ValoresOficiais.Classes.Count && ValoresOficiais.Classes.All(c => comportamento.LatenciaPorClasse.ContainsKey(c.Classe)),
            "latenciaPorClasse deve ter alta, media-alta, media e baixa");
        Exigir(
            comportamento.LatenciaPorClasse.Values.All(l => l.TipicoMs >= 0 && l.VariacaoMs >= 0 && Porcentagem(l.CaudaLentaPct)),
            "latência deve ser positiva, e a cauda lenta, de 0 a 100");
        Exigir(Porcentagem(comportamento.TaxaDeErroPct), "taxaDeErroPct deve ser de 0 a 100");
        Exigir(Porcentagem(comportamento.RecursoIndisponivelPct), "recursoIndisponivelPct deve ser de 0 a 100");
        foreach (var status in comportamento.PesosPorStatus.Keys)
        {
            Exigir(statusDeclarados.Contains(status) && !ValoresOficiais.StatusExclusivos.Contains(status), $"status {status} não pode ser configurado");
        }

        Exigir(comportamento.PesosPorStatus.Values.All(p => p >= 0), "peso não pode ser negativo");
        Exigir(comportamento.TaxaDeErroPct == 0 || comportamento.PesosPorStatus.Values.Any(p => p > 0), "taxa de erro exige ao menos um peso positivo");
        Exigir(comportamento.Janela is null || (comportamento.Janela.DuracaoMin > 0 && comportamento.Janela.IntervaloMin > 0), "janela deve ter valores positivos");

        var limites = instituicao.Limites;
        Exigir(limites.Qca >= 0 && limites.QcaPorReceptora.Values.All(q => q >= 0), "QCA não pode ser negativo");
        int[] positivos =
        [
            limites.Tpm.Alta ?? 1, limites.Tpm.MediaAlta, limites.Tpm.Media, limites.Tpm.Baixa,
            limites.LimiteMensal.Alta, limites.LimiteMensal.SaldosELimites, limites.LimiteMensal.MediaAlta, limites.LimiteMensal.Media, limites.LimiteMensal.Baixa,
            limites.CapacidadeTps, limites.PageSizeMaximo, limites.ValidadeDaChaveMin, limites.RetryAfterSeg,
        ];
        Exigir(positivos.All(v => v > 0), "limites devem ser positivos");
        Exigir(limites.PageSizeMaximo <= 1_000, "pageSizeMaximo não pode passar de 1000");
        Exigir(limites.PreparacaoDeRecursosSeg >= 0, "preparacaoDeRecursosSeg não pode ser negativo");
        return erros;
    }

    private static bool Porcentagem(decimal valor) => valor is >= 0 and <= 100;
}

/// <summary>Comparação com o SLA oficial, por classe (RN11, UI04).</summary>
internal sealed record LinhaDeSla(string Classe, int SlaMs, int P95Ms, bool DentroDoSla);

internal static class ComparacaoDeSla
{
    public static IReadOnlyList<LinhaDeSla> Comparar(Comportamento comportamento) =>
        [.. ValoresOficiais.Classes.Select(c =>
        {
            var latencia = comportamento.LatenciaPorClasse[c.Classe];
            var p95 = latencia.TipicoMs + latencia.VariacaoMs;
            return new LinhaDeSla(c.Classe, c.SlaMs, p95, p95 <= c.SlaMs);
        })];
}

/// <summary>Valor abaixo do oficial (RN26, UI05).</summary>
internal sealed record Aviso(string Campo, int Oficial, int Configurado);

internal static class Avisos
{
    public static IReadOnlyList<Aviso> Calcular(Limites limites)
    {
        var oficiais = ValoresOficiais.Limites();
        (string Campo, int Oficial, int Configurado)[] comparacoes =
        [
            ("tpm.alta", ValoresOficiais.TpmDaAlta(limites.Qca), limites.Tpm.Alta ?? int.MaxValue),
            ("tpm.mediaAlta", oficiais.Tpm.MediaAlta, limites.Tpm.MediaAlta),
            ("tpm.media", oficiais.Tpm.Media, limites.Tpm.Media),
            ("tpm.baixa", oficiais.Tpm.Baixa, limites.Tpm.Baixa),
            ("capacidadeTps", oficiais.CapacidadeTps, limites.CapacidadeTps),
        ];
        (string Campo, int Oficial, int Configurado)[] mensais = limites.LimiteOperacionalLigado
            ?
            [
                ("limiteMensal.alta", oficiais.LimiteMensal.Alta, limites.LimiteMensal.Alta),
                ("limiteMensal.saldosELimites", oficiais.LimiteMensal.SaldosELimites, limites.LimiteMensal.SaldosELimites),
                ("limiteMensal.mediaAlta", oficiais.LimiteMensal.MediaAlta, limites.LimiteMensal.MediaAlta),
                ("limiteMensal.media", oficiais.LimiteMensal.Media, limites.LimiteMensal.Media),
                ("limiteMensal.baixa", oficiais.LimiteMensal.Baixa, limites.LimiteMensal.Baixa),
            ]
            : [];
        return [.. comparacoes.Concat(mensais).Where(c => c.Configurado < c.Oficial).Select(c => new Aviso(c.Campo, c.Oficial, c.Configurado))];
    }
}

/// <summary>JSON Merge Patch (RFC 7386).</summary>
internal static class AplicadorDeMerge
{
    public static void Aplicar(JsonObject alvo, JsonObject alteracao)
    {
        foreach (var (chave, valor) in alteracao)
        {
            if (valor is null)
            {
                alvo.Remove(chave);
            }
            else if (valor is JsonObject filho && alvo[chave] is JsonObject atual)
            {
                Aplicar(atual, filho);
            }
            else
            {
                alvo[chave] = valor.DeepClone();
            }
        }
    }
}

/// <summary>Campos diferentes entre duas versões, para o histórico (RN05).</summary>
internal static class Diferencas
{
    public static IReadOnlyList<Mudanca> Entre(JsonObject antes, JsonObject depois)
    {
        var valoresAntes = Achatar(antes);
        var valoresDepois = Achatar(depois);
        return [.. valoresAntes.Keys.Union(valoresDepois.Keys)
            .Order(StringComparer.Ordinal)
            .Select(campo => new Mudanca(campo, valoresAntes.GetValueOrDefault(campo), valoresDepois.GetValueOrDefault(campo)))
            .Where(m => m.Antes != m.Depois)];
    }

    private static Dictionary<string, string?> Achatar(JsonObject objeto)
    {
        var valores = new Dictionary<string, string?>(StringComparer.Ordinal);
        Visitar(objeto, string.Empty, valores);
        return valores;
    }

    private static void Visitar(JsonObject objeto, string prefixo, Dictionary<string, string?> valores)
    {
        foreach (var (chave, valor) in objeto)
        {
            var campo = prefixo + chave;
            if (valor is JsonObject filho)
            {
                Visitar(filho, campo + ".", valores);
            }
            else
            {
                valores[campo] = valor?.ToJsonString();
            }
        }
    }
}
