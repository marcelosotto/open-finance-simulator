using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Simulador.Massa.Contratos;

namespace Simulador.Massa.Persistencia;

/// <summary>Grava a massa em JSON, com os campos consultados ao lado (research R08).</summary>
internal sealed class RepositorioMongo : IRepositorioDeMassa, IDisposable
{
    private const string Id = "_id";
    private const string Conteudo = "doc";
    private const string CampoLote = "loteId";
    private const string CampoBanco = "banco";
    private const string CampoCliente = "cliente";
    private const string CampoPerfil = "perfil";
    private const string CampoRecurso = "recursoId";
    private const string CampoMundo = "doMundo";
    private const string CampoCancelar = "cancelar";
    private const string CampoValor = "valor";
    private const string ChavePosicoes = "posicoes";
    private const string ChaveMundo = "mundo";

    private static readonly FilterDefinitionBuilder<BsonDocument> Filtro = Builders<BsonDocument>.Filter;
    private static readonly FilterDefinition<BsonDocument> Tudo = FilterDefinition<BsonDocument>.Empty;
    private static readonly ReplaceOptions Upsert = new() { IsUpsert = true };

    private readonly MongoClient _cliente;
    private readonly IMongoCollection<BsonDocument> _perfis;
    private readonly IMongoCollection<BsonDocument> _distribuicoes;
    private readonly IMongoCollection<BsonDocument> _clientes;
    private readonly IMongoCollection<BsonDocument> _consentimentos;
    private readonly IMongoCollection<BsonDocument> _recursos;
    private readonly IMongoCollection<BsonDocument> _mundo;
    private readonly IMongoCollection<BsonDocument> _ajustes;
    private readonly IMongoCollection<BsonDocument> _lotes;
    private readonly IMongoCollection<BsonDocument> _contadores;

    public RepositorioMongo(string conexao, string nomeDaBase)
    {
        _cliente = new MongoClient(conexao);
        var banco = _cliente.GetDatabase(nomeDaBase);
        _perfis = banco.GetCollection<BsonDocument>("perfis_dados");
        _distribuicoes = banco.GetCollection<BsonDocument>("distribuicoes");
        _clientes = banco.GetCollection<BsonDocument>("clientes");
        _consentimentos = banco.GetCollection<BsonDocument>("consentimentos");
        _recursos = banco.GetCollection<BsonDocument>("recursos");
        _mundo = banco.GetCollection<BsonDocument>("mundo_virtual");
        _ajustes = banco.GetCollection<BsonDocument>("ajustes");
        _lotes = banco.GetCollection<BsonDocument>("lotes");
        _contadores = banco.GetCollection<BsonDocument>("contadores");
    }

    public async Task Preparar(CancellationToken cancelamento)
    {
        foreach (var colecao in new[] { _clientes, _consentimentos, _recursos, _ajustes })
        {
            await Indice(colecao, CampoLote, cancelamento).ConfigureAwait(false);
        }

        foreach (var colecao in new[] { _consentimentos, _recursos, _ajustes })
        {
            await Indice(colecao, CampoBanco, cancelamento).ConfigureAwait(false);
        }

        await Indice(_consentimentos, CampoCliente, cancelamento).ConfigureAwait(false);
        await Indice(_recursos, CampoCliente, cancelamento).ConfigureAwait(false);
        await Indice(_recursos, CampoPerfil, cancelamento).ConfigureAwait(false);
        await Indice(_ajustes, CampoRecurso, cancelamento).ConfigureAwait(false);
        await Indice(_ajustes, CampoMundo, cancelamento).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<PerfilDeDados>> ListarPerfis(CancellationToken cancelamento) => Listar<PerfilDeDados>(_perfis, Tudo, cancelamento);

    public Task<PerfilDeDados?> ObterPerfil(string nome, CancellationToken cancelamento) => Obter<PerfilDeDados>(_perfis, nome, cancelamento);

    public Task SalvarPerfil(PerfilDeDados perfil, CancellationToken cancelamento) => Salvar(_perfis, perfil.Nome, perfil, cancelamento);

    public Task ApagarPerfil(string nome, CancellationToken cancelamento) => _perfis.DeleteOneAsync(PorId(nome), cancelamento);

    public async Task<bool> PerfilEmUso(string nome, CancellationToken cancelamento) =>
        await _recursos.Find(Filtro.Eq(CampoPerfil, nome)).AnyAsync(cancelamento).ConfigureAwait(false);

    public Task<IReadOnlyList<DistribuicaoNomeada>> ListarDistribuicoes(CancellationToken cancelamento) => Listar<DistribuicaoNomeada>(_distribuicoes, Tudo, cancelamento);

    public Task SalvarDistribuicao(DistribuicaoNomeada distribuicao, CancellationToken cancelamento) => Salvar(_distribuicoes, distribuicao.Nome, distribuicao, cancelamento);

    public Task ApagarDistribuicao(string nome, CancellationToken cancelamento) => _distribuicoes.DeleteOneAsync(PorId(nome), cancelamento);

    public async Task<long> ReservarPosicoes(int quantidade, CancellationToken cancelamento)
    {
        var anterior = await _contadores.FindOneAndUpdateAsync(
            PorId(ChavePosicoes),
            Builders<BsonDocument>.Update.Inc(CampoValor, (long)quantidade),
            new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.Before },
            cancelamento).ConfigureAwait(false);
        return anterior is null ? 0 : anterior[CampoValor].ToInt64();
    }

