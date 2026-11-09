using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using Simulador.Catalogo.Contratos;
using Simulador.Comum.Erros;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Limites;

/// <summary>O corpo das recusas vale em toda operação que as declara (RNF01).</summary>
public class RespostasDeLimiteTests
{
    [Theory]
    [InlineData("429", "TOO_MANY_REQUESTS", 75)]
    [InlineData("423", "LOCKED", 69)]
    [InlineData("529", "SITE_IS_OVERLOADED", 75)]
    public void Corpo_valida_contra_o_contrato(string status, string codigo, int operacoes)
    {
        var corpo = JsonSerializer.Serialize(EnvelopeDeErro.Criar(codigo, "Título", "Detalhe", new FakeTimeProvider()), EnvelopeDeErro.OpcoesJson);
        var declaram = ServicosDeSpecs.LerOperacoes(TravaDasSpecs.Ler(Repositorio.Caminho("specs.lock.json")), Repositorio.PastaDasSpecs())
            .Where(o => o.Respostas.ContainsKey(status))
            .ToList();

        Assert.Equal(operacoes, declaram.Count);
        Assert.All(declaram, o => Assert.Empty(ValidadorDeContrato.Validar(o.OperationId, corpo, status)));
    }
}
