using System.Globalization;
using System.Net.Http.Json;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = "Observability.Homework.Client";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName)
            .AddAttributes(new []{ new KeyValuePair<string, object>("LocalDatetime3", DateTime.Now.ToString(CultureInfo.InvariantCulture)) }))
    .AddJaegerExporter()
    .AddHttpClientInstrumentation(builder => 
        builder.EnrichWithHttpRequestMessage = (activity, _) =>
        {
            activity.SetTag("LocalDatetime2", DateTime.Now.ToString(CultureInfo.InvariantCulture));
        })
    .Build();

using var httpClient = new HttpClient();

var body = new { client = new { id = serviceName }, product = new { type = 0 } };
var response = await httpClient.PostAsJsonAsync("http://localhost:52216/order", body);
response.EnsureSuccessStatusCode();