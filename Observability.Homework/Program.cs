/*
 * У вас есть сервис, который имеет один Post эндпоинт /order. Через него можно для клиента оформить заказ на одну позицию.
 * Если заказ - пицца, то она делается. В коде можно посмотреть флоу. Если отменить запрос во время приготовления, то это равносильно отказу от заказа.
 * В случае отказа пицца выбрасывается. Кроме того, все пиццы на одном из этапов попадают в "печь". Будем считать, что печь вмещает бесконечное количество пицц.
 *
 *  1. Необходимо затащить в проект подключение трейсов, метрик, логов. Трейсы должны отправляться в Егерь, метрики - в Прометей, логи - с помощью Serilog в консоль.
 *  1.1 Сделать так, чтобы логер в релизной сборке писал json, а в дебаг сборке писал обычный текст в консоль.
 *  1.2* Сделать так, чтобы в дебаге в логе поменялись местами timestamp и level
 *  1.3* Сделать так, чтобы родительский спан запроса обогащался местным временем (можно взять время вашего компьютера)
 *
 *  2. Организовать разумное логирование в проекте. Не стоит упарываться в trace и debug.
 *  2.1 Настроить, чтобы в релизе минимальный уровень логов был Warning, а в дебаге - Information.
 *  2.2 Сделать так, чтобы все логи внутри PizzaBakeryService содержали информацию о клиенте, при этом клиент нельзя изменять контракт сервиса, прокидывать в него клиента.
 *  2.3** Сделать так, чтобы логи, которые ниже минимального уровня логирования, не выделяли лишнюю память.
 *
 *  3. Внедрить трейсы в процесс
 *  3.1 Проверить трейсы в Егере
 *  3.2 Сделать отдельный проект-сервис (как на уроке), который будет дергать АПИ. Сделать так, чтобы в Егере в трейсах был сквозной трейс на два сервиса.
 *
 *  4. Внедрить метрики в эндпоинт.
 *  4.1 Метрика заказов по типу. Указать в тегах клиента.
 *  4.2 Метрика времени приготовления пицц.
 *  4.3 Метрика отмен заказа.
 *  4.4 Метрика по подгоревшим пиццам.
 *  4.5 Метрика "текущее количество пицц в печи". Не использовать counter и upDownCounter
 *  4.6** Построить графики по этим метрикам в графане. Для этого вам нужен язык запросов PromQl. Советую использовать chatGPT, он очень хорошо генерирует запросы PromQL
 */

using Microsoft.AspNetCore.Mvc;
using Observability.Homework;
using Observability.Homework.Models;
using Observability.Homework.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Observability.Homework.Middlewares;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

var serviceName = "Observability.Tracing";

builder.Services.AddOpenTelemetry()
    .WithTracing(tcb =>
    {
        tcb
            .AddSource(serviceName)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName))
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter();
    })
    .WithMetrics(mpb => mpb
        .AddPrometheusExporter()
        .AddMeter(PizzeriaMetrics.MeterName));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
builder.Services.AddSingleton<IPizzaBakeryService, PizzaBakeryService>();
builder.Services.AddSingleton<PizzeriaMetrics>();

builder.Services.AddOurCustomLogging(builder.Logging, builder.Environment);

var app = builder.Build();

app.UseMiddleware<RequestHeadersLoggingMiddleware>();
app.MapPrometheusScrapingEndpoint();

app.MapPost("/order",
    async ([FromBody] Order order, IPizzaBakeryService pizzaBakeryService, [FromServices] ILogger<Program> logger,
        [FromServices] Tracer tracer,
        [FromServices] PizzeriaMetrics metrics,
        CancellationToken cancellationToken) =>
    {
        using var span = tracer.StartActiveSpan("Some method with tracing");
        var clientId = order.Client.Id;
        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["ClientId"] = clientId,
               }))
        {
            if (order.Product.Type is ProductType.Pizza)
                await pizzaBakeryService.DoPizza(order.Product, cancellationToken);

            metrics.ProductSoldByType(order.Product, clientId);

            return Results.Ok(order.Product);
        }
    });

app.MapGet("/", (IPizzaBakeryService pizzaBakeryService) =>
    pizzaBakeryService.SomeMethodWithTracing());
app.Run();
