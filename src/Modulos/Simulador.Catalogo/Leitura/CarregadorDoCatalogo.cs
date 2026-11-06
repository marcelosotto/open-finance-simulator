using Microsoft.Extensions.Configuration;
using Simulador.Catalogo.Contratos;
using Simulador.Catalogo.Dominio;

namespace Simulador.Catalogo.Leitura;

/// <summary>Onde estão a trava, as specs e a tabela.</summary>
internal sealed record OpcoesDoCatalogo(string ArquivoDaTrava, string PastaDasSpecs, string ArquivoDaTabela)
{
    public static OpcoesDoCatalogo Ler(IConfiguration configuracao)
    {
        var base_ = AppContext.BaseDirectory;
        return new(
            configuracao["Simulador:ArquivoDaTrava"] ?? Path.Combine(base_, "specs.lock.json"),
            configuracao["Simulador:PastaDasSpecs"] ?? Path.Combine(base_, "specs"),
            configuracao["Simulador:ArquivoDaTabela"] ?? Path.Combine(base_, "config", "tabela-de-endpoints.json"));
    }
}

/// <summary>Carrega o catálogo na subida: trava, conferência de hash e leitura das specs.</summary>
internal static class CarregadorDoCatalogo
{
    public static CatalogoCarregado Carregar(OpcoesDoCatalogo opcoes)
    {
        TravaDasSpecs trava;
        try
        {
            trava = TravaDasSpecs.Ler(opcoes.ArquivoDaTrava);
        }
        catch (TravaInvalidaException erro)
        {
            return CatalogoCarregado.Falho(string.Empty, [erro.Message]);
        }

        var falhas = ServicosDeSpecs.Conferir(trava, opcoes.PastaDasSpecs);
        if (falhas.Count > 0)
        {
            return CatalogoCarregado.Falho(trava.Commit, [.. falhas.Select(f => f.ToString())]);
        }

        TabelaDeEndpoints tabela;
        try
        {
            tabela = TabelaDeEndpoints.Ler(opcoes.ArquivoDaTabela);
        }
        catch (TabelaInvalidaException erro)
        {
            return CatalogoCarregado.Falho(trava.Commit, [erro.Message]);
        }

        var excluidas = tabela.Excluidas.ToHashSet();
        var publicadas = ServicosDeSpecs.LerOperacoes(trava, opcoes.PastaDasSpecs)
            .Where(o => !excluidas.Contains(o.Chave))
            .ToList();
        return new CatalogoCarregado(trava.Commit, publicadas, tabela.Classes, ProblemasDeCobertura(publicadas, tabela));
    }

    /// <summary>FR-015: operação sem linha, ou linha sem operação, deixa o catálogo não pronto.</summary>
    private static List<string> ProblemasDeCobertura(IReadOnlyList<OperacaoDoCatalogo> publicadas, TabelaDeEndpoints tabela)
    {
        var chaves = publicadas.Select(o => o.Chave).ToHashSet();
        var linhas = tabela.Classes.Select(c => (c.Contrato, c.Metodo, c.Caminho)).ToHashSet();
        return
        [
            .. publicadas.Where(o => !linhas.Contains(o.Chave))
                .Select(o => $"{o.Contrato} {o.Metodo} {o.Caminho}: operação sem linha na tabela de endpoints."),
            .. linhas.Where(l => !chaves.Contains(l))
                .Select(l => $"{l.Contrato} {l.Metodo} {l.Caminho}: linha da tabela sem operação no catálogo."),
        ];
    }
}
