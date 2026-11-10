using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulador.Consentimentos.Contratos;
using Simulador.Consentimentos.Dominio;

namespace Simulador.Consentimentos.Http;

/// <summary>Administração do painel de consentimentos (contracts/admin-painel.md, RN95 a RN99).</summary>
internal static class RotasDoPainel
{
    private const string Base = "/admin/consentimentos";
    private const string Pf = "PF";
    private const string Operador = "operador";
    private const int TentativasDeEscrita = 5;

    public static void Mapear(IEndpointRouteBuilder administracao)
    {
        administracao.MapGet(Base + "/opcoes", Opcoes);
        administracao.MapPost(Base, Criar);
        administracao.MapGet(Base + "/{consentId}", Consultar);
        administracao.MapPost(Base + "/{consentId}/aprovar", (string consentId, HttpContext contexto, IAmbienteDosConsentimentos ambiente) => Decidir(consentId, aprovar: true, contexto, ambiente));
        administracao.MapPost(Base + "/{consentId}/recusar", (string consentId, HttpContext contexto, IAmbienteDosConsentimentos ambiente) => Decidir(consentId, aprovar: false, contexto, ambiente));
    }

    private static IResult Erros(int status, IEnumerable<string> erros) => Results.Json(new { erros }, statusCode: status);

    private static async Task<IResult> Opcoes(string? cliente, string? banco, HttpContext contexto, IAmbienteDosConsentimentos ambiente, IRepositorioDeReceptoras receptoras)
    {
        if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(banco))
        {
            return Erros(StatusCodes.Status422UnprocessableEntity, [Painel.PedidoInvalido]);
        }

        var cancelamento = contexto.RequestAborted;
        if (await ambiente.Banco(banco, cancelamento).ConfigureAwait(false) is not { } dadosDoBanco
            || await ambiente.Cliente(cliente, cancelamento).ConfigureAwait(false) is not { } dadosDoCliente)
        {
            return Results.NotFound();
        }

