using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Simulador.Host;

/// <summary>
/// OpenTelemetry só para a saúde do simulador (RNF09). Nenhum corpo, cabeçalho ou parâmetro de
/// chamada é registrado (RN18): os traces levam a rota, não a URL com a consulta.
/// </summary>
internal static class Observabilidade
{
    public static void AdicionarObservabilidade(this WebApplicationBuilder builder)
    {
        var endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return;
        }

        var recurso = ResourceBuilder.CreateDefault().AddService("simulador-open-finance");
        builder.Logging.AddOpenTelemetry(log =>
        {
            log.SetResourceBuilder(recurso);
            log.AddOtlpExporter();
        });
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metricas => metricas.SetResourceBuilder(recurso).AddAspNetCoreInstrumentation().AddOtlpExporter())
            .WithTracing(traces => traces
                .SetResourceBuilder(recurso)
                .AddAspNetCoreInstrumentation(opcoes =>
                {
                    opcoes.EnrichWithHttpRequest = (atividade, _) => atividade.SetTag("url.query", null);
                })
                .AddOtlpExporter());
    }
}
