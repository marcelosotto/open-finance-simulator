using Simulador.Catalogo.Contratos;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Unidade.Catalogo;

/// <summary>Spec 004: escopos e permissões saem das specs travadas (princípio VII).</summary>
public class EscoposEPermissoesTests
{
    private static readonly IReadOnlyList<OperacaoDoCatalogo> Operacoes =
        ServicosDeSpecs.LerOperacoes(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs());

    private static OperacaoDoCatalogo Operacao(string contrato, string metodo, string caminho) =>
        Operacoes.Single(o => o.Contrato == contrato && o.Metodo == metodo && o.Caminho == caminho);

    [Fact]
    public void Criacao_exige_o_escopo_consents() => Assert.Contains("consents", Operacao("consents", "POST", "/consents").Escopos!);

    [Fact]
    public void Renovacao_exige_o_consentimento() => Assert.Contains("consent:consentId", Operacao("consents", "POST", "/consents/{consentId}/extends").Escopos!);

    [Fact]
    public void Permissao_vem_do_caminho()
    {
        Assert.Equal(["ACCOUNTS_BALANCES_READ"], Operacao("accounts", "GET", "/accounts/{accountId}/balances").Permissoes);
        Assert.Equal(["RESOURCES_READ"], Operacao("resources", "GET", "/resources").Permissoes);
    }
}
