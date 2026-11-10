using System.Globalization;
using System.Text.Json.Nodes;
using Simulador.Cenarios.Contratos;
using Simulador.Comum.Comportamento;

namespace Simulador.Cenarios.Dominio;

/// <summary>Catálogo de defeitos de dado (RN110, RN115).</summary>
public static class Defeitos
{
    public const string CampoObrigatorioAusente = "campo-obrigatorio-ausente";
    public const string FormatoInvalido = "formato-invalido";
    public const string EnumInvalido = "enum-invalido";
    public const string ValorIncoerente = "valor-incoerente";
    public const string TransactionIdRepetido = "transaction-id-repetido";
    public const string TotalRecordsIncoerente = "total-records-incoerente";
    public const string DataForaDaJanela = "data-fora-da-janela";
    public const string DadoMudaNaPaginacao = "dado-muda-na-paginacao";

    /// <summary>Teto, em módulo, da <c>quantidade</c> de <c>dado-muda-na-paginacao</c>: o maior page-size (RN117).</summary>
    public const int QuantidadeMaxima = 1000;

    public static IReadOnlyList<ItemDoCatalogo> Catalogo { get; } =
    [
        new(CampoObrigatorioAusente, "Tira um campo obrigatório do primeiro item; sem campo, o primeiro obrigatório presente, começando pelo terminado em Amount e depois do último para o primeiro."),
        new(FormatoInvalido, "Reescreve em dd/MM/aaaa um campo de data em texto do primeiro item; sem campo, o primeiro presente."),
        new(EnumInvalido, "Troca um enum do primeiro item por VALOR_FORA_DA_LISTA; sem campo, type ou o primeiro enum presente."),
        new(ValorIncoerente, "Troca a currency do primeiro objeto de valor do primeiro item por USD."),
        new(TransactionIdRepetido, "Copia o transactionId do primeiro item no segundo."),
        new(TotalRecordsIncoerente, "Soma 1 a meta.totalRecords."),
        new(DataForaDaJanela, "Leva as datas da primeira transação para dois anos antes do instante da chamada."),
        new(DadoMudaNaPaginacao, "Na página seguinte, acrescenta (quantidade positiva) ou tira (negativa, de -1000 a 1000) itens e ajusta totalRecords e totalPages."),
    ];

    /// <summary>Os defeitos que aceitam <c>campo</c> (RN117).</summary>
    public static IReadOnlySet<string> DeCampo { get; } = new HashSet<string>(StringComparer.Ordinal) { CampoObrigatorioAusente, FormatoInvalido, EnumInvalido };

    public static bool Existe(string? nome) => nome is not null && Catalogo.Any(d => d.Nome == nome);

    /// <summary>De -1000 a 1000, sem o zero (RN117).</summary>
    public static bool QuantidadeValida(int quantidade) => quantidade is not 0 and >= -QuantidadeMaxima and <= QuantidadeMaxima;
}

/// <summary>Catálogo de anomalias de protocolo (RN113).</summary>
public static class CatalogoDeAnomalias
{
    public static IReadOnlyList<ItemDoCatalogo> Itens { get; } =
    [
        new(Anomalias.RevogadoCom403, "Troca todo 401 do portão de segurança por 403 FORBIDDEN, inclusive o de consentimento revogado; o 401 de POST /token não muda."),
        new(Anomalias.ChaveRecusadaCom422, "Responde 422 PARAMETRO_INVALIDO à pagination-key inválida ou expirada, em vez de gerar nova chave."),
        new(Anomalias.InteracaoDiferente, "Devolve outro x-fapi-interaction-id, diferente do recebido."),
    ];

    public static bool Existe(string nome) => Itens.Any(a => a.Nome == nome);
}

/// <summary>Aplica um defeito ao corpo já montado de uma resposta 2XX (RN111, RN115, RN117).</summary>
public static class AplicacaoDeDefeito
{
    private const string Dados = "data";
    private const string Propriedades = "properties";
    private const string Meta = "meta";
    private const string TotalDeRegistros = "totalRecords";
    private const string TotalDePaginas = "totalPages";
    private const string IdDaTransacao = "transactionId";
    private const string Formato = "format";
    private const string Data = "date";
    private const string DataHora = "date-time";
    private const string Lista = "enum";
    private const string Tipo = "type";
    private const string Moeda = "currency";
    private const string MoedaIncoerente = "USD";
    private const string ForaDaLista = "VALOR_FORA_DA_LISTA";
    private const string Sufixo = "Id";
    private const string SufixoDeValor = "Amount";
    private const string TamanhoDaPagina = "page-size=";
    private const int TamanhoMaximoDoId = 100;
    private const int AnosForaDaJanela = 2;