    public Task DevolverPosicoes(int quantidade, CancellationToken cancelamento) =>
        _contadores.UpdateOneAsync(PorId(ChavePosicoes), Builders<BsonDocument>.Update.Inc(CampoValor, -(long)quantidade), cancellationToken: cancelamento);

    public Task InserirLote(Lote lote, CancellationToken cancelamento) =>
        _lotes.InsertOneAsync(Documento(lote.Id, lote), cancellationToken: cancelamento);

    public Task AtualizarLote(Lote lote, CancellationToken cancelamento) =>
        _lotes.UpdateOneAsync(PorId(lote.Id), Builders<BsonDocument>.Update.Set(Conteudo, Json(lote)), cancellationToken: cancelamento);

    public Task<Lote?> ObterLote(string id, CancellationToken cancelamento) => Obter<Lote>(_lotes, id, cancelamento);

    public Task<IReadOnlyList<Lote>> ListarLotes(CancellationToken cancelamento) => Listar<Lote>(_lotes, Tudo, cancelamento);

    public Task SolicitarCancelamento(string id, CancellationToken cancelamento) =>
        _lotes.UpdateOneAsync(PorId(id), Builders<BsonDocument>.Update.Set(CampoCancelar, true), cancellationToken: cancelamento);

    public async Task<bool> CancelamentoSolicitado(string id, CancellationToken cancelamento) =>
        await _lotes.Find(Filtro.And(PorId(id), Filtro.Eq(CampoCancelar, true))).AnyAsync(cancelamento).ConfigureAwait(false);

    public async Task GravarMassa(IReadOnlyList<MassaGerada> massa, CancellationToken cancelamento)
    {
        await Inserir(_clientes, massa.Select(m => Documento(m.Cliente.Documento, m.Cliente, (CampoLote, m.Cliente.LoteId))), cancelamento).ConfigureAwait(false);
        await Inserir(_consentimentos, massa.SelectMany(m => m.Consentimentos).Select(c => Documento(c.ConsentId, c, (CampoLote, c.LoteId), (CampoBanco, c.Banco), (CampoCliente, c.Cliente))), cancelamento).ConfigureAwait(false);
        await Inserir(_recursos, massa.SelectMany(m => m.Recursos).Select(r => Documento(r.ResourceId, r, (CampoLote, r.LoteId), (CampoBanco, r.Banco), (CampoCliente, r.Cliente), (CampoPerfil, r.Perfil))), cancelamento).ConfigureAwait(false);
    }

    public Task<ClienteSimulado?> ObterCliente(string documento, CancellationToken cancelamento) => Obter<ClienteSimulado>(_clientes, documento, cancelamento);

    public Task<Consentimento?> ObterConsentimento(string consentId, CancellationToken cancelamento) => Obter<Consentimento>(_consentimentos, consentId, cancelamento);

    public Task<Recurso?> ObterRecurso(string resourceId, CancellationToken cancelamento) => Obter<Recurso>(_recursos, resourceId, cancelamento);

    public async Task<IReadOnlyList<ClienteSimulado>> ListarClientesDoLote(string id, int inicio, int quantidade, CancellationToken cancelamento)
    {
        var documentos = await _clientes.Find(Filtro.Eq(CampoLote, id)).Sort(Builders<BsonDocument>.Sort.Ascending(Id)).Skip(inicio).Limit(quantidade).ToListAsync(cancelamento).ConfigureAwait(false);
        return [.. documentos.Select(Ler<ClienteSimulado>)];
    }

    public Task<IReadOnlyList<Recurso>> ListarRecursosDoCliente(string documento, CancellationToken cancelamento) => Listar<Recurso>(_recursos, Filtro.Eq(CampoCliente, documento), cancelamento);

    public Task<IReadOnlyList<Consentimento>> ListarConsentimentosDoCliente(string documento, CancellationToken cancelamento) => Listar<Consentimento>(_consentimentos, Filtro.Eq(CampoCliente, documento), cancelamento);

