using System.Net.Http.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = "Observability.Homework.Client";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName))
    .AddJaegerExporter()
    .AddHttpClientInstrumentation()
    .Build();

using var httpClient = new HttpClient();

var body = new { client = new { id = "best-client" }, product = new { type = 0 } };
var response = await httpClient.PostAsJsonAsync("http://localhost:5216/order", body);
response.EnsureSuccessStatusCode();
Console.WriteLine("Response received successfully.");