using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulador.Testes.Host;

namespace Simulador.Testes.Integracao.Consentimentos;

/// <summary>Receptora padrão já cadastrada ao subir o ambiente (spec 010, Q14, RN124).</summary>
public sealed class ReceptoraPadraoTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    private static Uri U(string caminho) => new(caminho, UriKind.Relative);

    private async Task<JsonElement[]> Receptoras(FabricaDoSimulador fabrica)
    {
        var texto = await fabrica.ClienteAdministracao().GetStringAsync(U("/admin/receptoras"), _ct);
        return [.. JsonDocument.Parse(texto).RootElement.EnumerateArray().Select(r => r.Clone())];
    }

    [Fact]
    public async Task Sobe_com_a_XP_com_id_ficticio_marcado_credencial_de_teste_e_historico()
    {
        using var fabrica = new FabricaDoSimulador("hml").ComReceptoraPadrao();
        var receptora = Assert.Single(await Receptoras(fabrica));

        Assert.Equal("XP", receptora.GetProperty("nome").GetString());
        Assert.Equal("hml-p-receptora-padrao", receptora.GetProperty("organisationId").GetString());
        Assert.StartsWith("teste-", receptora.GetProperty("usuario").GetString(), StringComparison.Ordinal);
        Assert.StartsWith("teste-senha-", receptora.GetProperty("senha").GetString(), StringComparison.Ordinal);

        var historico = await fabrica.ClienteAdministracao().GetStringAsync(U("/admin/massa/historico"), _ct);
        Assert.Contains("receptora-criada", historico, StringComparison.Ordinal);
        Assert.Contains("sistema", historico, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Com_a_marca_vazia_o_id_nao_tem_prefixo_de_ambiente()
    {
        using var fabrica = new FabricaDoSimulador(string.Empty).ComReceptoraPadrao();
        Assert.Equal("p-receptora-padrao", Assert.Single(await Receptoras(fabrica)).GetProperty("organisationId").GetString());
    }

    [Fact]
    public async Task Nome_id_e_credenciais_vem_da_configuracao_e_o_token_sai_com_elas()
    {
        using var fabrica = new FabricaDoSimulador()
            .ComReceptoraPadrao("Receptora do App")
            .Com("Simulador:ReceptoraPadrao:OrganisationId", "org-ficticia-1")
            .Com("Simulador:ReceptoraPadrao:Usuario", "app-teste")
            .Com("Simulador:ReceptoraPadrao:Senha", "senha-de-teste-configurada");

        var receptora = Assert.Single(await Receptoras(fabrica));
        Assert.Equal("Receptora do App", receptora.GetProperty("nome").GetString());
        Assert.Equal("org-ficticia-1", receptora.GetProperty("organisationId").GetString());

        using var token = await AtalhosDeConsentimento.PedirToken(fabrica.ClienteOpenFinance(), "banco-estavel", "app-teste", "senha-de-teste-configurada", "consents");
        Assert.Equal(HttpStatusCode.OK, token.StatusCode);
    }

    [Fact]
    public async Task Nome_vazio_desliga_e_outras_receptoras_continuam_podendo_ser_criadas()
    {
        using var desligada = new FabricaDoSimulador();
        Assert.Empty(await Receptoras(desligada));

        using var ligada = new FabricaDoSimulador().ComReceptoraPadrao();
        await AtalhosDeConsentimento.CriarReceptora(ligada.ClienteAdministracao(), "Outra");
        Assert.Equal(2, (await Receptoras(ligada)).Length);
    }

    [Fact]
    public async Task Subir_de_novo_no_mesmo_MongoDB_nao_duplica_nem_troca_as_credenciais()
    {
        var baseDeDados = MongoDeTeste.NovaBase();
        string senha;
        using (var primeira = new FabricaDoSimulador().ComMongo().Com("Simulador:Mongo:Base", baseDeDados).ComReceptoraPadrao())
        {
            senha = Assert.Single(await Receptoras(primeira)).GetProperty("senha").GetString()!;
        }

        using var segunda = new FabricaDoSimulador().ComMongo().Com("Simulador:Mongo:Base", baseDeDados).ComReceptoraPadrao("Outro Nome");
        var receptora = Assert.Single(await Receptoras(segunda));
        Assert.Equal("XP", receptora.GetProperty("nome").GetString());
        Assert.Equal(senha, receptora.GetProperty("senha").GetString());
    }

    [Fact]
    public async Task Usuario_configurado_ja_em_uso_nao_cria_e_avisa_no_log()
    {
        var baseDeDados = MongoDeTeste.NovaBase();
        string usuarioDeOutra;
        using (var antes = new FabricaDoSimulador().ComMongo().Com("Simulador:Mongo:Base", baseDeDados))
        {
            (usuarioDeOutra, _, _) = await AtalhosDeConsentimento.CriarReceptora(antes.ClienteAdministracao(), "Outra");
        }

        var avisos = new RegistrosCapturados();
        using var fabrica = new FabricaDoSimulador().ComMongo().Com("Simulador:Mongo:Base", baseDeDados)
            .ComReceptoraPadrao().Com("Simulador:ReceptoraPadrao:Usuario", usuarioDeOutra)
            .ComServicos(s => s.AddSingleton<ILoggerProvider>(avisos));

        var receptoras = await Receptoras(fabrica);
        Assert.DoesNotContain(receptoras, r => r.GetProperty("nome").GetString() == "XP");
        Assert.Contains(avisos.Mensagens, m => m.Contains(usuarioDeOutra, StringComparison.Ordinal) && m.Contains("RN124", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Trocar_a_marca_no_mesmo_MongoDB_nao_cria_uma_segunda_XP()
    {
        var baseDeDados = MongoDeTeste.NovaBase();
        using (var hml = new FabricaDoSimulador("hml").ComMongo().Com("Simulador:Mongo:Base", baseDeDados).ComReceptoraPadrao())
        {
            Assert.Single(await Receptoras(hml));
        }

        using var vazia = new FabricaDoSimulador(string.Empty).ComMongo().Com("Simulador:Mongo:Base", baseDeDados).ComReceptoraPadrao();
        var receptora = Assert.Single(await Receptoras(vazia));
        Assert.Equal("hml-p-receptora-padrao", receptora.GetProperty("organisationId").GetString());
    }

    [Fact]
    public async Task Historico_perdido_numa_subida_e_gravado_na_proxima_uma_vez_so()
    {
        var baseDeDados = MongoDeTeste.NovaBase();
        using (var repositorio = new Simulador.Consentimentos.Persistencia.RepositorioMongo(MongoDeTeste.Conexao, baseDeDados))
        {
            await repositorio.Preparar(_ct);
            await repositorio.Inserir(new Simulador.Consentimentos.Contratos.Receptora("hml-p-receptora-padrao", "XP", "teste-u", "teste-s", DateTimeOffset.UnixEpoch), _ct);
        }

        for (var subida = 0; subida < 2; subida++)
        {
            using var fabrica = new FabricaDoSimulador().ComMongo().Com("Simulador:Mongo:Base", baseDeDados).ComReceptoraPadrao();
            var historico = await fabrica.ClienteAdministracao().GetStringAsync(U("/admin/massa/historico"), _ct);
            Assert.Equal(1, historico.Split("receptora-criada").Length - 1);
        }
    }
}

/// <summary>Guarda as mensagens de log para conferir avisos.</summary>
internal sealed class RegistrosCapturados : ILoggerProvider
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _mensagens = new();

    public IEnumerable<string> Mensagens => _mensagens;

    public ILogger CreateLogger(string categoryName) => new Registro(_mensagens);

    public void Dispose()
    {
    }

    private sealed class Registro(System.Collections.Concurrent.ConcurrentQueue<string> mensagens) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            mensagens.Enqueue(formatter(state, exception));
    }
}
