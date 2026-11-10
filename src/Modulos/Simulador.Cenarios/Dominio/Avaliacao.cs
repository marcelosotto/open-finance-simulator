using Simulador.Cenarios.Contratos;

namespace Simulador.Cenarios.Dominio;

public static class Efeitos
{
    public const string Latencia = "latencia";
    public const string Status = "status";
    public const string Timeout = "timeout";
    public const string Indisponibilidade = "indisponibilidade";
    public const string Transporte = "transporte";
    public const string Estado = "estado";
    public const string Defeito = "defeito";
    public const string Corte = "corte";
    public const string ConexaoCortada = "conexao-cortada";
    public const string CorpoTruncado = "corpo-truncado";
    public const string JsonInvalido = "json-invalido";
}

/// <summary>Qual regra vale na chamada (RN14, RN15).</summary>
internal static class SelecaoDeRegras
{
    private const int StatusNormal = 200;
    private const int StatusForaDoAr = 503;

    public static RegraDeCenario? Vencedora(IEnumerable<RegraDeCenario> regras, ChamadaAvaliada chamada, DateTimeOffset instante, IReadOnlySet<int> declarados) =>
        Mais(regras.Where(r => r.Efeito.Tipo is not (Efeitos.Estado or Efeitos.Defeito) && Casa(r, chamada, instante) && Age(r.Efeito, declarados)));

    public static RegraDeCenario? VencedoraDeEstado(IEnumerable<RegraDeCenario> regras, ChamadaAvaliada chamada, DateTimeOffset instante) =>
        Mais(regras.Where(r => r.Efeito.Tipo == Efeitos.Estado && Casa(r, chamada, instante)));

    /// <summary>Regras de defeito disputam só entre si (RN111).</summary>
    public static RegraDeCenario? VencedoraDeDefeito(IEnumerable<RegraDeCenario> regras, ChamadaAvaliada chamada, DateTimeOffset instante) =>
        Mais(regras.Where(r => r.Efeito.Tipo == Efeitos.Defeito && Casa(r, chamada, instante)));

    /// <summary>Número em [0, 1) da posição na sequência da semente (RN54).</summary>
    public static double SorteioDaSequencia(long semente, long posicao)
    {
        unchecked
        {
            var z = (ulong)semente + ((ulong)posicao * 0x9E3779B97F4A7C15UL);
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (z >> 11) * (1d / (1L << 53));
        }
    }

    private static RegraDeCenario? Mais(IEnumerable<RegraDeCenario> regras) =>
        regras.OrderByDescending(Alcance).ThenByDescending(Filtros).ThenByDescending(r => r.CriadaEm).ThenBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();

    private static bool Casa(RegraDeCenario regra, ChamadaAvaliada chamada, DateTimeOffset instante) =>
        (regra.Instituicao is null || regra.Instituicao == chamada.Instituicao)
        && (regra.Endpoint is null || regra.Endpoint == chamada.Endpoint)
        && (regra.Cliente is null || regra.Cliente == chamada.Cliente)
        && (regra.Recurso is null || regra.Recurso == chamada.Recurso)
        && (regra.Inicio is not { } inicio || instante >= inicio)
        && (regra.Fim is not { } fim || instante < fim);

    private static bool Age(EfeitoDaRegra efeito, IReadOnlySet<int> declarados) =>
        efeito.Status is not { } status || status is StatusNormal or StatusForaDoAr || declarados.Contains(status);

    private static int Alcance(RegraDeCenario regra)
    {
        if (regra.Recurso is not null)
        {
            return 4;
        }

        if (regra.Cliente is not null)
        {
            return 3;
        }

        return regra.Endpoint is not null ? 2 : 1;
    }

    private static int Filtros(RegraDeCenario regra) =>
        new[] { regra.Instituicao, regra.Endpoint, regra.Cliente, regra.Recurso }.Count(f => f is not null);
}

/// <summary>Perfil e efeito viram espera e resposta (RN10, RN12, RN13, RN15, RN16).</summary>
internal static class AvaliacaoDoPerfil
{
    private const double Metade = 0.5;
    private const double P95 = 0.95;
    private const int StatusNormal = 200;
    private const int ForaDoAr = 503;
    private const int TempoEsgotado = 504;
    private const int EsperaOficialMs = 15_000;

