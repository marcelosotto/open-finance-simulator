using System.Globalization;
using System.Text.RegularExpressions;
using Simulador.Massa.Contratos;

namespace Simulador.Massa.Dominio;

/// <summary>Tudo de que o gerador precisa; nada vem de fora disso (RN54).</summary>
internal sealed record ParametrosDeGeracao(
    long Semente,
    string Natureza,
    Distribuicao Distribuicao,
    IReadOnlyDictionary<string, PerfilDeDados> Perfis,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ProdutosPorBanco,
    int ConsentimentosPorCliente,
    IReadOnlyList<string>? Permissoes,
    int HistoricoMeses,
    DateTimeOffset Data,
    string? LoteId);

/// <summary>Gera cliente, consentimentos e recursos. Atributos saem da semente e do índice; identidade, da posição (research R02).</summary>
internal sealed class GeradorDeMassa(ParametrosDeGeracao parametros, IAmbienteDaMassa ambiente)
{
    public const int Versao = 1;
    public const string Disponivel = "AVAILABLE";
    public const string Autorizado = "AUTHORISED";

    private static readonly string[] Nomes = ["Ana", "Bruno", "Carla", "Diego", "Elisa", "Fábio", "Gabriela", "Heitor"];
    private static readonly string[] Sobrenomes = ["Alves", "Barros", "Costa", "Dias", "Moura", "Nunes", "Prado", "Teles"];
    private static readonly string[] Empresas = ["Aurora", "Horizonte", "Atlas", "Sol", "Vale", "Ponte", "Norte", "Mirante"];

    private readonly CicloDeBancos _ciclo = new(parametros.Semente, parametros.Distribuicao.Bancos);
    private readonly DateTimeOffset _criadoEm = new(parametros.Data.UtcDateTime.Date, TimeSpan.Zero);
    private readonly bool _virtual = parametros.Natureza == Naturezas.Virtual;

    public string BancoDoItem(long indice) => _ciclo.BancoDe(indice);

    public long? IndiceNoBanco(string banco, long n) => _ciclo.IndiceNoBanco(banco, n);

    /// <summary>Gera um cliente pela distribuição (lote e mundo virtual).</summary>
    public MassaGerada Gerar(long indice, long posicao)
    {
        var sorteio = Sorteio.Para(parametros.Semente, "cliente", indice);
        var tipo = sorteio.Chance(parametros.Distribuicao.PjPct) ? TiposDePessoa.Pj : TiposDePessoa.Pf;
        var perfil = Escolher(sorteio);
        var bancosDosConsentimentos = Enumerable.Range(0, parametros.ConsentimentosPorCliente)
            .Select(k => _ciclo.BancoDe((indice * parametros.ConsentimentosPorCliente) + k))
            .ToList();
        return Montar(indice, posicao, tipo, perfil, nome: null, sorteio, bancosDosConsentimentos.Distinct(StringComparer.Ordinal).ToList(), bancosDosConsentimentos);
    }

    /// <summary>Gera um cliente escolhido pelo usuário, sem consentimento (UC06).</summary>
    public MassaGerada GerarAvulso(long posicao, string tipo, string perfil, string? nome, IReadOnlyList<string> bancos) =>
        Montar(0, posicao, tipo, perfil, nome, Sorteio.Para(parametros.Semente, "cliente", 0), bancos, []);

    private string Escolher(Sorteio sorteio)
    {
        var sorteado = sorteio.Entre(0, 9_999);
        var acumulado = 0m;
        return parametros.Distribuicao.Perfis.OrderBy(p => p.Key, StringComparer.Ordinal)
            .First(p => (acumulado += p.Value) * 100 > sorteado).Key;
    }