    /// <summary>Verdadeiro quando o corpo mudou; falso deixa a resposta normal.</summary>
    public static bool Aplicar(JsonObject corpo, JsonObject schemaDaResposta, DefeitoDaChamada defeito, DateTimeOffset instante, bool paginaSeguinte)
    {
        ArgumentNullException.ThrowIfNull(corpo);
        ArgumentNullException.ThrowIfNull(schemaDaResposta);
        ArgumentNullException.ThrowIfNull(defeito);
        if (corpo[Dados] is not { } dados || schemaDaResposta[Propriedades]?[Dados] is not JsonObject esquemaDeDados)
        {
            return false;
        }

        var lista = dados as JsonArray;
        var esquema = lista is null ? esquemaDeDados : esquemaDeDados["items"] as JsonObject;
        var primeiro = lista is null ? dados as JsonObject : lista.FirstOrDefault() as JsonObject;
        return defeito.Nome switch
        {
            Defeitos.CampoObrigatorioAusente => primeiro is not null && TirarObrigatorio(primeiro, esquema, defeito.Campo),
            Defeitos.FormatoInvalido => primeiro is not null && QuebrarFormato(primeiro, esquema, defeito.Campo, instante),
            Defeitos.EnumInvalido => primeiro is not null && QuebrarEnum(primeiro, esquema, defeito.Campo),
            Defeitos.ValorIncoerente => primeiro is not null && TrocarMoeda(primeiro),
            Defeitos.TransactionIdRepetido => lista is not null && RepetirId(lista),
            Defeitos.TotalRecordsIncoerente => SomarAoTotal(corpo, 1, null),
            Defeitos.DataForaDaJanela => TirarDaJanela(lista ?? Enumerable.Repeat<JsonNode?>(dados, 1), esquema, instante),
            Defeitos.DadoMudaNaPaginacao => paginaSeguinte && lista is not null && Mudar(corpo, lista, esquema, Math.Clamp(defeito.Quantidade ?? 1, -Defeitos.QuantidadeMaxima, Defeitos.QuantidadeMaxima)),
            _ => false,
        };
    }

    private static IEnumerable<KeyValuePair<string, JsonNode?>> CamposDo(JsonObject? esquema) =>
        esquema?[Propriedades] as JsonObject ?? [];

    private static List<string> Obrigatorios(JsonObject? esquema) =>
        [.. (esquema?["required"] as JsonArray ?? []).Select(n => n?.GetValue<string>()).OfType<string>()];

    private static string? Texto(JsonNode? no) => no is JsonValue valor && valor.TryGetValue<string>(out var texto) ? texto : null;

    /// <summary>
    /// <c>date</c> ou <c>date-time</c> pelo <c>format</c> do contrato, ou pelo nome, porque muitos campos de data
    /// só têm <c>pattern</c> (como <c>transactionDateTime</c> em Contas).
    /// </summary>
    private static string? FormatoDeData(string nome, JsonNode? definicao) => Texto(definicao?[Formato]) switch
    {
        Data => Data,
        DataHora => DataHora,
        _ when nome.EndsWith("DateTime", StringComparison.Ordinal) => DataHora,
        _ when nome.EndsWith("Date", StringComparison.Ordinal) => Data,
        _ => null,
    };

    /// <summary>O campo informado, ou o primeiro candidato presente no item, para o defeito não falhar em silêncio (RN117).</summary>
    private static string? Alvo(JsonObject item, string? campo, IEnumerable<string> candidatos, Func<string, bool> elegivel) =>
        campo is not null
            ? (item.ContainsKey(campo) && elegivel(campo) ? campo : null)
            : candidatos.FirstOrDefault(n => item.ContainsKey(n) && elegivel(n));

    private static bool TirarObrigatorio(JsonObject item, JsonObject? esquema, string? campo)
    {
        var obrigatorios = Obrigatorios(esquema);
        var deValor = obrigatorios.Where(n => n.EndsWith(SufixoDeValor, StringComparison.Ordinal)).Take(1);
        var alvo = Alvo(item, campo, deValor.Concat(Enumerable.Reverse(obrigatorios)), static _ => true);
        return alvo is not null && item.Remove(alvo);
    }

