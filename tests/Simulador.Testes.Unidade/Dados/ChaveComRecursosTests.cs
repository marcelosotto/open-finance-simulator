using Simulador.Dados.Dominio;

namespace Simulador.Testes.Unidade.Dados;

/// <summary>A chave das listas de recursos leva o conjunto que entrou na emissão (RN37).</summary>
public sealed class ChaveComRecursosTests
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Segredo = [1, 2, 3];

    [Fact]
    public void Chave_leva_e_devolve_os_recursos()
    {
        var marcados = RecursosDaChave.Marcar([0, 2, 9], 10);
        var leitura = ChaveDePaginacao.Abrir(ChaveDePaginacao.Emitir(Agora, Agora, "c", Segredo, marcados), "c", Segredo, Agora, 60);

        Assert.Equal(Agora, leitura?.Emissao);
        Assert.Equal([0, 2, 9], RecursosDaChave.Posicoes(leitura!.Recursos!).Order());
    }

    [Fact]
    public void Chave_sem_recursos_devolve_nulo_no_conjunto()
    {
        var leitura = ChaveDePaginacao.Abrir(ChaveDePaginacao.Emitir(Agora, Agora, "c", Segredo), "c", Segredo, Agora, 60);
        Assert.Null(leitura?.Recursos);
        Assert.Equal(Agora, leitura?.Emissao);
    }

    [Fact]
    public void Conjunto_vazio_e_conjunto_e_nao_ausencia()
    {
        var marcados = RecursosDaChave.Marcar([], 0);
        var leitura = ChaveDePaginacao.Abrir(ChaveDePaginacao.Emitir(Agora, Agora, "c", Segredo, marcados), "c", Segredo, Agora, 60);
        Assert.NotNull(leitura?.Recursos);
        Assert.Empty(RecursosDaChave.Posicoes(leitura!.Recursos!));
    }

    [Fact]
    public void Conjunto_trocado_invalida_a_chave()
    {
        var chave = ChaveDePaginacao.Emitir(Agora, Agora, "c", Segredo, RecursosDaChave.Marcar([0], 2));
        var partes = chave.Split('.');
        var trocada = $"{partes[0]}.{partes[1]}.{RecursosDaChave.Marcar([0, 1], 2)}.{partes[3]}";

        Assert.Null(ChaveDePaginacao.Abrir(trocada, "c", Segredo, Agora, 60));
        Assert.Null(ChaveDePaginacao.Abrir($"{partes[0]}.{partes[1]}.a.b.{partes[3]}", "c", Segredo, Agora, 60));
        Assert.Null(ChaveDePaginacao.Abrir("semponto", "c", Segredo, Agora, 60));
    }
}