    private MassaGerada Montar(
        long indice,
        long posicao,
        string tipo,
        string perfil,
        string? nome,
        Sorteio sorteio,
        IReadOnlyList<string> bancos,
        IReadOnlyList<string> bancosDosConsentimentos)
    {
        var codigo = Base36.Codificar(posicao);
        var documento = tipo == TiposDePessoa.Pj ? Documentos.Cnpj(posicao, _virtual) : Documentos.Cpf(posicao, _virtual);
        var cliente = new ClienteSimulado(documento, tipo, ambiente.MarcarTexto(nome ?? Nome(tipo, sorteio)), ambiente.Marca, parametros.Natureza, parametros.LoteId);

        var recursos = new List<Recurso>();
        foreach (var banco in bancos)
        {
            AcrescentarRecursos(recursos, indice, codigo, documento, banco, parametros.Perfis[perfil]);
        }

        var permissoes = Permissoes.Para(tipo, parametros.Permissoes);
        var consentimentos = bancosDosConsentimentos.Select((banco, k) => new Consentimento(
            ambiente.ConsentId(parametros.Natureza, $"c{codigo}x{k.ToString(CultureInfo.InvariantCulture)}"),
            documento,
            banco,
            Autorizado,
            permissoes,
            [.. recursos.Where(r => r.Banco == banco && Produtos.Coberto(r.Produto, permissoes)).Select(r => r.ResourceId)],
            _criadoEm,
            ambiente.Marca,
            parametros.Natureza,
            parametros.LoteId)).ToList();
        return new MassaGerada(cliente, consentimentos, recursos);
    }

    private void AcrescentarRecursos(List<Recurso> recursos, long indice, string codigo, string documento, string banco, PerfilDeDados perfil)
    {
        var oferecidos = parametros.ProdutosPorBanco[banco];
        foreach (var produto in perfil.Produtos.Where(oferecidos.Contains).Order(StringComparer.Ordinal))
        {
            if (!Sorteio.Para(parametros.Semente, "produto", indice, banco, produto).Chance(Porcentagem(produto)))
            {
                continue;
            }

            recursos.Add(new Recurso(
                ambiente.Identificador(parametros.Natureza, $"r{codigo}x{recursos.Count.ToString(CultureInfo.InvariantCulture)}"),
                documento,
                banco,
                Produtos.TipoDe(produto),
                produto,
                Disponivel,
                perfil.Nome,
                perfil.SaldoInicial,
                Sorteio.Derivar(parametros.Semente, "dados", indice, banco, produto),
                _criadoEm,
                DateOnly.FromDateTime(_criadoEm.UtcDateTime).AddMonths(-parametros.HistoricoMeses),
                ambiente.Marca,
                parametros.Natureza,
                parametros.LoteId));
        }
    }

    private decimal Porcentagem(string produto) =>
        parametros.Distribuicao.Produtos is { } mapa && mapa.TryGetValue(produto, out var porcentagem) ? porcentagem : 100m;

    private static string Nome(string tipo, Sorteio sorteio) => tipo == TiposDePessoa.Pj
        ? $"{Empresas[sorteio.Entre(0, Empresas.Length - 1)]} Comércio Ltda"
        : $"{Nomes[sorteio.Entre(0, Nomes.Length - 1)]} {Sobrenomes[sorteio.Entre(0, Sobrenomes.Length - 1)]}";
}

/// <summary>Um lançamento calculado, antes de virar transação.</summary>
internal readonly record struct Lancamento(int Posicao, long Centavos, bool Credito, int Tipo);

/// <summary>Transações saem de recurso, dia e semente, com ajustes por cima (RN46, RN51, RN53).</summary>
internal static class CalculoDeTransacoes
{
    public static string Chave(string recursoId, DateOnly dia, int posicao) =>
        string.Create(CultureInfo.InvariantCulture, $"transacao:{recursoId}:{dia:yyyy-MM-dd}:{posicao}");

