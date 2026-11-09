using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Simulador.Cenarios.Contratos;

namespace Simulador.Cenarios.Persistencia;

/// <summary>Grava regras e pacotes em <c>cenarios</c>, em JSON.</summary>
internal sealed class RepositorioMongo : IRepositorioDeCenarios, IDisposable
{
    private const string Id = "_id";
    private const string Conteudo = "doc";
    private const string PrefixoDeRegra = "regra:";
    private const string PrefixoDePacote = "pacote:";

    private readonly MongoClient _cliente;
    private readonly IMongoCollection<BsonDocument> _cenarios;

    public RepositorioMongo(string conexao, string nomeDaBase)
    {
        _cliente = new MongoClient(conexao);
        _cenarios = _cliente.GetDatabase(nomeDaBase).GetCollection<BsonDocument>("cenarios");
    }

    public Task<IReadOnlyList<RegraDeCenario>> ListarRegras(CancellationToken cancelamento) => Listar<RegraDeCenario>(PrefixoDeRegra, cancelamento);

    public Task<RegraDeCenario?> ObterRegra(string id, CancellationToken cancelamento) => Obter<RegraDeCenario>(PrefixoDeRegra + id, cancelamento);

    public Task SalvarRegra(RegraDeCenario regra, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(regra);
        return Salvar(PrefixoDeRegra + regra.Id, regra, cancelamento);
    }

    public Task ApagarRegra(string id, CancellationToken cancelamento) => Apagar(PrefixoDeRegra + id, cancelamento);

    public Task<IReadOnlyList<PacoteDeCenario>> ListarPacotes(CancellationToken cancelamento) => Listar<PacoteDeCenario>(PrefixoDePacote, cancelamento);

    public Task<PacoteDeCenario?> ObterPacote(string nome, CancellationToken cancelamento) => Obter<PacoteDeCenario>(PrefixoDePacote + nome, cancelamento);

    public Task SalvarPacote(PacoteDeCenario pacote, CancellationToken cancelamento)
    {
        ArgumentNullException.ThrowIfNull(pacote);
        return Salvar(PrefixoDePacote + pacote.Nome, pacote, cancelamento);
    }

    public Task ApagarPacote(string nome, CancellationToken cancelamento) => Apagar(PrefixoDePacote + nome, cancelamento);

    public void Dispose() => _cliente.Dispose();

    private static FilterDefinition<BsonDocument> PorId(string id) => Builders<BsonDocument>.Filter.Eq(Id, id);

    private static T Ler<T>(BsonDocument documento) => JsonSerializer.Deserialize<T>(documento[Conteudo].AsString, JsonDosCenarios.Opcoes)!;

    private async Task<IReadOnlyList<T>> Listar<T>(string prefixo, CancellationToken cancelamento)
    {
        var filtro = Builders<BsonDocument>.Filter.Regex(Id, new BsonRegularExpression($"^{prefixo}"));
        var documentos = await _cenarios.Find(filtro).ToListAsync(cancelamento).ConfigureAwait(false);
        return [.. documentos.Select(Ler<T>)];
    }

    private async Task<T?> Obter<T>(string id, CancellationToken cancelamento)
        where T : class
    {
        var documento = await _cenarios.Find(PorId(id)).FirstOrDefaultAsync(cancelamento).ConfigureAwait(false);
        return documento is null ? null : Ler<T>(documento);
    }

    private Task Salvar<T>(string id, T valor, CancellationToken cancelamento) =>
        _cenarios.ReplaceOneAsync(
            PorId(id),
            new BsonDocument { [Id] = id, [Conteudo] = JsonSerializer.Serialize(valor, JsonDosCenarios.Opcoes) },
            new ReplaceOptions { IsUpsert = true },
            cancelamento);

    private Task Apagar(string id, CancellationToken cancelamento) => _cenarios.DeleteOneAsync(PorId(id), cancelamento);
}
