using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Simulador.Comum.Comportamento;
using Simulador.Comum.Erros;
using Simulador.Dados.Contratos;
using Simulador.Dados.Dominio;

namespace Simulador.Dados.Aplicacao;

/// <summary>Segredo da chave de paginação, lido na subida.</summary>
internal sealed record SegredoDaChave(byte[] Valor)
{
    public static SegredoDaChave Ler(IConfiguration configuracao) =>
        new(configuracao["Simulador:Token:Chave"] is { Length: > 0 } chave ? Encoding.UTF8.GetBytes(chave) : RandomNumberGenerator.GetBytes(32));
}

/// <summary>Responde as APIs de dados a partir do contrato e da massa (UC16, UC17).</summary>
internal sealed class ServicoDeDados(IAmbienteDosDados ambiente, TimeProvider relogio, HoraReal hora, SegredoDaChave segredo) : IDadosDasApis
{
    private const string Disponivel = "AVAILABLE";
    private const string Credito = "CREDITO";
    private const string Debito = "DEBITO";
    private const string Valor = ".amount";
    private static readonly HashSet<string> TipoDaConta = new(StringComparer.Ordinal) { "type" };
    private const int ItensDaSublista = 2;
    private const int SegundosDaPrimeiraHora = 3_599;
    private static readonly TimeOnly UltimoSegundoDaPrimeiraHora = new(0, 59, 59);
    private const decimal LimiteDoCartao = 50_000m;
    private static readonly ConcurrentDictionary<string, JsonObject> Schemas = new(StringComparer.Ordinal);

    /// <summary>Itens a responder, ou a recusa; nas listas de recursos, o conjunto que vai na chave.</summary>
    private readonly record struct Itens(IReadOnlyList<Preenchimento> Lista, IResult? Recusa = null, string? Recursos = null);

    public bool Atende(string contrato) => Familias.De(contrato) is not null;

    public async Task<IResult> Responder(HttpContext contexto, OperacaoDeDados operacao)
    {
        var familia = Familias.De(operacao.Contrato)!;
        if (ambiente.Consentimento(contexto) is not { } consentimento)
        {
            return Proibido("RESOURCE_FORBIDDEN");
        }

        var cancelamento = contexto.RequestAborted;
        var (tipo, parametro) = Classificador.De(operacao.Caminho);
        var bruto = parametro is null ? null : contexto.Request.RouteValues[parametro] as string;
        RecursoDeDados? recurso = null;
        if (parametro is not null)
        {
            var id = tipo == TipoDeOperacao.NotaDeNegociacao ? IdsDerivados.RecursoDaNota(bruto!) : bruto;
            (recurso, var recusa) = await Acessar(contexto, id, familia, consentimento, cancelamento).ConfigureAwait(false);
            if (recusa is not null)
            {
                return recusa;
            }
        }

        var banco = contexto.Request.RouteValues["banco"] as string ?? string.Empty;
        var limites = await ambiente.Limites(banco, cancelamento).ConfigureAwait(false);
        var query = contexto.Request.Query;
        var pedido = operacao.MinimoDoTamanho is { } minimo
            ? PedidoDePagina.Ler(query["page"], query["page-size"], minimo, limites.PageSizeMaximo)
            : new PedidoDePagina(1, PedidoDePagina.TamanhoPadrao);
        if (pedido is null)
        {
            return Erro422("PARAMETRO_INVALIDO");
        }

        var filtro = tipo == TipoDeOperacao.Faturas ? "DueDate" : familia.FiltroDeData;
        string? de = query[$"from{filtro}"];
        string? ate = query[$"to{filtro}"];
        var filtros = FiltrosOficiais.Campos
            .Where(f => operacao.ParametrosDeConsulta.Contains(f.Key, StringComparer.Ordinal) && !string.IsNullOrEmpty(query[f.Key]))
            .Select(f => (Campo: f.Value, Valor: query[f.Key].ToString()))
            .ToList();
        var consulta = $"{operacao.OperationId}|{consentimento.ConsentId}|{contexto.Request.Path}|{de}|{ate}|{string.Join(',', filtros)}";
        var agora = relogio.GetUtcNow();
        var emissao = operacao.TemChave
            ? ChaveDePaginacao.Abrir(query["pagination-key"], consulta, segredo.Valor, agora, limites.ValidadeDaChaveMin)
            : null;
        var instante = emissao?.Emissao ?? agora;
        var corte = emissao?.EmissaoReal ?? hora.Relogio.GetUtcNow();

        var itens = await Montar(tipo, familia, consentimento, recurso, contexto.Request.RouteValues["billId"] as string, de, ate, instante, corte, emissao?.Recursos, operacao.SchemaDaResposta, cancelamento).ConfigureAwait(false);
        if (itens.Recusa is not null)
        {
            return itens.Recusa;
        }

        var chave = operacao.TemChave ? ChaveDePaginacao.Emitir(instante, corte, consulta, segredo.Valor, itens.Recursos) : null;

        // Chave válida é página seguinte: não confere nem conta a cota (RN27, UC19).
        var cota = emissao is null ? await ambiente.Cota(contexto, operacao, consentimento, cancelamento).ConfigureAwait(false) : null;
        return await Saida(contexto, operacao, itens.Lista, pedido, chave, consentimento, recurso, cota, filtros).ConfigureAwait(false);
    }

