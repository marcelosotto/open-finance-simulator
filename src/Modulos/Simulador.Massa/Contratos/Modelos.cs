using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Simulador.Massa.Contratos;

/// <summary>Modelo de como os dados de um cliente se parecem (RN55).</summary>
public sealed record PerfilDeDados(
    string Nome,
    int TransacoesMin,
    int TransacoesMax,
    decimal ValorMin,
    decimal ValorMax,
    decimal SaldoInicial,
    IReadOnlyList<string> TiposDeLancamento,
    IReadOnlyList<string> Produtos);

/// <summary>Como a massa se divide (RN56).</summary>
public sealed record Distribuicao(
    IReadOnlyDictionary<string, decimal> Bancos,
    IReadOnlyDictionary<string, decimal> Perfis,
    IReadOnlyDictionary<string, decimal>? Produtos = null,
    decimal PjPct = 0);

/// <summary>Distribuição salva para reuso (UC05).</summary>
public sealed record DistribuicaoNomeada(string Nome, Distribuicao Distribuicao);

/// <summary>Natureza do dado (RN50).</summary>
public static class Naturezas
{
    public const string Persistido = "persistido";
    public const string Virtual = "virtual";
}

/// <summary>Tipo de pessoa do cliente.</summary>
public static class TiposDePessoa
{
    public const string Pf = "PF";
    public const string Pj = "PJ";
}

/// <summary>Cliente simulado, identificado pelo documento (RN49).</summary>
public sealed record ClienteSimulado(string Documento, string Tipo, string Nome, string Marca, string Natureza, string? LoteId = null);

/// <summary>Produto do cliente em um banco (RN49).</summary>
public sealed record Recurso(
    string ResourceId,
    string Cliente,
    string Banco,
    string Tipo,
    string Produto,
    string Estado,
    string Perfil,
    decimal SaldoInicial,
    long SementeDeDados,
    DateTimeOffset CriadoEm,
    DateOnly InicioDoHistorico,
    string Marca,
    string Natureza,
    string? LoteId = null,
    IReadOnlyList<EstadoAnterior>? EstadosAnteriores = null,
    int Versao = 0);

/// <summary>Consentimento gerado, sempre <c>AUTHORISED</c> e sem receptora fixa (RN80).</summary>
public sealed record Consentimento(
    string ConsentId,
    string Cliente,
    string Banco,
    string Estado,
    IReadOnlyList<string> Permissoes,
    IReadOnlyList<string> Recursos,
    DateTimeOffset CriadoEm,
    string Marca,
    string Natureza,
    string? LoteId = null,
    DateTimeOffset? ExpiraEm = null,
    string? Receptora = null,
    string? RejeitadoPor = null,
    string? Razao = null,
    DateTimeOffset? AtualizadoEm = null,
    string? UsuarioLogado = null,
    int AprovacoesPendentes = 0,
    IReadOnlyList<Extensao>? Extensoes = null,
    int Versao = 0,
    bool AlcadaRecusada = false,
    IReadOnlyList<DecisaoDeAlcada>? Decisoes = null);

/// <summary>Uma aprovação ou recusa de múltipla alçada (RN99).</summary>
public sealed record DecisaoDeAlcada(string Autor, string Decisao, DateTimeOffset Em);

/// <summary>Uma renovação do consentimento (RN87).</summary>
public sealed record Extensao(
    DateTimeOffset? NovaValidade,
    DateTimeOffset? ValidadeAnterior,
    string Documento,
    string Rel,
    DateTimeOffset SolicitadaEm,
    string? Ip = null,
    string? UserAgent = null);

/// <summary>Um cliente com o que foi gerado para ele.</summary>
public sealed record MassaGerada(ClienteSimulado Cliente, IReadOnlyList<Consentimento> Consentimentos, IReadOnlyList<Recurso> Recursos);

/// <summary>Transação calculada, nunca gravada (RN51).</summary>
public sealed record Transacao(string Id, DateOnly Dia, int Posicao, decimal Valor, string Direcao, string Tipo, string Nome, string Estado = EstadosDeTransacao.Efetivada);