    public static TimeSpan Amostra(int tipicoMs, int variacaoMs, double sorteio)
    {
        double ms;
        if (sorteio <= Metade)
        {
            var piso = (double)Math.Max(0, tipicoMs - variacaoMs);
            ms = piso + ((tipicoMs - piso) * sorteio / Metade);
        }
        else if (sorteio <= P95)
        {
            ms = tipicoMs + (variacaoMs * (sorteio - Metade) / (P95 - Metade));
        }
        else
        {
            ms = tipicoMs + variacaoMs + (variacaoMs * (sorteio - P95) / (1 - P95));
        }

        return TimeSpan.FromMilliseconds(ms);
    }

    public static bool JanelaAtiva(int? duracaoMin, int? intervaloMin, DateTimeOffset instante)
    {
        if (duracaoMin is not { } duracao || intervaloMin is not { } intervalo)
        {
            return false;
        }

        var minutos = (instante.UtcDateTime.Hour * 60) + instante.UtcDateTime.Minute;
        return minutos % intervalo < duracao;
    }

    public static DecisaoDeComportamento Decidir(PerfilDaChamada perfil, EfeitoDaRegra? efeito, DateTimeOffset instante, IReadOnlySet<int> declarados, Func<double> sorteio)
    {
        TimeSpan Normal() => Amostra(perfil.TipicoMs, perfil.VariacaoMs, sorteio());
        TimeSpan Segurar(int? espera) => TimeSpan.FromMilliseconds(espera ?? EsperaOficialMs);

        return efeito?.Tipo switch
        {
            Efeitos.Status when efeito.Status == TempoEsgotado => new DecisaoDeComportamento(Segurar(efeito.EsperaMs), TempoEsgotado),
            Efeitos.Status => new DecisaoDeComportamento(Normal(), efeito.Status == StatusNormal ? null : efeito.Status),
            Efeitos.Timeout => new DecisaoDeComportamento(Segurar(efeito.EsperaMs), TempoEsgotado),
            Efeitos.Indisponibilidade when efeito.Resultado == Efeitos.Corte => new DecisaoDeComportamento(Normal(), Transporte: Efeitos.ConexaoCortada),
            Efeitos.Indisponibilidade when efeito.Resultado == "504" => new DecisaoDeComportamento(Segurar(efeito.EsperaMs), TempoEsgotado),
            Efeitos.Indisponibilidade => new DecisaoDeComportamento(Normal(), ForaDoAr),
            Efeitos.Transporte => new DecisaoDeComportamento(Normal(), Transporte: efeito.Falha),
            _ => DoPerfil(perfil, efeito?.LatenciaMs, instante, declarados, sorteio),
        };
    }

    private static DecisaoDeComportamento DoPerfil(PerfilDaChamada perfil, int? latenciaMs, DateTimeOffset instante, IReadOnlySet<int> declarados, Func<double> sorteio)
    {
        var porcentagem = 100d;
        TimeSpan Latencia() => latenciaMs is { } fixa ? TimeSpan.FromMilliseconds(fixa) : Amostra(perfil.TipicoMs, perfil.VariacaoMs, sorteio());
        if (JanelaAtiva(perfil.JanelaDuracaoMin, perfil.JanelaIntervaloMin, instante))
        {
            return new DecisaoDeComportamento(Latencia(), ForaDoAr);
        }

        if (latenciaMs is null && sorteio() * porcentagem < (double)perfil.CaudaLentaPct)
        {
            return new DecisaoDeComportamento(TimeSpan.FromMilliseconds(EsperaOficialMs), TempoEsgotado);
        }

        var espera = Latencia();
        if (sorteio() * porcentagem >= (double)perfil.TaxaDeErroPct || Sorteado(perfil.Pesos, declarados, sorteio) is not { } status)
        {
            return new DecisaoDeComportamento(espera);
        }

        return status == TempoEsgotado
            ? new DecisaoDeComportamento(TimeSpan.FromMilliseconds(EsperaOficialMs), TempoEsgotado)
            : new DecisaoDeComportamento(espera, status);
    }

    private static int? Sorteado(IReadOnlyDictionary<int, decimal> pesos, IReadOnlySet<int> declarados, Func<double> sorteio)
    {
        var validos = pesos.Where(p => p.Value > 0 && declarados.Contains(p.Key)).OrderBy(p => p.Key).ToList();
        if (validos.Count == 0)
        {
            return null;
        }

        var ponto = (decimal)sorteio() * validos.Sum(p => p.Value);
        var indice = 0;
        while (indice < validos.Count - 1 && (ponto -= validos[indice].Value) >= 0)
        {
            indice++;
        }

        return validos[indice].Key;
    }
}

