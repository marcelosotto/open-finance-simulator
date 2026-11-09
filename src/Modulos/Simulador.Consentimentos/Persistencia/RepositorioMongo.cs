using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Simulador.Consentimentos.Contratos;

namespace Simulador.Consentimentos.Persistencia;

/// <summary>Grava as receptoras em JSON, com o usuário ao lado para consulta.</summary>
internal sealed class RepositorioMongo : IRepositorioDeReceptoras, IDisposable
{
    private const string Id = "_id";
    private const string Conteudo = "doc";
    private const string Usuario = "usuario";
    private const int ChaveDuplicada = 11000;

    private static readonly FilterDefinitionBuilder<BsonDocument> Filtro = Builders<BsonDocument>.Filter;

    private readonly MongoClient _cliente;
    private readonly IMongoCollection<BsonDocument> _receptoras;

    public RepositorioMongo(string conexao, string nomeDaBase)
    {
        _cliente = new MongoClient(conexao);
        _receptoras = _cliente.GetDatabase(nomeDaBase).GetCollection<BsonDocument>("receptoras");
    }

    public Task Preparar(CancellationToken cancelamento) =>
        _receptoras.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending(Usuario), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancelamento);

    public async Task<bool> Inserir(Receptora receptora, CancellationToken cancelamento)
    {
        try
        {
            var documento = new BsonDocument { { Id, receptora.OrganisationId }, { Usuario, receptora.Usuario }, { Conteudo, JsonSerializer.Serialize(receptora, JsonDosConsentimentos.Opcoes) } };
            await _receptoras.InsertOneAsync(documento, cancellationToken: cancelamento).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException erro) when (erro.WriteError.Code == ChaveDuplicada)
        {
            return false;
        }
    }

    public Task<Receptora?> Obter(string organisationId, CancellationToken cancelamento) => Um(Filtro.Eq(Id, organisationId), cancelamento);

    public Task<Receptora?> ObterPorUsuario(string usuario, CancellationToken cancelamento) => Um(Filtro.Eq(Usuario, usuario), cancelamento);

    public async Task<IReadOnlyList<Receptora>> Listar(CancellationToken cancelamento)
    {
        var documentos = await _receptoras.Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending(Id)).ToListAsync(cancelamento).ConfigureAwait(false);
        return [.. documentos.Select(Ler)];
    }

    public void Dispose() => _cliente.Dispose();

    private static Receptora Ler(BsonDocument documento) => JsonSerializer.Deserialize<Receptora>(documento[Conteudo].AsString, JsonDosConsentimentos.Opcoes)!;

    private async Task<Receptora?> Um(FilterDefinition<BsonDocument> filtro, CancellationToken cancelamento)
    {
        var documento = await _receptoras.Find(filtro).FirstOrDefaultAsync(cancelamento).ConfigureAwait(false);
        return documento is null ? null : Ler(documento);
    }
}