/// <summary>Estados oficiais de uma transação (RN44).</summary>
public static class EstadosDeTransacao
{
    public const string Futuro = "LANCAMENTO_FUTURO";
    public const string Processando = "TRANSACAO_PROCESSANDO";
    public const string Efetivada = "TRANSACAO_EFETIVADA";
}

/// <summary>Situações de um lote.</summary>
public static class SituacoesDeLote
{
    public const string EmAndamento = "em_andamento";
    public const string Concluido = "concluido";
    public const string Parcial = "parcial";
}

/// <summary>Massa criada de uma vez (UC07).</summary>
public sealed record Lote(
    string Id,
    int Quantidade,
    Distribuicao Distribuicao,
    int ConsentimentosPorCliente,
    IReadOnlyList<string>? Permissoes,
    long Semente,
    int VersaoDoGerador,
    int HistoricoMeses,
    long PosicaoInicial,
    DateTimeOffset Data,
    int Processados,
    string Situacao,
    string? Motivo,
    long Consentimentos,
    long Recursos,
    DateTimeOffset AtualizadoEm);

/// <summary>Definição do mundo virtual; os itens são calculados (RN52).</summary>
public sealed record MundoVirtual(int Total, long Semente, Distribuicao Distribuicao);

/// <summary>
/// Valor que um ajuste teve no intervalo, para a chave de paginação (RN37): na hora do banco e na hora real.
/// Sem a hora real (gravado antes dela), vale a hora gravada.
/// </summary>
public sealed record ValorAnterior(decimal Valor, DateTimeOffset De, DateTimeOffset Ate, DateTimeOffset? DeReal = null, DateTimeOffset? AteReal = null);

/// <summary>Estado anterior de um recurso, só lido de documentos antigos; não é mais gravado.</summary>
public sealed record EstadoAnterior(string Estado, DateTimeOffset Ate);

/// <summary>O que valia na emissão de uma chave, comparando só hora real (RN37).</summary>
public static class NoInstante
{
    public static decimal? Valor(Ajuste ajuste, DateTimeOffset emissaoReal)
    {
        ArgumentNullException.ThrowIfNull(ajuste);
        var feitoEm = ajuste.AtualizadoEmReal ?? ajuste.AtualizadoEm;
        return feitoEm is null || feitoEm <= emissaoReal
            ? ajuste.Valor
            : ajuste.ValoresAnteriores?.FirstOrDefault(v => (v.DeReal ?? v.De) <= emissaoReal && emissaoReal < (v.AteReal ?? v.Ate))?.Valor;
    }
}

/// <summary>Alteração persistida sobre um dado calculado (RN53).</summary>
public sealed record Ajuste(
    string Alvo,
    string Tipo,
    bool DoMundo,
    decimal? Valor = null,
    string? Estado = null,
    string? RejeitadoPor = null,
    string? RecursoId = null,
    string? Banco = null,
    string? LoteId = null,
    string? Razao = null,
    DateTimeOffset? ExpiraEm = null,
    DateTimeOffset? AtualizadoEm = null,
    IReadOnlyList<Extensao>? Extensoes = null,
    IReadOnlyList<ValorAnterior>? ValoresAnteriores = null,
    IReadOnlyList<EstadoAnterior>? EstadosAnteriores = null,
    int Versao = 0,
    DateTimeOffset? AtualizadoEmReal = null);

/// <summary>Quantidades de massa.</summary>
public sealed record ResumoDeMassa(long Clientes, long Consentimentos, long Recursos, long Ajustes);

/// <summary>O que já existe na massa persistida, para Primeiros passos (UI18).</summary>
public sealed record ContagemDaMassa(long Clientes, long Consentimentos, long Recursos, long Lotes);

/// <summary>Um campo alterado em uma operação da massa.</summary>
public sealed record MudancaDaMassa(string Campo, string? Antes, string? Depois);

/// <summary>Linha do histórico (RN71).</summary>
public sealed record EventoDaMassa(DateTimeOffset Data, string Autor, string Acao, IReadOnlyList<MudancaDaMassa> Mudancas);

/// <summary>Grava a massa.</summary>
public interface IRepositorioDeMassa
{
    Task Preparar(CancellationToken cancelamento);

    Task<IReadOnlyList<PerfilDeDados>> ListarPerfis(CancellationToken cancelamento);

    Task<PerfilDeDados?> ObterPerfil(string nome, CancellationToken cancelamento);

