using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulador.Comum.Aleatorio;
using Simulador.Comum.Comportamento;
using Simulador.Comum.Erros;
using Simulador.Consentimentos.Contratos;
using Simulador.Consentimentos.Dominio;

namespace Simulador.Consentimentos.Http;

/// <summary>Chave do token, lida na subida (research R01).</summary>
internal sealed record ChaveDoToken(byte[] Valor);

/// <summary>Respostas comuns no envelope oficial.</summary>
internal static class Saidas
{
    public static string Data(DateTimeOffset valor) => valor.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static IResult Erro(int status, string code, string title, TimeProvider relogio) =>
        Results.Json(EnvelopeDeErro.Criar(code, title, title, relogio), EnvelopeDeErro.OpcoesJson, statusCode: status);

    public static IResult Json(object valor, int status = StatusCodes.Status200OK) =>
        Results.Json(valor, JsonDosConsentimentos.Opcoes, statusCode: status);

    public static string Self(HttpContext contexto) =>
        $"{contexto.Request.Scheme}://{contexto.Request.Host}{contexto.Request.Path}{contexto.Request.QueryString}";

    public static object Meta(TimeProvider relogio) => new { requestDateTime = Data(relogio.GetUtcNow()) };
}

/// <summary>Primeira etapa da avaliação da chamada (RN38, RN39).</summary>
internal sealed partial class PortaoDeSeguranca(IAmbienteDosConsentimentos ambiente, ChaveDoToken chave, TimeProvider relogio, IGeradorAleatorio aleatorio) : IPortaoDeSeguranca
{
    public const string ItemDoToken = "sim:token";
    public const string ItemDoConsentimento = "sim:consentimento";
    private const string CabecalhoDeCorrelacao = "x-fapi-interaction-id";
    private const string PrefixoBearer = "Bearer ";

    public async Task<IResult?> Avaliar(HttpContext contexto, OperacaoProtegida operacao)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(operacao);
        if (operacao.Escopos.Count == 0)
        {
            return null;
        }

        var recebido = contexto.Request.Headers[CabecalhoDeCorrelacao].ToString();
        if (!Correlacao().IsMatch(recebido))
        {
            contexto.Response.Headers[CabecalhoDeCorrelacao] = NovaCorrelacao();
            return Saidas.Erro(StatusCodes.Status400BadRequest, "CABECALHO_INVALIDO", "Cabeçalho inválido", relogio);
        }

        contexto.Response.Headers[CabecalhoDeCorrelacao] = recebido;
        var autorizacao = contexto.Request.Headers.Authorization.ToString();
        var token = autorizacao.StartsWith(PrefixoBearer, StringComparison.Ordinal) ? CodecDeToken.Ler(autorizacao[PrefixoBearer.Length..], chave.Valor) : null;
        var banco = contexto.Request.RouteValues["banco"] as string ?? string.Empty;
        var consentimento = token?.ConsentId is { } id && RegrasDoPortao.ConfereEstado(operacao)
            ? await ambiente.Consentimento(id, contexto.RequestAborted).ConfigureAwait(false)
            : null;