    private async Task<(RecursoDeDados? Recurso, IResult? Recusa)> Acessar(HttpContext contexto, string? id, Familia familia, ConsentimentoDaChamada consentimento, CancellationToken cancelamento)
    {
        var recurso = id is not null && consentimento.Recursos.Contains(id)
            ? await ambiente.Recurso(id, cancelamento).ConfigureAwait(false)
            : null;
        if (recurso is null || recurso.Tipo != familia.TipoDeRecurso || recurso.Cliente != consentimento.Cliente)
        {
            return (null, Proibido("RESOURCE_FORBIDDEN"));
        }

        if (recurso.Estado != Disponivel)
        {
            return (null, Proibido($"STATUS_RESOURCE_{recurso.Estado}"));
        }

        return await EtapaDeComportamento.RecursoIndisponivel(contexto).ConfigureAwait(false)
            ? (null, Proibido("STATUS_RESOURCE_TEMPORARILY_UNAVAILABLE"))
            : (recurso, null);
    }

    private async Task<Itens> Montar(
        TipoDeOperacao tipo,
        Familia familia,
        ConsentimentoDaChamada consentimento,
        RecursoDeDados? recurso,
        string? faturaId,
        string? de,
        string? ate,
        DateTimeOffset instante,
        DateTimeOffset corte,
        string? recursosDaChave,
        string schema,
        CancellationToken cancelamento)
    {
        var hoje = DateOnly.FromDateTime(instante.UtcDateTime);
        switch (tipo)
        {
            case TipoDeOperacao.Cadastral:
                return await Cadastro(consentimento, cancelamento).ConfigureAwait(false);
            case TipoDeOperacao.ListaDeRecursos:
                return await Disponiveis(familia, consentimento, recursosDaChave, cancelamento).ConfigureAwait(false);
            case TipoDeOperacao.Transacoes or TipoDeOperacao.TransacoesRecentes:
                var periodo = JanelaDeDatas.Transacoes(de, ate, hoje, tipo == TipoDeOperacao.TransacoesRecentes, familia.AceitaFuturo);
                return await DasTransacoes(recurso!, periodo, instante, corte, cancelamento).ConfigureAwait(false);
            case TipoDeOperacao.Faturas:
                return await Faturas(recurso!, JanelaDeDatas.Faturas(de, ate, hoje), instante, corte, cancelamento).ConfigureAwait(false);
            case TipoDeOperacao.TransacoesDaFatura:
                return await DaFatura(recurso!, faturaId!, de, ate, hoje, instante, corte, cancelamento).ConfigureAwait(false);
            case TipoDeOperacao.Sublista:
                return new Itens([.. Enumerable.Range(1, ItensDaSublista).Select(i => Item(recurso!, new Dictionary<string, string>(StringComparer.Ordinal), string.Create(CultureInfo.InvariantCulture, $"{recurso!.ResourceId}-i{i}")))]);
            case TipoDeOperacao.NotaDeNegociacao:
                return new Itens([Item(recurso!, new Dictionary<string, string>(StringComparer.Ordinal) { ["brokerNoteId"] = IdsDerivados.DaNota(recurso!.ResourceId) }, recurso.ResourceId)]);
            default:
                return await Detalhe(recurso!, instante, corte, schema, cancelamento).ConfigureAwait(false);
        }
    }