    public async Task<bool> TemMassaNoBanco(string banco, CancellationToken cancelamento) =>
        await _recursos.Find(Filtro.Eq(CampoBanco, banco)).AnyAsync(cancelamento).ConfigureAwait(false)
        || await _consentimentos.Find(Filtro.Eq(CampoBanco, banco)).AnyAsync(cancelamento).ConfigureAwait(false);

    public async Task<ResumoDeMassa> ContarDoLote(string id, CancellationToken cancelamento)
    {
        var filtro = Filtro.Eq(CampoLote, id);
        return new ResumoDeMassa(
            await _clientes.CountDocumentsAsync(filtro, cancellationToken: cancelamento).ConfigureAwait(false),
            await _consentimentos.CountDocumentsAsync(filtro, cancellationToken: cancelamento).ConfigureAwait(false),
            await _recursos.CountDocumentsAsync(filtro, cancellationToken: cancelamento).ConfigureAwait(false),
            await _ajustes.CountDocumentsAsync(filtro, cancellationToken: cancelamento).ConfigureAwait(false));
    }

    public async Task<ResumoDeMassa> ContarDoBanco(string banco, CancellationToken cancelamento)
    {
        var filtro = Filtro.Eq(CampoBanco, banco);
        return new ResumoDeMassa(
            (await ClientesSoDoBanco(banco, cancelamento).ConfigureAwait(false)).Count,
            await _consentimentos.CountDocumentsAsync(filtro, cancellationToken: cancelamento).ConfigureAwait(false),
            await _recursos.CountDocumentsAsync(filtro, cancellationToken: cancelamento).ConfigureAwait(false),
            await _ajustes.CountDocumentsAsync(AjustesDoBanco(banco), cancellationToken: cancelamento).ConfigureAwait(false));
    }

    public async Task<ResumoDeMassa> ContarTudo(CancellationToken cancelamento) => new(
        await _clientes.CountDocumentsAsync(Tudo, cancellationToken: cancelamento).ConfigureAwait(false),
        await _consentimentos.CountDocumentsAsync(Tudo, cancellationToken: cancelamento).ConfigureAwait(false),
        await _recursos.CountDocumentsAsync(Tudo, cancellationToken: cancelamento).ConfigureAwait(false),
        await _ajustes.CountDocumentsAsync(Tudo, cancellationToken: cancelamento).ConfigureAwait(false));

    public async Task ApagarLote(string id, CancellationToken cancelamento)
    {
        var filtro = Filtro.Eq(CampoLote, id);
        await _ajustes.DeleteManyAsync(filtro, cancelamento).ConfigureAwait(false);
        await _recursos.DeleteManyAsync(filtro, cancelamento).ConfigureAwait(false);
        await _consentimentos.DeleteManyAsync(filtro, cancelamento).ConfigureAwait(false);
        await _clientes.DeleteManyAsync(filtro, cancelamento).ConfigureAwait(false);
        await _lotes.DeleteOneAsync(PorId(id), cancelamento).ConfigureAwait(false);
    }

    public async Task ApagarDoBanco(string banco, CancellationToken cancelamento)
    {
        var orfaos = await ClientesSoDoBanco(banco, cancelamento).ConfigureAwait(false);
        var filtro = Filtro.Eq(CampoBanco, banco);
        await _ajustes.DeleteManyAsync(AjustesDoBanco(banco), cancelamento).ConfigureAwait(false);
        await _recursos.DeleteManyAsync(filtro, cancelamento).ConfigureAwait(false);
        await _consentimentos.DeleteManyAsync(filtro, cancelamento).ConfigureAwait(false);
        foreach (var bloco in orfaos.Chunk(1_000))
        {
            await _clientes.DeleteManyAsync(Filtro.In(Id, bloco), cancelamento).ConfigureAwait(false);
        }
    }

    public async Task ApagarTudo(CancellationToken cancelamento)
    {
        foreach (var colecao in new[] { _ajustes, _recursos, _consentimentos, _clientes, _lotes, _contadores })
        {
            await colecao.DeleteManyAsync(Tudo, cancelamento).ConfigureAwait(false);
        }
    }

    public Task SalvarAjuste(Ajuste ajuste, CancellationToken cancelamento) => _ajustes.ReplaceOneAsync(
        PorId(ajuste.Alvo),
        Documento(ajuste.Alvo, ajuste, (CampoLote, ajuste.LoteId), (CampoBanco, ajuste.Banco), (CampoRecurso, ajuste.RecursoId), (CampoMundo, ajuste.DoMundo)),
        Upsert,
        cancelamento);

    public Task<Ajuste?> ObterAjuste(string alvo, CancellationToken cancelamento) => Obter<Ajuste>(_ajustes, alvo, cancelamento);

