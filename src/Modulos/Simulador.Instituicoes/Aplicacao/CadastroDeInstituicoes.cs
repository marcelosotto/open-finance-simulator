using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using Simulador.Instituicoes.Contratos;
using Simulador.Instituicoes.Dominio;

namespace Simulador.Instituicoes.Aplicacao;

/// <summary>Corpo do cadastro (UC01). Só o nome é obrigatório (RN03).</summary>
internal sealed record PedidoDeCriacao(
    string Nome,
    string? Perfil = null,
    bool InspiradoEmBancoReal = false,
    string? Publico = null,
    IReadOnlyList<string>? Produtos = null,
    string? Marca = null);

/// <summary>Corpo de mudança do relógio (UC28).</summary>
internal sealed record PedidoDeRelogio(DateTimeOffset? FixadoEm = null, decimal? Fator = null);

internal enum TipoDeResultado
{
    Ok,
    NaoEncontrada,
    Repetida,
    Invalida,
}

internal sealed record ResultadoDoCadastro(TipoDeResultado Tipo, Instituicao? Instituicao = null, IReadOnlyList<string>? Erros = null, string? Resultado = null)
{
    public static ResultadoDoCadastro Sucesso(Instituicao instituicao, string? resultado = null) => new(TipoDeResultado.Ok, instituicao, Resultado: resultado);

    public static ResultadoDoCadastro NaoEncontrada { get; } = new(TipoDeResultado.NaoEncontrada, Erros: ["instituição não encontrada"]);

    public static ResultadoDoCadastro Repetida { get; } = new(TipoDeResultado.Repetida, Erros: ["nome já usado por outra instituição"]);

    public static ResultadoDoCadastro Invalida(params IReadOnlyList<string> erros) => new(TipoDeResultado.Invalida, Erros: erros);
}