/// <summary>O <c>code</c> de cada status simulado (RN13).</summary>
public static class CodigosDeErro
{
    private const string Consentimentos = "consents";
    private const int NaoProcessado = 422;

    private static readonly Dictionary<int, string> Nomes = new()
    {
        [400] = "BAD_REQUEST",
        [401] = "UNAUTHORIZED",
        [403] = "FORBIDDEN",
        [404] = "NOT_FOUND",
        [405] = "METHOD_NOT_ALLOWED",
        [406] = "NOT_ACCEPTABLE",
        [415] = "UNSUPPORTED_MEDIA_TYPE",
        [NaoProcessado] = "UNPROCESSABLE_ENTITY",
        [423] = "LOCKED",
        [429] = "TOO_MANY_REQUESTS",
        [500] = "INTERNAL_SERVER_ERROR",
        [503] = "SERVICE_UNAVAILABLE",
        [504] = "GATEWAY_TIMEOUT",
        [529] = "SITE_IS_OVERLOADED",
    };

    public static string De(int status, string contrato, string metodo)
    {
        if (status == NaoProcessado && contrato == Consentimentos)
        {
            return string.Equals(metodo, "DELETE", StringComparison.OrdinalIgnoreCase) ? "CONSENTIMENTO_EM_STATUS_REJEITADO" : "ERRO_NAO_MAPEADO";
        }

        return Nomes.GetValueOrDefault(status, "ERROR");
    }
}

/// <summary>Regras de uma regra de cenário (RN13, RN14, RN100, RN108).</summary>
internal static class ValidacaoDeRegra
{
    private static readonly HashSet<string> Estados = new(StringComparer.Ordinal) { "AVAILABLE", "TEMPORARILY_UNAVAILABLE", "UNAVAILABLE", "PENDING_AUTHORISATION" };
    private static readonly HashSet<string> Falhas = new(StringComparer.Ordinal) { Efeitos.ConexaoCortada, Efeitos.CorpoTruncado, Efeitos.JsonInvalido };
    private static readonly HashSet<string> Resultados = new(StringComparer.Ordinal) { "503", "504", Efeitos.Corte };

    public static IReadOnlyList<string> Validar(RegraDeCenario regra, IReadOnlySet<int> statusDeclarados, Func<string, bool> operacaoExiste)
    {
        var erros = new List<string>();
        void Exigir(bool condicao, string mensagem)
        {
            if (!condicao)
            {
                erros.Add(mensagem);
            }
        }

        Exigir(regra.Instituicao is not null || regra.Endpoint is not null || regra.Cliente is not null || regra.Recurso is not null, "informe ao menos um filtro de alcance");
        Exigir(regra.Endpoint is null || operacaoExiste(regra.Endpoint), "endpoint desconhecido");
        Exigir(regra.FrequenciaPct is >= 0 and <= 100, "frequenciaPct deve ser de 0 a 100");
        Exigir(regra is not { Inicio: { } inicio, Fim: { } fim } || inicio < fim, "início deve vir antes do fim");
        Exigir(regra.Efeito.Tipo != Efeitos.Estado || regra.FrequenciaPct == 100, "regra de estado exige frequência de 100%");
        Exigir(regra.Efeito.EsperaMs is null or > 0, "esperaMs deve ser positivo");
        Exigir(EfeitoValido(regra.Efeito, statusDeclarados), "efeito inválido");
        return erros;
    }

    private static bool EfeitoValido(EfeitoDaRegra efeito, IReadOnlySet<int> statusDeclarados) => efeito.Tipo switch
    {
        Efeitos.Latencia => efeito.LatenciaMs is >= 0,
        Efeitos.Status => efeito.Status is { } status && (status is 200 or 503 || (status >= 400 && statusDeclarados.Contains(status))),
        Efeitos.Timeout => true,
        Efeitos.Indisponibilidade => efeito.Resultado is { } resultado && Resultados.Contains(resultado),
        Efeitos.Transporte => efeito.Falha is { } falha && Falhas.Contains(falha),
        Efeitos.Estado => efeito.Estado is { } estado && Estados.Contains(estado),
        Efeitos.Defeito => Defeitos.Existe(efeito.Defeito)
            && (efeito.Campo is null || Defeitos.DeCampo.Contains(efeito.Defeito!))
            && (efeito.Quantidade is null || (efeito.Defeito == Defeitos.DadoMudaNaPaginacao && efeito.Quantidade != 0)),
        _ => false,
    };
}
