// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;
using Observability.Homework.Models;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceName = "Observability.Homework.Tracing.Client";

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .AddHttpClientInstrumentation()
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName))
    .AddJaegerExporter()
    .Build();
Random random = new Random();
while (true)
{
    Parallel.For(0, 30,  _ =>
    {
        using var httpClient = new HttpClient();
        var order = Order.Create((ProductType)random.Next(0, 3));
        var response = httpClient.PostAsync("http://localhost:5216/order/", JsonContent.Create(order)).Result;
        response.EnsureSuccessStatusCode();
        Console.WriteLine("Response received successfully.{0}", order.Product.Type);
    });

}