/// <summary>Casos de uso do cadastro (UC01, UC02, UC03).</summary>
internal sealed class CadastroDeInstituicoes(
    IRepositorioDeInstituicoes repositorio,
    IConsultaDeClientes clientes,
    ILimpezaDeAnomalias anomalias,
    IAmbienteDasInstituicoes ambiente,
    TimeProvider relogio) : IConsultaDeInstituicoes
{
    private const string AutorPadrao = "operador";
    private const decimal FatorMaximo = 100_000m;

    private static readonly HashSet<string> CamposEditaveis = new(StringComparer.Ordinal)
    {
        "nome", "marca", "publico", "produtos", "comportamento", "limites",
    };

    public Task<Instituicao?> Obter(string endereco, CancellationToken cancelamento) => repositorio.Obter(endereco, cancelamento);

    public Task<IReadOnlyList<Instituicao>> Listar(CancellationToken cancelamento) => repositorio.Listar(cancelamento);

    public Task<IReadOnlyList<EntradaDeHistorico>> Historico(string endereco, CancellationToken cancelamento) =>
        repositorio.LerHistorico(endereco, cancelamento);

    public async Task<ResultadoDoCadastro> Criar(PedidoDeCriacao pedido, string? autor, CancellationToken cancelamento)
    {
        var perfil = CatalogoDePerfis.Obter(pedido.Perfil ?? ValoresOficiais.PerfilPadrao);
        if (perfil is null)
        {
            return ResultadoDoCadastro.Invalida($"perfil desconhecido: {pedido.Perfil}");
        }

        if (string.IsNullOrWhiteSpace(pedido.Nome))
        {
            return ResultadoDoCadastro.Invalida("nome é obrigatório");
        }

        if (Endereco.Gerar(pedido.Nome) is not { } endereco)
        {
            return ResultadoDoCadastro.Invalida("nome não gera um endereço válido");
        }

        var nome = NomeDoBanco.Aplicar(pedido.Nome, ambiente.Marca, pedido.InspiradoEmBancoReal);
        var instituicao = Nova(endereco, nome, perfil, pronta: false) with
        {
            Publico = pedido.Publico ?? ValoresOficiais.PublicoPadrao,
            Produtos = pedido.Produtos ?? ValoresOficiais.Produtos,
            Marca = pedido.Marca ?? nome,
        };
        var erros = Validacao.Validar(instituicao, ambiente.StatusDeclarados);
        if (erros.Count > 0)
        {
            return ResultadoDoCadastro.Invalida(erros);
        }

        if (await NomeEmUso(nome, endereco, cancelamento).ConfigureAwait(false)
            || !await repositorio.Inserir(instituicao, cancelamento).ConfigureAwait(false))
        {
            return ResultadoDoCadastro.Repetida;
        }

        await Registrar(endereco, autor, "criada", [], cancelamento).ConfigureAwait(false);
        return ResultadoDoCadastro.Sucesso(instituicao);
    }

    public async Task<ResultadoDoCadastro> Alterar(string endereco, JsonObject alteracao, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.Obter(endereco, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return ResultadoDoCadastro.NaoEncontrada;
        }

        var desconhecidos = alteracao.Select(c => c.Key).Where(c => !CamposEditaveis.Contains(c)).ToList();
        if (desconhecidos.Count > 0)
        {
            return ResultadoDoCadastro.Invalida($"campos não editáveis: {string.Join(", ", desconhecidos)}");
        }

        var antes = ComoJson(atual);
        var alvo = antes.DeepClone().AsObject();
        AplicadorDeMerge.Aplicar(alvo, alteracao);

        Instituicao alterada;
        try
        {
            alterada = alvo.Deserialize<Instituicao>(JsonDasInstituicoes.Opcoes)!;
        }
        catch (JsonException)
        {
            return ResultadoDoCadastro.Invalida("corpo com campo ou valor inválido");
        }

        var erros = Validacao.Validar(alterada, ambiente.StatusDeclarados);
        if (erros.Count > 0)
        {
            return ResultadoDoCadastro.Invalida(erros);
        }

        if (alterada.Nome != atual.Nome && await NomeEmUso(alterada.Nome, endereco, cancelamento).ConfigureAwait(false))
        {
            return ResultadoDoCadastro.Repetida;
        }

        var mudancas = Diferencas.Entre(antes, ComoJson(alterada));
        if (mudancas.Count > 0)
        {
            await repositorio.Substituir(alterada, cancelamento).ConfigureAwait(false);
            await Registrar(endereco, autor, "alterada", mudancas, cancelamento).ConfigureAwait(false);
        }

        return ResultadoDoCadastro.Sucesso(alterada);
    }

    /// <summary>Apaga, ou inativa quando tem clientes ou é banco pronto (RN06).</summary>
    public async Task<ResultadoDoCadastro> Remover(string endereco, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.Obter(endereco, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return ResultadoDoCadastro.NaoEncontrada;
        }

        if (!atual.Pronta && !await clientes.TemClientes(endereco, cancelamento).ConfigureAwait(false))
        {
            await repositorio.Apagar(endereco, cancelamento).ConfigureAwait(false);
            await anomalias.Apagar(endereco, cancelamento).ConfigureAwait(false);
            await Registrar(endereco, autor, "apagada", [], cancelamento).ConfigureAwait(false);
            return ResultadoDoCadastro.Sucesso(atual, "apagada");
        }

        return await MudarSituacao(atual, Situacoes.Inativa, "inativada", autor, cancelamento).ConfigureAwait(false);
    }

    public async Task<ResultadoDoCadastro> Reativar(string endereco, string? autor, CancellationToken cancelamento) =>
        await repositorio.Obter(endereco, cancelamento).ConfigureAwait(false) is { } atual
            ? await MudarSituacao(atual, Situacoes.Ativa, "reativada", autor, cancelamento).ConfigureAwait(false)
            : ResultadoDoCadastro.NaoEncontrada;

    /// <summary>Fixa ou acelera o relógio (RN73, UC28).</summary>
    public async Task<ResultadoDoCadastro> MudarRelogio(string endereco, PedidoDeRelogio pedido, string? autor, CancellationToken cancelamento)
    {
        if (await repositorio.Obter(endereco, cancelamento).ConfigureAwait(false) is not { } atual)
        {
            return ResultadoDoCadastro.NaoEncontrada;
        }

        if ((pedido.FixadoEm is null) == (pedido.Fator is null) || pedido.Fator is <= 0 or > FatorMaximo)
        {
            return ResultadoDoCadastro.Invalida($"informe fixadoEm ou fator, maior que 0 e até {FatorMaximo}");
        }

        var real = relogio.GetUtcNow();
        var novo = pedido.FixadoEm is { } fixado ? new RelogioSimulado(fixado, real, 0) : new RelogioSimulado(atual.Relogio?.Agora(real) ?? real, real, pedido.Fator!.Value);
        return await TrocarRelogio(atual, novo, autor, cancelamento).ConfigureAwait(false);
    }

    public async Task<ResultadoDoCadastro> VoltarAoRelogioReal(string endereco, string? autor, CancellationToken cancelamento) =>
        await repositorio.Obter(endereco, cancelamento).ConfigureAwait(false) is { } atual
            ? await TrocarRelogio(atual, null, autor, cancelamento).ConfigureAwait(false)
            : ResultadoDoCadastro.NaoEncontrada;

    /// <summary>Monta uma instituição copiando o perfil (RN04).</summary>
    public Instituicao Nova(string endereco, string nome, PerfilDeComportamento perfil, bool pronta) => new(
        endereco,
        nome,
        ambiente.NovoOrganisationId(),
        perfil.Nome,
        nome,
        ValoresOficiais.PublicoPadrao,
        ValoresOficiais.Produtos,
        Situacoes.Ativa,
        pronta,
        perfil.Comportamento,
        perfil.Limites);

    public Task Registrar(string endereco, string? autor, string acao, IReadOnlyList<Mudanca> mudancas, CancellationToken cancelamento) =>
        repositorio.AcrescentarHistorico(
            new EntradaDeHistorico(endereco, relogio.GetUtcNow(), string.IsNullOrWhiteSpace(autor) ? AutorPadrao : autor, acao, mudancas),
            cancelamento);

    private async Task<ResultadoDoCadastro> TrocarRelogio(Instituicao atual, RelogioSimulado? novo, string? autor, CancellationToken cancelamento)
    {
        var alterada = atual with { Relogio = novo };
        await repositorio.Substituir(alterada, cancelamento).ConfigureAwait(false);
        await Registrar(atual.Endereco, autor, "relogio-alterado", [new Mudanca("relogio", atual.Relogio?.ToString(), novo?.ToString())], cancelamento).ConfigureAwait(false);
        return ResultadoDoCadastro.Sucesso(alterada);
    }

    private static JsonObject ComoJson(Instituicao instituicao) =>
        JsonSerializer.SerializeToNode(instituicao, JsonDasInstituicoes.Opcoes)!.AsObject();

    private async Task<ResultadoDoCadastro> MudarSituacao(Instituicao atual, string situacao, string acao, string? autor, CancellationToken cancelamento)
    {
        if (atual.Situacao == situacao)
        {
            return ResultadoDoCadastro.Sucesso(atual, acao);
        }

        var nova = atual with { Situacao = situacao };
        await repositorio.Substituir(nova, cancelamento).ConfigureAwait(false);
        await Registrar(atual.Endereco, autor, acao, [new Mudanca("situacao", atual.Situacao, situacao)], cancelamento).ConfigureAwait(false);
        return ResultadoDoCadastro.Sucesso(nova, acao);
    }

    private async Task<bool> NomeEmUso(string nome, string proprioEndereco, CancellationToken cancelamento)
    {
        var endereco = Endereco.Gerar(nome);
        var todas = await repositorio.Listar(cancelamento).ConfigureAwait(false);
        return todas.Any(i => i.Endereco != proprioEndereco && (i.Endereco == endereco || Endereco.Gerar(i.Nome) == endereco));
    }
}