    /// <summary>Só reescreve texto: um objeto, lista ou número informado em <c>campo</c> deixa a resposta normal (RN117).</summary>
    private static bool QuebrarFormato(JsonObject item, JsonObject? esquema, string? campo, DateTimeOffset instante)
    {
        var datas = CamposDo(esquema).Where(c => FormatoDeData(c.Key, c.Value) is not null).Select(c => c.Key);
        var alvo = Alvo(item, campo, datas, n => Texto(item[n]) is not null);
        if (alvo is null)
        {
            return false;
        }

        var valor = Texto(item[alvo])!;
        var dia = valor.Length >= 10 && DateOnly.TryParseExact(valor[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lido)
            ? lido
            : DateOnly.FromDateTime(instante.UtcDateTime);
        item[alvo] = dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool QuebrarEnum(JsonObject item, JsonObject? esquema, string? campo)
    {
        var enums = CamposDo(esquema).Where(c => c.Value?[Lista] is not null).Select(c => c.Key).ToList();
        var alvo = Alvo(item, campo, enums.Where(n => n == Tipo).Concat(enums), static _ => true);
        if (alvo is null)
        {
            return false;
        }

        item[alvo] = ForaDaLista;
        return true;
    }

    private static bool TrocarMoeda(JsonObject item)
    {
        var valor = item.Select(c => c.Value).OfType<JsonObject>().FirstOrDefault(o => o.ContainsKey("amount") && o.ContainsKey(Moeda));
        if (valor is null)
        {
            return false;
        }

        valor[Moeda] = MoedaIncoerente;
        return true;
    }

    private static bool RepetirId(JsonArray lista)
    {
        if (lista.Count < 2 || lista[0] is not JsonObject primeiro || lista[1] is not JsonObject segundo
            || primeiro[IdDaTransacao] is not { } id || !segundo.ContainsKey(IdDaTransacao))
        {
            return false;
        }

        segundo[IdDaTransacao] = id.DeepClone();
        return true;
    }

    private static bool TirarDaJanela(IEnumerable<JsonNode?> itens, JsonObject? esquema, DateTimeOffset instante)
    {
        if (itens.OfType<JsonObject>().FirstOrDefault(i => i.ContainsKey(IdDaTransacao)) is not { } transacao)
        {
            return false;
        }

        var dia = instante.UtcDateTime.Date.AddYears(-AnosForaDaJanela);
        var mudou = false;
        foreach (var (nome, definicao) in CamposDo(esquema))
        {
            var formato = FormatoDeData(nome, definicao);
            if (formato is not null && transacao.ContainsKey(nome))
            {
                transacao[nome] = formato == Data
                    ? dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    : dia.ToString("yyyy-MM-dd'T'HH:mm:ss'.000Z'", CultureInfo.InvariantCulture);
                mudou = true;
            }
        }

        return mudou;
    }

    private static bool Mudar(JsonObject corpo, JsonArray lista, JsonObject? esquema, int quantidade)
    {
        if (lista.Count == 0 || quantidade == 0)
        {
            return false;
        }

        if (quantidade < 0)
        {
            var tirar = Math.Min(-quantidade, lista.Count);
            for (var i = 0; i < tirar; i++)
            {
                lista.RemoveAt(0);
            }

            SomarAoTotal(corpo, -tirar, TamanhoDe(corpo));
            return true;
        }

        if (lista[0] is not JsonObject modelo || Identificador(modelo, esquema) is not { } identificador)
        {
            return false;
        }

        for (var n = 1; n <= quantidade; n++)
        {
            var copia = modelo.DeepClone().AsObject();
            copia[identificador.Campo] = ComSufixo(identificador.Id, n);
            lista.Insert(n - 1, copia);
        }

        SomarAoTotal(corpo, quantidade, TamanhoDe(corpo));
        return true;
    }

    private static (string Campo, string Id)? Identificador(JsonObject item, JsonObject? esquema) =>
        Obrigatorios(esquema).Concat(item.Select(c => c.Key))
            .Where(n => n.EndsWith(Sufixo, StringComparison.Ordinal))
            .Select(n => (Campo: n, Id: Texto(item[n])))
            .Where(p => p.Id is not null)
            .Select(p => ((string Campo, string Id)?)(p.Campo, p.Id!))
            .FirstOrDefault();

    private static string ComSufixo(string id, int n)
    {
        var sufixo = string.Create(CultureInfo.InvariantCulture, $"-N{n}");
        return (id.Length + sufixo.Length > TamanhoMaximoDoId ? id[..(TamanhoMaximoDoId - sufixo.Length)] : id) + sufixo;
    }

    private static int? TamanhoDe(JsonObject corpo)
    {
        var self = Texto(corpo["links"]?["self"]);
        var inicio = self?.IndexOf(TamanhoDaPagina, StringComparison.Ordinal) ?? -1;
        if (inicio < 0)
        {
            return null;
        }

        var resto = self![(inicio + TamanhoDaPagina.Length)..];
        var fim = resto.IndexOf('&', StringComparison.Ordinal);
        return int.TryParse(fim < 0 ? resto : resto[..fim], NumberStyles.None, CultureInfo.InvariantCulture, out var tamanho) && tamanho > 0 ? tamanho : null;
    }

    private static bool SomarAoTotal(JsonObject corpo, int diferenca, int? tamanho)
    {
        if (corpo[Meta] is not JsonObject meta || meta[TotalDeRegistros] is not JsonValue valor || !valor.TryGetValue<int>(out var total))
        {
            return false;
        }

        var novo = Math.Max(0, total + diferenca);
        meta[TotalDeRegistros] = novo;
        if (tamanho is { } porPagina && meta.ContainsKey(TotalDePaginas))
        {
            meta[TotalDePaginas] = (novo + porPagina - 1) / porPagina;
        }

        return true;
    }
}
