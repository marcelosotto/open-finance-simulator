using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Tabela;

public class LeitorDaTabelaOficialTests
{
    private const string Exemplo = """
        # Tabela oficial de endpoints

        ## APIs sem limite de tráfego ou operacional

        | API | Endpoint | Frequência | SLA (ms) |
        |---|---|---|---|
        | Consentimento | `POST /consents` | Alta | 1.500 |
        | Recursos | `GET /resources` | Alta | 1.500 |

        ## APIs de dados

        ### Contas

        | Endpoint | Frequência | SLA (ms) | TPM | Limite mensal |
        |---|---|---|---|---|
        | `GET /accounts` | Baixa | 4.000 | 1.000 | 8 |
        | `GET /accounts/{accountId}/balances` | Alta | 1.500 | Faixa QCA | 420 |
        | `GET /accounts/{accountId}` | Média-alta | 1.500 | 2.000 | 120 |
        | `GET /accounts/{accountId}/x` | Média | 2.000 | 1.500 | 30 |

        ## Operações fora da tabela

        | Contrato | Método | Caminho | Motivo |
        |---|---|---|---|
        | accounts | GET | /accounts/{accountId}/y | Não consta na referência do portal |
        """;

    [Fact]
    public void Le_secoes_linhas_e_conversoes()
    {
        var tabela = LeitorDaTabelaOficial.Interpretar(Exemplo);

        Assert.Equal(6, tabela.Linhas.Count);
        Assert.Contains(new LinhaDaTabela("Consentimento", "POST", "/consents", "alta", 1500, null, false, null), tabela.Linhas);
        Assert.Contains(new LinhaDaTabela("Recursos", "GET", "/resources", "alta", 1500, null, false, null), tabela.Linhas);
        Assert.Contains(new LinhaDaTabela("Contas", "GET", "/accounts", "baixa", 4000, 1000, false, 8), tabela.Linhas);
        Assert.Contains(new LinhaDaTabela("Contas", "GET", "/accounts/{accountId}/balances", "alta", 1500, null, true, 420), tabela.Linhas);
        Assert.Contains(new LinhaDaTabela("Contas", "GET", "/accounts/{accountId}", "media-alta", 1500, 2000, false, 120), tabela.Linhas);
        Assert.Contains(new LinhaDaTabela("Contas", "GET", "/accounts/{accountId}/x", "media", 2000, 1500, false, 30), tabela.Linhas);
    }

    [Fact]
    public void Le_a_secao_de_exclusoes()
    {
        var exclusao = Assert.Single(LeitorDaTabelaOficial.Interpretar(Exemplo).Exclusoes);

        Assert.Equal(new ExclusaoDaTabela("accounts", "GET", "/accounts/{accountId}/y", "Não consta na referência do portal"), exclusao);
    }

    [Fact]
    public void Le_a_tabela_real_de_docs()
    {
        var tabela = LeitorDaTabelaOficial.Ler(Repositorio.Caminho("docs", "01-requisitos", "tabela-de-endpoints.md"));

        Assert.Equal(75, tabela.Linhas.Count);
        Assert.Equal(15, tabela.Linhas.Select(l => l.Secao).Distinct().Count());
    }
}