/// <summary>Cria os bancos prontos que faltam, sem tocar nos existentes (RN02).</summary>
internal sealed class Semeadura(
    CadastroDeInstituicoes cadastro,
    IRepositorioDeInstituicoes repositorio,
    IAmbienteDasInstituicoes ambiente) : IHostedService
{
    private const string Autor = "sistema";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var (nome, perfil, qca, nomeReal) in BancosProntos.Todos)
        {
            var endereco = Endereco.Gerar(nome)!;
            if (await repositorio.Obter(endereco, cancellationToken).ConfigureAwait(false) is not null)
            {
                continue;
            }

            var modelo = CatalogoDePerfis.Obter(perfil)!;
            var instituicao = cadastro.Nova(endereco, NomeDoBanco.Aplicar(nome, ambiente.Marca, nomeReal), modelo with { Limites = modelo.Limites with { Qca = qca } }, pronta: true);
            if (await repositorio.Inserir(instituicao, cancellationToken).ConfigureAwait(false))
            {
                await cadastro.Registrar(endereco, Autor, "criada", [], cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Sem o cadastro de clientes da spec 003, nenhuma instituição tem clientes.</summary>
internal sealed class SemClientes : IConsultaDeClientes
{
    public Task<bool> TemClientes(string endereco, CancellationToken cancelamento) => Task.FromResult(false);
}

/// <summary>Sem o módulo de cenários, nenhuma anomalia está ligada (RN114).</summary>
internal sealed class SemAnomalias : IConsultaDeAnomalias, ILimpezaDeAnomalias
{
    public Task<IReadOnlyList<string>> Ligadas(string endereco, CancellationToken cancelamento) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task Apagar(string endereco, CancellationToken cancelamento) => Task.CompletedTask;
}
