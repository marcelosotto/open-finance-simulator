using System.Text.Json.Nodes;
using Simulador.Catalogo.Contratos;
using Simulador.Testes.Host;
using Simulador.Testes.Infra;

namespace Simulador.Testes.Integracao.Contrato;

public class AdaptadorDeSchemaTests
{
    private static JsonNode Schema(string json) => JsonNode.Parse(json)!;

    [Fact]
    public void Nullable_aceita_nulo()
    {
        var schema = Schema("""{"type":"object","properties":{"a":{"type":"string","nullable":true}}}""");

        Assert.Empty(ValidadorDeContrato.Validar(schema, """{"a":null}"""));
        Assert.Empty(ValidadorDeContrato.Validar(schema, """{"a":"x"}"""));
        Assert.NotEmpty(ValidadorDeContrato.Validar(schema, """{"a":1}"""));
    }

    [Fact]
    public void Nullable_com_enum_aceita_nulo()
    {
        var schema = Schema("""{"type":"string","enum":["A","B"],"nullable":true}""");

        Assert.Empty(ValidadorDeContrato.Validar(schema, "null"));
        Assert.NotEmpty(ValidadorDeContrato.Validar(schema, "\"C\""));
    }

    [Fact]
    public void Sem_nullable_recusa_nulo() =>
        Assert.NotEmpty(ValidadorDeContrato.Validar(Schema("""{"type":"string"}"""), "null"));

    [Fact]
    public void Palavras_do_openapi_sao_ignoradas()
    {
        var schema = Schema("""{"type":"string","example":"x","x-extra":1,"deprecated":false,"maxLength":3}""");

        var adaptado = ValidadorDeContrato.Adaptar(schema)!.AsObject();

        Assert.False(adaptado.ContainsKey("example"));
        Assert.False(adaptado.ContainsKey("x-extra"));
        Assert.NotEmpty(ValidadorDeContrato.Validar(schema, "\"abcd\""));
    }

    [Fact]
    public void Ref_para_components_e_resolvido()
    {
        var documento = Schema("""
            {"components":{"schemas":{"Nome":{"type":"string","maxLength":2}}},
             "x":{"type":"object","properties":{"n":{"$ref":"#/components/schemas/Nome"}}}}
            """).AsObject();

        var resolvido = ServicosDeSpecs.ResolverReferencias(documento["x"], documento)!;

        Assert.Equal(2, resolvido["properties"]!["n"]!["maxLength"]!.GetValue<int>());
        Assert.NotEmpty(ValidadorDeContrato.Validar(resolvido, """{"n":"abc"}"""));
    }

    [Fact]
    public void Ref_ciclica_fica_como_ref()
    {
        var documento = Schema("""
            {"components":{"schemas":{"No":{"type":"object","properties":{"filho":{"$ref":"#/components/schemas/No"}}}}}}
            """).AsObject();

        var resolvido = ServicosDeSpecs.ResolverReferencias(documento["components"]!["schemas"]!["No"], documento)!;

        var filho = resolvido["properties"]!["filho"]!;
        Assert.Equal("object", filho["type"]!.GetValue<string>());
        Assert.Equal("#/components/schemas/No", filho["properties"]!["filho"]!["$ref"]!.GetValue<string>());
    }

    [Fact]
    public void Ref_inexistente_gera_erro()
    {
        var documento = Schema("""{"x":{"$ref":"#/components/schemas/Nada"}}""").AsObject();

        Assert.Throws<InvalidOperationException>(() => ServicosDeSpecs.ResolverReferencias(documento["x"], documento));
    }

    [Fact]
    public void Schema_real_de_transacoes_e_adaptado()
    {
        _ = Repositorio.PastaDasSpecs();
        var schema = ValidadorDeContrato.SchemaDaResposta("accountsGetAccountsAccountIdTransactions");

        Assert.NotEmpty(ValidadorDeContrato.Validar(schema, "{}"));
    }
}