    Task SalvarPerfil(PerfilDeDados perfil, CancellationToken cancelamento);

    Task ApagarPerfil(string nome, CancellationToken cancelamento);

    Task<bool> PerfilEmUso(string nome, CancellationToken cancelamento);

    Task<IReadOnlyList<DistribuicaoNomeada>> ListarDistribuicoes(CancellationToken cancelamento);

    Task SalvarDistribuicao(DistribuicaoNomeada distribuicao, CancellationToken cancelamento);

    Task ApagarDistribuicao(string nome, CancellationToken cancelamento);

    /// <summary>Reserva posições de identidade e devolve a primeira.</summary>
    Task<long> ReservarPosicoes(int quantidade, CancellationToken cancelamento);

    Task DevolverPosicoes(int quantidade, CancellationToken cancelamento);

    Task InserirLote(Lote lote, CancellationToken cancelamento);

    Task AtualizarLote(Lote lote, CancellationToken cancelamento);

    Task<Lote?> ObterLote(string id, CancellationToken cancelamento);

    Task<IReadOnlyList<Lote>> ListarLotes(CancellationToken cancelamento);

    Task SolicitarCancelamento(string id, CancellationToken cancelamento);

    Task<bool> CancelamentoSolicitado(string id, CancellationToken cancelamento);

    Task GravarMassa(IReadOnlyList<MassaGerada> massa, CancellationToken cancelamento);

    Task SalvarConsentimento(Consentimento consentimento, CancellationToken cancelamento);

    /// <summary>Grava só se a versão guardada for a esperada; ausente vale 0 (RN82).</summary>
    Task<bool> SalvarConsentimentoSeVersao(Consentimento consentimento, int versaoEsperada, CancellationToken cancelamento);

    Task SalvarRecurso(Recurso recurso, CancellationToken cancelamento);

    /// <summary>Falso se outra chamada gravou o recurso desde a leitura.</summary>
    Task<bool> SalvarRecursoSeVersao(Recurso recurso, int versaoEsperada, CancellationToken cancelamento);

    Task<ClienteSimulado?> ObterCliente(string documento, CancellationToken cancelamento);

    Task<Consentimento?> ObterConsentimento(string consentId, CancellationToken cancelamento);

    Task<Recurso?> ObterRecurso(string resourceId, CancellationToken cancelamento);

    Task<IReadOnlyList<ClienteSimulado>> ListarClientesDoLote(string id, int inicio, int quantidade, CancellationToken cancelamento);

    Task<IReadOnlyList<Recurso>> ListarRecursosDoCliente(string documento, CancellationToken cancelamento);

    Task<IReadOnlyList<Consentimento>> ListarConsentimentosDoCliente(string documento, CancellationToken cancelamento);

    Task<bool> TemMassaNoBanco(string banco, CancellationToken cancelamento);

    /// <summary>Clientes persistidos com produto no banco, ou todos sem banco, em ordem de documento (UI19).</summary>
    Task<IReadOnlyList<ClienteSimulado>> ListarClientes(string? banco, int inicio, int quantidade, CancellationToken cancelamento);

    /// <summary>Contagem do que está persistido (UI18).</summary>
    Task<ContagemDaMassa> Contar(CancellationToken cancelamento);

    Task<ResumoDeMassa> ContarDoLote(string id, CancellationToken cancelamento);

    /// <summary>Clientes contados são só os que ficam sem produto em outro banco.</summary>
    Task<ResumoDeMassa> ContarDoBanco(string banco, CancellationToken cancelamento);

    Task<ResumoDeMassa> ContarTudo(CancellationToken cancelamento);

    Task ApagarLote(string id, CancellationToken cancelamento);

    Task ApagarDoBanco(string banco, CancellationToken cancelamento);

    /// <summary>Apaga clientes, consentimentos, recursos, ajustes e lotes, e zera as posições.</summary>
    Task ApagarTudo(CancellationToken cancelamento);

    Task SalvarAjuste(Ajuste ajuste, CancellationToken cancelamento);

    /// <summary>Grava só se a versão guardada for a esperada; ausente vale 0 (RN82).</summary>
    Task<bool> SalvarAjusteSeVersao(Ajuste ajuste, int versaoEsperada, CancellationToken cancelamento);