    private async Task<Itens> Cadastro(ConsentimentoDaChamada consentimento, CancellationToken cancelamento)
    {
        var cliente = await ambiente.Cliente(consentimento.Cliente, cancelamento).ConfigureAwait(false);
        var valores = new Dictionary<string, string>(StringComparer.Ordinal);
        if (cliente is not null)
        {
            valores["civilName"] = cliente.Nome;
            valores["companyName"] = cliente.Nome;
            valores["cpfNumber"] = cliente.Documento;
            valores["cnpjNumber"] = cliente.Documento;
        }

        return new Itens([new Preenchimento(valores, IdsDerivados.BaseDoConsentimento(consentimento.ConsentId), Documentos.Semente(consentimento.Cliente))]);
    }

    /// <summary>
    /// Na primeira página, os recursos disponíveis agora. Nas seguintes, o conjunto que a chave levou,
    /// sem reavaliar criação, estado ou regra de estado (RN37).
    /// </summary>
    private async Task<Itens> Disponiveis(Familia familia, ConsentimentoDaChamada consentimento, string? recursosDaChave, CancellationToken cancelamento)
    {
        var naEmissao = recursosDaChave is null ? null : RecursosDaChave.Posicoes(recursosDaChave);
        var itens = new List<Preenchimento>();
        var posicoes = new List<int>();
        for (var posicao = 0; posicao < consentimento.Recursos.Count; posicao++)
        {
            if (naEmissao?.Contains(posicao) == false)
            {
                continue;
            }

            var recurso = await ambiente.Recurso(consentimento.Recursos[posicao], cancelamento).ConfigureAwait(false);
            if (recurso is not null && recurso.Tipo == familia.TipoDeRecurso && (naEmissao is not null || recurso.Estado == Disponivel))
            {
                itens.Add(Item(recurso, new Dictionary<string, string>(StringComparer.Ordinal), recurso.ResourceId));
                posicoes.Add(posicao);
            }
        }

        return new Itens(itens, Recursos: recursosDaChave ?? RecursosDaChave.Marcar(posicoes, consentimento.Recursos.Count));
    }

    private async Task<Itens> DasTransacoes(RecursoDeDados recurso, Periodo periodo, DateTimeOffset instante, DateTimeOffset corte, CancellationToken cancelamento)
    {
        if (RecusaDeData(periodo) is { } recusa)
        {
            return new Itens([], recusa);
        }

        var transacoes = await ambiente.Transacoes(recurso.ResourceId, periodo.De, periodo.Ate, instante, corte, cancelamento).ConfigureAwait(false);
        return new Itens(Ordenadas(transacoes).Select(t => DeTransacao(recurso, t, null, instante)).ToList());
    }

    private async Task<Itens> DaFatura(RecursoDeDados recurso, string billId, string? de, string? ate, DateOnly hoje, DateTimeOffset instante, DateTimeOffset corte, CancellationToken cancelamento)
    {
        if (IdsDerivados.MesDaFatura(billId, recurso.ResourceId) is not { } mes || mes >= InicioDoMes(hoje) || Vencimento(mes) < hoje.AddMonths(-JanelaDeDatas.MesesDeHistorico))
        {
            return new Itens([], Proibido("RESOURCE_FORBIDDEN"));
        }

        var fim = mes.AddMonths(1).AddDays(-1);
        var transacoes = await ambiente.Transacoes(recurso.ResourceId, mes, fim, instante, corte, cancelamento).ConfigureAwait(false);
        if (transacoes.Count == 0)
        {
            return new Itens([], Proibido("RESOURCE_FORBIDDEN"));
        }

        var periodo = JanelaDeDatas.DaFatura(de, ate, mes, fim);
        if (RecusaDeData(periodo) is { } recusa)
        {
            return new Itens([], recusa);
        }

        var doPeriodo = transacoes.Where(t => t.Dia >= periodo.De && t.Dia <= periodo.Ate);
        return new Itens(Ordenadas(doPeriodo).Select(t => DeTransacao(recurso, t, billId, instante)).ToList());
    }

