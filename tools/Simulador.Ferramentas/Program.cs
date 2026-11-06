using Simulador.Catalogo.Contratos;
using Simulador.Ferramentas;
using Simulador.Ferramentas.Comandos;

return await Roteador.Executar(args, Console.Out).ConfigureAwait(false);

namespace Simulador.Ferramentas
{
    /// <summary>Despacha <c>specs baixar|verificar|diferencas</c> e <c>cobertura conferir</c>.</summary>
    internal static class Roteador
    {
        public const string Ajuda = """
            Uso: dotnet run --project tools/Simulador.Ferramentas -- <comando> [opções]

              specs baixar      [--lock specs.lock.json] [--pasta .specs/<commit>] [--token <token>]
              specs verificar   [--lock specs.lock.json] [--pasta .specs/<commit>]
              specs diferencas  --base <lock> --novo <lock> [--resumo <arquivo.md>] [--token <token>]
              cobertura conferir --relatorios <glob> [--dominio 100] [--total 80] [--filtro-dominio "*.Dominio.*"]
            """;

        public static async Task<int> Executar(string[] args, TextWriter saida)
        {
            var comando = string.Join(' ', args.Take(2));
            var opcoes = new Opcoes(args.Skip(2));
            try
            {
                return comando switch
                {
                    "specs baixar" => await BaixarSpecs.Executar(opcoes, saida).ConfigureAwait(false),
                    "specs verificar" => await VerificarSpecs.Executar(opcoes, saida).ConfigureAwait(false),
                    "specs diferencas" => await DiferencasSpecs.Executar(opcoes, saida).ConfigureAwait(false),
                    "cobertura conferir" => await ConferirCobertura.Executar(opcoes, saida).ConfigureAwait(false),
                    _ => await MostrarAjuda(saida).ConfigureAwait(false),
                };
            }
            catch (TravaInvalidaException erro)
            {
                await saida.WriteLineAsync($"FALHA {erro.Message}").ConfigureAwait(false);
                return 1;
            }
        }

        private static async Task<int> MostrarAjuda(TextWriter saida)
        {
            await saida.WriteLineAsync(Ajuda).ConfigureAwait(false);
            return 1;
        }
    }
}