        var veredito = RegrasDoPortao.Avaliar(token, banco, relogio.GetUtcNow(), operacao, contexto.Request.RouteValues["consentId"] as string, consentimento);
        switch (veredito)
        {
            case VereditoDoPortao.NaoAutorizado:
                return Saidas.Erro(StatusCodes.Status401Unauthorized, "NAO_AUTORIZADO", "Não autorizado", relogio);
            case VereditoDoPortao.Proibido:
                return Saidas.Erro(StatusCodes.Status403Forbidden, "PROIBIDO", "Acesso proibido", relogio);
            default:
                contexto.Items[ItemDoToken] = token;
                contexto.Items[ItemDoConsentimento] = consentimento;
                return null;
        }
    }

    /// <summary>UUID versão 4 saído do gerador injetado (princípio VI).</summary>
    private string NovaCorrelacao()
    {
        const string Hexadecimal = "0123456789abcdef";
        var d = new string([.. Enumerable.Range(0, 32).Select(i => i switch
        {
            12 => '4',
            16 => '8',
            _ => Hexadecimal[aleatorio.ProximoInteiro(Hexadecimal.Length)],
        })]);
        return $"{d[..8]}-{d[8..12]}-{d[12..16]}-{d[16..20]}-{d[20..]}";
    }

    [GeneratedRegex("^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex Correlacao();
}

/// <summary>Corpo de <c>POST /consents</c> e de <c>/extends</c>.</summary>
internal sealed record CorpoDePedido(DadosDoPedido? Data);

internal sealed record DadosDoPedido(
    Pessoa? LoggedUser,
    Pessoa? BusinessEntity,
    IReadOnlyList<string>? Permissions,
    string? ExpirationDateTime);

internal sealed record Pessoa(Documento? Document);

internal sealed record Documento(string? Identification, string? Rel);

/// <summary>Operações oficiais de Consentimentos e Recursos (UC09, UC12, UC13, UC14).</summary>
internal sealed partial class ApiOficial(IAmbienteDosConsentimentos ambiente, TimeProvider relogio)
{
    /// <summary>Quantas vezes reavaliar quando outra chamada grava antes (RN82).</summary>
    private const int TentativasDeEscrita = 5;

    private const string Pj = "PJ";
    private const string Pf = "PF";

    private IResult Erro422(string code) => Saidas.Erro(StatusCodes.Status422UnprocessableEntity, code, "Pedido não processado", relogio);

    private IResult Invalido() => Saidas.Erro(StatusCodes.Status400BadRequest, "PARAMETRO_INVALIDO", "Requisição malformada", relogio);

    private IResult NaoEncontrado() => Saidas.Erro(StatusCodes.Status404NotFound, "NAO_ENCONTRADO", "Recurso não encontrado", relogio);

    private IResult ErroDeConcorrencia() => Saidas.Erro(StatusCodes.Status500InternalServerError, "ERRO_INTERNO", "Erro interno", relogio);

    private IResult Proibido() => Saidas.Erro(StatusCodes.Status403Forbidden, "PROIBIDO", "Acesso proibido", relogio);

    public async Task<IResult> Criar(HttpContext contexto)
    {
        var token = (TokenDeAcesso)contexto.Items[PortaoDeSeguranca.ItemDoToken]!;
        var dados = (await LerCorpo(contexto).ConfigureAwait(false))?.Data;
        if (!CorpoValido(dados, out var expira) || dados!.Permissions is not { } pedidas || !Permissoes.TodasConhecidas(pedidas))
        {
            return Invalido();
        }

        var cpf = dados.LoggedUser!.Document!.Identification!;

        var permissoes = pedidas.Distinct(StringComparer.Ordinal).ToList();
        var cnpj = dados.BusinessEntity?.Document?.Identification;
        var agora = relogio.GetUtcNow();
        if (Permissoes.Validar(permissoes, cnpj is not null) is { } codigo)
        {
            return Erro422(codigo);
        }

        if (expira is { } data && data <= agora)
        {
            return Erro422(Ciclo.DataInvalida);
        }

        var cancelamento = contexto.RequestAborted;
        var cliente = await ambiente.Cliente(cnpj ?? cpf, cancelamento).ConfigureAwait(false);
        var usuario = cnpj is null ? cliente : await ambiente.Cliente(cpf, cancelamento).ConfigureAwait(false);
        if (cliente?.Tipo != (cnpj is null ? Pf : Pj) || usuario?.Tipo != Pf)
        {
            return Erro422(Ciclo.NaoMapeado);
        }

        var banco = await ambiente.Banco(token.Banco, cancelamento).ConfigureAwait(false);
        var aplicaveis = Permissoes.Aplicaveis(permissoes, banco!.Produtos);
        if (!Permissoes.TemFuncional(aplicaveis))
        {
            return Erro422(Permissoes.SemFuncionais);
        }

        if (await EtapaDeComportamento.Aplicar(contexto).ConfigureAwait(false) is { } desvio)
        {
            return desvio;
        }

        if (await ambiente.NovoConsentId(cancelamento).ConfigureAwait(false) is not { } id)
        {
            return Erro422(Ciclo.NaoMapeado);
        }

        var recursos = cliente!.Recursos.Where(r => r.Banco == token.Banco && Permissoes.Cobre(r.Tipo, aplicaveis)).Select(r => r.ResourceId).ToList();
        var novo = new DadosDoConsentimento(id, cliente.Documento, token.Banco, Ciclo.Autorizado, aplicaveis, recursos, agora, ambiente.Marca, "persistido", null, expira, token.OrganisationId, null, null, agora, cpf, 0, []);
        await ambiente.Inserir(novo, cancelamento).ConfigureAwait(false);
        Registrar(contexto, novo);
        return Saidas.Json(Corpo(contexto, novo), StatusCodes.Status201Created);
    }

    public async Task<IResult> Consultar(HttpContext contexto)
    {
        var (consentimento, recusa) = await DaReceptora(contexto).ConfigureAwait(false);
        if (recusa is not null)
        {
            return recusa;
        }

        if (await EtapaDeComportamento.Aplicar(contexto).ConfigureAwait(false) is { } desvio)
        {
            return desvio;
        }

        Registrar(contexto, consentimento!);
        return Saidas.Json(Corpo(contexto, consentimento!));
    }

    public async Task<IResult> Revogar(HttpContext contexto)
    {
        for (var tentativa = 0; tentativa < TentativasDeEscrita; tentativa++)
        {
            var (consentimento, recusa) = await DaReceptora(contexto).ConfigureAwait(false);
            if (recusa is not null)
            {
                return recusa;
            }

            if (consentimento!.Estado == Ciclo.Rejeitado)
            {
                return Erro422("CONSENTIMENTO_EM_STATUS_REJEITADO");
            }

            if (tentativa == 0 && await EtapaDeComportamento.Aplicar(contexto).ConfigureAwait(false) is { } desvio)
            {
                return desvio;
            }

            if (await ambiente.Atualizar(Ciclo.Rejeitar(consentimento, Ciclo.PorTpp, Ciclo.Revogado, relogio.GetUtcNow()), contexto.RequestAborted).ConfigureAwait(false))
            {
                return Results.NoContent();
            }
        }

        return ErroDeConcorrencia();
    }

    public async Task<IResult> Renovar(HttpContext contexto)
    {
        var ip = contexto.Request.Headers["x-fapi-customer-ip-address"].ToString();
        var agente = contexto.Request.Headers["x-customer-user-agent"].ToString();
        if (ip.Length is 0 or > 100 || agente.Length > 255 || !TextoSemBordas().IsMatch(agente))
        {
            return Invalido();
        }

        var dados = (await LerCorpo(contexto).ConfigureAwait(false))?.Data;
        if (!CorpoValido(dados, out var nova))
        {
            return Invalido();
        }

        var documento = dados!.LoggedUser!.Document!;
        var cpf = documento.Identification!;
        var cancelamento = contexto.RequestAborted;
        for (var tentativa = 0; tentativa < TentativasDeEscrita; tentativa++)
        {
            var (consentimento, recusa) = await DoToken(contexto).ConfigureAwait(false);
            if (recusa is not null)
            {
                return recusa;
            }

            var agora = relogio.GetUtcNow();
            if (Ciclo.ErroDaRenovacao(consentimento!, dados.ExpirationDateTime, nova, agora) is { } codigo)
            {
                return Erro422(codigo);
            }

            var cliente = await ambiente.Cliente(consentimento!.Cliente, cancelamento).ConfigureAwait(false);
            var usuario = await ambiente.Cliente(cpf, cancelamento).ConfigureAwait(false);
            if (cliente is null || !Ciclo.PodeRenovar(consentimento, cliente.Tipo, cpf, usuario?.Tipo == Pf, dados.BusinessEntity?.Document?.Identification))
            {
                return Erro422(Ciclo.NaoMapeado);
            }

            if (tentativa == 0 && await EtapaDeComportamento.Aplicar(contexto).ConfigureAwait(false) is { } desvio)
            {
                return desvio;
            }

            var extensao = new DadosDaExtensao(nova, consentimento.ExpiraEm, cpf, documento.Rel!, agora, ip, agente);
            var renovado = consentimento with { ExpiraEm = nova, AtualizadoEm = agora, Extensoes = [extensao, .. consentimento.Extensoes] };
            if (await ambiente.Atualizar(renovado, cancelamento).ConfigureAwait(false))
            {
                Registrar(contexto, renovado);
                return Saidas.Json(Corpo(contexto, renovado, comRejeicao: false), StatusCodes.Status201Created);
            }
        }

        return ErroDeConcorrencia();
    }

    public async Task<IResult> Extensoes(HttpContext contexto)
    {
        var (consentimento, recusa) = await DaReceptora(contexto).ConfigureAwait(false);
        if (recusa is not null)
        {
            return recusa;
        }

        var (pagina, tamanho, totalPaginas, foraDaFaixa, invalida) = Paginar(contexto, consentimento!.Extensoes.Count);
        if (invalida)
        {
            return Erro422("PARAMETRO_INVALIDO");
        }

        if (foraDaFaixa)
        {
            return Erro422("PAGE_NOT_FOUND");
        }

        if (await EtapaDeComportamento.Aplicar(contexto).ConfigureAwait(false) is { } desvio)
        {
            return desvio;
        }

        var daPagina = consentimento.Extensoes.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        if (daPagina.Count > 0)
        {
            Registrar(contexto, consentimento);
        }

        var dados = daPagina.Select(e => new
        {
            expirationDateTime = e.NovaValidade is { } nova ? Saidas.Data(nova) : null,
            loggedUser = new { document = new { identification = e.Documento, rel = e.Rel } },
            requestDateTime = Saidas.Data(e.SolicitadaEm),
            previousExpirationDateTime = e.ValidadeAnterior is { } anterior ? Saidas.Data(anterior) : null,
            xFapiCustomerIpAddress = e.Ip,
            xCustomerUserAgent = e.UserAgent,
        }).ToList();
        return Saidas.Json(new
        {
            data = dados,
            links = Links(contexto, pagina, tamanho, totalPaginas),
            meta = new { totalRecords = consentimento.Extensoes.Count, totalPages = totalPaginas, requestDateTime = Saidas.Data(relogio.GetUtcNow()) },
        });
    }

    public async Task<IResult> Recursos(HttpContext contexto)
    {
        var consentimento = (DadosDoConsentimento)contexto.Items[PortaoDeSeguranca.ItemDoConsentimento]!;
        var cancelamento = contexto.RequestAborted;
        var agora = relogio.GetUtcNow();
        var banco = await ambiente.Banco(consentimento.Banco, cancelamento).ConfigureAwait(false);
        switch (Preparacao.Situacao(consentimento.CriadoEm, banco!.PreparacaoSeg, agora))
        {
            case SituacaoDaPreparacao.Preparando:
                return Results.StatusCode(StatusCodes.Status202Accepted);
            case SituacaoDaPreparacao.Esgotada:
                return Saidas.Erro(StatusCodes.Status504GatewayTimeout, "GATEWAY_TIMEOUT", "Tempo esgotado", relogio);
        }

        var itens = new List<DadosDoRecurso>();
        foreach (var id in consentimento.Recursos)
        {
            if (await ambiente.RecursoNoConsentimento(id, consentimento, agora, cancelamento).ConfigureAwait(false) is { } recurso)
            {
                itens.Add(recurso);
            }
        }

        var (pagina, tamanho, totalPaginas, foraDaFaixa, invalida) = Paginar(contexto, itens.Count);
        if (invalida)
        {
            return Erro422("PARAMETRO_INVALIDO");
        }

        if (foraDaFaixa)
        {
            return Erro422("PAGE_NOT_FOUND");
        }

        if (await EtapaDeComportamento.Aplicar(contexto).ConfigureAwait(false) is { } desvio)
        {
            return desvio;
        }

        var daPagina = itens.Skip((pagina - 1) * tamanho).Take(tamanho).ToList();
        daPagina.ForEach(r => ambiente.RegistrarNatureza(contexto, r.Natureza));
        return Saidas.Json(new
        {
            data = daPagina.Select(r => new { resourceId = r.ResourceId, type = r.Tipo, status = r.Estado }),
            links = Links(contexto, pagina, tamanho, totalPaginas),
            meta = new { requestDateTime = Saidas.Data(relogio.GetUtcNow()), totalRecords = itens.Count, totalPages = totalPaginas },
        });
    }

    private static object Links(HttpContext contexto, int pagina, int tamanho, int total)
    {
        var baseDoLink = $"{contexto.Request.Scheme}://{contexto.Request.Host}{contexto.Request.Path}";
        string Pagina(int numero) => string.Create(CultureInfo.InvariantCulture, $"{baseDoLink}?page={numero}&page-size={tamanho}");
        return new
        {
            self = Pagina(pagina),
            first = pagina > 1 ? Pagina(1) : null,
            prev = pagina > 1 ? Pagina(pagina - 1) : null,
            next = pagina < total ? Pagina(pagina + 1) : null,
            last = pagina < total ? Pagina(total) : null,
        };
    }

    /// <summary>Página pedida e tamanho aplicado, de 25 a 1.000 (RN31, RN33).</summary>
    private static (int Pagina, int Tamanho, int TotalPaginas, bool ForaDaFaixa, bool Invalida) Paginar(HttpContext contexto, int total)
    {
        const int Padrao = 25;
        const int Maximo = 1000;
        var (paginaValida, pagina) = Inteiro(contexto.Request.Query["page"], 1);
        var (tamanhoValido, valor) = Inteiro(contexto.Request.Query["page-size"], Padrao);
        var tamanho = Math.Clamp(valor, Padrao, Maximo);
        var totalPaginas = total == 0 ? 0 : ((total - 1) / tamanho) + 1;
        return (pagina, tamanho, totalPaginas, pagina > Math.Max(totalPaginas, 1), !paginaValida || !tamanhoValido);
    }

    private static (bool Valido, int Numero) Inteiro(string? texto, int padrao)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return (true, padrao);
        }

        return int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var numero) && numero >= 1 ? (true, numero) : (false, padrao);
    }

    private static bool CorpoValido(DadosDoPedido? dados, out DateTimeOffset? data)
    {
        data = null;
        return dados is not null
            && PessoaValida(dados.LoggedUser, CpfOficial(), RelDeCpf())
            && (dados.BusinessEntity is null || PessoaValida(dados.BusinessEntity, CnpjOficial(), RelDeCnpj()))
            && DataValida(dados.ExpirationDateTime, out data);
    }

    private static bool PessoaValida(Pessoa? pessoa, Regex identificacao, Regex rel) =>
        pessoa?.Document is { Identification: { } numero, Rel: { } tipo } && identificacao.IsMatch(numero) && rel.IsMatch(tipo);

    private static bool DataValida(string? texto, out DateTimeOffset? data)
    {
        data = null;
        if (texto is null)
        {
            return true;
        }

        if (texto.Length > 20 || !DataZulu().IsMatch(texto) || !DateTimeOffset.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var lida))
        {
            return false;
        }

        data = lida;
        return true;
    }

    [GeneratedRegex(@"^[0-9]{11}\z")]
    private static partial Regex CpfOficial();

    [GeneratedRegex(@"^[A-Z]{3}\z")]
    private static partial Regex RelDeCpf();

    [GeneratedRegex(@"^[0-9A-Z]{12}[0-9]{2}\z")]
    private static partial Regex CnpjOficial();

    [GeneratedRegex(@"^[A-Z]{4}\z")]
    private static partial Regex RelDeCnpj();

    [GeneratedRegex(@"^[0-9]{4}-(1[0-2]|0?[1-9])-(3[01]|[12][0-9]|0?[1-9])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]Z\z")]
    private static partial Regex DataZulu();

    [GeneratedRegex(@"^[^\s](.*[^\s])?\z")]
    private static partial Regex TextoSemBordas();

    private static async Task<CorpoDePedido?> LerCorpo(HttpContext contexto)
    {
        try
        {
            return await JsonSerializer.DeserializeAsync<CorpoDePedido>(contexto.Request.Body, JsonDosConsentimentos.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private object Corpo(HttpContext contexto, DadosDoConsentimento c, bool comRejeicao = true) => new
    {
        data = new
        {
            consentId = c.ConsentId,
            creationDateTime = Saidas.Data(c.CriadoEm),
            status = c.Estado,
            statusUpdateDateTime = Saidas.Data(c.AtualizadoEm ?? c.CriadoEm),
            permissions = c.Permissoes,
            expirationDateTime = c.ExpiraEm is { } expira ? Saidas.Data(expira) : null,
            rejection = comRejeicao && c.Estado == Ciclo.Rejeitado ? new { rejectedBy = c.RejeitadoPor, reason = new { code = c.Razao } } : null,
        },
        links = new { self = Saidas.Self(contexto) },
        meta = Saidas.Meta(relogio),
    };

    private void Registrar(HttpContext contexto, DadosDoConsentimento consentimento) => ambiente.RegistrarNatureza(contexto, consentimento.Natureza);

    private async Task<(DadosDoConsentimento? Consentimento, IResult? Recusa)> Carregar(HttpContext contexto, TokenDeAcesso token)
    {
        var id = contexto.Request.RouteValues["consentId"] as string ?? string.Empty;
        var consentimento = await ambiente.Consentimento(id, contexto.RequestAborted).ConfigureAwait(false);
        return consentimento is null || consentimento.Banco != token.Banco
            ? (null, NaoEncontrado())
            : (Ciclo.Efetivo(consentimento, relogio.GetUtcNow()), null);
    }

    private async Task<(DadosDoConsentimento? Consentimento, IResult? Recusa)> DaReceptora(HttpContext contexto)
    {
        var token = (TokenDeAcesso)contexto.Items[PortaoDeSeguranca.ItemDoToken]!;
        var (consentimento, recusa) = await Carregar(contexto, token).ConfigureAwait(false);
        return recusa is null && !RegrasDoPortao.ServeA(consentimento!, token.OrganisationId) ? (null, Proibido()) : (consentimento, recusa);
    }

    private Task<(DadosDoConsentimento? Consentimento, IResult? Recusa)> DoToken(HttpContext contexto) =>
        Carregar(contexto, (TokenDeAcesso)contexto.Items[PortaoDeSeguranca.ItemDoToken]!);
}

internal sealed record PedidoDeReceptora(string? Nome, string? OrganisationId = null);

internal sealed record PedidoDeRevogacao(string? Motivo = null);

internal sealed record PedidoDeEstado(string? Estado);

internal sealed record PedidoDeProduto(string? Banco, string? Produto, string? Estado = null);

/// <summary><c>POST /{banco}/token</c> e administração (contracts/token-e-admin.md).</summary>
internal static class Rotas
{
    private const int TentativasDeEscrita = 5;

    private static readonly string[] ProdutosValidos = ["contas", "cartoes", "emprestimos", "financiamentos", "adiantamentos", "direitos-creditorios", "investimentos", "cambio", "fundos", "renda-variavel", "renda-fixa-credito", "tesouro-direto"];

    public static void MapearToken(IEndpointRouteBuilder openFinance) =>
        openFinance.MapPost("/{banco}/token", Emitir);

    public static void MapearAdministracao(IEndpointRouteBuilder administracao)
    {
        administracao.MapPost("/admin/receptoras", CriarReceptora);
        administracao.MapGet("/admin/receptoras", async (IRepositorioDeReceptoras repositorio, CancellationToken t) =>
            Saidas.Json(await repositorio.Listar(t).ConfigureAwait(false)));
        administracao.MapGet("/admin/receptoras/{organisationId}", async (string organisationId, IRepositorioDeReceptoras repositorio, CancellationToken t) =>
            await repositorio.Obter(organisationId, t).ConfigureAwait(false) is { } receptora ? Saidas.Json(receptora) : Results.NotFound());
        administracao.MapPost("/admin/consentimentos/{consentId}/revogar", RevogarNaAdministracao);
        administracao.MapPut("/admin/recursos/{resourceId}/estado", MudarEstado);
        administracao.MapPost("/admin/clientes/{documento}/produtos", AdicionarProduto);
        RotasDoPainel.Mapear(administracao);
    }

    internal static string? Autor(HttpContext contexto) => Simulador.Comum.Http.AutorDoPedido.Ler(contexto);

    internal static IResult ErroAdmin(int status, string mensagem) => Results.Json(new { erros = new[] { mensagem } }, statusCode: status);

    private static IResult ErroOAuth(int status, string codigo) => Results.Json(new { error = codigo }, statusCode: status);

    private static async Task<IResult> Emitir(
        string banco,
        HttpContext contexto,
        IAmbienteDosConsentimentos ambiente,
        IRepositorioDeReceptoras receptoras,
        ChaveDoToken chave,
        TimeProvider relogio)
    {
        var cancelamento = contexto.RequestAborted;
        var dadosDoBanco = await ambiente.Banco(banco, cancelamento).ConfigureAwait(false);
        if (dadosDoBanco is null)
        {
            return Saidas.Erro(StatusCodes.Status404NotFound, "NAO_ENCONTRADO", "Recurso não encontrado", relogio);
        }

        if (!dadosDoBanco.Ativo)
        {
            return Saidas.Erro(StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE", "Serviço indisponível", relogio);
        }

        if (!contexto.Request.HasFormContentType)
        {
            return ErroOAuth(StatusCodes.Status400BadRequest, "invalid_request");
        }

        var form = await contexto.Request.ReadFormAsync(cancelamento).ConfigureAwait(false);
        var escopos = form["scope"].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var consentId = escopos.FirstOrDefault(e => e.StartsWith("consent:", StringComparison.Ordinal))?["consent:".Length..];
        var daReceptora = escopos.Contains(RegrasDoPortao.EscopoDaReceptora);
        if (form["grant_type"] != "client_credentials" || (!daReceptora && consentId is null))
        {
            return ErroOAuth(StatusCodes.Status400BadRequest, "invalid_request");
        }

        var receptora = await receptoras.ObterPorUsuario(form["client_id"].ToString(), cancelamento).ConfigureAwait(false);
        if (receptora is null || !CodecDeToken.Iguais(receptora.Senha, form["client_secret"].ToString()))
        {
            return ErroOAuth(StatusCodes.Status401Unauthorized, "invalid_client");
        }

        var agora = relogio.GetUtcNow();
        if (!daReceptora)
        {
            var consentimento = await ambiente.Consentimento(consentId!, cancelamento).ConfigureAwait(false);
            if (consentimento is null || consentimento.Banco != banco || Ciclo.Efetivo(consentimento, agora).Estado != Ciclo.Autorizado
                || !RegrasDoPortao.ServeA(consentimento, receptora.OrganisationId))
            {
                return ErroOAuth(StatusCodes.Status400BadRequest, "invalid_scope");
            }
        }

        var token = new TokenDeAcesso(daReceptora ? TipoDeToken.Receptora : TipoDeToken.Consentimento, receptora.OrganisationId, banco, daReceptora ? null : consentId, agora.AddSeconds(CodecDeToken.ValidadeSegundos));
        return Results.Json(new { access_token = CodecDeToken.Emitir(token, chave.Valor), token_type = "Bearer", expires_in = CodecDeToken.ValidadeSegundos, scope = form["scope"].ToString() });
    }

    private static async Task<IResult> CriarReceptora(PedidoDeReceptora pedido, HttpContext contexto, IRepositorioDeReceptoras repositorio, IAmbienteDosConsentimentos ambiente, IGeradorAleatorio aleatorio, TimeProvider relogio)
    {
        if (string.IsNullOrWhiteSpace(pedido.Nome))
        {
            return ErroAdmin(StatusCodes.Status422UnprocessableEntity, "nome é obrigatório");
        }

        var organisationId = string.IsNullOrWhiteSpace(pedido.OrganisationId) ? ambiente.NovoOrganisationId() : pedido.OrganisationId;
        var sufixo = string.Create(CultureInfo.InvariantCulture, $"{aleatorio.ProximoInteiro(int.MaxValue):x8}");
        var receptora = new Receptora(organisationId, pedido.Nome, $"teste-{sufixo}", $"teste-senha-{aleatorio.ProximoInteiro(int.MaxValue):x8}{aleatorio.ProximoInteiro(int.MaxValue):x8}", relogio.GetUtcNow());
        if (!await repositorio.Inserir(receptora, contexto.RequestAborted).ConfigureAwait(false))
        {
            return ErroAdmin(StatusCodes.Status409Conflict, "receptora repetida");
        }

        await ambiente.RegistrarHistorico(Autor(contexto), "receptora-criada", "receptora", null, organisationId, contexto.RequestAborted).ConfigureAwait(false);
        return Saidas.Json(new { receptora.OrganisationId, receptora.Nome, receptora.Usuario, receptora.Senha, receptora.CriadaEm, credencialDeTeste = true }, StatusCodes.Status201Created);
    }

    private static async Task<IResult> RevogarNaAdministracao(string consentId, HttpContext contexto, IAmbienteDosConsentimentos ambiente)
    {
        var fraude = contexto.Request.HasJsonContentType()
            && (await contexto.Request.ReadFromJsonAsync<PedidoDeRevogacao>(JsonDosConsentimentos.Opcoes, contexto.RequestAborted).ConfigureAwait(false))?.Motivo == "fraude";
        for (var tentativa = 0; tentativa < TentativasDeEscrita; tentativa++)
        {
            if (await ambiente.Consentimento(consentId, contexto.RequestAborted).ConfigureAwait(false) is not { } guardado)
            {
                return Results.NotFound();
            }

            var agora = await ambiente.AgoraNoBanco(guardado.Banco, contexto.RequestAborted).ConfigureAwait(false);
            var consentimento = Ciclo.Efetivo(guardado, agora);
            if (consentimento.Estado == Ciclo.Rejeitado)
            {
                return ErroAdmin(StatusCodes.Status409Conflict, "consentimento já rejeitado");
            }

            var rejeitado = fraude ? Ciclo.Rejeitar(consentimento, Ciclo.PorInstituicao, Ciclo.Fraude, agora) : Ciclo.Rejeitar(consentimento, Ciclo.PorUsuario, Ciclo.Revogado, agora);
            if (await ambiente.Atualizar(rejeitado, contexto.RequestAborted).ConfigureAwait(false))
            {
                await ambiente.RegistrarHistorico(Autor(contexto), "consentimento-revogado", "consentimento", consentimento.Estado, rejeitado.Estado, contexto.RequestAborted).ConfigureAwait(false);
                return Saidas.Json(rejeitado);
            }
        }

        return ErroAdmin(StatusCodes.Status409Conflict, "consentimento em alteração");
    }

    private static async Task<IResult> MudarEstado(string resourceId, PedidoDeEstado pedido, HttpContext contexto, IAmbienteDosConsentimentos ambiente)
    {
        for (var tentativa = 0; tentativa < TentativasDeEscrita; tentativa++)
        {
            if (await ambiente.Recurso(resourceId, contexto.RequestAborted).ConfigureAwait(false) is not { } recurso)
            {
                return Results.NotFound();
            }

            if (pedido.Estado is not { } estado || !EstadosDeRecurso.Existe(estado) || !EstadosDeRecurso.Permitida(recurso.Tipo, recurso.Estado, estado))
            {
                return ErroAdmin(StatusCodes.Status422UnprocessableEntity, "transição proibida");
            }

            if (recurso.Estado == estado)
            {
                return Saidas.Json(recurso);
            }

            if (await ambiente.MudarEstado(recurso, estado, contexto.RequestAborted).ConfigureAwait(false))
            {
                await ambiente.RegistrarHistorico(Autor(contexto), "recurso-alterado", "estado", recurso.Estado, estado, contexto.RequestAborted).ConfigureAwait(false);
                return Saidas.Json(recurso with { Estado = estado, Versao = recurso.Versao + 1 });
            }
        }

        return ErroAdmin(StatusCodes.Status409Conflict, "recurso em alteração");
    }

    /// <summary>Liga o recurso ao consentimento vigente, sem desfazer o que outra chamada gravou.</summary>
    private static async Task<bool> Anexar(IAmbienteDosConsentimentos ambiente, string consentId, string resourceId, CancellationToken cancelamento)
    {
        for (var tentativa = 0; tentativa < TentativasDeEscrita; tentativa++)
        {
            var atual = await ambiente.Consentimento(consentId, cancelamento).ConfigureAwait(false);
            if (atual is null || Ciclo.Efetivo(atual, await ambiente.AgoraNoBanco(atual.Banco, cancelamento).ConfigureAwait(false)).Estado != Ciclo.Autorizado
                || await ambiente.Atualizar(atual with { Recursos = [.. atual.Recursos, resourceId] }, cancelamento).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<IResult> AdicionarProduto(string documento, PedidoDeProduto pedido, HttpContext contexto, IAmbienteDosConsentimentos ambiente)
    {
        var cancelamento = contexto.RequestAborted;
        if (await ambiente.Cliente(documento, cancelamento).ConfigureAwait(false) is not { } cliente)
        {
            return Results.NotFound();
        }

        var estado = pedido.Estado ?? EstadosDeRecurso.Disponivel;
        if (cliente.Natureza == "virtual" || pedido.Produto is not { } produto || !ProdutosValidos.Contains(produto) || pedido.Banco is not { } banco
            || await ambiente.Banco(banco, cancelamento).ConfigureAwait(false) is null || !EstadosDeRecurso.Existe(estado))
        {
            return ErroAdmin(StatusCodes.Status422UnprocessableEntity, "pedido inválido");
        }

        if (await ambiente.AdicionarProduto(documento, banco, produto, estado, cancelamento).ConfigureAwait(false) is not { } recurso)
        {
            return ErroAdmin(StatusCodes.Status422UnprocessableEntity, "saldo de documentos esgotado");
        }

        if (Permissoes.Agrupado(recurso.Tipo))
        {
            foreach (var consentId in cliente.Consentimentos.Where(c => c.Banco == banco && Permissoes.Cobre(recurso.Tipo, c.Permissoes)).Select(c => c.ConsentId))
            {
                if (!await Anexar(ambiente, consentId, recurso.ResourceId, cancelamento).ConfigureAwait(false))
                {
                    return ErroAdmin(StatusCodes.Status409Conflict, "consentimento em alteração");
                }
            }
        }

        await ambiente.RegistrarHistorico(Autor(contexto), "produto-adicionado", "recurso", null, recurso.ResourceId, cancelamento).ConfigureAwait(false);
        return Saidas.Json(recurso, StatusCodes.Status201Created);
    }
}