        var opcoes = Painel.Opcoes(dadosDoCliente, banco, dadosDoBanco.Produtos);
        var cadastradas = await receptoras.Listar(cancelamento).ConfigureAwait(false);
        return Saidas.Json(new
        {
            cliente = dadosDoCliente.Documento,
            tipo = dadosDoCliente.Tipo,
            banco,
            semProduto = opcoes.SemProduto,
            grupos = opcoes.Grupos.Select(g => new { nome = g.Nome, permissoes = g.Permissoes }),
            contas = opcoes.Contas.Select(id => new { resourceId = id, marcada = true }),
            cartoes = opcoes.Cartoes.Select(id => new { resourceId = id, marcada = true }),
            agrupados = opcoes.Agrupados,
            receptoras = cadastradas.Select(r => new { organisationId = r.OrganisationId, nome = r.Nome }),
        });
    }

    private static async Task<IResult> Criar(HttpContext contexto, IAmbienteDosConsentimentos ambiente, IRepositorioDeReceptoras receptoras)
    {
        var cancelamento = contexto.RequestAborted;
        var pedido = await Ler(contexto).ConfigureAwait(false);
        if (pedido is not { Cliente: { Length: > 0 } documento, Banco: { Length: > 0 } banco })
        {
            return Erros(StatusCodes.Status422UnprocessableEntity, [Painel.PedidoInvalido]);
        }

        if (await ambiente.Banco(banco, cancelamento).ConfigureAwait(false) is not { } dadosDoBanco
            || await ambiente.Cliente(documento, cancelamento).ConfigureAwait(false) is not { } cliente)
        {
            return Results.NotFound();
        }

        var receptoraExiste = pedido.Receptora is { Length: > 0 } receptora && await receptoras.Obter(receptora, cancelamento).ConfigureAwait(false) is not null;
        var avaliacao = Painel.Avaliar(pedido, cliente, banco, dadosDoBanco.Produtos, receptoraExiste);
        if (avaliacao.Erros.Count > 0)
        {
            return Erros(StatusCodes.Status422UnprocessableEntity, avaliacao.Erros);
        }

        if (await ambiente.NovoConsentId(cancelamento).ConfigureAwait(false) is not { } id)
        {
            return Rotas.ErroAdmin(StatusCodes.Status422UnprocessableEntity, "saldo de documentos esgotado");
        }

        var agora = await ambiente.AgoraNoBanco(banco, cancelamento).ConfigureAwait(false);
        var novo = new DadosDoConsentimento(
            id,
            cliente.Documento,
            banco,
            Ciclo.Autorizado,
            avaliacao.Permissoes,
            avaliacao.Recursos,
            agora,
            ambiente.Marca,
            "persistido",
            null,
            pedido.PrazoMeses is { } meses ? agora.AddMonths(meses) : null,
            pedido.Receptora,
            null,
            null,
            agora,
            cliente.Tipo == Pf ? cliente.Documento : null,
            pedido.AprovacoesPendentes ?? 0,
            []);
        await ambiente.Inserir(novo, cancelamento).ConfigureAwait(false);
        await ambiente.RegistrarHistorico(Rotas.Autor(contexto), "consentimento-criado-no-painel", "consentimento", null, id, cancelamento).ConfigureAwait(false);
        return Saidas.Json(await Visao(ambiente, novo, agora, cancelamento).ConfigureAwait(false), StatusCodes.Status201Created);
    }

    private static async Task<IResult> Consultar(string consentId, HttpContext contexto, IAmbienteDosConsentimentos ambiente)
    {
        var cancelamento = contexto.RequestAborted;
        if (await ambiente.Consentimento(consentId, cancelamento).ConfigureAwait(false) is not { } consentimento)
        {
            return Results.NotFound();
        }

        var agora = await ambiente.AgoraNoBanco(consentimento.Banco, cancelamento).ConfigureAwait(false);
        return Saidas.Json(await Visao(ambiente, consentimento, agora, cancelamento).ConfigureAwait(false));
    }

    private static async Task<IResult> Decidir(string consentId, bool aprovar, HttpContext contexto, IAmbienteDosConsentimentos ambiente)
    {
        var cancelamento = contexto.RequestAborted;
        var autor = Rotas.Autor(contexto) is { } informado && !string.IsNullOrWhiteSpace(informado) ? informado : Operador;
        for (var tentativa = 0; tentativa < TentativasDeEscrita; tentativa++)
        {
            if (await ambiente.Consentimento(consentId, cancelamento).ConfigureAwait(false) is not { } guardado)
            {
                return Results.NotFound();
            }

            var agora = await ambiente.AgoraNoBanco(guardado.Banco, cancelamento).ConfigureAwait(false);
            if (Alcadas.ErroDaDecisao(Ciclo.Efetivo(guardado, agora), agora) is { } erro)
            {
                return Rotas.ErroAdmin(StatusCodes.Status409Conflict, erro);
            }

            var decidido = aprovar ? Alcadas.Aprovar(guardado, autor, agora) : Alcadas.Recusar(guardado, autor, agora);
            if (await ambiente.Atualizar(decidido, cancelamento).ConfigureAwait(false))
            {
                await ambiente.RegistrarHistorico(
                    autor,
                    aprovar ? "alcada-aprovada" : "alcada-recusada",
                    "aprovacoesPendentes",
                    Alcadas.Nome(Alcadas.Situacao(guardado, agora)) + ":" + guardado.AprovacoesPendentes,
                    Alcadas.Nome(Alcadas.Situacao(decidido, agora)) + ":" + decidido.AprovacoesPendentes,
                    cancelamento).ConfigureAwait(false);
                return Saidas.Json(await Visao(ambiente, decidido, agora, cancelamento).ConfigureAwait(false));
            }
        }

        return Rotas.ErroAdmin(StatusCodes.Status409Conflict, "consentimento em alteração");
    }

    private static async Task<object> Visao(IAmbienteDosConsentimentos ambiente, DadosDoConsentimento guardado, DateTimeOffset agora, CancellationToken cancelamento)
    {
        var consentimento = Ciclo.Efetivo(guardado, agora);
        var recursos = new List<object>();
        foreach (var id in consentimento.Recursos)
        {
            if (await ambiente.RecursoNoConsentimento(id, consentimento, agora, cancelamento).ConfigureAwait(false) is { } recurso)
            {
                recursos.Add(new { resourceId = recurso.ResourceId, tipo = recurso.Tipo, estado = recurso.Estado });
            }
        }

        return new
        {
            consentId = consentimento.ConsentId,
            cliente = consentimento.Cliente,
            banco = consentimento.Banco,
            receptora = consentimento.Receptora,
            estado = consentimento.Estado,
            permissoes = consentimento.Permissoes,
            criadoEm = consentimento.CriadoEm,
            expiraEm = consentimento.ExpiraEm,
            rejeitadoPor = consentimento.RejeitadoPor,
            razao = consentimento.Razao,
            alcada = new
            {
                situacao = Alcadas.Nome(Alcadas.Situacao(consentimento, agora)),
                aprovacoesPendentes = consentimento.AprovacoesPendentes,
                prazo = Alcadas.FimDoPrazo(consentimento),
            },
            decisoes = (consentimento.Decisoes ?? []).Select(d => new { autor = d.Autor, decisao = d.Decisao, em = d.Em }),
            recursos,
        };
    }

    private static async Task<PedidoDoPainel?> Ler(HttpContext contexto)
    {
        if (!contexto.Request.HasJsonContentType())
        {
            return null;
        }

        try
        {
            return await contexto.Request.ReadFromJsonAsync<PedidoDoPainel>(JsonDosConsentimentos.Opcoes, contexto.RequestAborted).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