    public static IEnumerable<Lancamento> DoDia(Recurso recurso, PerfilDeDados perfil, DateOnly dia)
    {
        var sorteio = new Sorteio(Sorteio.Derivar(recurso.SementeDeDados, dia.DayNumber));
        var volume = sorteio.Entre(perfil.TransacoesMin, perfil.TransacoesMax);
        var minimo = (long)(perfil.ValorMin * 100);
        var maximo = (long)(perfil.ValorMax * 100);
        for (var posicao = 0; posicao < volume; posicao++)
        {
            yield return new Lancamento(posicao, sorteio.Entre(minimo, maximo), sorteio.Chance(50), (int)sorteio.Entre(0, perfil.TiposDeLancamento.Count - 1));
        }
    }

    public static IReadOnlyList<Transacao> Transacoes(
        Recurso recurso,
        PerfilDeDados perfil,
        DateOnly de,
        DateOnly ate,
        DateOnly hoje,
        IReadOnlyDictionary<string, decimal> ajustes)
    {
        var transacoes = new List<Transacao>();
        var inicio = Maior(de, recurso.InicioDoHistorico);
        var fim = Menor(ate, hoje);
        for (var dia = inicio; dia <= fim; dia = dia.AddDays(1))
        {
            foreach (var l in DoDia(recurso, perfil, dia))
            {
                var tipo = perfil.TiposDeLancamento[l.Tipo];
                transacoes.Add(new Transacao(
                    string.Create(CultureInfo.InvariantCulture, $"{recurso.ResourceId}-t{dia:yyyyMMdd}x{l.Posicao}"),
                    dia,
                    l.Posicao,
                    Valor(recurso, dia, l, ajustes),
                    l.Credito ? "CREDITO" : "DEBITO",
                    tipo,
                    Marcar(recurso.Marca, l.Credito ? $"{tipo} recebido" : $"{tipo} enviado")));
            }
        }

        return transacoes;
    }

    public static decimal Saldo(Recurso recurso, PerfilDeDados perfil, DateOnly hoje, IReadOnlyDictionary<string, decimal> ajustes)
    {
        var saldo = recurso.SaldoInicial;
        for (var dia = recurso.InicioDoHistorico; dia <= hoje; dia = dia.AddDays(1))
        {
            foreach (var l in DoDia(recurso, perfil, dia))
            {
                saldo += l.Credito ? Valor(recurso, dia, l, ajustes) : -Valor(recurso, dia, l, ajustes);
            }
        }

        return saldo;
    }

    /// <summary>O texto leva a marca gravada no recurso, que nunca muda (RN63, RN64).</summary>
    private static string Marcar(string marca, string texto) => marca.Length == 0 ? texto : $"[{marca.ToUpperInvariant()}] {texto}";

    private static decimal Valor(Recurso recurso, DateOnly dia, Lancamento lancamento, IReadOnlyDictionary<string, decimal> ajustes) =>
        ajustes.Count > 0 && ajustes.TryGetValue(Chave(recurso.ResourceId, dia, lancamento.Posicao), out var valor) ? valor : lancamento.Centavos / 100m;

    private static DateOnly Maior(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Menor(DateOnly a, DateOnly b) => a < b ? a : b;
}

/// <summary>Referência a um item do mundo virtual, lida do próprio ID (research R05).</summary>
internal readonly record struct ReferenciaVirtual(long Indice, int Numero);

/// <summary>Lê IDs virtuais.</summary>
internal static partial class IdsVirtuais
{
    public static ReferenciaVirtual? Recurso(string id) => Ler(id, 'r');

    public static ReferenciaVirtual? Consentimento(string id) => Ler(id, 'c');

    private static ReferenciaVirtual? Ler(string id, char letra)
    {
        var achado = Padrao().Match(id);
        if (!achado.Success || achado.Groups[1].Value[0] != letra
            || Base36.Decodificar(achado.Groups[2].Value) is not { } indice
            || !int.TryParse(achado.Groups[3].ValueSpan, NumberStyles.None, CultureInfo.InvariantCulture, out var numero))
        {
            return null;
        }

        return new ReferenciaVirtual(indice, numero);
    }

    [GeneratedRegex(@"(?:^|[-:])v[-:]([rc])([0-9a-z]+)x([0-9]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex Padrao();
}