    public Task<IReadOnlyList<Ajuste>> ListarAjustesDoRecurso(string recursoId, CancellationToken cancelamento) => Listar<Ajuste>(_ajustes, Filtro.Eq(CampoRecurso, recursoId), cancelamento);

    public Task<long> ContarAjustesDoMundo(CancellationToken cancelamento) => _ajustes.CountDocumentsAsync(Filtro.Eq(CampoMundo, true), cancellationToken: cancelamento);

    public Task ApagarAjustesDoMundo(CancellationToken cancelamento) => _ajustes.DeleteManyAsync(Filtro.Eq(CampoMundo, true), cancelamento);

    public Task<MundoVirtual?> ObterMundo(CancellationToken cancelamento) => Obter<MundoVirtual>(_mundo, ChaveMundo, cancelamento);

    public Task SalvarMundo(MundoVirtual mundo, CancellationToken cancelamento) => Salvar(_mundo, ChaveMundo, mundo, cancelamento);

    public void Dispose() => _cliente.Dispose();

    private static FilterDefinition<BsonDocument> PorId(string id) => Filtro.Eq(Id, id);

    private static FilterDefinition<BsonDocument> AjustesDoBanco(string banco) => Filtro.And(Filtro.Eq(CampoBanco, banco), Filtro.Eq(CampoMundo, false));

    private static string Json<T>(T valor) => JsonSerializer.Serialize(valor, JsonDaMassa.Opcoes);

    private static BsonDocument Documento<T>(string id, T valor, params (string Campo, BsonValue? Valor)[] campos)
    {
        var documento = new BsonDocument { { Id, id }, { Conteudo, Json(valor) } };
        foreach (var (campo, valorDoCampo) in campos)
        {
            documento[campo] = valorDoCampo ?? BsonNull.Value;
        }

        return documento;
    }

    private static T Ler<T>(BsonDocument documento) => JsonSerializer.Deserialize<T>(documento[Conteudo].AsString, JsonDaMassa.Opcoes)!;

    private static Task<string> Indice(IMongoCollection<BsonDocument> colecao, string campo, CancellationToken cancelamento) =>
        colecao.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(campo)), cancellationToken: cancelamento);

    private static async Task Inserir(IMongoCollection<BsonDocument> colecao, IEnumerable<BsonDocument> documentos, CancellationToken cancelamento)
    {
        var lista = documentos.ToList();
        if (lista.Count > 0)
        {
            await colecao.InsertManyAsync(lista, cancellationToken: cancelamento).ConfigureAwait(false);
        }
    }

    private static Task<ReplaceOneResult> Salvar<T>(IMongoCollection<BsonDocument> colecao, string id, T valor, CancellationToken cancelamento) =>
        colecao.ReplaceOneAsync(PorId(id), Documento(id, valor), Upsert, cancelamento);

    private static async Task<T?> Obter<T>(IMongoCollection<BsonDocument> colecao, string id, CancellationToken cancelamento)
        where T : class
    {
        var documento = await colecao.Find(PorId(id)).FirstOrDefaultAsync(cancelamento).ConfigureAwait(false);
        return documento is null ? null : Ler<T>(documento);
    }

    private static async Task<IReadOnlyList<T>> Listar<T>(IMongoCollection<BsonDocument> colecao, FilterDefinition<BsonDocument> filtro, CancellationToken cancelamento)
    {
        var documentos = await colecao.Find(filtro).Sort(Builders<BsonDocument>.Sort.Ascending(Id)).ToListAsync(cancelamento).ConfigureAwait(false);
        return [.. documentos.Select(Ler<T>)];
    }

    /// <summary>Clientes com produto ou consentimento no banco e em nenhum outro (UC27).</summary>
    private async Task<HashSet<string>> ClientesSoDoBanco(string banco, CancellationToken cancelamento)
    {
        var doBanco = await Clientes(Filtro.Eq(CampoBanco, banco), cancelamento).ConfigureAwait(false);
        doBanco.ExceptWith(await Clientes(Filtro.Ne(CampoBanco, banco), cancelamento).ConfigureAwait(false));
        return doBanco;
    }

    private async Task<HashSet<string>> Clientes(FilterDefinition<BsonDocument> filtro, CancellationToken cancelamento)
    {
        var clientes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var colecao in new[] { _recursos, _consentimentos })
        {
            using var cursor = await colecao.DistinctAsync<string>(CampoCliente, filtro, cancellationToken: cancelamento).ConfigureAwait(false);
            while (await cursor.MoveNextAsync(cancelamento).ConfigureAwait(false))
            {
                clientes.UnionWith(cursor.Current);
            }
        }

        return clientes;
    }
}
