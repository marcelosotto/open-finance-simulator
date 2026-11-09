using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Simulador.Instituicoes.Contratos;

namespace Simulador.Instituicoes.Persistencia;

/// <summary>Grava em <c>instituicoes</c> e <c>historico</c>, em JSON (research R03 e R07).</summary>
internal sealed class RepositorioMongo : IRepositorioDeInstituicoes, IDisposable
{
    private const string Id = "_id";
    private const string Conteudo = "doc";
    private const string CampoInstituicao = "instituicao";

    private readonly MongoClient _cliente;
    private readonly IMongoCollection<BsonDocument> _instituicoes;
    private readonly IMongoCollection<BsonDocument> _historico;

    public RepositorioMongo(string conexao, string nomeDaBase)
    {
        _cliente = new MongoClient(conexao);
        var banco = _cliente.GetDatabase(nomeDaBase);
        _instituicoes = banco.GetCollection<BsonDocument>("instituicoes");
        _historico = banco.GetCollection<BsonDocument>("historico");
    }

    public async Task<Instituicao?> Obter(string endereco, CancellationToken cancelamento)
    {
        var documento = await _instituicoes.Find(PorEndereco(endereco)).FirstOrDefaultAsync(cancelamento).ConfigureAwait(false);
        return documento is null ? null : Ler<Instituicao>(documento);
    }

    public async Task<IReadOnlyList<Instituicao>> Listar(CancellationToken cancelamento)
    {
        var documentos = await _instituicoes.Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending(Id))
            .ToListAsync(cancelamento).ConfigureAwait(false);
        return [.. documentos.Select(Ler<Instituicao>)];
    }

    public async Task<bool> Inserir(Instituicao instituicao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(instituicao);
        try
        {
            await _instituicoes.InsertOneAsync(Documento(instituicao), cancellationToken: cancelamento).ConfigureAwait(false);
            return true;
        }
        catch (MongoWriteException erro) when (erro.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public Task Substituir(Instituicao instituicao, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(instituicao);
        return _instituicoes.ReplaceOneAsync(PorEndereco(instituicao.Endereco), Documento(instituicao), cancellationToken: cancelamento);
    }

    public Task Apagar(string endereco, CancellationToken cancelamento) =>
        _instituicoes.DeleteOneAsync(PorEndereco(endereco), cancelamento);

    public Task AcrescentarHistorico(EntradaDeHistorico entrada, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(entrada);
        var documento = new BsonDocument
        {
            { CampoInstituicao, entrada.Instituicao },
            { Conteudo, JsonSerializer.Serialize(entrada, JsonDasInstituicoes.Opcoes) },
        };
        return _historico.InsertOneAsync(documento, cancellationToken: cancelamento);
    }

    public async Task<IReadOnlyList<EntradaDeHistorico>> LerHistorico(string endereco, CancellationToken cancelamento)
    {
        var documentos = await _historico.Find(Builders<BsonDocument>.Filter.Eq(CampoInstituicao, endereco))
            .Sort(Builders<BsonDocument>.Sort.Descending(Id))
            .ToListAsync(cancelamento).ConfigureAwait(false);
        return [.. documentos.Select(Ler<EntradaDeHistorico>)];
    }

    public void Dispose() => _cliente.Dispose();

    private static FilterDefinition<BsonDocument> PorEndereco(string endereco) => Builders<BsonDocument>.Filter.Eq(Id, endereco);

    private static BsonDocument Documento(Instituicao instituicao) => new()
    {
        { Id, instituicao.Endereco },
        { Conteudo, JsonSerializer.Serialize(instituicao, JsonDasInstituicoes.Opcoes) },
    };

    private static T Ler<T>(BsonDocument documento) =>
        JsonSerializer.Deserialize<T>(documento[Conteudo].AsString, JsonDasInstituicoes.Opcoes)!;
}