    private async Task<Itens> Faturas(RecursoDeDados recurso, Periodo periodo, DateTimeOffset instante, DateTimeOffset corte, CancellationToken cancelamento)
    {
        if (RecusaDeData(periodo) is { } recusa)
        {
            return new Itens([], recusa);
        }

        var hoje = DateOnly.FromDateTime(instante.UtcDateTime);
        var ate = periodo.Ate < hoje ? periodo.Ate : hoje;
        var transacoes = await ambiente.Transacoes(recurso.ResourceId, InicioDoMes(periodo.De).AddMonths(-1), ate, instante, corte, cancelamento).ConfigureAwait(false);
        var itens = transacoes
            .Where(t => InicioDoMes(t.Dia) < InicioDoMes(hoje))
            .GroupBy(t => InicioDoMes(t.Dia))
            .Select(g => (Mes: g.Key, Vencimento: Vencimento(g.Key), Total: Math.Abs(Soma(g))))
            .Where(f => f.Vencimento >= periodo.De && f.Vencimento <= periodo.Ate)
            .OrderByDescending(f => f.Mes)
            .Select(f => Item(
                recurso,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["billId"] = IdsDerivados.DaFatura(recurso.ResourceId, f.Mes),
                    ["dueDate"] = Data(f.Vencimento),
                    ["billTotalAmount" + Valor] = Dinheiro(f.Total),
                },
                IdsDerivados.DaFatura(recurso.ResourceId, f.Mes)))
            .ToList();
        return new Itens(itens);
    }

    private async Task<Itens> Detalhe(RecursoDeDados recurso, DateTimeOffset instante, DateTimeOffset corte, string schema, CancellationToken cancelamento)
    {
        var valores = new Dictionary<string, string>(StringComparer.Ordinal);
        if (recurso.Tipo == "ACCOUNT" && schema.Contains("\"availableAmount\"", StringComparison.Ordinal))
        {
            valores["availableAmount" + Valor] = Dinheiro(await ambiente.Saldo(recurso.ResourceId, cancelamento).ConfigureAwait(false));
        }
        else if (recurso.Tipo == "CREDIT_CARD_ACCOUNT" && schema.Contains("\"usedAmount\"", StringComparison.Ordinal))
        {
            var hoje = DateOnly.FromDateTime(instante.UtcDateTime);
            var usado = Math.Abs(Soma(await ambiente.Transacoes(recurso.ResourceId, InicioDoMes(hoje), hoje, instante, corte, cancelamento).ConfigureAwait(false)));
            valores["usedAmount" + Valor] = Dinheiro(usado);
            valores["limitAmount" + Valor] = Dinheiro(LimiteDoCartao + usado);
            valores["availableAmount" + Valor] = Dinheiro(LimiteDoCartao);
        }

        return new Itens([Item(recurso, valores, recurso.ResourceId)]);
    }

    private static IEnumerable<TransacaoDeDados> Ordenadas(IEnumerable<TransacaoDeDados> transacoes) =>
        transacoes.OrderByDescending(t => t.Dia).ThenBy(t => t.Posicao);

    private static decimal Soma(IEnumerable<TransacaoDeDados> transacoes) => transacoes.Sum(t => t.Credito ? -t.Valor : t.Valor);

    private static DateOnly InicioDoMes(DateOnly dia) => new(dia.Year, dia.Month, 1);

    private static DateOnly Vencimento(DateOnly mes) => mes.AddMonths(1).AddDays(9);

    private static string Data(DateOnly dia) => dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Dinheiro(decimal valor) => valor.ToString("F2", CultureInfo.InvariantCulture);

    private static Preenchimento Item(RecursoDeDados recurso, Dictionary<string, string> valores, string baseDosIds)
    {
        foreach (var campo in new[] { "accountId", "creditCardAccountId", "contractId", "investmentId", "operationId" })
        {
            valores[campo] = recurso.ResourceId;
        }

        var semente = Documentos.Semente(baseDosIds);
        var conta = recurso.Tipo == "ACCOUNT";
        if (conta)
        {
            valores.TryAdd("branchCode", "0001");
            valores.TryAdd("checkDigit", string.Create(CultureInfo.InvariantCulture, $"{semente % 10}"));
        }

        return new Preenchimento(valores, baseDosIds, semente, conta ? TipoDaConta : null);
    }

    private static Preenchimento DeTransacao(RecursoDeDados recurso, TransacaoDeDados t, string? billId, DateTimeOffset instante)
    {
        var semente = Documentos.Semente(t.Id);
        var momento = Momento(t, instante);
        var valores = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["transactionId"] = t.Id,
            ["transactionName"] = t.Nome,
            ["type"] = t.Tipo,
            ["creditDebitType"] = t.Credito ? Credito : Debito,
            ["transactionDateTime"] = momento.ToString("yyyy-MM-dd'T'HH:mm:ss'.000Z'", CultureInfo.InvariantCulture),
            ["transactionDate"] = Data(t.Dia),
            ["transactionConversionDate"] = Data(t.Dia),
            ["completedAuthorisedPaymentType"] = t.Estado,
            ["transactionAmount" + Valor] = Dinheiro(t.Valor),
            ["amount"] = Dinheiro(t.Valor),
            ["partieCnpjCpf"] = Documentos.Cpf(semente),
            ["partiePersonType"] = "PESSOA_NATURAL",
            ["partieCompeCode"] = "260",
            ["partieBranchCode"] = "0001",
            ["partieNumber"] = string.Create(CultureInfo.InvariantCulture, $"{semente % 100_000_000:D8}"),
            ["partieCheckDigit"] = string.Create(CultureInfo.InvariantCulture, $"{semente % 10}"),
            ["brokerNoteId"] = IdsDerivados.DaNota(recurso.ResourceId),
            ["payeeMCC"] = string.Create(CultureInfo.InvariantCulture, $"{1000 + (semente % 9000)}"),
        };
        if (billId is not null)
        {
            valores["billId"] = billId;
        }

        if (recurso.Tipo == "CREDIT_CARD_ACCOUNT")
        {
            TiposDoCartao.Preencher(valores, t.Credito, semente);
        }

        return Item(recurso, valores, t.Id) with { VariamPorSemente = null };
    }

    /// <summary>Nunca depois do instante da consulta, exceto lançamento futuro (RN44).</summary>
    private static DateTime Momento(TransacaoDeDados t, DateTimeOffset instante)
    {
        var fixo = t.Dia.ToDateTime(UltimoSegundoDaPrimeiraHora).AddSeconds(-Math.Min(t.Posicao, SegundosDaPrimeiraHora));
        var agora = instante.UtcDateTime;
        return DateOnly.FromDateTime(agora) == t.Dia && fixo > agora ? agora : fixo;
    }

    private IResult? RecusaDeData(Periodo periodo) => periodo.Erro switch
    {
        ErroDeData.NaoInformado => Erro422("PARAMETRO_NAO_INFORMADO"),
        ErroDeData.Invalido => Erro422("PARAMETRO_INVALIDO"),
        _ => null,
    };

    private async Task<IResult> Saida(
        HttpContext contexto,
        OperacaoDeDados operacao,
        IReadOnlyList<Preenchimento> itens,
        PedidoDePagina pedido,
        string? chave,
        ConsentimentoDaChamada consentimento,
        RecursoDeDados? recurso,
        ICotaDeDados? cota,
        List<(string Campo, string Valor)> filtros)
    {
        var schema = Schemas.GetOrAdd(operacao.OperationId, _ => JsonNode.Parse(operacao.SchemaDaResposta)!.AsObject());
        var propriedades = schema["properties"]!.AsObject();
        var esquemaDeDados = propriedades["data"];
        var lista = esquemaDeDados!["type"]?.GetValue<string>() == "array";
        var paginada = operacao.MinimoDoTamanho is not null;
        if (lista && filtros.Count > 0)
        {
            itens = [.. itens.Where(i => Corresponde(esquemaDeDados["items"], i, filtros))];
        }

        var total = lista ? itens.Count : 1;
        if (paginada && pedido.ForaDaFaixa(total))
        {
            return Erro422("PAGE_NOT_FOUND");
        }

        if (cota is not null && await cota.Esgotada(contexto.RequestAborted).ConfigureAwait(false))
        {
            return Results.Json(
                EnvelopeDeErro.Criar("LOCKED", "Recurso bloqueado", "A operação foi recusada porque o limite de consultas foi atingido.", relogio),
                EnvelopeDeErro.OpcoesJson,
                statusCode: StatusCodes.Status423Locked);
        }

        var desfecho = await EtapaDeComportamento.Decidir(contexto).ConfigureAwait(false);
        if (desfecho is { Segue: false })
        {
            return (await EtapaDeComportamento.Concluir(contexto, desfecho).ConfigureAwait(false))!;
        }

        JsonNode? dados;
        if (lista)
        {
            var pagina = paginada ? itens.Skip((pedido.Pagina - 1) * pedido.Tamanho).Take(pedido.Tamanho) : itens;
            var saida = new JsonArray();
            foreach (var item in pagina)
            {
                saida.Add(GeradorPorSchema.Gerar(esquemaDeDados["items"], item));
            }

            dados = saida;
        }
        else
        {
            dados = GeradorPorSchema.Gerar(esquemaDeDados, itens[0]);
        }

        ambiente.RegistrarNatureza(contexto, recurso?.Natureza ?? consentimento.Natureza);
        var raiz = new JsonObject { ["data"] = dados };
        if (propriedades["links"] is JsonObject links)
        {
            raiz["links"] = Links(contexto, links, pedido, pedido.TotalDePaginas(total), paginada && lista, chave);
        }

        raiz["meta"] = Meta(propriedades["meta"] as JsonObject, total, pedido.TotalDePaginas(total));
        var resposta = Results.Text(raiz.ToJsonString(EnvelopeDeErro.OpcoesJson), "application/json", Encoding.UTF8);
        if (cota is not null)
        {
            await cota.Contar(contexto.RequestAborted).ConfigureAwait(false);
        }

        // A cota é contada antes da espera simulada, para não alargar a corrida (ADR-008).
        await EtapaDeComportamento.Esperar(contexto, desfecho).ConfigureAwait(false);
        return resposta;
    }

    private static bool Corresponde(JsonNode? schema, Preenchimento item, List<(string Campo, string Valor)> filtros)
    {
        return filtros.All(f => GeradorPorSchema.Campo(schema, f.Campo, item) == f.Valor);
    }

    private JsonObject Meta(JsonObject? schema, int total, int paginas)
    {
        var meta = new JsonObject();
        var campos = schema?["properties"]?.AsObject();
        if (campos?.ContainsKey("totalRecords") == true)
        {
            meta["totalRecords"] = total;
            meta["totalPages"] = paginas;
        }

        meta["requestDateTime"] = relogio.GetUtcNow().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        return meta;
    }

    private static JsonObject Links(HttpContext contexto, JsonObject schema, PedidoDePagina pedido, int paginas, bool paginada, string? chave)
    {
        var campos = schema["properties"]!.AsObject();
        var baseDoLink = $"{contexto.Request.Scheme}://{contexto.Request.Host}{contexto.Request.Path}";
        var filtros = string.Concat(contexto.Request.Query
            .Where(q => q.Key is not ("page" or "page-size" or "pagination-key"))
            .Select(q => $"&{q.Key}={Uri.EscapeDataString(q.Value.ToString())}"));
        string Pagina(int numero) => string.Create(
            CultureInfo.InvariantCulture,
            $"{baseDoLink}?page={numero}&page-size={pedido.Tamanho}{(chave is null ? string.Empty : $"&pagination-key={Uri.EscapeDataString(chave)}")}{filtros}");

        var links = new JsonObject { ["self"] = paginada ? Pagina(pedido.Pagina) : $"{baseDoLink}{contexto.Request.QueryString}" };
        if (paginada)
        {
            Adicionar(links, campos, "first", pedido.Pagina > 1, () => Pagina(1));
            Adicionar(links, campos, "prev", pedido.Pagina > 1, () => Pagina(pedido.Pagina - 1));
            Adicionar(links, campos, "next", pedido.Pagina < paginas, () => Pagina(pedido.Pagina + 1));
            Adicionar(links, campos, "last", pedido.Pagina < paginas, () => Pagina(paginas));
        }

        return links;
    }

    private static void Adicionar(JsonObject links, JsonObject campos, string nome, bool condicao, Func<string> valor)
    {
        if (condicao && campos.ContainsKey(nome))
        {
            links[nome] = valor();
        }
    }

    private IResult Proibido(string codigo) => Erro(StatusCodes.Status403Forbidden, codigo, "Acesso proibido");

    private IResult Erro422(string codigo) => Erro(StatusCodes.Status422UnprocessableEntity, codigo, "Pedido não processado");

    private IResult Erro(int status, string codigo, string titulo) =>
        Results.Json(EnvelopeDeErro.Criar(codigo, titulo, titulo, relogio), EnvelopeDeErro.OpcoesJson, statusCode: status);
}