    Task<Ajuste?> ObterAjuste(string alvo, CancellationToken cancelamento);

    Task<IReadOnlyList<Ajuste>> ListarAjustesDoRecurso(string recursoId, CancellationToken cancelamento);

    Task<long> ContarAjustesDoMundo(CancellationToken cancelamento);

    Task ApagarAjustesDoMundo(CancellationToken cancelamento);

    Task<MundoVirtual?> ObterMundo(CancellationToken cancelamento);

    Task SalvarMundo(MundoVirtual mundo, CancellationToken cancelamento);
}

/// <summary>Leitura da massa persistida e virtual, já com ajustes, para as specs 004 e 005.</summary>
public interface IConsultaDeMassa
{
    Task<MassaGerada?> ObterCliente(string documento, CancellationToken cancelamento);

    Task<Consentimento?> ObterConsentimento(string consentId, CancellationToken cancelamento);

    Task<Recurso?> ObterRecurso(string resourceId, CancellationToken cancelamento);

    /// <summary>Transações do período, ou nulo se o recurso não existe.</summary>
    Task<IReadOnlyList<Transacao>?> Transacoes(string resourceId, DateOnly de, DateOnly ate, CancellationToken cancelamento);

    /// <summary>Saldo calculado até hoje, ou nulo se o recurso não existe (RN46).</summary>
    /// <summary>Como <c>Transacoes</c>, no instante pedido: ajustes posteriores não aparecem (RN37).</summary>
    /// <summary>As transações até o instante do banco, com os ajustes feitos até o corte em hora real; sem corte, os ajustes atuais (RN37).</summary>
    Task<IReadOnlyList<Transacao>?> TransacoesEm(string resourceId, DateOnly de, DateOnly ate, DateTimeOffset instante, DateTimeOffset? corte, CancellationToken cancelamento);

    Task<decimal?> Saldo(string resourceId, CancellationToken cancelamento);

    Task<bool> BancoTemClientes(string banco, CancellationToken cancelamento);
}

/// <summary>Mudanças da spec 004 na massa; em dado virtual, viram ajuste (RN53).</summary>
public interface IEscritaDeMassa
{
    Task<string?> NovoConsentId(CancellationToken cancelamento);

    Task InserirConsentimento(Consentimento consentimento, CancellationToken cancelamento);

    /// <summary>Falso se outra chamada gravou desde a leitura (RN82).</summary>
    Task<bool> AtualizarConsentimento(Consentimento consentimento, CancellationToken cancelamento);

    /// <summary>Falso se outra chamada gravou desde a leitura.</summary>
    Task<bool> MudarEstadoDoRecurso(Recurso recurso, string estado, CancellationToken cancelamento);

    /// <summary>Nulo se o cliente não existe, é virtual ou acabou o saldo de documentos.</summary>
    Task<Recurso?> AdicionarProduto(string documento, string banco, string produto, string estado, CancellationToken cancelamento);
}

/// <summary>O que o módulo precisa do ambiente; o host implementa (ADR-007).</summary>
public interface IAmbienteDaMassa
{
    string Marca { get; }

    string Identificador(string natureza, string corpo);

    string ConsentId(string natureza, string corpo);

    string MarcarTexto(string texto);

    /// <summary>Produtos oferecidos por cada instituição cadastrada (RN08).</summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ProdutosDosBancos(CancellationToken cancelamento);

    /// <summary>A hora do banco, para escritas que entram em resposta (RN73).</summary>
    Task<DateTimeOffset> AgoraNoBanco(string banco, CancellationToken cancelamento);

    Task RegistrarHistorico(EventoDaMassa evento, CancellationToken cancelamento);

    Task<IReadOnlyList<EventoDaMassa>> LerHistorico(CancellationToken cancelamento);

    Task<int> ContarInstituicoesCriadas(IReadOnlySet<string> exceto, CancellationToken cancelamento);

    Task ApagarInstituicoesCriadas(IReadOnlySet<string> exceto, string autor, CancellationToken cancelamento);
}

/// <summary>Formato JSON da administração e da gravação.</summary>
public static class JsonDaMassa
{
    public static JsonSerializerOptions Opcoes { get; } = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
}
