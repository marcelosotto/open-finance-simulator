using Simulador.Catalogo.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Catalogo;

public class CatalogoTests
{
    private static readonly IReadOnlyList<OperacaoDoCatalogo> Operacoes = ServicosDeSpecs.LerOperacoes(
        TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs());

    [Fact]
    public void Mesmo_caminho_em_contratos_diferentes_gera_caminhos_publicados_diferentes()
    {
        var contas = Operacoes.Single(o => o.Contrato == "accounts" && o.Caminho == "/accounts");
        var cartoes = Operacoes.Single(o => o.Contrato == "credit-cards-accounts" && o.Caminho == "/accounts");

        Assert.Equal("/{banco}/open-banking/accounts/v2/accounts", contas.CaminhoPublicado);
        Assert.Equal("/{banco}/open-banking/credit-cards-accounts/v2/accounts", cartoes.CaminhoPublicado);
    }

    [Fact]
    public void OperationId_e_unico_no_catalogo() =>
        Assert.Equal(Operacoes.Count, Operacoes.Select(o => o.OperationId).Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void Chave_contrato_metodo_caminho_e_unica() =>
        Assert.Equal(Operacoes.Count, Operacoes.Select(o => o.Chave).Distinct().Count());

    [Fact]
    public void Caminho_publicado_e_unico_por_metodo() =>
        Assert.Equal(Operacoes.Count, Operacoes.Select(o => (o.Metodo, o.CaminhoPublicado)).Distinct().Count());
}
