using System.Net.Http.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = "Observability.Tracing.Client";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .AddHttpClientInstrumentation()
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName))
    .AddJaegerExporter()
    .Build();

using var httpClient = new HttpClient();

var getResponse = await httpClient.GetAsync("https://localhost:7205");
getResponse.EnsureSuccessStatusCode();

var order = new
{
    Client = new { Id = "12345" },
    Product = new { Type = 1 }
};

var response = await httpClient.PostAsJsonAsync("https://localhost:7205/order", order);

response.EnsureSuccessStatusCode();
